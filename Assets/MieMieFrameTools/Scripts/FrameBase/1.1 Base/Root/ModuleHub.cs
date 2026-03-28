namespace MieMieFrameWork
{
    using System;
    using System.Collections.Generic;
    using MieMieFrameWork.UI;
    using Sirenix.OdinInspector;
    using UnityEngine; 

    /// <summary>
    /// 游戏根节点管理器 - 负责框架核心系统的初始化和管理
    /// </summary> 
    public class ModuleHub : SingletonMono<ModuleHub>  
    {
        [field: SerializeField, LabelText("游戏设置")]
        public FrameSetting FrameSetting { get; private set; }

        private Dictionary<Type, I_ManagerBase> managerDict = new Dictionary<Type, I_ManagerBase>();
        
        #region Unity 生命周期

        protected override void Awake()
        {
            base.Awake();
            InitializeFramework();
        }

        private void OnDestroy()
        {
            CleanupFramework();
        }

        #endregion

        #region 框架初始化

        /// <summary>
        /// 初始化整个框架系统
        /// </summary>
        private void InitializeFramework()
        {
            try
            {
                if (this.FrameSetting is null) throw new Exception("游戏配置为空");
                {
                    //初始化配置文件
                    this.FrameSetting.Initialize();
                    //初始化所有管理器
                    GetAllManager();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameRoot] 框架初始化失败: {ex.Message}");
            }
        }

        private void GetAllManager()
        {
            List<I_ManagerBase> managers = new List<I_ManagerBase>();
            managers.AddRange(this.transform.GetComponents<I_ManagerBase>());
            //获取特殊的UIManager
            managers.Add(GameObject.FindFirstObjectByType<UICoreMgr>());

            foreach (var manager in managers)
            {
                managerDict[manager.GetType()] = manager;

                manager.Init();
            }
        }

        public T GetManager<T>() where T : I_ManagerBase
        {
            if (managerDict.TryGetValue(typeof(T), out var manager))
            {
                if (manager is T typedManager)
                {
                    return typedManager;
                }
                else
                {
                    throw new Exception($"管理器 {typeof(T).Name} 类型不匹配");
                }
            }
            else
            {
                throw new Exception($"管理器 {typeof(T).Name} 不存在");
            }
        }
        /// <summary>
        /// 清理框架资源
        /// </summary>
        private void CleanupFramework()
        {
            // 清理事件中心
            EventCenter.ClearAllListeners();

        }

        #endregion

    }
}