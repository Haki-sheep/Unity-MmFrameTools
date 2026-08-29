namespace MieMieFrameWork.Business.Samples.DCES
{
    using System;

    /// <summary>
    /// 玩家伤害静态纯函数
    /// 无字段无依赖 入参全走方法参数
    /// </summary>
    internal static class PlayerDamageMath
    {
        /// <summary>
        /// 将伤害限制为非负整数
        /// </summary>
        /// <param name="damage">原始伤害</param>
        /// <returns>非负伤害</returns>
        public static int ClampNonNegative(int damage)
        {
            return Math.Max(0, damage);
        }

        /// <summary>
        /// 按百分比减免伤害
        /// </summary>
        /// <param name="damage">原始伤害</param>
        /// <param name="reductionPercent">减免百分比</param>
        /// <returns>减免后伤害</returns>
        public static int ApplyReduction(int damage, int reductionPercent)
        {
            int clampedDamage = ClampNonNegative(damage);
            int clampedPercent = reductionPercent;
            if (clampedPercent < 0)
            {
                clampedPercent = 0;
            }

            if (clampedPercent > 100)
            {
                clampedPercent = 100;
            }

            int reducedDamage = clampedDamage * (100 - clampedPercent) / 100;
            return ClampNonNegative(reducedDamage);
        }

        /// <summary>
        /// 计算实际扣血与剩余生命
        /// </summary>
        /// <param name="currentHealth">当前生命</param>
        /// <param name="damage">请求伤害</param>
        /// <param name="appliedDamage">实际扣血</param>
        /// <param name="remainingHealth">剩余生命</param>
        public static void ResolveHealth(
            int currentHealth,
            int damage,
            out int appliedDamage,
            out int remainingHealth)
        {
            int validDamage = ClampNonNegative(damage);
            appliedDamage = Math.Min(currentHealth, validDamage);
            remainingHealth = currentHealth - appliedDamage;
        }
    }
}
