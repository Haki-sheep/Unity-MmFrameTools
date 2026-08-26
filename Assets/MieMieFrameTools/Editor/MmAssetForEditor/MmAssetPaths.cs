/// <summary>
/// MmAsset 在 MieMieFrameTools 内的路径常量
/// </summary>

namespace MieMieFrameWork.Asset
{
public static class MmAssetPaths
{
    /// <summary>
    /// 框架根
    /// </summary>
    public const string MieMieRoot = "Assets/MieMieFrameTools";

    /// <summary>
    /// MmAsset 运行时模块根
    /// </summary>
    public const string ModuleRoot =
        MieMieRoot + "/Scripts/Frame/B_Assets/MmAssetsMethod/MmAsset";

    /// <summary>
    /// MmAsset 编辑器根
    /// </summary>
    public const string EditorRoot = MieMieRoot + "/Editor/MmAssetForEditor";

    /// <summary>
    /// 打包模块配置 SO
    /// </summary>
    public const string AssetBundleConfigAsset =
        EditorRoot + "/Configuration/AssetBundleConfig.asset";

    /// <summary>
    /// 编辑器中间产物 Generated
    /// </summary>
    public const string GeneratedAssetFolder = ModuleRoot + "/Generated";

    /// <summary>
    /// Runtime 目录
    /// </summary>
    public const string RuntimeFolder = ModuleRoot + "/Runtime";

    /// <summary>
    /// Resources 目录资源路径前缀
    /// </summary>
    public const string ResourcesAssetFolder = ModuleRoot + "/Resources";

    /// <summary>
    /// 磁盘上的模块根绝对路径
    /// </summary>
    public static string ModuleRootDiskPath =>
        UnityEngine.Application.dataPath
        + "/MieMieFrameTools/Scripts/Frame/B_Assets/MmAssetsMethod/MmAsset";

    /// <summary>
    /// Generated 磁盘路径
    /// </summary>
    public static string GeneratedDiskPath => ModuleRootDiskPath + "/Generated";

    /// <summary>
    /// Resources 磁盘路径
    /// </summary>
    public static string ResourcesDiskPath => ModuleRootDiskPath + "/Resources/";

    /// <summary>
    /// Runtime 磁盘路径
    /// </summary>
    public static string RuntimeDiskPath => ModuleRootDiskPath + "/Runtime";
}
}
