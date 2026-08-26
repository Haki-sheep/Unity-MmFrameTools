using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace MieMieFrameTools.Diagnostics
{
    /// <summary>
    /// 通用代码性能追踪工具
    /// </summary>
    public static class PerformanceTrace
    {
        /// <summary>
        /// 追踪数据栈
        /// </summary>
        private static readonly Stack<TraceSample> sampleList = new(16);

        /// <summary>
        /// 开始记录当前代码段
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Start(
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Profiler.BeginSample(memberName);
            var sample = new TraceSample
            {
                StartTimestamp = Stopwatch.GetTimestamp(),
                StartAllocatedBytes = GC.GetAllocatedBytesForCurrentThread(),
                StartFrame = Time.frameCount,
                MemberName = memberName,
                FilePath = filePath,
                StartLine = lineNumber
            };
            sampleList.Push(sample);
        }

        /// <summary>
        /// 结束记录并输出耗时与 GC
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void End()
        {
            if (sampleList.Count == 0)
            {
                Debug.LogWarning("[PerformanceTrace] End 缺少对应的 Start");
                return;
            }

            long endTimestamp = Stopwatch.GetTimestamp();
            long endAllocatedBytes = GC.GetAllocatedBytesForCurrentThread();
            int endFrame = Time.frameCount;
            var sample = sampleList.Pop();
            Profiler.EndSample();

            double elapsedMilliseconds =
                (endTimestamp - sample.StartTimestamp) * 1000d / Stopwatch.Frequency;
            long allocatedBytes = endAllocatedBytes - sample.StartAllocatedBytes;
            string fileName = Path.GetFileName(sample.FilePath);
            Debug.Log(
                $"[PerformanceTrace] {fileName} {sample.MemberName} L{sample.StartLine}" +
                $"  {elapsedMilliseconds:F3} ms  GC {allocatedBytes} B" +
                $"  Frame {sample.StartFrame} -> {endFrame}");
        }

        /// <summary>
        /// 单次追踪数据
        /// </summary>
        private struct TraceSample
        {
            /// <summary>
            /// 开始时间戳
            /// </summary>
            public long StartTimestamp;

            /// <summary>
            /// 开始线程分配字节数
            /// </summary>
            public long StartAllocatedBytes;

            /// <summary>
            /// 开始帧
            /// </summary>
            public int StartFrame;

            /// <summary>
            /// 调用函数名
            /// </summary>
            public string MemberName;

            /// <summary>
            /// 调用文件路径
            /// </summary>
            public string FilePath;

            /// <summary>
            /// 开始行号
            /// </summary>
            public int StartLine;
        }
    }
}
