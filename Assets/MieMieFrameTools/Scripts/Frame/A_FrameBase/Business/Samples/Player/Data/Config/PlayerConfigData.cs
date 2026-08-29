namespace MieMieFrameWork.Business.Samples.DCES
{
    /// <summary>
    /// 玩家模块只读配置
    /// 初始化后不允许修改
    /// </summary>
    internal sealed class PlayerConfigData
    {
        public int DamageReductionPercent { get; }

        /// <summary>
        /// 创建玩家配置
        /// 保存伤害减免百分比
        /// </summary>
        /// <param name="damageReductionPercent">伤害减免百分比</param>
        public PlayerConfigData(int damageReductionPercent)
        {
            if (damageReductionPercent < 0)
            {
                damageReductionPercent = 0;
            }

            if (damageReductionPercent > 100)
            {
                damageReductionPercent = 100;
            }

            DamageReductionPercent = damageReductionPercent;
        }
    }
}
