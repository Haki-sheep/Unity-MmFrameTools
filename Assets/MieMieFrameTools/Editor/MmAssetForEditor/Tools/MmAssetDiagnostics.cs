using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MmAsset 工程自检工具
/// </summary>

namespace MieMieFrameWork.Asset
{
public static class MmAssetDiagnostics
{
    /// <summary> 失败弹窗最多展示的错误条数 </summary>
    private const int MaxDialogErrorLine = 8;

    /// <summary>
    /// 运行完整自检
    /// </summary>
    public static bool ValidateProject()
    {
        return ValidateProject(new List<string>(), new List<string>());
    }

    /// <summary>
    /// 运行完整自检并回填错误与警告
    /// </summary>
    public static bool ValidateProject(List<string> errorList, List<string> warningList)
    {
        errorList.Clear();
        warningList.Clear();
        ValidateSettings(errorList, warningList);
        ValidateModules(errorList);

        foreach (string warning in warningList)
            Debug.LogWarning("[MmAsset] " + warning);
        foreach (string error in errorList)
            Debug.LogError("[MmAsset] " + error);

        string message = "错误 " + errorList.Count + "  警告 " + warningList.Count;
        if (errorList.Count == 0)
            Debug.Log("[MmAsset] 自检通过 " + message);
        return errorList.Count == 0;
    }

    /// <summary>
    /// 组装自检失败弹窗文案 含模块名
    /// </summary>
    public static string FormatFailDialog(List<string> errorList, List<string> warningList)
    {
        int errorCount = errorList.Count;
        int warningCount = warningList != null ? warningList.Count : 0;
        var builder = new StringBuilder();
        builder.Append("自检未通过 错误 ").Append(errorCount);
        if (warningCount > 0)
            builder.Append(" 警告 ").Append(warningCount);
        builder.AppendLine();

        int showCount = errorCount < MaxDialogErrorLine ? errorCount : MaxDialogErrorLine;
        for (int i = 0; i < showCount; i++)
            builder.AppendLine("· " + errorList[i]);
        if (errorCount > showCount)
            builder.AppendLine("… 还有 " + (errorCount - showCount) + " 条错误");
        builder.Append("完整日志见 Console");
        return builder.ToString();
    }

    /// <summary>
    /// 菜单运行完整自检
    /// </summary>
    [MenuItem("Tools/MieMieFrameWork/MmAsset/运行自检")]
    private static void ValidateProjectMenu()
    {
        var errorList = new List<string>();
        var warningList = new List<string>();
        bool ok = ValidateProject(errorList, warningList);
        if (ok)
        {
            string passMessage = WarningCountMessage(warningList.Count);
            EditorUtility.DisplayDialog("MmAsset", passMessage, "确定");
            return;
        }

        EditorUtility.DisplayDialog("MmAsset", FormatFailDialog(errorList, warningList), "确定");
    }

    /// <summary>
    /// 自检通过弹窗文案
    /// </summary>
    private static string WarningCountMessage(int warningCount)
    {
        if (warningCount == 0)
            return "自检通过";
        return "自检通过 警告 " + warningCount + " 条 详见 Console";
    }

    /// <summary>
    /// 校验运行时设置
    /// </summary>
    private static void ValidateSettings(
        List<string> errorList,
        List<string> warningList)
    {
        var settings = BundleSettings.Instance;
        if (settings == null)
        {
            errorList.Add("缺少 Resources/BundleSettings.asset");
            return;
        }

        if (settings.buildAssetBundleOptions != E_BuildAssetBundleOptions.ChunkBasedCompression)
            warningList.Add("正式资源建议使用 LZ4 ChunkBasedCompression");
        if (settings.maxHotThreadCount <= 0)
            errorList.Add("最大热更线程数必须大于零");
        if (settings.bundleEncryptToggle.isEncrypt
            && string.IsNullOrWhiteSpace(settings.bundleEncryptToggle.encryptKey))
            errorList.Add("已开启加密但密钥为空");
        if (settings.buildBundleType == E_RuntimeBundleMode.Hot
            && string.IsNullOrWhiteSpace(settings.downloadUrl))
            errorList.Add("热更模式下载地址为空");
    }

    #region 模块校验

    /// <summary>
    /// 校验模块构建配置
    /// </summary>
    private static void ValidateModules(List<string> errorList)
    {
        var config = BuildBundleConfigura.Instance;
        if (config == null)
        {
            errorList.Add("缺少 AssetBundleConfig.asset");
            return;
        }

        var moduleNameHashList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var aliasOwnerDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var moduleData in config.bundleModuleDataList)
        {
            string moduleName = moduleData.moduleName;
            if (!moduleNameHashList.Add(moduleName))
                errorList.Add(FormatModuleIssue(moduleName, "模块名称重复"));
            if (!moduleData.isBuild)
                continue;
            if (!Enum.TryParse(moduleName, out BundleModuleEnum _))
                errorList.Add(FormatModuleIssue(moduleName, "模块枚举缺少"));
            if (moduleData.sharedDependencyReferenceCount < 2)
                errorList.Add(FormatModuleIssue(moduleName, "共享依赖阈值必须大于等于二"));

            ValidateWholePackFiles(moduleName, moduleData.wholePackFiles, errorList);
            ValidateDirectoryPacks(moduleName, "预制体分包", moduleData.prefabPacks, errorList);
            ValidateDirectoryPacks(moduleName, "子文件夹分包", moduleData.subFolderPacks, errorList);
            ValidateDirectoryPacks(moduleName, "场景分包", moduleData.scenePacks, errorList);
            ValidateAssetAliasList(moduleName, moduleData.assetAliasList, aliasOwnerDict, errorList);
        }
    }

    /// <summary>
    /// 校验整包路径与 AB 名称
    /// </summary>
    private static void ValidateWholePackFiles(
        string moduleName,
        BundleFileInfo[] wholePackFiles,
        List<string> errorList)
    {
        if (wholePackFiles == null)
            return;

        foreach (var fileInfo in wholePackFiles)
        {
            if (string.IsNullOrWhiteSpace(fileInfo.abName))
                errorList.Add(FormatModuleIssue(moduleName, "整包 AB 名称为空"));
            else
            {
                try
                {
                    BuildBundleComplier.ValidateBundleName(fileInfo.abName);
                }
                catch (ArgumentException exception)
                {
                    errorList.Add(FormatModuleIssue(moduleName, exception.Message));
                }
            }

            if (string.IsNullOrWhiteSpace(fileInfo.bundlePath))
            {
                errorList.Add(FormatModuleIssue(moduleName, "整包路径为空 " + fileInfo.abName));
                continue;
            }

            if (!Directory.Exists(fileInfo.bundlePath))
            {
                errorList.Add(FormatModuleIssue(
                    moduleName,
                    "整包 " + fileInfo.abName + " 目录不存在 " + fileInfo.bundlePath));
            }
        }
    }

    /// <summary>
    /// 校验分包目录是否存在
    /// </summary>
    private static void ValidateDirectoryPacks(
        string moduleName,
        string packKind,
        string[] pathList,
        List<string> errorList)
    {
        if (pathList == null)
            return;

        foreach (string path in pathList)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                errorList.Add(FormatModuleIssue(moduleName, packKind + " 路径为空"));
                continue;
            }

            if (!Directory.Exists(path))
                errorList.Add(FormatModuleIssue(moduleName, packKind + " 目录不存在 " + path));
        }
    }

    /// <summary>
    /// 校验资源别名
    /// </summary>
    private static void ValidateAssetAliasList(
        string moduleName,
        AssetAliasInfo[] assetAliasList,
        Dictionary<string, string> aliasOwnerDict,
        List<string> errorList)
    {
        if (assetAliasList == null)
            return;

        foreach (var aliasInfo in assetAliasList)
        {
            if (string.IsNullOrWhiteSpace(aliasInfo.alias))
            {
                errorList.Add(FormatModuleIssue(moduleName, "资源别名不能为空"));
                continue;
            }

            if (!aliasOwnerDict.TryAdd(aliasInfo.alias, moduleName))
            {
                errorList.Add(FormatModuleIssue(
                    moduleName,
                    "资源别名重复 " + aliasInfo.alias + " 已在 " + aliasOwnerDict[aliasInfo.alias]));
            }

            if (aliasInfo.asset == null)
                errorList.Add(FormatModuleIssue(moduleName, "资源别名未指定目标 " + aliasInfo.alias));
        }
    }

    /// <summary>
    /// 给问题补上模块名前缀
    /// </summary>
    private static string FormatModuleIssue(string moduleName, string message)
    {
        return "[" + moduleName + "] " + message;
    }

    #endregion
}
}
