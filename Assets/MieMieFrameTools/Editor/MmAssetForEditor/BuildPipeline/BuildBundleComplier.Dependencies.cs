using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Asset
{
public partial class BuildBundleComplier
{
    /// <summary>
    /// AB 名称合法字符规则
    /// </summary>
    private static readonly Regex bundleNameRegex = new(
        "^[A-Za-z0-9_-]+$",
        RegexOptions.Compiled);

    /// <summary>
    /// 收集场景与场景依赖
    /// </summary>
    public static void BuildAllScene()
    {
        if (moduleData.scenePacks == null || moduleData.scenePacks.Length == 0)
            return;

        var sceneGuidList = AssetDatabase.FindAssets("t:Scene", moduleData.scenePacks);
        foreach (string sceneGuid in sceneGuidList)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
            string bundleName = GenerateBundleName(Path.GetFileNameWithoutExtension(scenePath));
            var dependencyList = new List<string>();
            foreach (string dependency in AssetDatabase.GetDependencies(scenePath, true))
            {
                if (!IsBuildableAsset(dependency))
                    continue;
                if (!dependencyList.Contains(dependency))
                    dependencyList.Add(dependency);
                if (!allBundlePathList.Contains(dependency))
                    allBundlePathList.Add(dependency);
            }
            allSceneBundleDict[bundleName] = dependencyList;
        }
    }

    /// <summary>
    /// 自动提取跨包共享依赖
    /// </summary>
    public static void ExtractSharedDependencies()
    {
        ExpandImplicitDependencies(allFolderBundleDict);
        ExpandImplicitDependencies(allPrefabBundleDict);
        ExpandImplicitDependencies(allSceneBundleDict);

        if (!moduleData.autoExtractSharedDependencies)
            return;

        var ownerNameHashListDict = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        CollectDependencyOwners(allFolderBundleDict, ownerNameHashListDict);
        CollectDependencyOwners(allPrefabBundleDict, ownerNameHashListDict);
        CollectDependencyOwners(allSceneBundleDict, ownerNameHashListDict);

        int referenceCount = Math.Max(2, moduleData.sharedDependencyReferenceCount);
        foreach (var item in ownerNameHashListDict)
        {
            if (item.Value.Count < referenceCount)
                continue;
            if (item.Key.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                continue;
            sharedDependencyPathList.Add(item.Key);
        }

        if (moduleData.shaderVariantCollection != null)
        {
            string shaderVariantPath = AssetDatabase.GetAssetPath(moduleData.shaderVariantCollection);
            if (!sharedDependencyPathList.Contains(shaderVariantPath))
                sharedDependencyPathList.Add(shaderVariantPath);
        }

        if (sharedDependencyPathList.Count == 0)
            return;

        RemoveSharedDependencies(allFolderBundleDict);
        RemoveSharedDependencies(allPrefabBundleDict);
        RemoveSharedDependencies(allSceneBundleDict);
        string commonBundleName = GenerateBundleName("common");
        allFolderBundleDict[commonBundleName] = new List<string>(sharedDependencyPathList);
    }

    /// <summary>
    /// 校验 AB 名称仅包含 ASCII 安全字符
    /// </summary>
    public static void ValidateBundleName(string bundleName)
    {
        if (!bundleNameRegex.IsMatch(bundleName))
            throw new ArgumentException("AB 名称仅允许英文数字下划线和短横线 " + bundleName);
    }

    /// <summary>
    /// 生成构建报告
    /// </summary>
    public static void GenerateBuildReport()
    {
        var report = new BundleBuildReport
        {
            moduleName = bundleModuleEnum.ToString(),
            platform = BundleSettings.Instance.buildTarget.ToString(),
            sharedDependencyList = new List<string>(sharedDependencyPathList),
            bundleList = new List<BundleBuildReportItem>(),
        };

        foreach (string filePath in Directory.GetFiles(bundleOutputPath))
        {
            string fileName = Path.GetFileName(filePath);
            if (!IsModuleBundleFile(fileName))
                continue;

            var fileInfo = new FileInfo(filePath);
            report.bundleList.Add(new BundleBuildReportItem
            {
                bundleName = fileName,
                sizeBytes = fileInfo.Length,
                assetList = new List<string>(
                    AssetDatabase.GetAssetPathsFromAssetBundle(fileName)),
            });
        }

        Directory.CreateDirectory(buildReportOutputPath);
        string reportPath = Path.Combine(
            buildReportOutputPath,
            bundleModuleEnum.ToString().ToLowerInvariant() + "_build_report.json");
        File.WriteAllText(
            reportPath,
            JsonConvert.SerializeObject(report, Formatting.Indented));
    }

    /// <summary>
    /// 扩展每个包的隐式依赖
    /// </summary>
    private static void ExpandImplicitDependencies(
        Dictionary<string, List<string>> bundleAssetListDict)
    {
        foreach (var item in bundleAssetListDict)
        {
            var expandedPathHashList = new HashSet<string>(item.Value, StringComparer.OrdinalIgnoreCase);
            var rootPathList = new List<string>(item.Value);
            foreach (string rootPath in rootPathList)
            {
                if (AssetDatabase.IsValidFolder(rootPath))
                    continue;
                foreach (string dependency in AssetDatabase.GetDependencies(rootPath, true))
                {
                    if (IsBuildableAsset(dependency))
                        expandedPathHashList.Add(dependency);
                }
            }
            item.Value.Clear();
            item.Value.AddRange(expandedPathHashList);
        }
    }

    /// <summary>
    /// 收集依赖所属包
    /// </summary>
    private static void CollectDependencyOwners(
        Dictionary<string, List<string>> bundleAssetListDict,
        Dictionary<string, HashSet<string>> ownerNameHashListDict)
    {
        foreach (var bundleItem in bundleAssetListDict)
        {
            foreach (string assetPath in bundleItem.Value)
            {
                if (!ownerNameHashListDict.TryGetValue(assetPath, out var ownerNameHashList))
                {
                    ownerNameHashList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    ownerNameHashListDict.Add(assetPath, ownerNameHashList);
                }
                ownerNameHashList.Add(bundleItem.Key);
            }
        }
    }

    /// <summary>
    /// 从业务包移除共享依赖
    /// </summary>
    private static void RemoveSharedDependencies(
        Dictionary<string, List<string>> bundleAssetListDict)
    {
        foreach (var item in bundleAssetListDict)
            item.Value.RemoveAll(sharedDependencyPathList.Contains);
    }

    /// <summary>
    /// 判断资源是否可进入 AB
    /// </summary>
    private static bool IsBuildableAsset(string assetPath)
    {
        if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            return false;
        if (AssetDatabase.IsValidFolder(assetPath))
            return false;
        if (assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            || assetPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            return false;
        return !assetPath.Contains("/Editor/", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// 模块构建报告
/// </summary>
[Serializable]
public sealed class BundleBuildReport
{
    /// <summary>模块名称</summary>
    public string moduleName;
    /// <summary>目标平台</summary>
    public string platform;
    /// <summary>共享依赖列表</summary>
    public List<string> sharedDependencyList;
    /// <summary>资源包列表</summary>
    public List<BundleBuildReportItem> bundleList;
}

/// <summary>
/// 单个资源包构建报告
/// </summary>
[Serializable]
public sealed class BundleBuildReportItem
{
    /// <summary>资源包名称</summary>
    public string bundleName;
    /// <summary>资源包字节数</summary>
    public long sizeBytes;
    /// <summary>资源路径列表</summary>
    public List<string> assetList;
}
}
