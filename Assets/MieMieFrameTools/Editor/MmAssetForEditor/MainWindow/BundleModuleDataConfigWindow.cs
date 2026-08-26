using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;


namespace MieMieFrameWork.Asset
{
[Serializable]
public class FolderPathEntry
{
    [FolderPath(RequireExistingPath = true)]
    [HideLabel]
    public string path;
}
/// <summary>
/// 配置模块打包信息类
/// 双击模块 打开此窗口
/// </summary>
public class BundleModuleConfigWindow : OdinEditorWindow
{
    /// <summary>
    /// CSharp 模块标识符规则
    /// </summary>
    private static readonly Regex moduleNameRegex = new(
        "^[A-Za-z][A-Za-z0-9_]*$",
        RegexOptions.Compiled);

    private const string PrefabPackDesc = "每个路径目录下的每一个独立 Prefab 单独打包";
    private const string SubFolderPackDesc = "每个路径目录下的每一个子文件夹都单独打包";
    private const string ScenePackDesc = "每个场景单独打包 并自动收集场景依赖";
    private const string WholePackDesc = "配置整包 AB 名称与 Bundle 路径";

    [Title("$moduleName", "模块分包配置", TitleAlignments.Centered)]
    [LabelText("模块名称")]
    [SerializeField] private string moduleName;

    [HideInInspector, SerializeField] private string sourceModuleName;
    [HideInInspector, SerializeField] private bool isNewModule;

    [BoxGroup("策略")]
    [LabelText("交付方式")]
    [SerializeField] private E_BundleDeliveryMode deliveryMode = E_BundleDeliveryMode.Hybrid;

    [BoxGroup("策略")]
    [LabelText("自动抽取共享依赖")]
    [SerializeField] private bool autoExtractSharedDependencies = true;

    [BoxGroup("策略")]
    [LabelText("共享引用阈值")]
    [MinValue(2)]
    [ShowIf("autoExtractSharedDependencies")]
    [SerializeField] private int sharedDependencyReferenceCount = 2;

    [BoxGroup("策略")]
    [LabelText("Shader 变体预热集合")]
    [SerializeField] private ShaderVariantCollection shaderVariantCollection;

    [PropertySpace(12)]
    [TabGroup("Pack", "预制体分包", order: 0)]
    [InfoBox(PrefabPackDesc, InfoMessageType.None)]
    [LabelText("预制体路径配置")]
    [FolderPath(RequireExistingPath = true)]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
    [SerializeField] private string[] prefabPackEntries;

    [TabGroup("Pack", "子文件分包", order: 1)]
    [InfoBox(SubFolderPackDesc, InfoMessageType.None)]
    [LabelText("文件夹子包路径配置")]
    [FolderPath(RequireExistingPath = true)]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
    [SerializeField] private string[] subFolderPackEntries;

    [TabGroup("Pack", "场景分包", order: 2)]
    [InfoBox(ScenePackDesc, InfoMessageType.None)]
    [LabelText("场景路径配置")]
    [FolderPath(RequireExistingPath = true)]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
    [SerializeField] private string[] scenePackEntries;

    [TabGroup("Pack", "模块整包", order: 3)]
    [InfoBox(WholePackDesc, InfoMessageType.None)]
    [TableList(ShowIndexLabels = true, AlwaysExpanded = true, DrawScrollView = true)]
    [SerializeField] private BundleFileInfo[] wholePackFiles;

    [TabGroup("Pack", "地址别名", order: 4)]
    [InfoBox("默认主地址为完整 Assets 路径 短名须在此显式注册 同名短名保存/导出时报错", InfoMessageType.None)]
    [TableList(ShowIndexLabels = true, AlwaysExpanded = true, DrawScrollView = true)]
    [SerializeField] private AssetAliasInfo[] assetAliasList;

    #region 地址别名

    /// <summary>
    /// 按文件名增量填充短名 已有资源或已有短名则跳过
    /// </summary>
    [TabGroup("Pack", "地址别名")]
    [Button("按文件名填充短名 仅唯一名", ButtonSizes.Medium)]
    private void FillAliasFromFileNames()
    {
        var folderArr = CollectPackFolderArr();
        if (folderArr.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先配置分包目录", "确定");
            return;
        }

        var pathByAliasDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var conflictAliasHashList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string[] guidArr = AssetDatabase.FindAssets(string.Empty, folderArr);
        foreach (string guid in guidArr)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
                continue;
            if (assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || assetPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                continue;

            string alias = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (string.IsNullOrEmpty(alias))
                continue;
            if (pathByAliasDict.TryGetValue(alias, out string existingPath)
                && !string.Equals(existingPath, assetPath, StringComparison.OrdinalIgnoreCase))
            {
                conflictAliasHashList.Add(alias);
                continue;
            }
            pathByAliasDict[alias] = assetPath;
        }

        foreach (string conflictAlias in conflictAliasHashList)
            pathByAliasDict.Remove(conflictAlias);

        var existingAliasHashList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var existingAssetPathHashList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resultList = new List<AssetAliasInfo>();
        if (assetAliasList != null)
        {
            foreach (var info in assetAliasList)
            {
                if (info == null)
                    continue;
                bool hasAlias = !string.IsNullOrWhiteSpace(info.alias);
                bool hasAsset = info.asset != null;
                if (!hasAlias && !hasAsset)
                    continue;
                resultList.Add(info);
                if (hasAlias)
                    existingAliasHashList.Add(info.alias);
                if (!hasAsset)
                    continue;
                string existingAssetPath = AssetDatabase.GetAssetPath(info.asset);
                if (!string.IsNullOrEmpty(existingAssetPath))
                    existingAssetPathHashList.Add(existingAssetPath);
            }
        }

        int addedCount = 0;
        foreach (var pair in pathByAliasDict)
        {
            // 已有同名短名或同一资源已在表里则跳过 支持改过后的增量刷新
            if (existingAliasHashList.Contains(pair.Key))
                continue;
            if (existingAssetPathHashList.Contains(pair.Value))
                continue;
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(pair.Value);
            if (asset == null)
                continue;
            resultList.Add(new AssetAliasInfo { alias = pair.Key, asset = asset });
            existingAliasHashList.Add(pair.Key);
            existingAssetPathHashList.Add(pair.Value);
            addedCount++;
        }

        assetAliasList = resultList.ToArray();
        string conflictText = conflictAliasHashList.Count > 0
            ? "\n撞名跳过 " + string.Join(" ", conflictAliasHashList)
            : string.Empty;
        EditorUtility.DisplayDialog(
            "填充短名",
            "新增 " + addedCount + " 条" + conflictText,
            "确定");
    }

    /// <summary> 一键追加到短名的后缀 </summary>
    [TabGroup("Pack", "地址别名")]
    [HorizontalGroup("Pack/SuffixRow", Width = 0.72f)]
    [LabelText("后缀")]
    [SerializeField]
    private string aliasSuffix;

    /// <summary>
    /// 给已有短名追加后缀 已带该后缀则跳过
    /// </summary>
    [TabGroup("Pack", "地址别名")]
    [HorizontalGroup("Pack/SuffixRow")]
    [Button("一键加后缀", ButtonSizes.Medium)]
    private void AppendAliasSuffix()
    {
        if (string.IsNullOrEmpty(aliasSuffix))
        {
            EditorUtility.DisplayDialog("提示", "请先填写后缀", "确定");
            return;
        }
        if (assetAliasList == null || assetAliasList.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "当前没有短名可追加", "确定");
            return;
        }

        int appendedCount = 0;
        foreach (var info in assetAliasList)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.alias))
                continue;
            if (info.alias.EndsWith(aliasSuffix, StringComparison.Ordinal))
                continue;
            info.alias += aliasSuffix;
            appendedCount++;
        }

        EditorUtility.DisplayDialog("一键加后缀", "已追加 " + appendedCount + " 条", "确定");
    }

    /// <summary>
    /// 收集当前模块已配置的分包目录
    /// </summary>
    private string[] CollectPackFolderArr()
    {
        var folderList = new List<string>();
        if (prefabPackEntries != null)
            folderList.AddRange(prefabPackEntries);
        if (subFolderPackEntries != null)
            folderList.AddRange(subFolderPackEntries);
        if (scenePackEntries != null)
            folderList.AddRange(scenePackEntries);
        if (wholePackFiles != null)
        {
            foreach (var pack in wholePackFiles)
            {
                if (!string.IsNullOrEmpty(pack.bundlePath))
                    folderList.Add(pack.bundlePath);
            }
        }

        return folderList.Where(p => !string.IsNullOrEmpty(p)).Distinct().ToArray();
    }

    #endregion

    #region 窗口开关

    /// <summary>
    /// 显示窗口
    /// </summary>
    /// <param name="name"></param>
    public static void ShowWindow(string name, bool isCreate = false)
    {
        EditorApplication.delayCall += () => OpenWindow(name, isCreate);
    }

    private static void OpenWindow(string name, bool isCreate)
    {
        var window = GetWindow<BundleModuleConfigWindow>(false, "模块分包配置", true);
        window.minSize = new Vector2(580, 480);
        window.position = GUIHelper.GetEditorWindowRect().AlignCenter(580, 480);
        window.isNewModule = isCreate;

        if (isCreate)
        {
            window.sourceModuleName = null;
            window.moduleName = name;
            window.prefabPackEntries = Array.Empty<string>();
            window.subFolderPackEntries = Array.Empty<string>();
            window.scenePackEntries = Array.Empty<string>();
            window.wholePackFiles = Array.Empty<BundleFileInfo>();
            window.assetAliasList = Array.Empty<AssetAliasInfo>();
        }
        else
        {
            window.sourceModuleName = name;
            var data = BuildBundleConfigura.Instance.GetBundleDataByName(name);
            if (data != null)
            {
                window.moduleName = data.moduleName;
                window.prefabPackEntries = data.prefabPacks ?? Array.Empty<string>();
                window.subFolderPackEntries = data.subFolderPacks ?? Array.Empty<string>();
                window.scenePackEntries = data.scenePacks ?? Array.Empty<string>();
                window.wholePackFiles = data.wholePackFiles ?? Array.Empty<BundleFileInfo>();
                window.assetAliasList = data.assetAliasList ?? Array.Empty<AssetAliasInfo>();
                window.deliveryMode = data.deliveryMode;
                window.autoExtractSharedDependencies = data.autoExtractSharedDependencies;
                window.sharedDependencyReferenceCount = data.sharedDependencyReferenceCount;
                window.shaderVariantCollection = data.shaderVariantCollection;
            }
            else
            {
                window.moduleName = name;
                window.prefabPackEntries = Array.Empty<string>();
                window.subFolderPackEntries = Array.Empty<string>();
                window.scenePackEntries = Array.Empty<string>();
                window.wholePackFiles = Array.Empty<BundleFileInfo>();
                window.assetAliasList = Array.Empty<AssetAliasInfo>();
            }
        }

        window.Show();
        window.Focus();
        window.Repaint();
    }

    #endregion

    #region 保存与删除

    [PropertySpace(8)]
    [HorizontalGroup("Footer", PaddingLeft = 0.55f)]
    [Button("保存配置", ButtonSizes.Medium)]
    private void OnClickSave() => SaveConfig();
    [PropertySpace(8)]
    [HorizontalGroup("Footer")]
    [Button("删除配置", ButtonSizes.Medium)]
    private void OnClickDelete() => DeleteConfig();

    /// <summary>
    /// 保存配置
    /// </summary>
    public void SaveConfig()
    {
        if (string.IsNullOrEmpty(moduleName))
        {
            UnityEditor.EditorUtility.DisplayDialog("提示", "模块名称不能为空", "确定");
            return;
        }
        if (!moduleNameRegex.IsMatch(moduleName))
        {
            EditorUtility.DisplayDialog("提示", "模块名称必须是英文开头的 CSharp 标识符", "确定");
            return;
        }
        var config = BuildBundleConfigura.Instance;
        BundleModuleData data = null;
        if (!isNewModule)
            data = config.GetBundleDataByName(sourceModuleName) ?? config.GetBundleDataByName(moduleName);

        var duplicate = config.GetBundleDataByName(moduleName);
        if (duplicate != null && duplicate != data)
        {
            EditorUtility.DisplayDialog("提示", $"模块名「{moduleName}」已存在", "确定");
            return;
        }

        if (data == null)
        {
            data = new BundleModuleData();
            config.bundleModuleDataList.Add(data);
        }

        data.moduleName = moduleName;
        data.prefabPacks = prefabPackEntries ?? Array.Empty<string>();
        data.subFolderPacks = subFolderPackEntries ?? Array.Empty<string>();
        data.scenePacks = scenePackEntries ?? Array.Empty<string>();
        data.wholePackFiles = wholePackFiles ?? Array.Empty<BundleFileInfo>();
        data.assetAliasList = assetAliasList ?? Array.Empty<AssetAliasInfo>();
        data.deliveryMode = deliveryMode;
        data.autoExtractSharedDependencies = autoExtractSharedDependencies;
        data.sharedDependencyReferenceCount = Math.Max(2, sharedDependencyReferenceCount);
        data.shaderVariantCollection = shaderVariantCollection;
        isNewModule = false;
        sourceModuleName = moduleName;

        config.SaveData();
        BundleEnumCreator.GenerateBundleModuleEnum();
        EditorUtility.DisplayDialog("提示", "保存配置成功", "确定");
        EditorApplication.delayCall += () =>
        {
            Close();
            BuildWindow.RefreshOrOpen();
        };
    }

    /// <summary>
    /// 删除配置
    /// </summary>
    public void DeleteConfig()
    {
        if (string.IsNullOrEmpty(moduleName) && string.IsNullOrEmpty(sourceModuleName))
        {
            EditorUtility.DisplayDialog("提示", "模块名称不能为空", "确定");
            return;
        }

        string displayName = string.IsNullOrEmpty(moduleName) ? sourceModuleName : moduleName;
        var confirmMessage = isNewModule
            ? $"确定要放弃新建模块「{displayName}」吗？"
            : $"确定要删除模块「{displayName}」的配置吗？此操作不可撤销。";

        if (!EditorUtility.DisplayDialog("确认删除", confirmMessage, "删除", "取消"))
            return;

        if (!isNewModule)
        {
            // 用打开时的源名称删除 避免改名未保存时删错或删空
            string removeName = string.IsNullOrEmpty(sourceModuleName) ? moduleName : sourceModuleName;
            BuildBundleConfigura.Instance.RemoveBundleDataByName(removeName);
            BundleEnumCreator.GenerateBundleModuleEnum();
        }

        EditorUtility.DisplayDialog("提示", "删除配置成功", "确定");
        EditorApplication.delayCall += () =>
        {
            Close();
            BuildWindow.RefreshOrOpen();
        };
    }

    #endregion
}
}
