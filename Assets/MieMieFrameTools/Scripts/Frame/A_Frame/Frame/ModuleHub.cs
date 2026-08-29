namespace MieMieFrameWork
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using MieMieFrameWork.Pool;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using UnityEngine.Serialization;

    /// <summary>
    /// 游戏根节点管理器 - 负责框架核心系统的初始化和管理
    /// </summary> 
    public class ModuleHub : SingletonMono<ModuleHub>
    {
        protected override bool DontDestroyOnLoadEnabled => true;

        private readonly Dictionary<Type, IManagerBase> managerDict = new Dictionary<Type, IManagerBase>();

        [FormerlySerializedAs("uICoreMgr")]
        [SerializeField, LabelText("UI管理器")]
        private MonoBehaviour uiCoreMgrBehaviour;

        [SerializeField, LabelText("存档子目录")]
        private string archiveSubFolder = "Archives";

        /// <summary>
        /// 对象池管理器配置
        /// </summary>
        [SerializeField, LabelText("对象池管理器配置")]
        private PoolManager.PoolManagerConfig poolManagerConfig = new PoolManager.PoolManagerConfig();

        /// <summary>音频管理器配置</summary>
        [SerializeField, LabelText("音频管理器配置")]
        private AudioManager.AudioManagerConfig audioManagerConfig = new AudioManager.AudioManagerConfig();

        /// <summary>
        /// 存档管理器实例 未编译存档核心时为 null
        /// </summary>
        private object archiveMgr;

        /// <summary>
        /// UI 管理器实例 未安装 com.hakisheep.mm-uiframe 时为 null
        /// </summary>
        private object uiCoreMgr;


        #region Unity 生命周期

        protected override void Awake()
        {
            base.Awake();
            InitializeFramework();
        }

        protected override void OnDestroy()
        {
            if (Instance == this)
            {
                CleanupFramework();
            }

            base.OnDestroy();
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
                InitArchiveMgr();
                InitUIHub();
                GetAllManager();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameRoot] 框架初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化存档管理器
        /// </summary>
        private void InitArchiveMgr()
        {
            Type archiveType = ResolveArchiveMgrType();
            if (archiveType == null)
            {
                Debug.LogWarning("[ModuleHub] 存档核心未编译 跳过 ArchiveMgr 初始化");
                return;
            }

            string folder = string.IsNullOrWhiteSpace(archiveSubFolder) ? "Archives" : archiveSubFolder.Trim();
            string rootPath = Path.Combine(Application.persistentDataPath, folder);
            archiveMgr = Activator.CreateInstance(archiveType, rootPath);
        }

        /// <summary>
        /// 解析 MiMieSaver.ArchiveMgr 类型
        /// </summary>
        private static Type ResolveArchiveMgrType()
        {
            const string archiveTypeName = "MiMieSaver.ArchiveMgr";

            Type archiveType = Type.GetType(archiveTypeName);
            if (archiveType != null)
                return archiveType;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                archiveType = assembly.GetType(archiveTypeName);
                if (archiveType != null)
                    return archiveType;
            }

            return null;
        }

        /// <summary>
        /// 是否已安装并初始化存档模块
        /// </summary>
        public bool HasArchive => archiveMgr != null;

        /// <summary>
        /// 获取存档管理器 使用 MiMieSaver 命名空间下的 IArchiveMgr 等类型
        /// </summary>
        public T GetArchive<T>() where T : class => archiveMgr as T;

        /// <summary>
        /// 初始化 UI 管理器钩子
        /// </summary>
        private void InitUIHub()
        {
            Type uiType = ResolveUIHubType();
            if (uiType == null)
            {
                Debug.LogWarning("[ModuleHub] UI 模块未安装或 MieMieUIFrameWork.UI 程序集未编译 跳过 UIHub 初始化");
                return;
            }

            Component uiComp = uiCoreMgrBehaviour;
            if (uiComp == null || !uiType.IsInstanceOfType(uiComp))
                uiComp = GetComponent(uiType);

            // UIRoot 与 FrameRoot 常为场景内两个物体 同物体找不到时全局搜一次
            if (uiComp == null)
                uiComp = FindAnyObjectByType(uiType, FindObjectsInactive.Include) as Component;

            if (uiComp == null)
            {
                Debug.LogWarning("[ModuleHub] 已安装 UI 模块但未找到 UIHub 组件 请挂到场景 UIRoot 或拖入序列化槽");
                return;
            }

            uiCoreMgr = uiComp;
            uiCoreMgrBehaviour = uiComp as MonoBehaviour;
        }

        /// <summary>
        /// 解析 MmUIFrameWork.Core.UIHub 类型
        /// </summary>
        private static Type ResolveUIHubType()
        {
            const string uiTypeName = "MmUIFrameWork.Core.UIHub";
            const string assemblyName = "MieMieUIFrameWork.UI";

            Type uiType = Type.GetType($"{uiTypeName}, {assemblyName}");
            if (uiType != null)
                return uiType;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name != assemblyName)
                    continue;

                uiType = assembly.GetType(uiTypeName);
                if (uiType != null)
                    return uiType;
            }

            return null;
        }

        /// <summary>
        /// 是否已安装并绑定 UI 模块
        /// </summary>
        public bool HasUI => uiCoreMgr != null;

        /// <summary>
        /// 获取 UI 管理器 需引用 MieMieUIFrameWork.UI 后使用 UIHub 类型
        /// </summary>
        public T GetUI<T>() where T : class => uiCoreMgr as T;

        private void GetAllManager()
        {
            var managers = new List<IManagerBase>();
            managers.Add(new MieMieFrameWork.Asset.MmAssetBootManager());
            managers.Add(new PoolManager(poolManagerConfig, transform));
            managers.Add(new AudioManager(audioManagerConfig, transform));
            managers.Add(new AsyncTaskManager());
            managers.Add(new UniTimerManager());
            managers.AddRange(this.transform.GetComponents<IManagerBase>());

            if (uiCoreMgr != null)
                managers.Add(new ReflectionManagerAdapter(uiCoreMgr, 10));

            foreach (var manager in managers.Where(m => m is not null)
                                                            .OrderBy(GetManagerPriority))
            {
                var managerType = manager is ReflectionManagerAdapter adapter
                    ? adapter.TargetType
                    : manager.GetType();
                if (managerDict.ContainsKey(managerType))
                {
                    Debug.LogError($"[ModuleHub] 发现重复管理器类型: {managerType.Name}，后续实例将被忽略。");
                    continue;
                }

                managerDict.Add(managerType, manager);
                manager.Init();
            }
        }

        /// <summary>
        /// 获取管理器优先级
        /// </summary>
        /// <param name="manager"></param>
        /// <returns></returns>
        private static int GetManagerPriority(IManagerBase manager)
        {
            if (manager is ReflectionManagerAdapter adapter)
                return adapter.Priority;

            var managerType = manager.GetType();
            var attr = (ManagerAttribute)Attribute.GetCustomAttribute(managerType, typeof(ManagerAttribute));
            return attr?.Priority ?? 0;
        }

        /// <summary>
        /// 获取管理器
        /// </summary>
        /// <typeparam name="T">管理器类型</typeparam>
        /// <returns>管理器实例</returns>
        /// <exception cref="Exception"></exception>
        public T GetManager<T>() where T : IManagerBase
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
            foreach (var manager in managerDict.Values)
            {
                if (manager is IDisposable disposableManager)
                {
                    disposableManager.Dispose();
                }
            }

            managerDict.Clear();
            MmGlobalEventBus.GlobalBus.Clear();
            archiveMgr = null;
            uiCoreMgr = null;
        }

        #endregion

        #region 管理器特性与接口
        public interface IManagerBase
        {
            public void Init();
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        public sealed class ManagerAttribute : Attribute
        {
            public int Priority { get; }

            public ManagerAttribute(int priority = 0)
            {
                Priority = priority;
            }
        }

        /// <summary>
        /// 反射适配可选模块管理器
        /// </summary>
        private sealed class ReflectionManagerAdapter : IManagerBase
        {
            /// <summary>
            /// 目标实例
            /// </summary>
            private readonly object target;

            /// <summary>
            /// Init 方法
            /// </summary>
            private readonly MethodInfo initMethod;

            /// <summary>
            /// 优先级
            /// </summary>
            private readonly int priority;

            public Type TargetType { get; }

            public ReflectionManagerAdapter(object target, int priority = 0)
            {
                this.target = target;
                this.priority = priority;
                TargetType = target.GetType();
                initMethod = TargetType.GetMethod("Init", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            }

            public void Init()
            {
                initMethod?.Invoke(target, null);
            }

            public int Priority => priority;
        }
        #endregion

    }
}
