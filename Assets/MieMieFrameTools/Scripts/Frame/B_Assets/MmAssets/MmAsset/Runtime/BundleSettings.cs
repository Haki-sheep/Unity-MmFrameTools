using System;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 运行时资源模式 离线或热更
/// </summary>

namespace MieMieFrameWork.Asset
{
public enum E_RuntimeBundleMode
{
    NotHot,
    Hot,
}

public enum E_LoadAssetType
{
    Editor,
    AssetBundle,
}

[CreateAssetMenu(fileName = "BundleSettings", menuName = "MmAsset/BundleSettings")]
public class BundleSettings : ScriptableObject
{
    /// <summary>资源包文件后缀 避免与场景 .unity 冲突</summary>
    public const string BundleFileExtension = ".ABAssetBundle";

    private static BundleSettings instance;

    public static BundleSettings Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<BundleSettings>("BundleSettings");
            return instance;
        }
    }

    [TitleGroup("资源加载热更设置")]
    [LabelText("下载地址"), SerializeField]
    public string downloadUrl;

    [TitleGroup("资源加载热更设置")]
    [LabelText("是否热更")]
    public E_RuntimeBundleMode buildBundleType;

    [TitleGroup("资源加载热更设置")]
    [LabelText("资源加载类型")]
    public E_LoadAssetType loadAssetType;

    [TitleGroup("资源加载热更设置")]
    [LabelText("最大热更线程数")]
    public int maxHotThreadCount;

    [TitleGroup("资源加载热更设置")]
    [LabelText("下载失败重试次数")]
    public int maxDownloadRetryCount = 3;

    [TitleGroup("资源加载热更设置")]
    [LabelText("资源最低客户端版本")]
    public string minimumClientVersion = "0.0.0";

    [FoldoutGroup("打包设置")]
    [InlineProperty, LabelText("加密设置")]
    public BundleEncryptToggle bundleEncryptToggle;

    [FoldoutGroup("打包设置"), LabelText("目标平台")]
    public E_BuildTarget buildTarget;

    [FoldoutGroup("打包设置"), LabelText("压缩格式")]
    public E_BuildAssetBundleOptions buildAssetBundleOptions;

    /// <summary>
    /// 模块目录名统一小写
    /// </summary>
    public string GetModuleFolderName(BundleModuleEnum bundleModuleEnum)
    {
        return bundleModuleEnum.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// StreamingAssets 内嵌 AB 目录
    /// </summary>
    public string GetBuiltInStreamAssetsPath(BundleModuleEnum bundleModuleEnum)
    {
        return Path.Combine(Application.streamingAssetsPath, "AssetBundle", GetModuleFolderName(bundleModuleEnum))
               + Path.DirectorySeparatorChar;
    }

    /// <summary>
    /// 随包解压落地目录
    /// </summary>
    public string GetDecompressAssetsPath(BundleModuleEnum bundleModuleEnum)
    {
        return Path.Combine(
                   Application.persistentDataPath,
                   "MmAsset",
                   GetModuleFolderName(bundleModuleEnum),
                   "decompress")
               + Path.DirectorySeparatorChar;
    }

    /// <summary>
    /// 热更 AB 落地目录
    /// </summary>
    public string GetHotAssetsSavePath(BundleModuleEnum bundleModuleEnum)
    {
        return Path.Combine(
                   Application.persistentDataPath,
                   "MmAsset",
                   GetModuleFolderName(bundleModuleEnum),
                   "hot")
               + Path.DirectorySeparatorChar;
    }

    /// <summary>
    /// 服务器热更清单本地缓存路径
    /// </summary>
    public string GetHotManifestServerPath(BundleModuleEnum bundleModuleEnum)
    {
        return Path.Combine(
            Application.persistentDataPath,
            "MmAsset",
            GetModuleFolderName(bundleModuleEnum),
            "manifest",
            "server.json");
    }

    /// <summary>
    /// 本地已生效热更清单路径
    /// </summary>
    public string GetHotManifestLocalPath(BundleModuleEnum bundleModuleEnum)
    {
        return Path.Combine(
            Application.persistentDataPath,
            "MmAsset",
            GetModuleFolderName(bundleModuleEnum),
            "manifest",
            "local.json");
    }

    /// <summary>
    /// 运行时解密缓存文件路径
    /// </summary>
    public string GetDecryptedBundlePath(BundleModuleEnum bundleModuleEnum, string abName)
    {
        return Path.Combine(
            Application.temporaryCachePath,
            "MmAsset",
            GetModuleFolderName(bundleModuleEnum),
            "decrypted",
            abName);
    }

    /// <summary>
    /// 地址表 AB 文件名 与打包产物一致
    /// </summary>
    public string GetBundleConfigFileName(BundleModuleEnum bundleModuleEnum)
    {
        return GetModuleFolderName(bundleModuleEnum) + "_abconfig" + BundleFileExtension;
    }

    /// <summary>
    /// 地址表包内 TextAsset 名 对应 Assets 下 json 资源名
    /// </summary>
    public string GetBundleConfigAssetName(BundleModuleEnum bundleModuleEnum)
    {
        return GetModuleFolderName(bundleModuleEnum) + "_AbConfig";
    }

    /// <summary>
    /// 随包清单 Resources 名称
    /// </summary>
    public string GetBuiltInManifestResourceName(BundleModuleEnum bundleModuleEnum)
    {
        return GetModuleFolderName(bundleModuleEnum) + "_builtin";
    }

    /// <summary>
    /// 远程热更清单地址
    /// </summary>
    public string GetHotManifestUrl(BundleModuleEnum bundleModuleEnum)
    {
        return downloadUrl.Trim().TrimEnd('/')
               + "/HotAssets/"
               + GetModuleFolderName(bundleModuleEnum)
               + "/hot_manifest.json";
    }

    /// <summary>
    /// 解析 AB 读盘路径 热更优先否则解压目录
    /// </summary>
    public string ResolveBundleFilePath(BundleModuleEnum bundleModuleEnum, string abName)
    {
        var hotPath = GetHotAssetsSavePath(bundleModuleEnum) + abName;
        if (File.Exists(hotPath))
            return hotPath;

        var decompressPath = GetDecompressAssetsPath(bundleModuleEnum) + abName;
        if (File.Exists(decompressPath))
            return decompressPath;

        return GetBuiltInStreamAssetsPath(bundleModuleEnum) + abName;
    }

    /// <summary>
    /// Editor 导出的模块地址表磁盘路径
    /// </summary>
    public string GetGeneratedAbConfigDiskPath(BundleModuleEnum bundleModuleEnum)
    {
        return Path.Combine(
            Application.dataPath,
            "MieMieFrameTools/Scripts/Frame/B_Assets/MmAssetsMethod/MmAsset/Generated",
            GetModuleFolderName(bundleModuleEnum) + "_AbConfig.json");
    }
}

/// <summary>
/// 加密设置
/// </summary>
[Serializable]
public class BundleEncryptToggle
{
    [LabelText("是否加密")]
    public bool isEncrypt;

    [LabelText("加密范围"), ShowIf("isEncrypt")]
    public E_BundleEncryptionScope encryptionScope;

    [LabelText("加密密钥"), ShowIf("isEncrypt")]
    public string encryptKey;
}

/// <summary>
/// 资源包加密范围
/// </summary>
public enum E_BundleEncryptionScope
{
    ConfigOnly,
    AllBundles,
}

/// <summary>
/// 目标平台枚举
/// </summary>
public enum E_BuildTarget
{
    iPhone = -1,
    StandaloneOSX = 2,
    StandaloneOSXUniversal = 3,
    iOS = 9,
    Android = 13,
    StandaloneLinux = 17,
    StandaloneWindows64 = 19,
}

/// <summary>
/// 压缩格式枚举
/// </summary>
public enum E_BuildAssetBundleOptions
{
    None = 0,
    UncompressedAssetBundle = 1,
    CollectDependencies = 2,
    CompleteAssets = 4,
    DisableWriteTypeTree = 8,
    DeterministicAssetBundle = 16,
    ForceRebuildAssetBundle = 32,
    IgnoreTypeTreeChanges = 64,
    AppendHashToAssetBundleName = 128,
    ChunkBasedCompression = 256,
    StrictMode = 512,
    DryRunBuild = 1024,
    DisableLoadAssetByFileName = 4096,
    DisableLoadAssetByFileNameWithExtension = 8192,
    AssetBundleStripUnityVersion = 32768,
    EnableProtection = 65536,
}
}
