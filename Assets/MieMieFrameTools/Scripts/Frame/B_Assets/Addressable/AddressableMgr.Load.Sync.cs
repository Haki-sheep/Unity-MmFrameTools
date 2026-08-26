namespace MieMieFrameWork.AddressableAsset
{
    using UnityEngine;
    using UnityEngine.AddressableAssets;

    /// <summary>
    /// 同步加载 会卡主线程 适合启动或工具
    /// </summary>
    public static partial class AddressableMgr
    {
        /// <summary>
        /// 同步加载组件
        /// </summary>
        public static T LoadComponent<T>(string address, Transform parent = null) where T : Component
        {
            GameObject go = LoadGameObject(address, parent);
            return go != null ? go.GetComponent<T>() : null;
        }

        /// <summary>
        /// 同步得到场景物体 用完 DestroyObject
        /// </summary>
        public static GameObject LoadGameObject(string address, Transform parent = null)
            => Instantiate(address, parent);

        /// <summary>
        /// 同步加载资源文件 进缓存并加引用 用完 ReleaseAsset
        /// </summary>
        public static T LoadAsset<T>(string address) where T : Object
        {
            state.EnsureReadySync();
            return LoadAssetSync<T>(address, retainOnCacheHit: true);
        }

        /// <summary>
        /// 同步实例化 不计引用 用完 DestroyObject
        /// </summary>
        public static GameObject Instantiate(string address, Transform parent = null)
        {
            state.EnsureReadySync();
            var handle = Addressables.InstantiateAsync(address, parent);
            GameObject result = handle.WaitForCompletion();
            if (result != null)
                PrepareInstance(result);
            else
                Debug.LogError($"[AddressableMgr] 无法实例化 {address}");
            return result;
        }

        #region 内部同步加载

        private static T LoadAssetSync<T>(string address, bool retainOnCacheHit) where T : Object
        {
            if (state.TryGetCached(address, retainOnCacheHit, out T cached))
                return cached;

            if (state.TryGetLoading(address, out var existing))
            {
                existing.WaitForCompletion();
                state.TryGetCached(address, retainOnCacheHit, out cached);
                return cached;
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            state.TrackLoading(address, handle);
            T result = handle.WaitForCompletion();
            state.UntrackLoading(address);

            if (result == null)
            {
                Debug.LogError($"[AddressableMgr] 加载失败 {address}");
                if (handle.IsValid())
                    Addressables.Release(handle);
                return null;
            }

            state.StoreAsset(address, result, handle);
            return result;
        }

        #endregion
    }
}
