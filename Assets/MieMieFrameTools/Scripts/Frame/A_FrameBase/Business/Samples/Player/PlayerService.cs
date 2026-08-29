namespace MieMieFrameWork.Business.Samples.DCES
{
    /// <summary>
    /// 玩家模块公共服务实现
    /// 负责管理模块内部对象并转发业务请求
    /// </summary>
    internal sealed class PlayerService : IPlayerService
    {
        /// <summary>
        /// 玩家运行时数据
        /// 是运行期间玩家生命的唯一真值
        /// </summary>
        private readonly PlayerRuntimeData runtimeData;

        /// <summary>
        /// 玩家伤害执行器
        /// 负责伤害写入链路
        /// </summary>
        private readonly PlayerDamageExecutor damageExecutor;

        public int CurrentHealth => runtimeData.CurrentHealth;

        /// <summary>
        /// 创建玩家服务
        /// 组装配置 计算器与执行器
        /// </summary>
        /// <param name="initialHealth">初始生命值</param>
        /// <param name="damageReductionPercent">伤害减免百分比</param>
        public PlayerService(int initialHealth, int damageReductionPercent)
        {
            var configData = new PlayerConfigData(damageReductionPercent);
            runtimeData = new PlayerRuntimeData(initialHealth);
            var damageCalculator = new PlayerDamageCalculator(configData);
            damageExecutor = new PlayerDamageExecutor(runtimeData, damageCalculator);
        }

        /// <summary>
        /// 转发玩家伤害请求
        /// 不在服务层重复实现业务规则
        /// </summary>
        /// <param name="request">伤害请求</param>
        /// <returns>伤害结果</returns>
        public PlayerDamageResult TakeDamage(PlayerDamageRequest request)
        {
            return damageExecutor.Execute(request);
        }
    }
}
