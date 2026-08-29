namespace MieMieFrameWork.Business.Samples.DCES
{
    /// <summary>
    /// 玩家模块运行时数据
    /// 只允许模块内部读取和修改
    /// </summary>
    internal sealed class PlayerRuntimeData
    {
        public int CurrentHealth { get; private set; }

        /// <summary>
        /// 创建玩家运行时数据
        /// 设置初始生命值
        /// </summary>
        /// <param name="initialHealth">初始生命值</param>
        public PlayerRuntimeData(int initialHealth)
        {
            CurrentHealth = initialHealth;
        }

        /// <summary>
        /// 应用伤害计算结果
        /// 本函数只负责修改运行时状态
        /// </summary>
        /// <param name="result">伤害结果</param>
        public void ApplyDamage(PlayerDamageResult result)
        {
            CurrentHealth = result.CurrentHealth;
        }
    }
}
