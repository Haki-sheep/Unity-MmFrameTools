using System;

/// <summary>
/// 模块资源交付方式
/// </summary>

namespace MieMieFrameWork.Asset
{
public enum E_BundleDeliveryMode
{
    BuiltIn,
    HotUpdate,
    Hybrid,
}

/// <summary>
/// 资源启动阶段
/// </summary>
public enum EAssetBootStage
{
    // 解压
    Decompress,
    // 检查版本
    CheckVersion,
    // 排队
    Queued,
    // 下载
    Download,
    // 加载配置
    LoadConfig,
    // 完成
    Completed,
}
/// <summary>
/// 资源启动进度
/// </summary>
public readonly struct AssetBootProgress
{
    public BundleModuleEnum Module { get; }
    public EAssetBootStage Stage { get; }
    public float Progress { get; }
    public float CompletedSizeMB { get; }
    public float TotalSizeMB { get; }
    public string Message { get; }

    /// <summary>
    /// 创建资源启动进度
    /// </summary>
    public AssetBootProgress(
        BundleModuleEnum module,
        EAssetBootStage stage,
        float progress,
        float completedSizeMB,
        float totalSizeMB,
        string message)
    {
        Module = module;
        Stage = stage;
        Progress = progress;
        CompletedSizeMB = completedSizeMB;
        TotalSizeMB = totalSizeMB;
        Message = message;
    }
}

/// <summary>
/// 热更版本检查结果
/// </summary>
public readonly struct HotUpdateCheckResult
{
    public bool NeedUpdate { get; }
    public float DownloadSizeMB { get; }

    /// <summary>
    /// 创建热更版本检查结果
    /// </summary>
    public HotUpdateCheckResult(bool needUpdate, float downloadSizeMB)
    {
        NeedUpdate = needUpdate;
        DownloadSizeMB = downloadSizeMB;
    }
}

/// <summary>
/// 客户端版本过低异常
/// </summary>
public sealed class AssetUpdateRequiredException : Exception
{
    public string MinimumVersion { get; }

    /// <summary>
    /// 创建客户端版本过低异常
    /// </summary>
    public AssetUpdateRequiredException(string minimumVersion)
        : base("客户端版本过低 最低版本 " + minimumVersion)
    {
        MinimumVersion = minimumVersion;
    }
}
}
