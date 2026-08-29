namespace MieMieFrameWork.Pool
{
    using UnityEngine;

    /// <summary>
    /// GameObject 对象池句柄
    /// </summary>
    public sealed class PoolHandle
    {
        /// <summary>
        /// 实际对象池
        /// </summary>
        private readonly GameObjPool gameObjPool;

        /// <summary>
        /// 对象池句柄
        /// </summary>
        internal PoolHandle(GameObjPool gameObjPool)
        {
            this.gameObjPool = gameObjPool;
        }

        /// <summary>
        /// 对象池 Key
        /// </summary>
        public EntityId PoolKey => gameObjPool.PoolKey;

        /// <summary>
        /// 预制体名称
        /// </summary>
        public string PrefabName => gameObjPool.PrefabName;

        /// <summary>
        /// 池内闲置数量
        /// </summary>
        public int PooledCount => gameObjPool.PooledCount;

        /// <summary>
        /// 当前借出数量
        /// </summary>
        public int ActiveCount => gameObjPool.ActiveCount;

        /// <summary>
        /// 池内累计创建数量
        /// </summary>
        public int TotalCreated => gameObjPool.TotalCreated;

        /// <summary>
        /// 池内闲置上限
        /// </summary>
        public int MaxSize => gameObjPool.MaxSize;

        /// <summary>
        /// 从对象池获取 GameObject
        /// </summary>
        public GameObject Get(Transform parent = null)
        {
            GameObject obj = gameObjPool.GetGameObj(parent);
            return obj ?? gameObjPool.CreateNew(parent);
        }

        /// <summary>
        /// 从对象池获取指定组件
        /// </summary>
        public T Get<T>(Transform parent = null) where T : Object
        {
            GameObject obj = Get(parent);
            if (obj == null)
                return null;

            if (typeof(T) == typeof(GameObject))
                return obj as T;

            return obj.GetComponent(typeof(T)) as T;
        }

        /// <summary>
        /// 将 GameObject 归还到当前对象池
        /// </summary>
        public bool Release(GameObject obj)
        {
            return gameObjPool.TryPushGameObj(obj);
        }

        /// <summary>
        /// 将 Component 对应的 GameObject 归还到当前对象池
        /// </summary>
        public bool Release(Component component)
        {
            return Release(component.gameObject);
        }

        /// <summary>
        /// 预热对象池
        /// </summary>
        public void Prewarm(int count)
        {
            gameObjPool.PreWarm(count);
        }

        /// <summary>
        /// 获取对象池运行时快照
        /// </summary>
        public GameObjPoolReporter GetReporter()
        {
            return gameObjPool.GetPoolReporter();
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            gameObjPool.Clear();
        }
    }
}
