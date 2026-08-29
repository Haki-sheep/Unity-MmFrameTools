namespace MiMieFSM.UpdateFsm
{
    using System;

    /// <summary>
    /// FSM Tick 调度接口
    /// </summary>
    public interface IFsmTickScheduler
    {
        /// <summary>
        /// 注册 Update
        /// </summary>
        void AddUpdateListener(Action action);

        /// <summary>
        /// 移除 Update
        /// </summary>
        void RemoveUpdateListener(Action action);

        /// <summary>
        /// 注册 LateUpdate
        /// </summary>
        void AddLateUpdateListener(Action action);

        /// <summary>
        /// 移除 LateUpdate
        /// </summary>
        void RemoveLateUpdateListener(Action action);

        /// <summary>
        /// 注册 FixedUpdate
        /// </summary>
        void AddFixedUpdateListener(Action action);

        /// <summary>
        /// 移除 FixedUpdate
        /// </summary>
        void RemoveFixedUpdateListener(Action action);
    }
}
