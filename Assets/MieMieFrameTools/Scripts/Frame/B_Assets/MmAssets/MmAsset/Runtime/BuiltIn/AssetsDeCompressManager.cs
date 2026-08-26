using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 随包资源提取管理器
/// </summary>

namespace MieMieFrameWork.Asset
{
public class AssetsDeCompressManager : IBuiltInAssets
{
    /// <summary>
    /// 需要提取的文件列表
    /// </summary>
    private readonly List<BuiltInBundleInfo> extractFileList = new();

    /// <summary>
    /// 已完成大小
    /// </summary>
    private float completedSizeMB;

    /// <summary>
    /// 总大小
    /// </summary>
    private float totalSizeMB;

    public float Progress => totalSizeMB <= 0f ? 1f : completedSizeMB / totalSizeMB;

    /// <summary>
    /// 提取随包资源到运行时目录
    /// </summary>
    public async UniTask ExtractAsync(
        BundleModuleEnum bundleModuleEnum,
        IProgress<AssetBootProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        completedSizeMB = 0f;
        totalSizeMB = 0f;
        extractFileList.Clear();

#if UNITY_ANDROID || UNITY_IOS
        CollectExtractFiles(bundleModuleEnum);
        var settings = BundleSettings.Instance;
        var builtInPath = settings.GetBuiltInStreamAssetsPath(bundleModuleEnum);
        var extractPath = settings.GetDecompressAssetsPath(bundleModuleEnum);
        Directory.CreateDirectory(extractPath);

        foreach (var info in extractFileList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sourcePath = builtInPath + info.fileName;
#if UNITY_IOS
            sourcePath = "file://" + sourcePath;
#endif
            var targetPath = extractPath + info.fileName;
            using var request = UnityWebRequest.Get(sourcePath);
            request.timeout = 30;
            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

            if (request.result != UnityWebRequest.Result.Success)
                throw new IOException("提取随包资源失败 " + sourcePath + " " + request.error);

            var bytes = request.downloadHandler.data;
            FileHelper.WriteFile(targetPath, bytes);
            completedSizeMB += bytes.Length / 1024f / 1024f;
            progress?.Report(CreateProgress(bundleModuleEnum, info.fileName));
        }
#endif

        completedSizeMB = totalSizeMB;
        progress?.Report(CreateProgress(bundleModuleEnum, "随包资源准备完成"));
    }

    /// <summary>
    /// 收集需要提取的随包文件
    /// </summary>
    private void CollectExtractFiles(BundleModuleEnum bundleModuleEnum)
    {
        var settings = BundleSettings.Instance;
        var resourceName = settings.GetBuiltInManifestResourceName(bundleModuleEnum);
        var textAsset = Resources.Load<TextAsset>(resourceName);
        // 纯热更模块没有随包清单 直接跳过提取
        if (textAsset == null)
            return;

        var builtInBundleConfig = JsonConvert.DeserializeObject<BuiltInBundleConfig>(textAsset.text);
        var extractPath = settings.GetDecompressAssetsPath(bundleModuleEnum);
        foreach (var info in builtInBundleConfig.builtInBundleInfoList)
        {
            var localFilePath = extractPath + info.fileName;
            if (File.Exists(localFilePath) && MD5.GetMd5FromFile(localFilePath) == info.md5)
                continue;

            extractFileList.Add(info);
            totalSizeMB += info.size / 1024f;
        }
    }

    /// <summary>
    /// 创建随包提取进度
    /// </summary>
    private AssetBootProgress CreateProgress(BundleModuleEnum bundleModuleEnum, string message)
    {
        return new AssetBootProgress(
            bundleModuleEnum,
            EAssetBootStage.Decompress,
            Progress,
            completedSizeMB,
            totalSizeMB,
            message);
    }
}
}
