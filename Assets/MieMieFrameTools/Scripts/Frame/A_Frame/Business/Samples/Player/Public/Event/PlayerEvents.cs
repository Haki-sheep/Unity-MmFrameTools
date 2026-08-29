namespace MieMieFrameWork.Business.Samples.DCES
{
    using MiMieEventBus;

    /// <summary>
    /// 玩家模块对外事件 Key
    /// 由 Executor 发布 外部模块订阅
    /// </summary>
    public static class PlayerEvents
    {
        /// <summary>
        /// 玩家生命变化事件
        /// 参数为实际伤害与剩余生命
        /// </summary>
        public static readonly EventKey<int, int> HealthChanged =
            new EventKey<int, int>("DCES.Player.HealthChanged");
    }
}
