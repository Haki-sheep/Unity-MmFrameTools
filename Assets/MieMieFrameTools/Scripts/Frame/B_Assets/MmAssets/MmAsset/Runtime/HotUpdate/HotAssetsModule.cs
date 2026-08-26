using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 单个资源模块热更流程
/// </summary>

namespace MieMieFrameWork.Asset
{
public sealed class HotAssetsModule
{
    /// <summary>
    /// 当前资源模块
    /// </summary>
    private readonly BundleModuleEnum currentModule;

    /// <summary>
    /// 单文件下载完成通知
    /// </summary>
    private readonly Action<HotFileInfo> bundleDownloaded;

    /// <summary>
    /// 需要下载的文件列表
    /// </summary>
    private readonly List<HotFileInfo> needDownloadFileList = new();

    /// <summary>
    /// 最新版本全部文件名集合
    /// </summary>
    private readonly HashSet<string> allHotAssetNameHashList = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 服务端热更清单
    /// </summary>
    private HotAssetsManifest serverAssetsManifest;

    /// <summary>
    /// 服务端清单缓存路径
    /// </summary>
    private string serverAssetsManifestPath;

    /// <summary>
    /// 本地生效清单路径
    /// </summary>
    private string localAssetsManifestPath;

    /// <summary>
    /// 本次需要下载总字节数
    /// </summary>
    private long totalDownloadByteCount;

    /// <summary>
    /// 本次已完成下载字节数
    /// </summary>
    private long completedDownloadByteCount;

    public int HotAssetsCount => allHotAssetNameHashList.Count;
    public float DownloadSizeMB => totalDownloadByteCount / 1024f / 1024f;

    #region 主流程

    /// <summary>
    /// 创建热更模块
    /// </summary>
    public HotAssetsModule(
        BundleModuleEnum currentModule,
        Action<HotFileInfo> bundleDownloaded)
    {
        this.currentModule = currentModule;
        this.bundleDownloaded = bundleDownloaded;
    }

    /// <summary>
    /// 检查资源版本
    /// </summary>
    public async UniTask<HotUpdateCheckResult> CheckAssetsVersionAsync(
        CancellationToken cancellationToken = default)
    {
        PrepareManifestPath();
        needDownloadFileList.Clear();
        allHotAssetNameHashList.Clear();
        totalDownloadByteCount = 0L;
        completedDownloadByteCount = 0L;

        await DownloadManifestAsync(cancellationToken);
        ValidateClientVersion();

        if (serverAssetsManifest.patchList == null || serverAssetsManifest.patchList.Count == 0)
            return new HotUpdateCheckResult(false, 0f);

        var latestPatch = serverAssetsManifest.patchList[serverAssetsManifest.patchList.Count - 1];
        CollectDownloadFiles(latestPatch);
        return new HotUpdateCheckResult(needDownloadFileList.Count > 0, DownloadSizeMB);
    }

    /// <summary>
    /// 执行资源热更
    /// </summary>
    public async UniTask UpdateAsync(
        IProgress<AssetBootProgress> progress,
        CancellationToken cancellationToken)
    {
        var checkResult = await CheckAssetsVersionAsync(cancellationToken);
        progress?.Report(new AssetBootProgress(
            currentModule,
            EAssetBootStage.CheckVersion,
            checkResult.NeedUpdate ? 0f : 1f,
            0f,
            checkResult.DownloadSizeMB,
            checkResult.NeedUpdate ? "发现资源更新" : "资源已是最新版本"));

        if (!checkResult.NeedUpdate)
            return;

        var settings = BundleSettings.Instance;
        var hotSavePath = settings.GetHotAssetsSavePath(currentModule);
        Directory.CreateDirectory(hotSavePath);
        var configFileList = needDownloadFileList.FindAll(
            info => info.abName.Contains("abconfig", StringComparison.OrdinalIgnoreCase));
        var contentFileList = needDownloadFileList.FindAll(
            info => !info.abName.Contains("abconfig", StringComparison.OrdinalIgnoreCase));
        if (configFileList.Count > 0)
        {
            var configDownloader = CreateDownloader(configFileList, hotSavePath, progress, 1);
            await configDownloader.DownloadAsync(cancellationToken);
        }
        if (contentFileList.Count > 0)
        {
            var contentDownloader = CreateDownloader(
                contentFileList,
                hotSavePath,
                progress,
                settings.maxHotThreadCount);
            await contentDownloader.DownloadAsync(cancellationToken);
        }
        SaveLocalManifest();
        CleanObsoleteFiles();
        progress?.Report(CreateDownloadProgress("热更资源下载完成"));
    }

    /// <summary>
    /// 判断热更目录是否包含指定资源包
    /// </summary>
    public bool HotAssetsIsExists(string abName)
    {
        return allHotAssetNameHashList.Contains(abName)
               && File.Exists(BundleSettings.Instance.GetHotAssetsSavePath(currentModule) + abName);
    }

    #endregion

    #region 内部实现

    /// <summary>
    /// 下载服务端热更清单
    /// </summary>
    private async UniTask DownloadManifestAsync(CancellationToken cancellationToken)
    {
        var url = BundleSettings.Instance.GetHotManifestUrl(currentModule);
        using var request = UnityWebRequest.Get(url);
        request.timeout = 30;
        await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

        if (request.result != UnityWebRequest.Result.Success)
            throw new IOException("下载热更清单失败 " + request.error);

        serverAssetsManifest = JsonConvert.DeserializeObject<HotAssetsManifest>(request.downloadHandler.text);
        Directory.CreateDirectory(Path.GetDirectoryName(serverAssetsManifestPath));
        File.WriteAllText(serverAssetsManifestPath, request.downloadHandler.text);
    }

    /// <summary>
    /// 校验客户端最低版本
    /// </summary>
    private void ValidateClientVersion()
    {
        if (string.IsNullOrWhiteSpace(serverAssetsManifest.minClientVersion))
            return;

        if (CompareVersion(Application.version, serverAssetsManifest.minClientVersion) < 0)
            throw new AssetUpdateRequiredException(serverAssetsManifest.minClientVersion);
    }

    /// <summary>
    /// 比较版本号
    /// </summary>
    private static int CompareVersion(string currentVersion, string minimumVersion)
    {
        var currentPartList = currentVersion.Split('.');
        var minimumPartList = minimumVersion.Split('.');
        int partCount = Math.Max(currentPartList.Length, minimumPartList.Length);
        for (int partIndex = 0; partIndex < partCount; partIndex++)
        {
            int currentPart = partIndex < currentPartList.Length && int.TryParse(currentPartList[partIndex], out int currentValue)
                ? currentValue
                : 0;
            int minimumPart = partIndex < minimumPartList.Length && int.TryParse(minimumPartList[partIndex], out int minimumValue)
                ? minimumValue
                : 0;
            int compareResult = currentPart.CompareTo(minimumPart);
            if (compareResult != 0)
                return compareResult;
        }

        return 0;
    }

    /// <summary>
    /// 收集需要下载的文件
    /// </summary>
    private void CollectDownloadFiles(HotAssetsPatch latestPatch)
    {
        var hotSavePath = BundleSettings.Instance.GetHotAssetsSavePath(currentModule);
        foreach (var info in latestPatch.fileList)
        {
            allHotAssetNameHashList.Add(info.abName);
            long fileSize = GetFileSize(info);
            var localPath = hotSavePath + info.abName;
            if (File.Exists(localPath)
                && string.Equals(MD5.GetMd5FromFile(localPath), info.md5, StringComparison.OrdinalIgnoreCase))
                continue;

            needDownloadFileList.Add(info);
            totalDownloadByteCount += fileSize;
        }
    }

    /// <summary>
    /// 创建模块下载器
    /// </summary>
    private AssetsDownLoader CreateDownloader(
        List<HotFileInfo> downloadFileList,
        string hotSavePath,
        IProgress<AssetBootProgress> progress,
        int threadCount)
    {
        var settings = BundleSettings.Instance;
        return new AssetsDownLoader(
            downloadFileList,
            serverAssetsManifest.downloadUrl,
            hotSavePath,
            threadCount,
            settings.maxDownloadRetryCount,
            info => OnFileDownloaded(info, progress));
    }

    /// <summary>
    /// 处理单文件下载完成
    /// </summary>
    private void OnFileDownloaded(
        HotFileInfo hotFileInfo,
        IProgress<AssetBootProgress> progress)
    {
        completedDownloadByteCount += GetFileSize(hotFileInfo);
        if (hotFileInfo.abName.Contains("abconfig", StringComparison.OrdinalIgnoreCase))
            AssetBundleManager.Instance.LoadAssetBundleConfig(currentModule);

        bundleDownloaded?.Invoke(hotFileInfo);
        progress?.Report(CreateDownloadProgress(hotFileInfo.abName));
    }

    /// <summary>
    /// 保存本地已生效清单
    /// </summary>
    private void SaveLocalManifest()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(localAssetsManifestPath));
        File.Copy(serverAssetsManifestPath, localAssetsManifestPath, true);
    }

    /// <summary>
    /// 清理当前版本不再使用的热更文件
    /// </summary>
    private void CleanObsoleteFiles()
    {
        var hotSavePath = BundleSettings.Instance.GetHotAssetsSavePath(currentModule);
        foreach (var filePath in Directory.GetFiles(hotSavePath))
        {
            var fileName = Path.GetFileName(filePath);
            if (fileName.EndsWith(".download", StringComparison.OrdinalIgnoreCase))
            {
                string targetName = fileName.Substring(0, fileName.Length - ".download".Length);
                if (!allHotAssetNameHashList.Contains(targetName))
                    File.Delete(filePath);
                continue;
            }
            if (!allHotAssetNameHashList.Contains(fileName))
                File.Delete(filePath);
        }
    }

    /// <summary>
    /// 初始化清单缓存路径
    /// </summary>
    private void PrepareManifestPath()
    {
        serverAssetsManifestPath = BundleSettings.Instance.GetHotManifestServerPath(currentModule);
        localAssetsManifestPath = BundleSettings.Instance.GetHotManifestLocalPath(currentModule);
    }

    /// <summary>
    /// 获取清单文件字节数
    /// </summary>
    private static long GetFileSize(HotFileInfo hotFileInfo)
    {
        return hotFileInfo.sizeBytes > 0L
            ? hotFileInfo.sizeBytes
            : (long)(hotFileInfo.size * 1024f);
    }

    /// <summary>
    /// 创建下载进度
    /// </summary>
    private AssetBootProgress CreateDownloadProgress(string message)
    {
        float progress = totalDownloadByteCount <= 0L
            ? 1f
            : (float)completedDownloadByteCount / totalDownloadByteCount;
        return new AssetBootProgress(
            currentModule,
            EAssetBootStage.Download,
            progress,
            completedDownloadByteCount / 1024f / 1024f,
            totalDownloadByteCount / 1024f / 1024f,
            message);
    }

    #endregion
}
}
