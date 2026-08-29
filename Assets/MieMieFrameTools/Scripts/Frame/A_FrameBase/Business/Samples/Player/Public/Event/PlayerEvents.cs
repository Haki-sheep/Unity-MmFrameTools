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
        /// 参数为本次伤害结果
        /// </summary>
        public static readonly EventKey<PlayerDamageResult> HealthChanged =
            new EventKey<PlayerDamageResult>("DCES.Player.HealthChanged");
    }
}
