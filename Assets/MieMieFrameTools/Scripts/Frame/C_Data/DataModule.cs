using System;

namespace MieMieFrameWork.Data
{
    /// <summary>
    /// 数据快照模块基类
    /// </summary>
    /// <typeparam name="TSnapshot">数据源生成的完整快照类型</typeparam>
    public abstract class DataModule<TSnapshot> : IDisposable
        where TSnapshot : class
    {
        /// <summary>当前生效的数据快照 </summary>
        private TSnapshot CurrentSnapshot;

        /// <summary> 模块是否已经初始化</summary>
        private bool Initialized;

        /// <summary> 模块是否已经释放</summary>
        private bool Disposed;

        public bool IsInitialized => Initialized;

        public int Revision { get; private set; }

        protected TSnapshot Snapshot
        {
            get
            {
                ThrowIfDisposed();
                if (!Initialized)
                    throw new InvalidOperationException("数据模块尚未初始化");

                return CurrentSnapshot;
            }
        }

        /// <summary>
        /// 创建首份数据快照
        /// </summary>
        public void Init()
        {
            ThrowIfDisposed();
            if (Initialized)
                throw new InvalidOperationException("数据模块只能初始化一次");

            CurrentSnapshot = CreateCheckedSnapshot();
            Initialized = true;
            Revision = 1;
        }

        /// <summary>
        /// 构建并替换当前数据快照
        /// </summary>
        public void Reload()
        {
            ThrowIfDisposed();
            if (!Initialized)
                throw new InvalidOperationException("数据模块尚未初始化");

            var nextSnapshot = CreateCheckedSnapshot();
            var previousSnapshot = CurrentSnapshot;
            CurrentSnapshot = nextSnapshot;
            Revision++;
            ReleaseSnapshot(previousSnapshot);
        }

        /// <summary>
        /// 释放当前数据快照
        /// </summary>
        public void Dispose()
        {
            if (Disposed)
                return;

            if (Initialized)
                ReleaseSnapshot(CurrentSnapshot);

            CurrentSnapshot = null;
            Initialized = false;
            Disposed = true;
        }

        /// <summary>
        /// 创建一份完整且可独立替换的数据快照
        /// </summary>
        protected abstract TSnapshot CreateSnapshot();

        /// <summary>
        /// 释放旧数据快照持有的资源
        /// </summary>
        /// <param name="snapshot">需要释放的数据快照</param>
        protected virtual void ReleaseSnapshot(TSnapshot snapshot)
        {
        }

        /// <summary>
        /// 创建并检查数据快照
        /// </summary>
        private TSnapshot CreateCheckedSnapshot()
        {
            var snapshot = CreateSnapshot();
            if (snapshot == null)
                throw new InvalidOperationException("数据源不能返回空快照");

            return snapshot;
        }

        /// <summary>
        /// 检查模块释放状态
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (Disposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}
