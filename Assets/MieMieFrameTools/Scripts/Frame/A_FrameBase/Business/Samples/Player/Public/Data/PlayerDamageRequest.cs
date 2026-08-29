namespace MieMieFrameWork.Business.Samples.DCES
{
    /// <summary>
    /// 玩家伤害业务输入
    /// 只保存本次行为需要的数据
    /// </summary>
    public readonly struct PlayerDamageRequest
    {
        public int Damage { get; }

        /// <summary>
        /// 创建玩家伤害请求
        /// 保存请求伤害值
        /// </summary>
        /// <param name="damage">请求伤害值</param>
        public PlayerDamageRequest(int damage)
        {
            Damage = damage;
        }
    }
}
