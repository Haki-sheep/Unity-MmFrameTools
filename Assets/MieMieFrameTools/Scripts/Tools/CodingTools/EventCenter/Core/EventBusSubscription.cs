namespace MiMieEventBus
{
    using System;

    /// <summary>
    /// 事件订阅令牌 Dispose 时取消订阅
    /// </summary>
    public sealed class EventBusSubscription : IDisposable
    {
        /// <summary>
        /// 取消订阅动作
        /// </summary>
        private Action unsubscribeAction;

        /// <summary>
        /// 是否已释放
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// 创建订阅令牌
        /// </summary>
        public EventBusSubscription(Action unsubscribeAction)
        {
            this.unsubscribeAction = unsubscribeAction;
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            unsubscribeAction?.Invoke();
            unsubscribeAction = null;
        }
    }
}

