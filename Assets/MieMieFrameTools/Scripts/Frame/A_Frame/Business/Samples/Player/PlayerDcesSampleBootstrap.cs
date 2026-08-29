namespace MieMieFrameWork.Business.Samples.DCES
{
    using UnityEngine;

    /// <summary>
    /// 玩家 DCES 示例组装入口
    /// 负责创建注册与注销玩家服务
    /// </summary>
    public sealed class PlayerDcesSampleBootstrap : MonoBehaviour
    {
        /// <summary>
        /// 玩家初始生命值
        /// 示例运行时可在 Inspector 调整
        /// </summary>
        [SerializeField]
        [Min(1)]
        private int initialHealth = 100;

        /// <summary>
        /// 伤害减免百分比
        /// 用于演示实例 Calculator 注入只读配置
        /// </summary>
        [SerializeField]
        [Range(0, 100)]
        private int damageReductionPercent = 20;

        /// <summary>
        /// 玩家服务实例
        /// 生命周期归当前组装入口所有
        /// </summary>
        private PlayerService playerService;

        /// <summary>
        /// 创建并注册玩家服务
        /// 是本示例唯一组装入口
        /// </summary>
        private void Awake()
        {
            playerService = new PlayerService(initialHealth, damageReductionPercent);
            GameHub.Register<IPlayerService>(playerService);
        }

        /// <summary>
        /// 注销玩家服务
        /// 避免静态注册表持有已销毁模块
        /// </summary>
        private void OnDestroy()
        {
            GameHub.Unregister<IPlayerService>();
            playerService = null;
        }
    }
}
