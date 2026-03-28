using System;

namespace MieMieFrameTools.ChainedFms
{
    public abstract class BaseGameStage : IState<BaseGameFlow>
    {
        // ------------- 此类独有的 ------------
        public event Action OnFinish;
        protected void Finish() => OnFinish?.Invoke();
        // --------------------------------------


        /// <summary>
        /// 初始化状态
        /// </summary>
        /// <param name="owner"></param>
        public void Init(BaseGameFlow owner) { }

        /// <summary>
        /// 是否是第一个状态
        /// </summary>
        /// <returns></returns>
        public abstract bool IsFirstState();

        /// <summary>
        /// 进入状态
        /// </summary>
        /// <param name="owner"></param>
        public abstract void OnEnter(BaseGameFlow owner);

        /// <summary>
        /// 退出状态
        /// </summary>
        /// <param name="owner"></param>
        public abstract void OnExit(BaseGameFlow owner);

    }
}