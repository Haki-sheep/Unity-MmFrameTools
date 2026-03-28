using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Editor.Protobuf
{
    /// <summary>
    /// protoc 编译器封装
    /// </summary>
    public static class ProtocGenerator
    {
        /// <summary>
        /// 默认 protoc 路径
        /// </summary>
        public static string DefaultProtocPath => Path.Combine(
            Application.dataPath, "Editor", "Tools", "protoc.exe");

        /// <summary>
        /// 编译器路径（读写 <see cref="ProtobufSettingsStore"/> 的 JSON）
        /// </summary>
        public static string ProtocPath
        {
            get
            {
                ProtobufSettingsStore.EnsureLoaded();
                string p = ProtobufSettingsStore.Data.protocPath;
                if (!string.IsNullOrEmpty(p) && File.Exists(p))
                    return p;
                return DefaultProtocPath;
            }
            set
            {
                ProtobufSettingsStore.EnsureLoaded();
                ProtobufSettingsStore.Data.protocPath = value ?? "";
                ProtobufSettingsStore.Save();
            }
        }

        /// <summary>
        /// protoc 是否已配置
        /// </summary>
        public static bool IsConfigured => !string.IsNullOrEmpty(ProtocPath) && File.Exists(ProtocPath);

        /// <summary>
        /// 编译单个 proto 文件
        /// </summary>
        /// <param name="protoPath">proto 文件路径</param>
        /// <param name="outputDir">输出目录</param>
        /// <param name="namespaceHint">仅用于日志提示；C# 命名空间由 .proto 内 option csharp_namespace 决定，protoc 无 --csharp_namespace 参数</param>
        /// <param name="log">日志回调</param>
        /// <returns>是否成功</returns>
        public static bool Compile(
            string protoPath,
            string outputDir,
            string @namespaceHint,
            Action<string> log = null)
        {
            if (!IsConfigured)
            {
                log?.Invoke("[Error] protoc 未配置，请先设置路径");
                return false;
            }

            if (!File.Exists(protoPath))
            {
                log?.Invoke($"[Error] proto 文件不存在: {protoPath}");
                return false;
            }

            // 确保输出目录存在
            Directory.CreateDirectory(outputDir);

            // 构建命令（勿使用 --csharp_namespace：官方 protoc 不支持该开关）
            var args = $"--csharp_out=\"{outputDir}\" --proto_path=\"{Path.GetDirectoryName(protoPath)}\" \"{protoPath}\"";

            if (!string.IsNullOrWhiteSpace(@namespaceHint))
            {
                log?.Invoke($"[Info] 命名空间提示: 请在 .proto 中写 option csharp_namespace = \"...\";（当前界面填写: {@namespaceHint}）");
            }

            log?.Invoke($"[Info] 编译: {Path.GetFileName(protoPath)}");
            log?.Invoke($"[Info] 输出: {outputDir}");
            log?.Invoke($"[Info] 命令: protoc {args}");

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = ProtocPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = outputDir,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    log?.Invoke("[Error] 无法启动 protoc 进程");
                    return false;
                }

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(stdout))
                {
                    log?.Invoke($"[Output] {stdout}");
                }

                if (!string.IsNullOrEmpty(stderr))
                {
                    log?.Invoke($"[Error] {stderr}");
                }

                if (process.ExitCode == 0)
                {
                    log?.Invoke($"[Success] {Path.GetFileNameWithoutExtension(protoPath)}.cs 生成成功");
                    return true;
                }
                else
                {
                    log?.Invoke($"[Error] 编译失败，退出码: {process.ExitCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                log?.Invoke($"[Error] 异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 查找项目中的 proto 文件
        /// </summary>
        /// <param name="searchIn">搜索根目录</param>
        /// <returns>proto 文件列表</returns>
        public static string[] FindProtoFiles(string searchIn)
        {
            if (!Directory.Exists(searchIn))
            {
                return Array.Empty<string>();
            }

            return Directory.GetFiles(searchIn, "*.proto", SearchOption.AllDirectories);
        }

        /// <summary>
        /// 获取 protoc 默认下载链接
        /// </summary>
        public static string GetDownloadUrl()
        {
#if UNITY_EDITOR_WIN
            return "https://github.com/protocolbuffers/protobuf/releases/download/v34.1/protoc-34.1-win64.zip";
#elif UNITY_EDITOR_OSX
            return "https://github.com/protocolbuffers/protobuf/releases/download/v25.1/protoc-25.1-osx-universal_binary.zip";
#else
            return "https://github.com/protocolbuffers/protobuf/releases/download/v25.1/protoc-25.1-linux-x86_64.zip";
#endif
        }
    }
}
