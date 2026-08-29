namespace MieMieFrameWork.Business.Samples.DCES
{
    /// <summary>
    /// 玩家伤害实例计算器
    /// 构造注入只读配置 构造后不可变
    /// </summary>
    internal sealed class PlayerDamageCalculator
    {
        /// <summary>
        /// 玩家只读配置
        /// 提供伤害减免等业务参数
        /// </summary>
        private readonly PlayerConfigData configData;

        /// <summary>
        /// 创建玩家伤害计算器
        /// 保存只读配置依赖
        /// </summary>
        /// <param name="configData">玩家配置</param>
        public PlayerDamageCalculator(PlayerConfigData configData)
        {
            this.configData = configData;
        }

        /// <summary>
        /// 计算减免后的伤害结果
        /// 业务规则走实例 纯公式委托静态 Math
        /// </summary>
        /// <param name="currentHealth">当前生命值</param>
        /// <param name="request">伤害请求</param>
        /// <returns>伤害结果</returns>
        public PlayerDamageResult Calculate(int currentHealth, PlayerDamageRequest request)
        {
            int mitigatedDamage = PlayerDamageMath.ApplyReduction(
                request.Damage,
                configData.DamageReductionPercent);
            return PlayerDamageMath.ResolveHealth(currentHealth, mitigatedDamage);
        }
    }
}
