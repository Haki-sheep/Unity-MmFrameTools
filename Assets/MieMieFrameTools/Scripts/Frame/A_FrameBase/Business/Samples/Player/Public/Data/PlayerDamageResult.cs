namespace MieMieFrameWork.Business.Samples.DCES
{
    /// <summary>
    /// 玩家伤害业务结果
    /// 对外隐藏模块内部运行时数据
    /// </summary>
    public readonly struct PlayerDamageResult
    {
        public int AppliedDamage { get; }

        public int CurrentHealth { get; }

        public bool IsDead => CurrentHealth == 0;

        /// <summary>
        /// 创建玩家伤害结果
        /// 保存实际伤害与剩余生命
        /// </summary>
        /// <param name="appliedDamage">实际伤害值</param>
        /// <param name="currentHealth">剩余生命值</param>
        public PlayerDamageResult(int appliedDamage, int currentHealth)
        {
            AppliedDamage = appliedDamage;
            CurrentHealth = currentHealth;
        }
    }
}
