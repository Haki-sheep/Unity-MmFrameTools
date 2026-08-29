namespace MieMieFrameWork.Business.Samples.DCES
{
    /// <summary>
    /// 玩家模块公共服务契约
    /// 跨模块命令与查询只走本接口
    /// </summary>
    public interface IPlayerService : IGameService
    {
        public int CurrentHealth { get; }

        /// <summary>
        /// 对玩家执行一次伤害
        /// 返回本次伤害结果
        /// </summary>
        /// <param name="request">伤害请求</param>
        /// <returns>伤害结果</returns>
        public PlayerDamageResult TakeDamage(PlayerDamageRequest request);
    }
}
