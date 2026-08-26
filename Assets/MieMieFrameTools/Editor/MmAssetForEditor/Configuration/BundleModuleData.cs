using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 模块构建配置类
/// 用于记录每一个模块的打包配置信息
/// </summary>

namespace MieMieFrameWork.Asset
{
[Serializable ]
public class BundleModuleData 
{
    /// <summary>模块 ID</summary>
    public long bundleId;
    /// <summary>模块名称</summary>
    public string moduleName;
    /// <summary>是否参与构建</summary>
    public bool isBuild;

    /// <summary>模块交付方式</summary>
    public E_BundleDeliveryMode deliveryMode = E_BundleDeliveryMode.Hybrid;
    /// <summary>是否自动提取共享依赖</summary>
    public bool autoExtractSharedDependencies = true;
    /// <summary>共享依赖最小引用包数量</summary>
    [MinValue(2)]
    public int sharedDependencyReferenceCount = 2;
    /// <summary>预制体分包目录列表</summary>
    public string[] prefabPacks;
    /// <summary>子文件夹分包目录列表</summary>
    public string[] subFolderPacks;
    /// <summary>场景分包目录列表</summary>
    public string[] scenePacks;
    /// <summary>模块整包配置列表</summary>
    public BundleFileInfo[] wholePackFiles;
    /// <summary>资源地址别名列表</summary>
    public AssetAliasInfo[] assetAliasList;
    /// <summary>Shader 变体预热集合</summary>
    public ShaderVariantCollection shaderVariantCollection;
}


/// <summary>
/// 单个资源包的类
/// 服务于BundleModuleData类 用于记录每一个单独打包的资源包的配置信息
/// </summary>
[Serializable]
public class BundleFileInfo
{
    /// <summary>AB 名称</summary>
    [LabelText("AB 名称")]
    public string abName;

    /// <summary>Bundle 路径</summary>
    [LabelText("Bundle 路径"), FolderPath(RequireExistingPath = true)]
    public string bundlePath;
}

/// <summary>
/// 资源地址别名配置
/// </summary>
[Serializable]
public sealed class AssetAliasInfo
{
    /// <summary>业务资源别名</summary>
    [LabelText("资源别名")]
    public string alias;

    /// <summary>目标资源</summary>
    [LabelText("目标资源")]
    public UnityEngine.Object asset;
}
}
