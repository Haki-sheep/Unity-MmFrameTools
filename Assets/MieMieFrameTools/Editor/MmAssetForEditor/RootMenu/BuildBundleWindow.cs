using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;



namespace MieMieFrameWork.Asset
{
public class BuildBundleWindow : BundleBehaviour
{
    /// <summary>
    /// 初始化
    /// </summary>
    public override void Init()
    {
        base.Init();
    }

    /// <summary>
    /// 左侧按钮
    /// </summary>
    public override void DrawLeftButton()
    {
        base.DrawLeftButton();
        if (GUILayout.Button(
                new GUIContent("删除", "删除勾选模块的配置与生成产物 不删除源资源"),
                GUILayout.Height(28),
                GUILayout.MinWidth(68)))
        {
            DeleteSelectedModules();
        }
         if (GUILayout.Button("打包", GUILayout.Height(28), GUILayout.MinWidth(88)))
        {
            Build();
        }
        if (GUILayout.Button(
                new GUIContent("导出地址表", "仅写 Generated AbConfig 不打 AB 供 Editor 短名加载"),
                GUILayout.Height(28),
                GUILayout.MinWidth(88)))
        {
            ExportAddressTable();
        }
    }

    /// <summary>
    /// 右侧按钮
    /// </summary>
    public override void DrawRightButton()
    {
        base.DrawRightButton();
        if (GUILayout.Button(
                new GUIContent("内嵌", "将 AssetBundle 复制到 StreamingAssets，需先完成「打包」"),
                GUILayout.Height(28), GUILayout.MinWidth(88)))
        {
            BuildPatchBundle();
        }
    }

    /// <summary>
    /// 仅导出地址表
    /// </summary>
    public void ExportAddressTable()
    {
        var selectedNameList = CollectSelectedModuleNameList();
        if (selectedNameList.Count == 0)
        {
            EditorUtility.DisplayDialog("MmAsset", "没有勾选要导出的模块", "确定");
            return;
        }

        if (!EnsureDiagnosticsPassed())
            return;

        EditorApplication.delayCall += () =>
        {
            foreach (var moduleData in moduleDataList)
            {
                if (moduleData.isBuild)
                    BuildBundleComplier.ExportAddressTableOnly(moduleData);
            }

            EditorUtility.DisplayDialog(
                "MmAsset",
                "地址表已导出\n" + string.Join("\n", selectedNameList),
                "确定");
        };
    }

    /// <summary>
    /// 打包资源
    /// </summary>
    public void Build()
    {
        var selectedNameList = CollectSelectedModuleNameList();
        if (selectedNameList.Count == 0)
        {
            EditorUtility.DisplayDialog("MmAsset", "没有勾选要打包的模块", "确定");
            return;
        }

        if (!EnsureDiagnosticsPassed())
            return;

        // 不能在 OnGUI 里同步打包 会触发 EndLayoutGroup 并可能导致文件写入失败
        EditorApplication.delayCall += () =>
        {
            foreach (var moduleData in moduleDataList)
            {
                if (moduleData.isBuild)
                {
                    BuildBundleComplier.BuildAsseetBundle(moduleData,
                                                          E_EditorBuildKind.AssetBundle);
                }
            }
        };
    }

    /// <summary>
    /// 内嵌资源
    /// </summary>
    public void BuildPatchBundle()
    {
        var selectedNameList = CollectSelectedModuleNameList();
        if (selectedNameList.Count == 0)
        {
            EditorUtility.DisplayDialog("MmAsset", "没有勾选要内嵌的模块", "确定");
            return;
        }

        if (!EnsureDiagnosticsPassed())
            return;

        foreach (var moduleData in moduleDataList)
        {
            if (moduleData.isBuild)
                BuildBundleComplier.CopyBundleToStreamingAssets(moduleData, true);
        }

        EditorUtility.DisplayDialog(
            "MmAsset",
            "已复制到 StreamingAssets\n" + string.Join("\n", selectedNameList),
            "确定");
    }

    /// <summary>
    /// 删除勾选模块的配置与生成产物
    /// </summary>
    private void DeleteSelectedModules()
    {
        var selectedNameList = CollectSelectedModuleNameList();
        if (selectedNameList.Count == 0)
        {
            EditorUtility.DisplayDialog("MmAsset", "没有勾选要删除的模块", "确定");
            return;
        }

        string moduleText = string.Join("\n", selectedNameList);
        bool confirmed = EditorUtility.DisplayDialog(
            "删除模块",
            "将删除以下模块的配置与全部生成产物 不会删除源资源\n\n" + moduleText,
            "删除",
            "取消");
        if (!confirmed)
            return;

        foreach (string moduleName in selectedNameList)
            DeleteModuleArtifacts(moduleName);

        BuildBundleConfigura.Instance.RemoveBundleDataByNameList(selectedNameList);
        BundleEnumCreator.GenerateBundleModuleEnum();
        AssetDatabase.Refresh();
        Init();
        EditorUtility.DisplayDialog("MmAsset", "已删除模块与生成产物\n" + moduleText, "确定");
    }

    /// <summary>
    /// 删除指定模块的构建生成物
    /// </summary>
    private static void DeleteModuleArtifacts(string moduleName)
    {
        string moduleNameLower = moduleName.ToLowerInvariant();
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string buildTarget = BundleSettings.Instance.buildTarget.ToString();
        string bundleOutputPath = Path.Combine(
            projectRoot,
            "BuildOutput",
            "Bundles",
            moduleNameLower,
            buildTarget);
        string hotOutputPath = Path.Combine(
            projectRoot,
            "BuildOutput",
            "Hot",
            moduleNameLower);
        string streamingAssetsPath = Path.Combine(
            Application.streamingAssetsPath,
            "AssetBundle",
            moduleNameLower);
        string generatedConfigPath = Path.Combine(
            MmAssetPaths.GeneratedDiskPath,
            moduleNameLower + "_AbConfig.json");
        string builtInManifestPath = Path.Combine(
            MmAssetPaths.ResourcesDiskPath,
            moduleNameLower + "_builtin.json");

        DeleteDirectory(bundleOutputPath);
        DeleteDirectory(hotOutputPath);
        DeleteDirectory(streamingAssetsPath);
        DeleteFile(generatedConfigPath);
        DeleteFile(builtInManifestPath);
    }

    /// <summary>
    /// 删除目录并忽略不存在目录
    /// </summary>
    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
    }

    /// <summary>
    /// 删除文件并忽略不存在文件
    /// </summary>
    private static void DeleteFile(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
}
