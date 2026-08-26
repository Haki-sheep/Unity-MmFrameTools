using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 热更资源接口
/// </summary>

namespace MieMieFrameWork.Asset
{
public interface IHotAssets
{
    event Action<HotFileInfo> BundleDownloaded;

    /// <summary>
    /// 更新指定资源模块
    /// </summary>
    UniTask HotAssetsAsync(
        BundleModuleEnum bundleModuleEnum,
        IProgress<AssetBootProgress> progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查指定资源模块版本
    /// </summary>
    UniTask<HotUpdateCheckResult> CheckAssetsVersionAsync(
        BundleModuleEnum bundleModuleEnum,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取热更模块
    /// </summary>
    HotAssetsModule GetHotAssetsModule(BundleModuleEnum bundleModuleEnum);

}
}
