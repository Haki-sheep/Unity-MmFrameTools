namespace MieMieFrameWork
{
    using MieMieFrameWork.Pool;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    /// <summary>
    /// Addressable资源管理器 - 简洁的Addressable资源加载解决方案
    /// 支持同步/异步加载，自动集成对象池管理
    /// </summary>
    public static class AddressableMgr
    {
        // 资源缓存字典
        private static Dictionary<string, (UnityEngine.Object asset, AsyncOperationHandle handle)> assetCache
            = new();

        // 资源引用计数
        private static Dictionary<string, int> referenceCount = new();

        // 加载中的异步操作
        private static Dictionary<string, AsyncOperationHandle> loadingOperations = new();

        // 通过 Addressables.InstantiateAsync 创建的实例集合
        private static HashSet<GameObject> instantiatedObjects = new();

        // 实例到地址的映射
        private static Dictionary<GameObject, string> instanceToAddress = new();

        // 初始化状态
        private static bool isInitialized = false;

        // 批量资源加载句柄（名字/标签混合加载产生的句柄集合，用于统一释放）
        private static List<AsyncOperationHandle> multiAssetHandles = new();

        #region 核心加载方法 - 参考ResourcesManager设计

        /// <summary>
        /// 检查是否需要使用对象池缓存
        /// </summary>
        private static bool ShouldUsePool<T>()
        {
            return ModuleHub.Instance?.FrameSetting.PoolCacheSet.Contains(typeof(T)) == true;
        }

        /// <summary>
        /// 创建普通类实例（自动使用对象池）
        /// </summary>
        public static T CreateInstance<T>() where T : class, new()
        {
            return ShouldUsePool<T>() ? ModuleHub.Instance.GetManager<PoolManager>().GetObject<T>() : new T();
        }

        /// <summary>
        /// 同步加载组件
        /// </summary>
        public static T LoadComponent<T>(string address, Transform parent = null) where T : Component
        {
            EnsureInitialized();
            
            if (ShouldUsePool<T>())
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
        /// 同步加载GameObject
        /// </summary>
        public static GameObject LoadGameObject(string address, Transform parent = null)
        {
            EnsureInitialized();
            
            if (ShouldUsePool<GameObject>())
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
        /// 异步加载组件
        /// </summary>
        public static void LoadComponentAsync<T>(string address, Action<T> onComplete, Transform parent = null) where T : Component
        {
            ModuleHub.Instance.GetManager<MonoManager>().StartCoroutine(LoadComponentAsyncCoroutine(address, onComplete, parent));
        }

        /// <summary>
        /// 异步加载GameObject
        /// </summary>
        public static void LoadGameObjectAsync(string address, Action<GameObject> onComplete, Transform parent = null)
        {
            ModuleHub.Instance.GetManager<MonoManager>().StartCoroutine(LoadGameObjectAsyncCoroutine(address, onComplete, parent));
        }

        #endregion

        #region 便捷方法

        /// <summary>
        /// 同步加载Unity资源文件（使用缓存）
        /// </summary>
        public static T LoadAsset<T>(string address) where T : UnityEngine.Object
        {
            EnsureInitialized();
            
            // 检查缓存
            if (assetCache.TryGetValue(address, out var cachedData) && cachedData.asset is T asset)
            {
                AddReference(address);
                return asset;
            }

            // 同步加载（仅用于预制体等必要场景）
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
            T result = handle.WaitForCompletion();

            if (result != null)
            {
                assetCache[address] = (result, handle);
                AddReference(address);
            }
            else
            {    
                if(result is null){
                    Debug.LogError($"无法从地址 {address} 加载资源 获取为Null");
                    Addressables.Release(handle);            
                    return null;
                }
                //如果是资源和T类型不一致
                if(result is not T)
                {   
                    Debug.LogError($"无法从地址 {address} 加载资源 资源与泛型不匹配");
                }
                Addressables.Release(handle);            
                return null;
            }

            return result;
        }

        /// <summary>
        /// 异步加载Unity资源文件
        /// </summary>
        public static void LoadAssetAsync<T>(string address, Action<T> onComplete) where T : UnityEngine.Object
        {
            ModuleHub.Instance.GetManager<MonoManager>().StartCoroutine(LoadAssetAsyncCoroutine(address, onComplete));
        }

        /// <summary>
        /// 异步批量加载资源（传入名字与标签的混合列表）
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="keys">名字与标签的混合列表，例如：["icon", "UI", "someLabel"]</param>
        /// <param name="onComplete">加载完成回调，返回资源列表</param>
        /// <param name="mergeMode">合并模式，默认 Union</param>
        public static void LoadAssetsAsync<T>(IList<object> keys, Action<List<T>> onComplete, Addressables.MergeMode mergeMode = Addressables.MergeMode.Union) where T : UnityEngine.Object
        {
            ModuleHub.Instance.GetManager<MonoManager>().StartCoroutine(LoadAssetsAsyncCoroutine(keys, onComplete, mergeMode));
        }

        /// <summary>
        /// 异步批量加载资源（分别传入名字列表与标签列表）
        /// </summary>
        public static void LoadAssetsAsync<T>(
        IList<string> names,
        IList<string> labels, Action<List<T>> onComplete, 
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
            ModuleHub.Instance.GetManager<MonoManager>().StartCoroutine(LoadAssetsAsyncCoroutine(keys, onComplete, mergeMode));
        }

        /// <summary>
        /// 同步实例化GameObject
        /// </summary>
        public static GameObject Instantiate(string address, Transform parent = null)
        {
            EnsureInitialized();
            
            var handle = Addressables.InstantiateAsync(address, parent);
            GameObject result = handle.WaitForCompletion();

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

        /// <summary>
        /// 异步实例化GameObject
        /// </summary>
        public static void InstantiateAsync(string address, Action<GameObject> onComplete, Transform parent = null)
        {
            ModuleHub.Instance.GetManager<MonoManager>().StartCoroutine(InstantiateAsyncCoroutine(address, onComplete, parent));
        }

        #endregion

        #region 资源管理

        /// <summary>
        /// 释放资源引用
        /// </summary>
        public static void ReleaseAsset(string address)
        {
            RemoveReference(address);
        }

        /// <summary>
        /// 安全销毁GameObject
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
            
            if (ShouldUsePool<GameObject>())
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
            if (ShouldUsePool<T>())
            {
                ModuleHub.Instance.GetManager<PoolManager>().PushObject(obj);
            }
        }

        /// <summary>
        /// 清理所有缓存
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

        private static void EnsureInitialized()
        {
            if (!isInitialized)
            {
                ModuleHub.Instance.GetManager<MonoManager>().StartCoroutine(InitializeAsync());
            }
        }

        /// <summary>
        /// 初始化Addressable资源管理器
        /// </summary>
        private static IEnumerator InitializeAsync()
        {
            if (isInitialized) yield break;

            var initHandle = Addressables.InitializeAsync();
            yield return initHandle;
            isInitialized = true;
        }

        /// <summary>
        /// 添加资源引用
        /// </summary>
        /// <param name="address">资源地址</param>
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

        #region 私有协程实现

        private static IEnumerator LoadAssetAsyncCoroutine<T>(string address, Action<T> onComplete) where T : UnityEngine.Object
        {
            EnsureInitialized();
            
            // 检查缓存
            if (assetCache.TryGetValue(address, out var cachedData) && cachedData.asset is T asset)
            {
                AddReference(address);
                onComplete?.Invoke(asset);
                yield break;
            }

            // 检查是否正在加载
            if (loadingOperations.ContainsKey(address))
            {
                yield return loadingOperations[address];

                if (assetCache.TryGetValue(address, out cachedData) && cachedData.asset is T cachedAsset)
                {
                    AddReference(address);
                    onComplete?.Invoke(cachedAsset);
                }
                else
                {
                    onComplete?.Invoke(null);
                }
                yield break;
            }

            // 开始加载
            var handle = Addressables.LoadAssetAsync<T>(address);
            loadingOperations[address] = handle;
            yield return handle;
            loadingOperations.Remove(address);

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                T result = handle.Result;
                assetCache[address] = (result, handle);
                AddReference(address);
                onComplete?.Invoke(result);
            }
            else
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
                onComplete?.Invoke(null);
            }
        }

        /// <summary>
        /// 异步批量加载资源协程（名字与标签混合 keys + 合并模式）
        /// </summary>
        private static IEnumerator LoadAssetsAsyncCoroutine<T>(IList<object> keys, Action<List<T>> onComplete, Addressables.MergeMode mergeMode) where T : UnityEngine.Object
        {
            EnsureInitialized();

            if (keys == null || keys.Count == 0)
            {
                onComplete?.Invoke(new List<T>());
                yield break;
            }

            var handle = Addressables.LoadAssetsAsync<T>(keys, null, mergeMode);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                List<T> results = new List<T>(handle.Result);
                // 保存句柄以便后续统一释放
                multiAssetHandles.Add(handle);
                onComplete?.Invoke(results);
            }
            else
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                onComplete?.Invoke(new List<T>());
            }
        }

        private static IEnumerator LoadComponentAsyncCoroutine<T>(string address, Action<T> onComplete, Transform parent) where T : Component
        {
            if (ShouldUsePool<T>())
            {
                GameObject prefab = null;
                yield return LoadAssetAsyncCoroutine<GameObject>(address, (result) => { prefab = result; });

                if (prefab != null)
                {
                    T component = ModuleHub.Instance.GetManager<PoolManager>().GetGameObj<T>(prefab, parent);
                    onComplete?.Invoke(component);
                    yield break;
                }
            }

            // 直接实例化
            GameObject instance = null;
            yield return InstantiateAsyncCoroutine(address, (result) => { instance = result; }, parent);

            if (instance != null)
            {
                T component = instance.GetComponent<T>();
                onComplete?.Invoke(component);
            }
            else
            {
                onComplete?.Invoke(null);
            }
        }

        private static IEnumerator LoadGameObjectAsyncCoroutine(string address, Action<GameObject> onComplete, Transform parent)
        {
            if (ShouldUsePool<GameObject>())
            {
                GameObject prefab = null;
                yield return LoadAssetAsyncCoroutine<GameObject>(address, (result) => { prefab = result; });

                if (prefab != null)
                {
                    GameObject pooledObj = ModuleHub.Instance.GetManager<PoolManager>().GetGameObj<GameObject>(prefab, parent);
                    onComplete?.Invoke(pooledObj);
                    yield break;
                }
            }

            yield return InstantiateAsyncCoroutine(address, onComplete, parent);
        }

        private static IEnumerator InstantiateAsyncCoroutine(string address, Action<GameObject> onComplete, Transform parent)
        {
            EnsureInitialized();
            
            var handle = Addressables.InstantiateAsync(address, parent);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject result = handle.Result;
                result.name = result.name.Replace("(Clone)", "");
                instantiatedObjects.Add(result);
                instanceToAddress[result] = address;
                AddReference(address);
                onComplete?.Invoke(result);
            }
            else
            {
                onComplete?.Invoke(null);
            }
        }

        #endregion
    }
}
