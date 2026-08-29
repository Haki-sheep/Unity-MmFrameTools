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
        /// 返回实际扣血量
        /// </summary>
        /// <param name="damage">请求伤害值</param>
        /// <returns>实际扣血量</returns>
        public int TakeDamage(int damage);
    }
}
