using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MieMieFrameTools.ChainedFms
{
    // 持有者接口
    public interface IOwner { 


    }
    
    public interface IState<TOwner> where TOwner : IOwner
    {
        /// <summary>
        /// 初始化状态
        /// </summary>
        void Init(TOwner owner);
        /// <summary>
        /// 进入状态
        /// </summary>
        void OnEnter(TOwner owner);
        /// <summary>
        /// 退出状态
        /// </summary>
        void OnExit(TOwner owner);
        /// <summary>
        /// 是否是第一个状态
        /// </summary>
        bool IsFirstState();
    }
}