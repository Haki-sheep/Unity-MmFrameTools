namespace MieMieFrameWork.Business.Samples.DCES
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 玩家 DCES 示例消费方
    /// 命令走 IPlayerService 通知走 PlayerEvents
    /// </summary>
    public sealed class PlayerDcesSampleConsumer : MonoBehaviour
    {
        /// <summary>
        /// 每次测试伤害值
        /// 示例运行时可在 Inspector 调整
        /// </summary>
        [SerializeField]
        [Min(1)]
        private int damage = 25;

        /// <summary>
        /// 玩家公共服务
        /// 启动时从 GameHub 获取一次
        /// </summary>
        private IPlayerService playerService;

        /// <summary>
        /// 生命变化事件订阅令牌
        /// 销毁时负责取消订阅
        /// </summary>
        private IDisposable healthChangedRegistration;

        /// <summary>
        /// 获取玩家服务并订阅模块事件
        /// Bootstrap 已在 Awake 完成注册
        /// </summary>
        private void Start()
        {
            playerService = GameHub.Get<IPlayerService>();
            
            healthChangedRegistration = MieMieFrameWork.MmGlobalEventBus.GlobalBus.Subscribe(
                PlayerEvents.HealthChanged,
                OnHealthChanged);

            Debug.Log($"[DCES Sample] 当前生命 {playerService.CurrentHealth}");
        }

        /// <summary>
        /// 通过公共服务请求玩家受伤
        /// 可从组件右键菜单执行
        /// </summary>
        [ContextMenu("DCES Sample Take Damage")]
        private void TakeDamage()
        {
            int appliedDamage = playerService.TakeDamage(damage);
            Debug.Log($"[DCES Sample] 实际伤害 {appliedDamage}");
        }

        /// <summary>
        /// 输出生命变化结果
        /// 只消费公共 EventKey 载荷
        /// </summary>
        /// <param name="appliedDamage">实际伤害</param>
        /// <param name="currentHealth">剩余生命</param>
        private void OnHealthChanged(int appliedDamage, int currentHealth)
        {
            bool isDead = currentHealth == 0;
            Debug.Log($"[DCES Sample] 剩余生命 {currentHealth} 死亡 {isDead}");
        }

        /// <summary>
        /// 释放事件订阅
        /// 避免总线持有已销毁消费方
        /// </summary>
        private void OnDestroy()
        {
            healthChangedRegistration?.Dispose();
            healthChangedRegistration = null;
            playerService = null;
        }
    }
}
