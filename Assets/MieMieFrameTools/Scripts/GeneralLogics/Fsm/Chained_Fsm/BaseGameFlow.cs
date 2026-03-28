namespace MieMieFrameTools.ChainedFms
{
    /// <summary>
    /// 游戏流程基类
    /// </summary>
    public abstract class BaseGameFlow : FSM<BaseGameFlow>, IOwner
    {
        /// <summary>
        /// 初始化流程
        /// </summary>
        public void InitFlow()
        {
            base.InitFsm(this);
            RunFsm();
        }

        protected override void FsmInitStateList()
        {
            // 初始化所有游戏阶段
            FsmRegisterStages();
        }
        protected abstract void FsmRegisterStages();
        protected override void FsmSetTransitions()
        {
            // 设置阶段转换规则
            SetupTransitions();
        }

        /// <summary>
        /// 设置转换规则（子类实现）
        /// </summary>
        protected abstract void SetupTransitions();

        /// <summary>
        /// 添加游戏阶段
        /// </summary>
        protected void AddStage<T>() where T : BaseGameStage, new()
        {
            FsmAddState(new T());
        }

        /// <summary>
        /// 切换到指定阶段（对外暴露）
        /// </summary>
        protected void SwitchToStage<T>() where T : class, IState<BaseGameFlow>
        {
            FsmSwitchTo<T>();
        }
    }
}