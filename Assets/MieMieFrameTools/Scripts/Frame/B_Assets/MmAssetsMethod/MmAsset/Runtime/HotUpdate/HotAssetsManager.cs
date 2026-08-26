using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 热更资源管理器
/// </summary>

namespace MieMieFrameWork.Asset
{
public sealed class HotAssetsManager : IHotAssets
{
    /// <summary>
    /// 所有热更模块字典
    /// </summary>
    private readonly Dictionary<BundleModuleEnum, HotAssetsModule> moduleDict = new();

    /// <summary>
    /// 模块更新串行锁
    /// </summary>
    private readonly SemaphoreSlim updateSemaphore = new(1, 1);

    public event Action<HotFileInfo> BundleDownloaded;

    /// <summary>
    /// 更新指定资源模块
    /// </summary>
    public async UniTask HotAssetsAsync(
        BundleModuleEnum bundleModuleEnum,
        IProgress<AssetBootProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        if (BundleSettings.Instance.buildBundleType == E_RuntimeBundleMode.NotHot)
            return;

        if (updateSemaphore.CurrentCount == 0)
        {
            progress?.Report(new AssetBootProgress(
                bundleModuleEnum,
                EAssetBootStage.Queued,
                0f,
                0f,
                0f,
                "等待热更队列"));
        }

        await updateSemaphore.WaitAsync(cancellationToken);
        try
        {
            var module = GetOrCreateModule(bundleModuleEnum);
            await module.UpdateAsync(progress, cancellationToken);
        }
        finally
        {
            updateSemaphore.Release();
        }
    }

    /// <summary>
    /// 检查指定资源模块版本
    /// </summary>
    public UniTask<HotUpdateCheckResult> CheckAssetsVersionAsync(
        BundleModuleEnum bundleModuleEnum,
        CancellationToken cancellationToken = default)
    {
        return GetOrCreateModule(bundleModuleEnum).CheckAssetsVersionAsync(cancellationToken);
    }

    /// <summary>
    /// 获取热更模块
    /// </summary>
    public HotAssetsModule GetHotAssetsModule(BundleModuleEnum bundleModuleEnum)
    {
        moduleDict.TryGetValue(bundleModuleEnum, out var module);
        return module;
    }

    /// <summary>
    /// 获取或创建热更模块
    /// </summary>
    private HotAssetsModule GetOrCreateModule(BundleModuleEnum bundleModuleEnum)
    {
        if (moduleDict.TryGetValue(bundleModuleEnum, out var module))
            return module;

        module = new HotAssetsModule(bundleModuleEnum, OnBundleDownloaded);
        moduleDict.Add(bundleModuleEnum, module);
        return module;
    }

    /// <summary>
    /// 转发单文件下载完成事件
    /// </summary>
    private void OnBundleDownloaded(HotFileInfo hotFileInfo)
    {
        BundleDownloaded?.Invoke(hotFileInfo);
    }
}
}
