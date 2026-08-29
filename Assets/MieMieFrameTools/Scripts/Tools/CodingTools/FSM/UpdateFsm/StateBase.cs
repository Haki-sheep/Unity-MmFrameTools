namespace MiMieFSM.UpdateFsm
{
    using System;

    /// <summary>
    /// 状态基类
    /// </summary>
    public abstract class StateBase
    {
        /// <summary>
        /// 黑板数据引用
        /// </summary>
        protected IFsmBlackboard blackboard;

        /// <summary>
        /// 状态名称
        /// </summary>
        public virtual string StateName => GetType().Name;

        /// <summary>
        /// 初始化状态
        /// </summary>
        public virtual void Init(IFsmBlackboard blackboard = null)
        {
            this.blackboard = blackboard;
            OnInit();
        }

        /// <summary>
        /// 反初始化状态
        /// </summary>
        public virtual void UnInit()
        {
            OnUnInit();
            blackboard = null;
        }

        /// <summary>
        /// 状态初始化时调用
        /// </summary>
        public virtual void OnInit() { }

        /// <summary>
        /// 状态反初始化时调用
        /// </summary>
        public virtual void OnUnInit() { }

        /// <summary>
        /// 状态进入时调用
        /// </summary>
        public virtual void OnEnter() { }

        /// <summary>
        /// 状态退出时调用
        /// </summary>
        public virtual void OnExit() { }

        /// <summary>
        /// 状态更新时调用
        /// </summary>
        public virtual void OnUpdate() { }

        /// <summary>
        /// 状态延迟更新时调用
        /// </summary>
        public virtual void OnLateUpdate() { }

        /// <summary>
        /// 状态物理更新时调用
        /// </summary>
        public virtual void OnFixedUpdate() { }

        /// <summary>
        /// 设置黑板数据
        /// </summary>
        protected void SetBlackboardValue<T>(EBlockBoardParme key, T value)
        {
            blackboard?.SetValue(key, value);
        }

        /// <summary>
        /// 获取黑板数据
        /// </summary>
        protected T GetBlackboardValue<T>(EBlockBoardParme key, T defaultValue = default)
        {
            return blackboard != null ? blackboard.GetValue(key, defaultValue) : defaultValue;
        }

        /// <summary>
        /// 检查黑板是否包含指定键
        /// </summary>
        protected bool HasBlackboardKey(EBlockBoardParme key)
        {
            return blackboard?.HasKey(key) ?? false;
        }

        /// <summary>
        /// 移除黑板数据
        /// </summary>
        protected void RemoveBlackboardValue(EBlockBoardParme key)
        {
            blackboard?.RemoveValue(key);
        }

        /// <summary>
        /// 获取状态调试信息
        /// </summary>
        public virtual string GetDebugInfo()
        {
            return StateName;
        }
    }
}
