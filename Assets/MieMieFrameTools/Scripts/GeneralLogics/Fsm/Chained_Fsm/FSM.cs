using System.Collections.Generic;
using System.Linq;

namespace MieMieFrameTools.ChainedFms
{
    public class FSM<TOwner> where TOwner : IOwner
    {
        // 属性
        protected TOwner owner;
        protected bool FsmIsRunning;

        // 容器
        protected List<IState<TOwner>> stateList;
        protected IState<TOwner> currentState;
        public IState<TOwner> CurrentActiveState => currentState;

        /// <summary>
        /// 构造后由外部注入 owner（解决 base(this) 在初始化列表中不可用的问题）
        /// </summary>
        public void InitFsm(TOwner owner)
        {
           stateList = new();
            this.owner = owner;
            FsmIsRunning = false;

        }

        /// <summary>
        /// 启动状态机
        /// </summary>
        public void RunFsm()
        {
            if (FsmIsRunning) return;

            // 1. 先让子类注册状态和转换规则
            FsmInitStateList();
            FsmSetTransitions();

            // 2. 现在 stateList 已包含所有状态，统一初始化
            foreach (var state in stateList)
                state?.Init(owner);

            FsmIsRunning = true;

            // 3. 启动第一个状态
            var firstState = stateList.FirstOrDefault(state => state.IsFirstState());
            if (firstState is not null)
            {
                currentState = firstState;
                currentState?.OnEnter(owner);
            }
        }

        /// <summary>
        /// 初始化状态
        /// </summary>
        protected virtual void FsmInitStateList() { }

        /// <summary>
        /// 设置状态机转换规则
        /// </summary>
        protected virtual void FsmSetTransitions() { }

        /// <summary>
        /// 添加状态
        /// </summary>
        protected void FsmAddState(IState<TOwner> state)
        {
            stateList?.Add(state);
        }

        /// <summary>
        /// 切换到指定状态
        /// </summary>
        protected void FsmSwitchTo<TState>(bool canRepeatState = false) where TState : class, IState<TOwner>
        {
            var targetState = GetState<TState>();
            if (targetState is not null)
            {
                SwitchTo(targetState,canRepeatState);
            }
        }

        /// <summary>
        /// 切换到指定状态，是否可以重复切换
        /// </summary>
        /// <param name="targetState">目标状态</param>
        /// <param name="canRepeatState">是否可以重复切换</param>
        /// <param name="canRepeatState"></param>
        private void SwitchTo(IState<TOwner> targetState,bool canRepeatState)
        {
            // 如果不可以重复切换
            if (!canRepeatState) { 
                if (targetState == currentState) return; 
            }

            currentState?.OnExit(owner);
            currentState = targetState;
            currentState?.OnEnter(owner);
        }

        /// <summary>
        /// 获取指定类型的状态
        /// </summary>
        protected TState GetState<TState>() where TState : class, IState<TOwner>
        {
            return stateList.FirstOrDefault(s => s is TState) as TState;
        }
    }
}