using System.IO;
using UnityEngine;



namespace MieMieFrameWork.Asset
{
public partial class BuildBundleComplier
{
    // 打包输出路径
    // 输出路径 BuildOutput/Bundles/模块名/平台/
    private static string bundleOutputPath =>
        Path.GetFullPath(
            $"{Application.dataPath}/../BuildOutput/Bundles/{bundleModuleEnum.ToString().ToLowerInvariant()}/{BundleSettings.Instance.buildTarget}/");

    // 一整个模块的配置文件资源路径
    // 输出路径 ModuleRoot/Generated/模块名_AbConfig.json
    private static string bundleConfigAssetPath =>
        $"{MmAssetPaths.GeneratedAssetFolder}/{bundleModuleEnum.ToString().ToLowerInvariant()}_AbConfig.json";

    // 一整个模块的配置文件输出路径
    private static string bundleConfigFilePath =>
        Path.Combine(
            MmAssetPaths.GeneratedDiskPath,
            $"{bundleModuleEnum.ToString().ToLowerInvariant()}_AbConfig.json");

    // 一整个模块的配置文件对应的 AB 包名
    // 模块名小写_abconfig + 后缀 与运行时 BundleSettings.GetBundleConfigFileName 一致
    private static string bundleConfigBundleName =>
        $"{bundleModuleEnum.ToString().ToLowerInvariant()}_abconfig{BundleSettings.BundleFileExtension}";

    // 内嵌资源的配置 Resource 资源路径
    private static string builtInResourcePath => MmAssetPaths.ResourcesDiskPath;

    // 标准内嵌资源输出路径 
    // 输出路径：StreamingAssets/AssetBundle/模块名小写/
    private static string standardStreamingAssetsPath =>
                            $"{Application.streamingAssetsPath}/AssetBundle/{bundleModuleEnum.ToString().ToLowerInvariant()}/";


    // 热更资源输出路径
    // 输出路径 BuildOutput/Hot/模块名/版本号/平台/
    private static string hotPatchOutputPath =>
        Path.GetFullPath(
            $"{Application.dataPath}/../BuildOutput/Hot/{bundleModuleEnum.ToString().ToLowerInvariant()}/{hotPatchVersion}/{BundleSettings.Instance.buildTarget}/");

    // 热更清单输出目录
    private static string hotManifestOutputPath =>
        Path.GetFullPath(
            $"{Application.dataPath}/../BuildOutput/Hot/{bundleModuleEnum.ToString().ToLowerInvariant()}/");

    // 构建报告输出目录
    private static string buildReportOutputPath =>
        Path.GetFullPath($"{Application.dataPath}/../BuildOutput/Reports/");
}
}
