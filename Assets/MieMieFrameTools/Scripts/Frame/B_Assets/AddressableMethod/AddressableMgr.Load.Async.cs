namespace MieMieFrameWork.AddressableAsset
{
    using Cysharp.Threading.Tasks;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    /// <summary>
    /// 异步加载
    /// </summary>
    public static partial class AddressableMgr
    {
        /// <summary>
        /// 异步加载组件
        /// </summary>
        public static async UniTask<T> LoadComponentAsync<T>(string address, Transform parent = null) where T : Component
        {
            GameObject go = await LoadGameObjectAsync(address, parent);
            return go != null ? go.GetComponent<T>() : null;
        }

        /// <summary>
        /// 异步得到场景物体 用完 DestroyObject
        /// </summary>
        public static UniTask<GameObject> LoadGameObjectAsync(string address, Transform parent = null)
            => InstantiateAsyncInternal(address, parent);

        /// <summary>
        /// 异步加载资源文件 加引用 用完 ReleaseAsset
        /// </summary>
        public static async UniTask<T> LoadAssetAsync<T>(string address) where T : Object
        {
            await state.EnsureReadyAsync();
            return await LoadAssetAsyncInternal<T>(address, retainOnHit: true);
        }

        /// <summary>
        /// 按 key 或标签批量加载 句柄由 ClearAllCache 释放
        /// </summary>
        public static async UniTask<List<T>> LoadAssetsAsync<T>(
            IList<object> keys,
            Addressables.MergeMode mergeMode = Addressables.MergeMode.Union) where T : Object
        {
            await state.EnsureReadyAsync();
            if (keys == null || keys.Count == 0)
                return new List<T>();

            var handle = Addressables.LoadAssetsAsync<T>(keys, null, mergeMode);
            await handle;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                state.AddBatchHandle(handle);
                return new List<T>(handle.Result);
            }
            if (handle.IsValid())
                Addressables.Release(handle);
            return new List<T>();
        }

        /// <summary>
        /// 按名称列表和标签列表批量加载
        /// </summary>
        public static async UniTask<List<T>> LoadAssetsAsync<T>(
            IList<string> names,
            IList<string> labels,
            Addressables.MergeMode mergeMode = Addressables.MergeMode.Union) where T : Object
        {
            var keys = new List<object>();
            if (names != null)
            {
                for (int i = 0; i < names.Count; i++)
                {
                    if (!string.IsNullOrEmpty(names[i]))
                        keys.Add(names[i]);
                }
            }
            if (labels != null)
            {
                for (int i = 0; i < labels.Count; i++)
                {
                    if (!string.IsNullOrEmpty(labels[i]))
                        keys.Add(labels[i]);
                }
            }
            return await LoadAssetsAsync<T>(keys, mergeMode);
        }

        /// <summary>
        /// 异步实例化 不计引用 用完 DestroyObject
        /// </summary>
        public static UniTask<GameObject> InstantiateAsync(string address, Transform parent = null)
            => InstantiateAsyncInternal(address, parent);

        #region 内部异步加载

        private static async UniTask<T> LoadAssetAsyncInternal<T>(string address, bool retainOnHit) where T : Object
        {
            if (state.TryGetCached(address, retainOnHit, out T cached))
                return cached;

            if (state.TryGetLoading(address, out var existing))
            {
                await existing;
                state.TryGetCached(address, retainOnHit, out cached);
                return cached;
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            state.TrackLoading(address, handle);
            await handle;
            state.UntrackLoading(address);

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
                return null;
            }

            state.StoreAsset(address, handle.Result, handle);
            return handle.Result;
        }

        private static async UniTask<GameObject> InstantiateAsyncInternal(string address, Transform parent)
        {
            await state.EnsureReadyAsync();
            var handle = Addressables.InstantiateAsync(address, parent);
            await handle;
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[AddressableMgr] 异步实例化失败 {address}");
                return null;
            }
            PrepareInstance(handle.Result);
            return handle.Result;
        }

        #endregion
    }
}
