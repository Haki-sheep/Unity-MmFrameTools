using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 随包资源提取接口
/// </summary>

namespace MieMieFrameWork.Asset
{
public interface IBuiltInAssets
{
    float Progress { get; }

    /// <summary>
    /// 提取随包资源到运行时目录
    /// </summary>
    UniTask ExtractAsync(
        BundleModuleEnum bundleModuleEnum,
        IProgress<AssetBootProgress> progress = null,
        CancellationToken cancellationToken = default);
}
}
