using UnityEngine;

namespace MiMieEventBus.Unity
{
    /// <summary>
    /// Unity 日志与时间桥接
    /// </summary>
    public static class EventBusUnityBootstrap
    {
        /// <summary>
        /// 注入 Unity 依赖
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            EventBusLog.LogError = Debug.LogError;
            EventBusTrace.NowFunc = () => Time.realtimeSinceStartup;
        }
    }
}
