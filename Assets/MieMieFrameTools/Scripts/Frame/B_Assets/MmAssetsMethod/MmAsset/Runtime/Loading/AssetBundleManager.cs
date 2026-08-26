using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using MieMieFrameWork;
using UnityEngine;

namespace MieMieFrameWork.Asset
{
    /// <summary>
    /// 资源地址表条目
    /// </summary>
    public sealed class BundleItem
    {
        /// <summary>资源路径</summary>
        public string path;
        /// <summary>资源别名</summary>
        public string alias;
        /// <summary>资源路径 CRC</summary>
        public uint crc;
        /// <summary>资源包名称</summary>
        public string bundleName;
        /// <summary>包内资源名称</summary>
        public string assetName;
        /// <summary>所属资源模块</summary>
        public BundleModuleEnum bundleModuleEnum;
        /// <summary>依赖资源包列表</summary>
        public List<string> dependencyList;
        /// <summary>资源包镜像</summary>
        public AssetBundle assetsBundle;
        /// <summary>已加载资源对象</summary>
        public UnityEngine.Object assetObj;
    }

    /// <summary>
    /// 资源包缓存
    /// </summary>
    public sealed class AssetBundleCache
    {
        /// <summary>所属资源模块</summary>
        public BundleModuleEnum bundleModuleEnum;
        /// <summary>资源包名称</summary>
        public string bundleName;
        /// <summary>资源包镜像</summary>
        public AssetBundle bundle;
        /// <summary>引用计数</summary>
        public int referenceCount;

        /// <summary>
        /// 重置资源包缓存
        /// </summary>
        public void Release()
        {
            bundleModuleEnum = BundleModuleEnum.None;
            bundleName = null;
            bundle = null;
            referenceCount = 0;
        }
    }

    /// <summary>
    /// AssetBundle 地址索引与生命周期管理器
    /// </summary>
    public sealed class AssetBundleManager : Singleton<AssetBundleManager>
    {
        /// <summary>
        /// CRC 到资源条目字典
        /// </summary>
        private readonly Dictionary<uint, BundleItem> bundleItemDict = new();

        /// <summary>
        /// 别名到资源条目字典
        /// </summary>
        private readonly Dictionary<string, BundleItem> aliasItemDict = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// AB 名称到资源条目列表字典
        /// </summary>
        private readonly Dictionary<string, List<BundleItem>> bundleItemListDict = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 已加载 AB 缓存字典
        /// </summary>
        private readonly Dictionary<string, AssetBundleCache> loadedBundleDict = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 进行中 AB 加载任务字典
        /// </summary>
        private readonly Dictionary<string, UniTaskCompletionSource<AssetBundle>> loadingBundleSourceDict = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 资源包缓存对象池
        /// </summary>
        private readonly ClassObjectPool<AssetBundleCache> assetBundleCachePool = new(32);

        #region 地址表

        /// <summary>
        /// 同步加载模块地址表
        /// </summary>
        public bool LoadAssetBundleConfig(BundleModuleEnum bundleModuleEnum)
        {
#if UNITY_EDITOR
            if (TryLoadEditorGeneratedConfig(bundleModuleEnum))
                return true;
#endif
            string configPath = ResolveConfigPath(bundleModuleEnum);
            if (string.IsNullOrEmpty(configPath))
                return false;

            string loadPath = PrepareDecryptedPath(bundleModuleEnum, configPath);
            var configBundle = AssetBundle.LoadFromFile(loadPath);
            if (configBundle == null)
                return false;

            var textAsset = configBundle.LoadAsset<TextAsset>(
                BundleSettings.Instance.GetBundleConfigAssetName(bundleModuleEnum));
            RegisterBundleConfig(bundleModuleEnum, textAsset.text);
            configBundle.Unload(false);
            return true;
        }

        /// <summary>
        /// 异步加载模块地址表
        /// </summary>
        public async UniTask<bool> LoadAssetBundleConfigAsync(
            BundleModuleEnum bundleModuleEnum,
            CancellationToken cancellationToken = default)
        {
#if UNITY_EDITOR
            if (TryLoadEditorGeneratedConfig(bundleModuleEnum))
                return true;
#endif
            string configPath = ResolveConfigPath(bundleModuleEnum);
            if (string.IsNullOrEmpty(configPath))
                return false;

            string loadPath = await PrepareDecryptedPathAsync(
                bundleModuleEnum,
                configPath,
                cancellationToken);
            var configBundle = await AssetBundle.LoadFromFileAsync(loadPath)
                .ToUniTask(cancellationToken: cancellationToken);
            if (configBundle == null)
                return false;

            var textAsset = await configBundle.LoadAssetAsync<TextAsset>(
                    BundleSettings.Instance.GetBundleConfigAssetName(bundleModuleEnum))
                .ToUniTask(cancellationToken: cancellationToken) as TextAsset;
            RegisterBundleConfig(bundleModuleEnum, textAsset.text);
            configBundle.Unload(false);
            return true;
        }

        /// <summary>
        /// 根据地址 O1 查询资源条目
        /// </summary>
        public bool TryGetBundleItem(string address, out BundleItem bundleItem)
        {
            if (aliasItemDict.TryGetValue(address, out bundleItem))
                return true;

            return bundleItemDict.TryGetValue(Crc32.GetCrc32(address), out bundleItem);
        }

        /// <summary>
        /// 根据 CRC O1 查询资源条目
        /// </summary>
        public bool TryGetBundleItem(uint crc, out BundleItem bundleItem)
        {
            return bundleItemDict.TryGetValue(crc, out bundleItem);
        }

        /// <summary>
        /// 根据 AB 名称 O1 查询包内资源
        /// </summary>
        public IReadOnlyList<BundleItem> GetBundleItemByABName(string bundleName)
        {
            return bundleItemListDict.TryGetValue(bundleName, out var bundleItemList)
                ? bundleItemList
                : Array.Empty<BundleItem>();
        }

        #endregion

        #region AB 加载

        /// <summary>
        /// 同步加载资源条目及依赖包
        /// </summary>
        public BundleItem LoadAssetBundle(uint crc)
        {
            if (!bundleItemDict.TryGetValue(crc, out var bundleItem))
                return null;

            if (bundleItem.assetsBundle == null)
                bundleItem.assetsBundle = LoadBundle(bundleItem.bundleName, bundleItem.bundleModuleEnum);

            foreach (string dependency in bundleItem.dependencyList)
                LoadBundle(dependency, bundleItem.bundleModuleEnum);
            return bundleItem;
        }

        /// <summary>
        /// 异步加载资源条目及依赖包
        /// </summary>
        public async UniTask<BundleItem> LoadAssetBundleAsync(
            BundleItem bundleItem,
            CancellationToken cancellationToken = default)
        {
            if (bundleItem.assetsBundle == null)
            {
                bundleItem.assetsBundle = await LoadBundleAsync(
                    bundleItem.bundleName,
                    bundleItem.bundleModuleEnum,
                    cancellationToken);
            }

            foreach (string dependency in bundleItem.dependencyList)
            {
                await LoadBundleAsync(
                    dependency,
                    bundleItem.bundleModuleEnum,
                    cancellationToken);
            }
            return bundleItem;
        }

        /// <summary>
        /// 同步加载指定 AB
        /// </summary>
        public AssetBundle LoadBundle(string abName, BundleModuleEnum bundleModuleEnum)
        {
            string cacheKey = CreateCacheKey(bundleModuleEnum, abName);
            if (loadedBundleDict.TryGetValue(cacheKey, out var cache))
            {
                cache.referenceCount++;
                return cache.bundle;
            }

            string sourcePath = BundleSettings.Instance.ResolveBundleFilePath(bundleModuleEnum, abName);
            string loadPath = PrepareDecryptedPath(bundleModuleEnum, sourcePath);
            var bundle = AssetBundle.LoadFromFile(loadPath);
            if (bundle == null)
                return null;

            AddBundleCache(cacheKey, bundleModuleEnum, abName, bundle);
            return bundle;
        }

        /// <summary>
        /// 异步加载指定 AB
        /// </summary>
        public async UniTask<AssetBundle> LoadBundleAsync(
            string abName,
            BundleModuleEnum bundleModuleEnum,
            CancellationToken cancellationToken = default)
        {
            string cacheKey = CreateCacheKey(bundleModuleEnum, abName);
            if (loadedBundleDict.TryGetValue(cacheKey, out var cache))
            {
                cache.referenceCount++;
                return cache.bundle;
            }

            if (loadingBundleSourceDict.TryGetValue(cacheKey, out var runningSource))
            {
                var runningBundle = await runningSource.Task.AttachExternalCancellation(cancellationToken);
                if (loadedBundleDict.TryGetValue(cacheKey, out var runningCache))
                    runningCache.referenceCount++;
                return runningBundle;
            }

            var loadingSource = new UniTaskCompletionSource<AssetBundle>();
            loadingBundleSourceDict.Add(cacheKey, loadingSource);
            try
            {
                string sourcePath = BundleSettings.Instance.ResolveBundleFilePath(bundleModuleEnum, abName);
                string loadPath = await PrepareDecryptedPathAsync(
                    bundleModuleEnum,
                    sourcePath,
                    cancellationToken);
                var bundle = await AssetBundle.LoadFromFileAsync(loadPath)
                    .ToUniTask(cancellationToken: cancellationToken);
                if (bundle == null)
                    throw new IOException("异步加载资源包失败 " + sourcePath);

                AddBundleCache(cacheKey, bundleModuleEnum, abName, bundle);
                loadingSource.TrySetResult(bundle);
                return bundle;
            }
            catch (Exception exception)
            {
                loadingSource.TrySetException(exception);
                throw;
            }
            finally
            {
                loadingBundleSourceDict.Remove(cacheKey);
            }
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 释放资源条目对应 AB 与依赖
        /// </summary>
        public void ReleaseAssets(BundleItem bundleItem, bool unloadAllLoadedObjects = false)
        {
            ReleaseBundle(bundleItem.bundleName, bundleItem.bundleModuleEnum, unloadAllLoadedObjects);
            foreach (string dependency in bundleItem.dependencyList)
                ReleaseBundle(dependency, bundleItem.bundleModuleEnum, unloadAllLoadedObjects);
            bundleItem.assetsBundle = null;
            bundleItem.assetObj = null;
        }

        /// <summary>
        /// 卸载指定模块全部 AB 与地址索引
        /// </summary>
        public void UnloadModule(
            BundleModuleEnum bundleModuleEnum,
            bool unloadAllLoadedObjects = true)
        {
            var removeCacheKeyList = new List<string>();
            foreach (var item in loadedBundleDict)
            {
                if (item.Value.bundleModuleEnum != bundleModuleEnum)
                    continue;
                item.Value.bundle.Unload(unloadAllLoadedObjects);
                item.Value.Release();
                assetBundleCachePool.Recycle(item.Value);
                removeCacheKeyList.Add(item.Key);
            }

            foreach (string cacheKey in removeCacheKeyList)
                loadedBundleDict.Remove(cacheKey);
            RemoveModuleConfig(bundleModuleEnum);
        }

        #endregion

        #region 内部实现

        /// <summary>
        /// 注册模块地址表
        /// </summary>
        private void RegisterBundleConfig(
            BundleModuleEnum bundleModuleEnum,
            string json)
        {
            RemoveModuleConfig(bundleModuleEnum);
            var bundleConfig = JsonUtility.FromJson<BundleConfig>(json);
            foreach (var info in bundleConfig.bundleInfoList)
            {
                var bundleItem = new BundleItem
                {
                    path = info.path,
                    alias = info.alias,
                    crc = info.crc,
                    bundleName = info.bundleName,
                    assetName = info.assetName,
                    bundleModuleEnum = bundleModuleEnum,
                    dependencyList = info.dependencielist ?? new List<string>(),
                };
                bundleItemDict[bundleItem.crc] = bundleItem;
                if (!string.IsNullOrWhiteSpace(bundleItem.alias))
                    aliasItemDict[bundleItem.alias] = bundleItem;

                if (!bundleItemListDict.TryGetValue(bundleItem.bundleName, out var bundleItemList))
                {
                    bundleItemList = new List<BundleItem>();
                    bundleItemListDict.Add(bundleItem.bundleName, bundleItemList);
                }
                bundleItemList.Add(bundleItem);
            }
        }

        /// <summary>
        /// 移除模块地址表
        /// </summary>
        private void RemoveModuleConfig(BundleModuleEnum bundleModuleEnum)
        {
            var removeCrcList = new List<uint>();
            var removeAliasList = new List<string>();
            foreach (var item in bundleItemDict)
            {
                if (item.Value.bundleModuleEnum != bundleModuleEnum)
                    continue;
                removeCrcList.Add(item.Key);
                if (!string.IsNullOrWhiteSpace(item.Value.alias))
                    removeAliasList.Add(item.Value.alias);
            }

            foreach (uint crc in removeCrcList)
                bundleItemDict.Remove(crc);
            foreach (string alias in removeAliasList)
                aliasItemDict.Remove(alias);

            var removeBundleNameList = new List<string>();
            foreach (var item in bundleItemListDict)
            {
                if (item.Value.Count > 0 && item.Value[0].bundleModuleEnum == bundleModuleEnum)
                    removeBundleNameList.Add(item.Key);
            }
            foreach (string bundleName in removeBundleNameList)
                bundleItemListDict.Remove(bundleName);
        }

        /// <summary>
        /// 解析模块地址表路径
        /// </summary>
        private static string ResolveConfigPath(BundleModuleEnum bundleModuleEnum)
        {
            var settings = BundleSettings.Instance;
            string fileName = settings.GetBundleConfigFileName(bundleModuleEnum);
            string path = settings.ResolveBundleFilePath(bundleModuleEnum, fileName);
            return File.Exists(path) ? path : null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor 模式下优先从 Generated AbConfig 注册短名地址表
        /// </summary>
        private bool TryLoadEditorGeneratedConfig(BundleModuleEnum bundleModuleEnum)
        {
            if (BundleSettings.Instance.loadAssetType != E_LoadAssetType.Editor)
                return false;

            string generatedPath = BundleSettings.Instance.GetGeneratedAbConfigDiskPath(bundleModuleEnum);
            if (!File.Exists(generatedPath))
                return false;

            string json = File.ReadAllText(generatedPath);
            RegisterBundleConfig(bundleModuleEnum, json);
            return true;
        }
#endif

        /// <summary>
        /// 获取同步可加载路径
        /// </summary>
        private static string PrepareDecryptedPath(
            BundleModuleEnum bundleModuleEnum,
            string sourcePath)
        {
            if (!BundleSettings.Instance.bundleEncryptToggle.isEncrypt
                || !AES.IsEncryptedFile(sourcePath))
                return sourcePath;

            string decryptedPath = BundleSettings.Instance.GetDecryptedBundlePath(
                bundleModuleEnum,
                Path.GetFileName(sourcePath));
            AES.AESFileDecryptToFile(
                sourcePath,
                decryptedPath,
                BundleSettings.Instance.bundleEncryptToggle.encryptKey);
            return decryptedPath;
        }

        /// <summary>
        /// 获取异步可加载路径
        /// </summary>
        private static async UniTask<string> PrepareDecryptedPathAsync(
            BundleModuleEnum bundleModuleEnum,
            string sourcePath,
            CancellationToken cancellationToken)
        {
            if (!BundleSettings.Instance.bundleEncryptToggle.isEncrypt
                || !AES.IsEncryptedFile(sourcePath))
                return sourcePath;

            string decryptedPath = BundleSettings.Instance.GetDecryptedBundlePath(
                bundleModuleEnum,
                Path.GetFileName(sourcePath));
            await UniTask.RunOnThreadPool(
                () => AES.AESFileDecryptToFile(
                    sourcePath,
                    decryptedPath,
                    BundleSettings.Instance.bundleEncryptToggle.encryptKey),
                cancellationToken: cancellationToken);
            return decryptedPath;
        }

        /// <summary>
        /// 加入资源包缓存
        /// </summary>
        private void AddBundleCache(
            string cacheKey,
            BundleModuleEnum bundleModuleEnum,
            string abName,
            AssetBundle bundle)
        {
            var cache = assetBundleCachePool.Spawn();
            cache.bundleModuleEnum = bundleModuleEnum;
            cache.bundleName = abName;
            cache.bundle = bundle;
            cache.referenceCount = 1;
            loadedBundleDict.Add(cacheKey, cache);
        }

        /// <summary>
        /// 释放指定资源包
        /// </summary>
        private void ReleaseBundle(
            string abName,
            BundleModuleEnum bundleModuleEnum,
            bool unloadAllLoadedObjects)
        {
            string cacheKey = CreateCacheKey(bundleModuleEnum, abName);
            if (!loadedBundleDict.TryGetValue(cacheKey, out var cache))
                return;

            cache.referenceCount--;
            if (cache.referenceCount > 0)
                return;

            cache.bundle.Unload(unloadAllLoadedObjects);
            loadedBundleDict.Remove(cacheKey);
            cache.Release();
            assetBundleCachePool.Recycle(cache);
        }

        /// <summary>
        /// 创建跨模块缓存键
        /// </summary>
        private static string CreateCacheKey(
            BundleModuleEnum bundleModuleEnum,
            string abName)
        {
            return (int)bundleModuleEnum + "|" + abName;
        }

        #endregion
    }
}
