using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

/// <summary>
/// 资源加载与对象池管理器
/// </summary>

namespace MieMieFrameWork.Asset
{
public sealed class ResourceManager : IResourcesInterface
{
    /// <summary>
    /// 已加载资源字典
    /// </summary>
    private readonly Dictionary<uint, BundleItem> loadedAssetDict = new();

    /// <summary>
    /// 对象池字典
    /// </summary>
    private readonly Dictionary<uint, Stack<CacheObject>> objectPoolDict = new();

    /// <summary>
    /// 已创建实例字典
    /// </summary>
    private readonly Dictionary<EntityId, CacheObject> allObjectDict = new();

    /// <summary>
    /// 等待资源包就绪任务字典
    /// </summary>
    private readonly Dictionary<uint, UniTaskCompletionSource<bool>> readySourceDict = new();

    /// <summary>
    /// CacheObject 类对象池
    /// </summary>
    private readonly ClassObjectPool<CacheObject> cacheObjectPool = new(64);

    /// <summary>
    /// 当前热更接口
    /// </summary>
    private IHotAssets hotAssets;

    /// <summary>
    /// 初始化并订阅热更内部事件
    /// </summary>
    public void Init(IHotAssets hotAssets)
    {
        if (this.hotAssets != null)
            this.hotAssets.BundleDownloaded -= OnBundleDownloaded;
        this.hotAssets = hotAssets;
        this.hotAssets.BundleDownloaded += OnBundleDownloaded;
    }

    #region 资源加载

    /// <summary>
    /// 同步加载资源
    /// </summary>
    public T LoadResource<T>(string path) where T : UnityEngine.Object
    {
        var bundleManager = AssetBundleManager.Instance;
        bool hasBundleItem = bundleManager.TryGetBundleItem(path, out var bundleItem);
        string assetPath = hasBundleItem ? bundleItem.path : path;
        uint crc = hasBundleItem ? bundleItem.crc : Crc32.GetCrc32(assetPath);
        if (loadedAssetDict.TryGetValue(crc, out var loadedItem) && loadedItem.assetObj != null)
            return loadedItem.assetObj as T;

#if UNITY_EDITOR
        if (BundleSettings.Instance.loadAssetType == E_LoadAssetType.Editor)
        {
            var editorAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
            CacheLoadedAsset(crc, bundleItem, assetPath, editorAsset);
            return editorAsset;
        }
#endif

        if (!hasBundleItem)
        {
            Debug.LogError("资源地址不存在 " + path);
            return null;
        }

        bundleItem = bundleManager.LoadAssetBundle(crc);
        if (bundleItem == null || bundleItem.assetsBundle == null)
            return null;

        var asset = bundleItem.assetsBundle.LoadAsset<T>(bundleItem.assetName);
        CacheLoadedAsset(crc, bundleItem, assetPath, asset);
        return asset;
    }

    /// <summary>
    /// 异步加载资源
    /// </summary>
    public async UniTask<T> LoadResourceAsync<T>(
        string path,
        CancellationToken cancellationToken = default)
        where T : UnityEngine.Object
    {
        var bundleManager = AssetBundleManager.Instance;
        bool hasBundleItem = bundleManager.TryGetBundleItem(path, out var bundleItem);
        string assetPath = hasBundleItem ? bundleItem.path : path;
        uint crc = hasBundleItem ? bundleItem.crc : Crc32.GetCrc32(assetPath);
        if (loadedAssetDict.TryGetValue(crc, out var loadedItem) && loadedItem.assetObj != null)
            return loadedItem.assetObj as T;

#if UNITY_EDITOR
        if (BundleSettings.Instance.loadAssetType == E_LoadAssetType.Editor)
        {
            var editorAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
            CacheLoadedAsset(crc, bundleItem, assetPath, editorAsset);
            return editorAsset;
        }
#endif

        if (!hasBundleItem)
            return null;

        await bundleManager.LoadAssetBundleAsync(bundleItem, cancellationToken);
        var asset = bundleItem.assetObj as T;
        if (asset == null)
        {
            asset = await bundleItem.assetsBundle.LoadAssetAsync<T>(bundleItem.assetName)
                .ToUniTask(cancellationToken: cancellationToken) as T;
        }

        CacheLoadedAsset(crc, bundleItem, assetPath, asset);
        return asset;
    }

    /// <summary>
    /// 预加载资源到缓存
    /// </summary>
    public void PreLoadResource<T>(string path) where T : UnityEngine.Object
    {
        LoadResource<T>(path);
    }

    /// <summary>
    /// 缓存已加载资源
    /// </summary>
    private void CacheLoadedAsset(
        uint crc,
        BundleItem bundleItem,
        string path,
        UnityEngine.Object asset)
    {
        if (asset == null)
            return;

        bundleItem ??= new BundleItem
        {
            path = path,
            crc = crc,
            dependencyList = new List<string>(),
        };
        bundleItem.assetObj = asset;
        loadedAssetDict[crc] = bundleItem;
    }

    #endregion

    #region 实例化与对象池

    /// <summary>
    /// 同步克隆预制体
    /// </summary>
    public GameObject Instantiate(
        string path,
        Transform parent = null,
        Vector3 localPosition = default,
        Vector3 localScale = default,
        Quaternion localRotation = default)
    {
        path = NormalizePrefabAddress(path);
        uint crc = ResolveCrc(path);
        var pooledObject = GetPooledObject(crc);
        if (pooledObject != null)
        {
            ApplyObjectTransform(pooledObject, parent, localPosition, localRotation, localScale);
            return pooledObject;
        }

        var prefab = LoadResource<GameObject>(path);
        if (prefab == null)
            return null;

        return CloneGameObject(path, crc, prefab, parent, localPosition, localScale, localRotation);
    }

    /// <summary>
    /// 异步克隆预制体
    /// </summary>
    public async UniTask<GameObject> InstantiateAsync(
        string path,
        Transform parent = null,
        Vector3 localPosition = default,
        Vector3 localScale = default,
        Quaternion localRotation = default,
        CancellationToken cancellationToken = default)
    {
        path = NormalizePrefabAddress(path);
        uint crc = ResolveCrc(path);
        var pooledObject = GetPooledObject(crc);
        if (pooledObject != null)
        {
            ApplyObjectTransform(pooledObject, parent, localPosition, localRotation, localScale);
            return pooledObject;
        }

        var prefab = await LoadResourceAsync<GameObject>(path, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (prefab == null)
            return null;

        return CloneGameObject(path, crc, prefab, parent, localPosition, localScale, localRotation);
    }

    /// <summary>
    /// 等待目标资源包就绪后异步克隆
    /// </summary>
    public async UniTask<GameObject> InstantiateWhenReadyAsync(
        string path,
        Transform parent = null,
        CancellationToken cancellationToken = default)
    {
        path = NormalizePrefabAddress(path);
        var bundleManager = AssetBundleManager.Instance;
        if (bundleManager.TryGetBundleItem(path, out var bundleItem))
        {
            string bundlePath = BundleSettings.Instance.ResolveBundleFilePath(
                bundleItem.bundleModuleEnum,
                bundleItem.bundleName);
            if (System.IO.File.Exists(bundlePath))
                return await InstantiateAsync(path, parent, cancellationToken: cancellationToken);
        }

        uint crc = ResolveCrc(path);
        if (!readySourceDict.TryGetValue(crc, out var readySource))
        {
            readySource = new UniTaskCompletionSource<bool>();
            readySourceDict.Add(crc, readySource);
        }

        await readySource.Task.AttachExternalCancellation(cancellationToken);
        return await InstantiateAsync(path, parent, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 预克隆对象并回收到对象池
    /// </summary>
    public void PreLoadObj(string path, int count = 1)
    {
        var objectList = new List<GameObject>(count);
        for (int objectIndex = 0; objectIndex < count; objectIndex++)
        {
            var instance = Instantiate(path);
            if (instance != null)
                objectList.Add(instance);
        }
        foreach (var instance in objectList)
            Release(instance);
    }

    /// <summary>
    /// 回收或销毁实例
    /// </summary>
    public void Release(GameObject obj, bool destroy = false)
    {
        if (obj == null)
            return;

        var entityId = obj.GetEntityId();
        if (!allObjectDict.TryGetValue(entityId, out var cacheObject))
        {
            Debug.LogError("回收失败 对象不是由 MmAsset 创建");
            return;
        }

        if (destroy)
        {
            DestroyCacheObject(cacheObject);
            return;
        }

        if (cacheObject.isPooled)
            return;

        if (!objectPoolDict.TryGetValue(cacheObject.crc, out var objectStack))
        {
            objectStack = new Stack<CacheObject>();
            objectPoolDict.Add(cacheObject.crc, objectStack);
        }

        cacheObject.isPooled = true;
        objectStack.Push(cacheObject);
        cacheObject.obj.transform.SetParent(MmAssetFrame.RecyclObjRoot, false);
        cacheObject.obj.SetActive(false);
    }

    /// <summary>
    /// 从对象池 O1 取出实例
    /// </summary>
    private GameObject GetPooledObject(uint crc)
    {
        if (!objectPoolDict.TryGetValue(crc, out var objectStack) || objectStack.Count == 0)
            return null;

        var cacheObject = objectStack.Pop();
        cacheObject.isPooled = false;
        cacheObject.obj.SetActive(true);
        return cacheObject.obj;
    }

    /// <summary>
    /// 克隆并登记实例
    /// </summary>
    private GameObject CloneGameObject(
        string path,
        uint crc,
        GameObject prefab,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Quaternion localRotation)
    {
        var instance = UnityEngine.Object.Instantiate(prefab, parent, false);
        instance.name = prefab.name;
        ApplyObjectTransform(instance, parent, localPosition, localRotation, localScale);

        var cacheObject = cacheObjectPool.Spawn();
        cacheObject.obj = instance;
        cacheObject.path = path;
        cacheObject.crc = crc;
        cacheObject.entityId = instance.GetEntityId();
        cacheObject.isPooled = false;
        allObjectDict.Add(cacheObject.entityId, cacheObject);
        return instance;
    }

    /// <summary>
    /// 销毁并回收缓存节点
    /// </summary>
    private void DestroyCacheObject(CacheObject cacheObject)
    {
        allObjectDict.Remove(cacheObject.entityId);
        if (objectPoolDict.TryGetValue(cacheObject.crc, out var objectStack) && cacheObject.isPooled)
            RemoveStackItem(objectStack, cacheObject);
        UnityEngine.Object.Destroy(cacheObject.obj);
        cacheObject.Release();
        cacheObjectPool.Recycle(cacheObject);
    }

    /// <summary>
    /// 从栈中移除指定对象
    /// </summary>
    private static void RemoveStackItem(
        Stack<CacheObject> objectStack,
        CacheObject target)
    {
        var temporaryStack = new Stack<CacheObject>();
        while (objectStack.Count > 0)
        {
            var current = objectStack.Pop();
            if (!ReferenceEquals(current, target))
                temporaryStack.Push(current);
        }
        while (temporaryStack.Count > 0)
            objectStack.Push(temporaryStack.Pop());
    }

    /// <summary>
    /// 设置实例 Transform
    /// </summary>
    private static void ApplyObjectTransform(
        GameObject obj,
        Transform parent,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale)
    {
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = localRotation;
        if (localScale != default)
            obj.transform.localScale = localScale;
    }

    #endregion

    #region 热更就绪

    /// <summary>
    /// 处理单 AB 下载完成
    /// </summary>
    private void OnBundleDownloaded(HotFileInfo hotFileInfo)
    {
        var bundleItemList = AssetBundleManager.Instance.GetBundleItemByABName(hotFileInfo.abName);
        foreach (var bundleItem in bundleItemList)
        {
            if (!readySourceDict.TryGetValue(bundleItem.crc, out var readySource))
                continue;
            readySource.TrySetResult(true);
            readySourceDict.Remove(bundleItem.crc);
        }
    }

    /// <summary>
    /// 取消全部资源就绪等待
    /// </summary>
    public void ClearAllAsyncLoadTask()
    {
        foreach (var readySource in readySourceDict.Values)
            readySource.TrySetCanceled();
        readySourceDict.Clear();
    }

    #endregion

    #region 便捷加载与清理

    /// <summary>
    /// 卸载 Texture
    /// </summary>
    public void Release(Texture texture)
    {
        if (texture != null)
            Resources.UnloadAsset(texture);
    }

    public Sprite LoadSprite(string path)
    {
        return LoadResource<Sprite>(AppendExtension(path, ".png"));
    }

    public Texture LoadTexture(string path)
    {
        return LoadResource<Texture>(AppendExtension(path, ".jpg"));
    }

    public AudioClip LoadAudio(string path)
    {
        return LoadResource<AudioClip>(path);
    }

    public TextAsset LoadTextAsset(string path)
    {
        return LoadResource<TextAsset>(path);
    }

    public Sprite LoadAtlasSprite(string atlasPath, string spriteName)
    {
        var spriteAtlas = LoadResource<SpriteAtlas>(AppendExtension(atlasPath, ".spriteatlas"));
        return spriteAtlas?.GetSprite(spriteName);
    }

    public UniTask<Texture> LoadTextureAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        return LoadResourceAsync<Texture>(
            AppendExtension(path, ".jpg"),
            cancellationToken);
    }

    public async UniTask<Sprite> LoadSpriteAsync(
        string path,
        Image image = null,
        bool setNativeSize = false,
        CancellationToken cancellationToken = default)
    {
        var sprite = await LoadResourceAsync<Sprite>(
            AppendExtension(path, ".png"),
            cancellationToken);
        if (image != null)
        {
            image.sprite = sprite;
            if (setNativeSize)
                image.SetNativeSize();
        }
        return sprite;
    }

    /// <summary>
    /// 卸载指定资源模块
    /// </summary>
    public void UnloadModule(
        BundleModuleEnum bundleModuleEnum,
        bool unloadAllLoadedObjects = true)
    {
        var destroyObjectList = new List<CacheObject>();
        foreach (var cacheObject in allObjectDict.Values)
        {
            if (AssetBundleManager.Instance.TryGetBundleItem(cacheObject.crc, out var bundleItem)
                && bundleItem.bundleModuleEnum == bundleModuleEnum)
                destroyObjectList.Add(cacheObject);
        }
        foreach (var cacheObject in destroyObjectList)
            DestroyCacheObject(cacheObject);

        var removeCrcList = new List<uint>();
        foreach (var item in loadedAssetDict)
        {
            if (item.Value.bundleModuleEnum == bundleModuleEnum)
                removeCrcList.Add(item.Key);
        }
        foreach (uint crc in removeCrcList)
        {
            loadedAssetDict.Remove(crc);
            objectPoolDict.Remove(crc);
        }

        AssetBundleManager.Instance.UnloadModule(bundleModuleEnum, unloadAllLoadedObjects);
    }

    /// <summary>
    /// 清理资源与对象池
    /// </summary>
    public void ClearResourcesAssets(
        bool absoluteCleaning,
        bool collectGarbage = false)
    {
        if (absoluteCleaning)
        {
            var objectList = new List<CacheObject>(allObjectDict.Values);
            foreach (var cacheObject in objectList)
                DestroyCacheObject(cacheObject);

            var moduleHashList = new HashSet<BundleModuleEnum>();
            foreach (var bundleItem in loadedAssetDict.Values)
                moduleHashList.Add(bundleItem.bundleModuleEnum);
            foreach (var eModule in moduleHashList)
                AssetBundleManager.Instance.UnloadModule(eModule, true);
            loadedAssetDict.Clear();
            objectPoolDict.Clear();
            ClearAllAsyncLoadTask();
        }
        else
        {
            var pooledObjectList = new List<CacheObject>();
            foreach (var objectStack in objectPoolDict.Values)
                pooledObjectList.AddRange(objectStack);
            foreach (var cacheObject in pooledObjectList)
                DestroyCacheObject(cacheObject);
            objectPoolDict.Clear();
        }

        if (collectGarbage)
        {
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }
    }

    /// <summary>
    /// 补全资源扩展名
    /// </summary>
    private static string AppendExtension(string address, string extension)
    {
        if (AssetBundleManager.Instance.TryGetBundleItem(address, out _))
            return address;
        return address.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
            ? address
            : address + extension;
    }

    /// <summary>
    /// 补全预制体扩展名
    /// </summary>
    private static string NormalizePrefabAddress(string address)
    {
        return AppendExtension(address, ".prefab");
    }

    /// <summary>
    /// 解析资源 CRC
    /// </summary>
    private static uint ResolveCrc(string address)
    {
        return AssetBundleManager.Instance.TryGetBundleItem(address, out var bundleItem)
            ? bundleItem.crc
            : Crc32.GetCrc32(address);
    }

    #endregion
}
}
