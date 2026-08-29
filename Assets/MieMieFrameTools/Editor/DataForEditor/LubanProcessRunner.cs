using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;

namespace MieMieFrameWork.Editor.DataForEditor
{
    /// <summary>
    /// Luban 脚本异步执行器
    /// </summary>
    internal sealed class LubanProcessRunner : IDisposable
    {
        /// <summary>
        /// 等待写入主线程的日志队列
        /// </summary>
        private readonly ConcurrentQueue<string> PendingLogQueue = new();

        /// <summary>
        /// 当前任务完整日志
        /// </summary>
        private readonly StringBuilder LogBuilder = new();

        /// <summary>
        /// 当前运行的脚本进程
        /// </summary>
        private Process RunningProcess;

        /// <summary>
        /// 是否已经订阅编辑器更新
        /// </summary>
        private bool UpdateSubscribed;

        public event Action StateChanged;

        public event Action<bool> Completed;

        public bool IsRunning => RunningProcess != null;

        public string Log => LogBuilder.ToString();

        public int? ExitCode { get; private set; }

        #region Lifecycle

        /// <summary>
        /// 启动脚本任务
        /// </summary>
        /// <param name="scriptPath">脚本绝对路径</param>
        public void Start(string scriptPath)
        {
            if (IsRunning)
                throw new InvalidOperationException("已有 Luban 脚本正在运行");

            if (!File.Exists(scriptPath))
                throw new FileNotFoundException("未找到 Luban 脚本", scriptPath);

            string extension = Path.GetExtension(scriptPath).ToLowerInvariant();
            if (extension != ".bat" && extension != ".cmd")
                throw new NotSupportedException("工作台目前只执行 bat 或 cmd 脚本");

            ClearLog();
            ExitCode = null;

            var startInfo = CreateStartInfo(scriptPath);
            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = false
            };
            process.OutputDataReceived += OnOutputReceived;
            process.ErrorDataReceived += OnErrorReceived;

            try
            {
                if (!process.Start())
                    throw new InvalidOperationException("无法启动 Luban 脚本进程");

                RunningProcess = process;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                AppendLog($"> {scriptPath}");
                SubscribeUpdate();
            }
            catch
            {
                process.OutputDataReceived -= OnOutputReceived;
                process.ErrorDataReceived -= OnErrorReceived;
                process.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 中止当前脚本任务
        /// </summary>
        public void Cancel()
        {
            if (RunningProcess == null)
                return;

            AppendLog("正在中止任务");
            RunningProcess.Kill();
        }

        /// <summary>
        /// 清空当前日志
        /// </summary>
        public void ClearLog()
        {
            while (PendingLogQueue.TryDequeue(out _))
            {
            }

            LogBuilder.Clear();
            StateChanged?.Invoke();
        }

        /// <summary>
        /// 释放脚本执行器
        /// </summary>
        public void Dispose()
        {
            UnsubscribeUpdate();
            if (RunningProcess == null)
                return;

            if (!RunningProcess.HasExited)
                RunningProcess.Kill();

            ReleaseProcess();
        }

        #endregion

        #region Process

        /// <summary>
        /// 创建脚本启动参数
        /// </summary>
        /// <param name="scriptPath">脚本绝对路径</param>
        private static ProcessStartInfo CreateStartInfo(string scriptPath)
        {
            string commandInterpreter = Environment.GetEnvironmentVariable("ComSpec");
            if (string.IsNullOrWhiteSpace(commandInterpreter))
                commandInterpreter = "cmd.exe";

            return new ProcessStartInfo
            {
                FileName = commandInterpreter,
                Arguments = $"/d /s /c \"\"{scriptPath}\"\"",
                WorkingDirectory = Path.GetDirectoryName(scriptPath),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
        }

        /// <summary>
        /// 接收标准输出
        /// </summary>
        /// <param name="sender">进程实例</param>
        /// <param name="eventArgs">输出事件参数</param>
        private void OnOutputReceived(object sender, DataReceivedEventArgs eventArgs)
        {
            if (eventArgs.Data != null)
                PendingLogQueue.Enqueue(eventArgs.Data);
        }

        /// <summary>
        /// 接收错误输出
        /// </summary>
        /// <param name="sender">进程实例</param>
        /// <param name="eventArgs">输出事件参数</param>
        private void OnErrorReceived(object sender, DataReceivedEventArgs eventArgs)
        {
            if (eventArgs.Data != null)
                PendingLogQueue.Enqueue($"[错误] {eventArgs.Data}");
        }

        /// <summary>
        /// 轮询脚本状态并同步日志
        /// </summary>
        private void OnEditorUpdate()
        {
            bool logChanged = FlushPendingLog();
            if (RunningProcess == null)
                return;

            if (!RunningProcess.HasExited)
            {
                if (logChanged)
                    StateChanged?.Invoke();
                return;
            }

            RunningProcess.WaitForExit();
            FlushPendingLog();
            int exitCode = RunningProcess.ExitCode;
            ExitCode = exitCode;
            ReleaseProcess();
            UnsubscribeUpdate();
            AppendLog(exitCode == 0 ? "任务执行成功" : $"任务执行失败 退出码 {exitCode}");
            StateChanged?.Invoke();
            Completed?.Invoke(exitCode == 0);
        }

        #endregion

        #region Log

        /// <summary>
        /// 将后台日志写入主日志
        /// </summary>
        private bool FlushPendingLog()
        {
            bool logChanged = false;
            while (PendingLogQueue.TryDequeue(out string line))
            {
                LogBuilder.AppendLine(line);
                logChanged = true;
            }

            return logChanged;
        }

        /// <summary>
        /// 追加一行主线程日志
        /// </summary>
        /// <param name="line">日志内容</param>
        private void AppendLog(string line)
        {
            LogBuilder.AppendLine(line);
        }

        #endregion

        #region EditorUpdate

        /// <summary>
        /// 订阅编辑器更新
        /// </summary>
        private void SubscribeUpdate()
        {
            if (UpdateSubscribed)
                return;

            EditorApplication.update += OnEditorUpdate;
            UpdateSubscribed = true;
        }

        /// <summary>
        /// 取消编辑器更新订阅
        /// </summary>
        private void UnsubscribeUpdate()
        {
            if (!UpdateSubscribed)
                return;

            EditorApplication.update -= OnEditorUpdate;
            UpdateSubscribed = false;
        }

        /// <summary>
        /// 释放当前进程对象
        /// </summary>
        private void ReleaseProcess()
        {
            RunningProcess.OutputDataReceived -= OnOutputReceived;
            RunningProcess.ErrorDataReceived -= OnErrorReceived;
            RunningProcess.Dispose();
            RunningProcess = null;
        }

        #endregion
    }
}
