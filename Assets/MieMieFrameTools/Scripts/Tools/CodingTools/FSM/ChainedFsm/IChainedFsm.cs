namespace MiMieFSM.ChainedFsm
{
    using System;

    /// <summary>
    /// 链式状态基类
    /// </summary>
    public abstract class IChainedFsm
    {
        /// <summary>
        /// 进入回调
        /// </summary>
        public Action OnEnterAction;

        /// <summary>
        /// 退出回调
        /// </summary>
        public Action OnExitAction;

        /// <summary>
        /// 进入状态
        /// </summary>
        public virtual void OnEnter()
        {
            OnEnterAction?.Invoke();
        }

        /// <summary>
        /// 退出状态
        /// </summary>
        public virtual void OnExit()
        {
            OnExitAction?.Invoke();
        }
    }
}
