namespace MieMieFrameWork
{
    using Cysharp.Threading.Tasks;
    using MieMieFrameWork.Pool;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    /// <summary>
    /// Addressable资源管理器
    /// 功能：
    /// 1. 加载：同步/异步加载资源
    /// 2. 实例化：同步/异步实例化预制体
    /// 3. 缓存：assetCache 字典缓存已加载的资源
    /// 4. 引用计数：referenceCount 追踪资源引用，防止提前释放
    /// 5. 对象池：自动集成 PoolManager，GameObject 类型按规则自动进池
    ///
    /// 句柄由 Addressables 原生层管理，通过 referenceCount 控制 Release 时机，无需手动管理。
    /// </summary>
    public static class AddressableMgr
    {
        #region 内部状态

        // 资源缓存字典：address → (asset, handle)
        private static Dictionary<string, (UnityEngine.Object asset, AsyncOperationHandle handle)> assetCache = new();

        // 资源引用计数：address → count
        private static Dictionary<string, int> referenceCount = new();

        // 加载中的异步操作：address → handle（防止重复加载）
        private static Dictionary<string, AsyncOperationHandle> loadingOperations = new();

        // 通过 Addressables.InstantiateAsync 创建的实例集合
        private static HashSet<GameObject> instantiatedObjects = new();

        // 实例到地址的映射
        private static Dictionary<GameObject, string> instanceToAddress = new();

        // 初始化状态标记
        private static bool isInitialized = false;

        // 批量资源加载句柄集合（用于统一释放）
        private static List<AsyncOperationHandle> multiAssetHandles = new();

        #endregion

        #region 加载 - 同步

        /// <summary>
        /// 同步加载组件，自动按规则决定是否进对象池
        /// </summary>
        public static T LoadComponent<T>(string address, Transform parent = null) where T : Component
        {
            EnsureInitialized();

            if (PoolDef.ShouldUsePool<T>())
            {
                GameObject prefab = LoadAsset<GameObject>(address);
                if (prefab == null)
                    throw new Exception($"无法从地址 {address} 加载预制体");
                return ModuleHub.Instance.GetManager<PoolManager>().GetGameObj<T>(prefab, parent);
            }
            else
            {
                GameObject instance = Instantiate(address, parent);
                return instance?.GetComponent<T>();
            }
        }

        /// <summary>
        /// 同步加载 GameObject，自动按规则决定是否进对象池
        /// </summary>
        public static GameObject LoadGameObject(string address, Transform parent = null)
        {
            EnsureInitialized();

            if (PoolDef.ShouldUsePool<GameObject>())
            {
                GameObject prefab = LoadAsset<GameObject>(address);
                if (prefab != null)
                {
                    return ModuleHub.Instance.GetManager<PoolManager>().GetGameObj<GameObject>(prefab, parent);
                }
            }

            return Instantiate(address, parent);
        }

        /// <summary>
        /// 同步加载 Unity 资源文件（使用缓存）
        /// </summary>
        public static T LoadAsset<T>(string address) where T : UnityEngine.Object
        {
            EnsureInitialized();

            var t0 = Time.realtimeSinceStartup;

            // 命中缓存
            if (assetCache.TryGetValue(address, out var cachedData) && cachedData.asset is T asset)
            {
                AddReference(address);
                return asset;
            }

            // 同步加载（WaitForCompletion 会阻塞当前线程）
            var tAsync = Time.realtimeSinceStartup;
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
            T result = handle.WaitForCompletion();
            var tWait = Time.realtimeSinceStartup;

            if (result != null)
            {
                assetCache[address] = (result, handle);
                AddReference(address);
            }
            else
            {
                if (result is null)
                {
                    Debug.LogError($"无法从地址 {address} 加载资源，获取为 Null");
                    Addressables.Release(handle);
                    return null;
                }
                if (result is not T)
                {
                    Debug.LogError($"无法从地址 {address} 加载资源，资源与泛型不匹配");
                }
                Addressables.Release(handle);
                return null;
            }

            return result;
        }

        /// <summary>
        /// 同步实例化 GameObject
        /// </summary>
        public static GameObject Instantiate(string address, Transform parent = null)
        {
            EnsureInitialized();

            var t0 = Time.realtimeSinceStartup;

            var handle = Addressables.InstantiateAsync(address, parent);
            GameObject result = handle.WaitForCompletion();
            var tWait = Time.realtimeSinceStartup;

            if (result != null)
            {
                result.name = result.name.Replace("(Clone)", "");
                instantiatedObjects.Add(result);
                instanceToAddress[result] = address;
                AddReference(address);
            }
            else
            {
                Debug.LogError($"无法实例化地址: {address}");
            }

            return result;
        }

        #endregion

        #region 加载 - 异步

        /// <summary>
        /// 异步加载组件
        /// </summary>
        public static async UniTask<T> LoadComponentAsync<T>(string address, Transform parent = null) where T : Component
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(address);
            await handle;
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError($"无法从地址 {address} 加载预制体");
                return null;
            }
            GameObject prefab = handle.Result;
            assetCache[address] = (prefab, handle);
            AddReference(address);

            if (PoolDef.ShouldUsePool<T>())
            {
                return ModuleHub.Instance.GetManager<PoolManager>().GetGameObj<T>(prefab, parent);
            }
            else
            {
                GameObject instance = await InstantiateAsyncInternal(address, parent);
                return instance?.GetComponent<T>();
            }
        }

        /// <summary>
        /// 异步加载 GameObject
        /// </summary>
        public static async UniTask<GameObject> LoadGameObjectAsync(string address, Transform parent = null)
        {
            if (PoolDef.ShouldUsePool<GameObject>())
            {
                var handle = Addressables.LoadAssetAsync<GameObject>(address);
                await handle;
                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                {
                    Debug.LogError($"无法从地址 {address} 加载预制体");
                    return null;
                }
                GameObject prefab = handle.Result;
                assetCache[address] = (prefab, handle);
                AddReference(address);
                return ModuleHub.Instance.GetManager<PoolManager>().GetGameObj<GameObject>(prefab, parent);
            }
            else
            {
                return await InstantiateAsyncInternal(address, parent);
            }
        }

        /// <summary>
        /// 异步加载 Unity 资源文件
        /// </summary>
        public static async UniTask<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            EnsureInitialized();

            // 命中缓存
            if (assetCache.TryGetValue(address, out var cachedData) && cachedData.asset is T asset)
            {
                AddReference(address);
                return asset;
            }

            // 检查是否正在加载中
            if (loadingOperations.TryGetValue(address, out var existingHandle))
            {
                await existingHandle;
                if (assetCache.TryGetValue(address, out cachedData) && cachedData.asset is T cachedAsset)
                {
                    AddReference(address);
                    return cachedAsset;
                }
                return null;
            }

            // 发起加载
            var handle = Addressables.LoadAssetAsync<T>(address);
            loadingOperations[address] = handle;
            await handle;
            loadingOperations.Remove(address);

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                T result = handle.Result;
                assetCache[address] = (result, handle);
                AddReference(address);
                return result;
            }
            else
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
                return null;
            }
        }

        /// <summary>
        /// 异步批量加载资源（传入名字与标签的混合列表）
        /// </summary>
        public static async UniTask<List<T>> LoadAssetsAsync<T>(IList<object> keys, Addressables.MergeMode mergeMode = Addressables.MergeMode.Union) where T : UnityEngine.Object
        {
            EnsureInitialized();

            if (keys == null || keys.Count == 0)
            {
                return new List<T>();
            }

            var handle = Addressables.LoadAssetsAsync<T>(keys, null, mergeMode);
            await handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                List<T> results = new List<T>(handle.Result);
                multiAssetHandles.Add(handle);
                return results;
            }
            else
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                return new List<T>();
            }
        }

        /// <summary>
        /// 异步批量加载资源（分别传入名字列表与标签列表）
        /// </summary>
        public static async UniTask<List<T>> LoadAssetsAsync<T>(
            IList<string> names,
            IList<string> labels,
            Addressables.MergeMode mergeMode = Addressables.MergeMode.Union) where T : UnityEngine.Object
        {
            List<object> keys = new();
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
        /// 异步实例化 GameObject
        /// </summary>
        public static async UniTask<GameObject> InstantiateAsync(string address, Transform parent = null)
        {
            return await InstantiateAsyncInternal(address, parent);
        }

        #endregion

        #region 生命周期管理

        /// <summary>
        /// 释放资源引用（引用计数 -1，为 0 时释放原生内存）
        /// </summary>
        public static void ReleaseAsset(string address)
        {
            RemoveReference(address);
        }

        /// <summary>
        /// 安全销毁 GameObject（从 Addressables 实例集合移除或普通销毁）
        /// </summary>
        public static void DestroyObject(GameObject obj)
        {
            if (obj == null) return;

            if (instantiatedObjects.Contains(obj))
            {
                if (instanceToAddress.TryGetValue(obj, out var address))
                {
                    RemoveReference(address);
                    instanceToAddress.Remove(obj);
                }
                Addressables.ReleaseInstance(obj);
                instantiatedObjects.Remove(obj);
            }
            else
            {
                UnityEngine.Object.Destroy(obj);
            }
        }

        /// <summary>
        /// 将对象放回对象池
        /// </summary>
        public static void ReturnToPool(GameObject obj)
        {
            if (obj == null) return;

            if (PoolDef.ShouldUsePool<GameObject>())
            {
                ModuleHub.Instance.GetManager<PoolManager>().PushGameObj(obj);
            }
            else
            {
                DestroyObject(obj);
            }
        }

        /// <summary>
        /// 将普通对象放回对象池
        /// </summary>
        public static void ReturnToPool<T>(T obj) where T : class
        {
            if (PoolDef.ShouldUsePool<T>())
            {
                ModuleHub.Instance.GetManager<PoolManager>().PushObject(obj);
            }
        }

        /// <summary>
        /// 清理所有缓存和实例（切换场景时调用）
        /// </summary>
        public static void ClearAllCache()
        {
            // 释放所有实例
            if (instantiatedObjects.Count > 0)
            {
                var temps = new List<GameObject>(instantiatedObjects);
                foreach (var obj in temps)
                {
                    if (obj != null)
                    {
                        Addressables.ReleaseInstance(obj);
                    }
                }
                instantiatedObjects.Clear();
                instanceToAddress.Clear();
            }

            // 释放所有缓存资源
            foreach (var kvp in assetCache)
            {
                if (kvp.Value.handle.IsValid())
                {
                    Addressables.Release(kvp.Value.handle);
                }
            }

            assetCache.Clear();
            referenceCount.Clear();
            loadingOperations.Clear();

            // 释放批量加载句柄
            if (multiAssetHandles.Count > 0)
            {
                for (int i = 0; i < multiAssetHandles.Count; i++)
                {
                    var handle = multiAssetHandles[i];
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                }
                multiAssetHandles.Clear();
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 确保 Addressable 系统已初始化
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!isInitialized)
            {
                InitializeAsync().Forget();
            }
        }

        /// <summary>
        /// 初始化 Addressable 资源管理系统
        /// </summary>
        private static async UniTaskVoid InitializeAsync()
        {
            if (isInitialized) return;

            var initHandle = Addressables.InitializeAsync();
            await initHandle;
            isInitialized = true;
        }

        /// <summary>
        /// 添加资源引用
        /// </summary>
        private static void AddReference(string address)
        {
            if (referenceCount.ContainsKey(address))
            {
                referenceCount[address]++;
            }
            else
            {
                referenceCount[address] = 1;
            }
        }

        /// <summary>
        /// 移除资源引用，引用归零时释放原生内存
        /// </summary>
        private static void RemoveReference(string address)
        {
            if (referenceCount.ContainsKey(address))
            {
                referenceCount[address]--;
                if (referenceCount[address] <= 0)
                {
                    referenceCount.Remove(address);

                    if (assetCache.TryGetValue(address, out var cachedData))
                    {
                        if (cachedData.handle.IsValid())
                        {
                            Addressables.Release(cachedData.handle);
                        }
                        assetCache.Remove(address);
                    }
                }
            }
        }

        #endregion

        #region 私有方法

        private static async UniTask<GameObject> InstantiateAsyncInternal(string address, Transform parent)
        {
            EnsureInitialized();

            var handle = Addressables.InstantiateAsync(address, parent);
            await handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject result = handle.Result;
                result.name = result.name.Replace("(Clone)", "");
                instantiatedObjects.Add(result);
                instanceToAddress[result] = address;
                AddReference(address);
                return result;
            }
            else
            {
                Debug.LogError($"无法实例化地址: {address}");
                return null;
            }
        }

        #endregion
    }
}
