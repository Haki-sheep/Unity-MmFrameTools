namespace MieMieFrameWork.Business.Samples.DCES
{
    /// <summary>
    /// 玩家伤害业务执行器
    /// 调用计算器后修改状态并发布模块事件
    /// </summary>
    internal sealed class PlayerDamageExecutor
    {
        /// <summary>
        /// 玩家运行时数据
        /// 由玩家服务创建并注入
        /// </summary>
        private readonly PlayerRuntimeData runtimeData;

        /// <summary>
        /// 玩家伤害计算器
        /// 由服务组装后注入
        /// </summary>
        private readonly PlayerDamageCalculator damageCalculator;

        /// <summary>
        /// 创建玩家伤害执行器
        /// 接收状态与计算器
        /// </summary>
        /// <param name="runtimeData">玩家运行时数据</param>
        /// <param name="damageCalculator">玩家伤害计算器</param>
        public PlayerDamageExecutor(
            PlayerRuntimeData runtimeData,
            PlayerDamageCalculator damageCalculator)
        {
            this.runtimeData = runtimeData;
            this.damageCalculator = damageCalculator;
        }

        /// <summary>
        /// 执行玩家伤害业务
        /// 先写状态再发布 PlayerEvents
        /// </summary>
        /// <param name="damage">请求伤害值</param>
        /// <returns>实际扣血量</returns>
        public int Execute(int damage)
        {
            damageCalculator.Calculate(
                runtimeData.CurrentHealth,
                damage,
                out int appliedDamage,
                out int remainingHealth);
            runtimeData.SetHealth(remainingHealth);
            MieMieFrameWork.MmGlobalEventBus.GlobalBus.Publish(
                PlayerEvents.HealthChanged,
                appliedDamage,
                remainingHealth);
            return appliedDamage;
        }
    }
}
