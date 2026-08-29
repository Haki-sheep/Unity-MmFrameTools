namespace MieMieFrameWork.Pool
{
    using Sirenix.OdinInspector;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using static MieMieFrameWork.ModuleHub;

    /// <summary>
    /// <summary>
    /// 对象池注册与句柄管理器
    /// </summary>
    /// </summary>
    [ManagerAttribute(2)]
    public class PoolManager : IManagerBase, IDisposable
    {
        [Serializable]
        public sealed class PoolManagerConfig
        {
            /// <summary>
            /// 对象池根节点
            /// </summary>
            [SerializeField]
            [LabelText("对象池根节点")]
            private Transform allGameObjectRoot;

            public Transform AllGameObjectRoot => allGameObjectRoot;
        }

        /// <summary>
        /// 全局实例 由 ModuleHub 创建时赋值 调用处直接访问免查注册表
        /// </summary>
        public static PoolManager Instance { get; internal set; }

        /// <summary>
        /// 默认池上限
        /// </summary>
        private const int DefaultMaxSize = 50;

        /// <summary>
        /// 服务根节点
        /// </summary>
        private readonly Transform serviceRoot;

        /// <summary>
        /// 对象池根节点
        /// </summary>
        private Transform AllGameObjectRoot;

        public Transform PoolRoot => AllGameObjectRoot;

        /// <summary>
        /// GameObject 池句柄字典
        /// </summary>
        private readonly Dictionary<EntityId, PoolHandle> poolHandleDict = new();

        /// <summary>
        /// 普通对象池字典
        /// </summary>
        private readonly Dictionary<string, ObjectPool> objectPoolDic = new();

        public PoolManager(PoolManagerConfig poolManagerConfig, Transform serviceRoot)
        {
            this.serviceRoot = serviceRoot;
            AllGameObjectRoot = poolManagerConfig.AllGameObjectRoot;
            Instance = this;
        }

        public void Init()
        {
            if (AllGameObjectRoot is null)
                AllGameObjectRoot = serviceRoot.Find("PoolRoot");
        }

        public void Dispose()
        {
            SelectClearPool();
            if (Instance == this)
                Instance = null;
        }


        #region GameObject 池

        /// <summary>
        /// 获取指定预制体的对象池句柄
        /// </summary>
        public PoolHandle GetPool(GameObject prefab, int maxSize = DefaultMaxSize)
        {
            EntityId poolKey = prefab.GetEntityId();
            if (poolHandleDict.TryGetValue(poolKey, out PoolHandle poolHandle))
                return poolHandle;

            GameObjPool pool = new GameObjPool(prefab, AllGameObjectRoot, maxSize);
            poolHandle = new PoolHandle(pool);
            poolHandleDict.Add(poolKey, poolHandle);
            return poolHandle;
        }

        /// <summary>
        /// 收集所有 GameObject 池快照
        /// </summary>
        public void CollectGameObjPoolInfoList(List<GameObjPoolReporter> resultList)
        {
            resultList.Clear();
            foreach (PoolHandle poolHandle in poolHandleDict.Values)
                resultList.Add(poolHandle.GetReporter());
        }

        #endregion

        #region  Object池

        /// <summary>
        /// 从对象池获取指定类型的对象
        /// </summary>
        public T GetObject<T>() where T : class, new()
        {
            string name = typeof(T).FullName;
            if (objectPoolDic.TryGetValue(name, out ObjectPool pool))
                return pool.GetObj() as T;

            return new T();
        }

        /// <summary>
        /// 将对象放回对象池
        /// </summary>
        public void PushObject(object obj)
        {
            string name = obj.GetType().FullName;
            if (objectPoolDic.TryGetValue(name, out ObjectPool pool))
                pool.PushObj(obj);
            else
                objectPoolDic.Add(name, new ObjectPool(obj));
        }

        #endregion

        #region  清理

        /// <summary>
        /// 清除对象池
        /// </summary>
        public void SelectClearPool(bool clearGameObject = true, bool clearObject = true)
        {
            if (clearGameObject)
            {
                foreach (PoolHandle poolHandle in poolHandleDict.Values)
                    poolHandle.Clear();
                poolHandleDict.Clear();
            }

            if (clearObject)
                objectPoolDic.Clear();
        }

        /// <summary>
        /// 清除所有 GameObject 池
        /// </summary>
        public void ClearAllGameObject() => SelectClearPool(true, false);

        /// <summary>
        /// 清除所有 Object 池
        /// </summary>
        public void ClearAllObject() => SelectClearPool(false, true);

        /// <summary>
        /// 清除指定预制体的池
        /// </summary>
        public void ClearGameObject(GameObject prefab)
        {
            EntityId poolKey = prefab.GetEntityId();
            if (!poolHandleDict.TryGetValue(poolKey, out PoolHandle poolHandle))
                return;

            poolHandle.Clear();
            poolHandleDict.Remove(poolKey);
        }

        /// <summary>
        /// 清除指定类型的 Object 池
        /// </summary>
        public void ClearObject<T>() => objectPoolDic.Remove(typeof(T).FullName);

        /// <summary>
        /// 清除指定类型的 Object 池
        /// </summary>
        public void ClearObject(Type type) => objectPoolDic.Remove(type.FullName);

        #endregion

    }
}
