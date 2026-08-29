namespace MiMieEventBus
{
    using System;

    /// 桥接类
    /// 用于将 EventBus 的日志和追踪功能与 Unity 的日志和追踪功能进行桥接
    /// 比如日志系统可以桥接UnityDebug,时间系统可以桥接Time.realtimeSinceStartup

    /// <summary>
    /// EventBus 日志桥接
    /// </summary>
    public static class EventBusLog
    {
        /// <summary>
        /// 错误日志委托 未注入时回退到 Console.Error
        /// </summary>
        public static Action<string> LogError { get; set; } = message =>
        {
            Console.Error.WriteLine(message);
        };
    }

    /// <summary>
    /// EventBus 追踪桥接
    /// </summary>
    public static class EventBusTrace
    {
        /// <summary>
        /// 最近触发的 Key 名称
        /// </summary>
        public static string LastKeyName { get; private set; } = string.Empty;

        /// <summary>
        /// 最近触发时间
        /// </summary>
        public static float LastTime { get; private set; }

        /// <summary>
        /// 时间读取 由 EventBusBootstrap 注入
        /// </summary>
        public static Func<float> NowFunc { get; set; } = () => 0f;

        /// <summary>
        /// 记录触发
        /// </summary>
        internal static void MarkTriggered(string keyName)
        {
            LastKeyName = keyName;
            LastTime = NowFunc();
        }
    }
}

