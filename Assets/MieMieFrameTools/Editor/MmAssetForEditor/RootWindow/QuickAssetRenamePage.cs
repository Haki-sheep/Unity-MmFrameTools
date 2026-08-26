using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MieMieFrameWork.Asset
{
/// <summary>
/// 快速配置资源名称页 批量追加约定后缀
/// </summary>
[Serializable]
public sealed class QuickAssetRenamePage
{
    /// <summary> 资源类型到后缀 </summary>
    public enum E_QuickAssetRenameType
    {
        Sprite,
        Prefab,
        AmClip,
        Audio,
        AmCc,
        So,
        Mat,
        Mesh,
        Tex,
        Font,
        Shader,
    }

    /// <summary> 预览项 </summary>
    private sealed class RenamePreviewItem
    {
        /// <summary> 原路径 </summary>
        public string oldPath;

        /// <summary> 新文件名无扩展名 </summary>
        public string newName;

        /// <summary> 新完整路径 </summary>
        public string newPath;

        /// <summary> 跳过原因 空表示待改名 </summary>
        public string skipReason;
    }

    /// <summary> 目标文件夹 </summary>
    [HideInInspector]
    [SerializeField]
    private DefaultAsset targetFolder;

    /// <summary> 资源类型 </summary>
    [HideInInspector]
    [SerializeField]
    private E_QuickAssetRenameType assetType = E_QuickAssetRenameType.Sprite;

    /// <summary> 是否递归扫描 </summary>
    [HideInInspector]
    [SerializeField]
    private bool recursiveScan = true;

    /// <summary> 预览列表 </summary>
    private readonly List<RenamePreviewItem> previewItemList = new List<RenamePreviewItem>();

    /// <summary> 预览滚动位置 </summary>
    private Vector2 previewScrollPos;

    /// <summary>
    /// 绘制页面
    /// </summary>
    [OnInspectorGUI]
    private void DrawPage()
    {
        EditorGUILayout.LabelField("快速配置资源名称", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "拖入文件夹 选择资源类型 扫描后预览再执行\n"
            + "已有正确后缀则跳过 只追加后缀 冲突则报错跳过该项\n"
            + "仅处理与所选类型匹配的资产 不改 Excel 不打包",
            MessageType.Info);

        GUILayout.Space(8f);
        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "目标文件夹",
            targetFolder,
            typeof(DefaultAsset),
            false);
        assetType = (E_QuickAssetRenameType)EditorGUILayout.EnumPopup("资源类型", assetType);
        recursiveScan = EditorGUILayout.Toggle("递归扫描", recursiveScan);

        string suffix = GetSuffix(assetType);
        EditorGUILayout.LabelField("将追加后缀", suffix);

        GUILayout.Space(8f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("扫描预览", GUILayout.Height(28f)))
            RefreshPreview();
        using (new EditorGUI.DisabledScope(previewItemList.Count == 0))
        {
            if (GUILayout.Button("执行改名", GUILayout.Height(28f)))
                ExecuteRename();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(8f);
        DrawPreviewList();
    }

    /// <summary>
    /// 绘制预览列表
    /// </summary>
    private void DrawPreviewList()
    {
        int pendingCount = 0;
        int skipCount = 0;
        for (int i = 0; i < previewItemList.Count; i++)
        {
            if (string.IsNullOrEmpty(previewItemList[i].skipReason))
                pendingCount++;
            else
                skipCount++;
        }

        EditorGUILayout.LabelField(
            $"预览  待改名 {pendingCount}  跳过 {skipCount}  合计 {previewItemList.Count}",
            EditorStyles.boldLabel);

        previewScrollPos = EditorGUILayout.BeginScrollView(previewScrollPos, GUILayout.MinHeight(220f));
        for (int i = 0; i < previewItemList.Count; i++)
        {
            RenamePreviewItem item = previewItemList[i];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(item.oldPath);
            if (string.IsNullOrEmpty(item.skipReason))
                EditorGUILayout.LabelField($"→ {item.newPath}", EditorStyles.miniLabel);
            else
                EditorGUILayout.LabelField($"跳过  {item.skipReason}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 刷新预览
    /// </summary>
    private void RefreshPreview()
    {
        previewItemList.Clear();
        string folderPath = GetValidFolderPath(targetFolder);
        if (string.IsNullOrEmpty(folderPath))
        {
            EditorUtility.DisplayDialog("快速配置资源名称", "请拖入有效的 Assets 文件夹", "确定");
            return;
        }

        string suffix = GetSuffix(assetType);
        string[] guidList = AssetDatabase.FindAssets(string.Empty, new[] { folderPath });
        for (int i = 0; i < guidList.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guidList[i]);
            if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
                continue;
            if (!recursiveScan && !IsDirectChild(folderPath, assetPath))
                continue;
            if (!IsAssetMatchType(assetPath, assetType))
                continue;

            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            var item = new RenamePreviewItem
            {
                oldPath = assetPath,
            };

            if (fileName.EndsWith(suffix, StringComparison.Ordinal))
            {
                item.skipReason = "已有正确后缀";
                previewItemList.Add(item);
                continue;
            }

            string newName = fileName + suffix;
            string dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string ext = Path.GetExtension(assetPath);
            string newPath = $"{dir}/{newName}{ext}";
            item.newName = newName;
            item.newPath = newPath;

            if (AssetDatabase.LoadMainAssetAtPath(newPath) != null)
                item.skipReason = "目标路径已存在";

            previewItemList.Add(item);
        }
    }

    /// <summary>
    /// 执行改名
    /// </summary>
    private void ExecuteRename()
    {
        if (previewItemList.Count == 0)
            RefreshPreview();

        int successCount = 0;
        int skipCount = 0;
        int failCount = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < previewItemList.Count; i++)
            {
                RenamePreviewItem item = previewItemList[i];
                if (!string.IsNullOrEmpty(item.skipReason))
                {
                    skipCount++;
                    continue;
                }

                if (AssetDatabase.LoadMainAssetAtPath(item.newPath) != null)
                {
                    failCount++;
                    Debug.LogError($"[QuickAssetRename] 冲突跳过 {item.oldPath} → {item.newPath}");
                    item.skipReason = "目标路径已存在";
                    continue;
                }

                string error = AssetDatabase.RenameAsset(item.oldPath, item.newName);
                if (!string.IsNullOrEmpty(error))
                {
                    failCount++;
                    Debug.LogError($"[QuickAssetRename] 改名失败 {item.oldPath} → {item.newName}  {error}");
                    item.skipReason = error;
                    continue;
                }

                successCount++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog(
            "快速配置资源名称",
            $"完成  成功 {successCount}  跳过 {skipCount}  失败 {failCount}",
            "确定");
        RefreshPreview();
    }

    /// <summary>
    /// 获取有效文件夹路径
    /// </summary>
    private static string GetValidFolderPath(DefaultAsset folder)
    {
        if (folder == null)
            return null;

        string path = AssetDatabase.GetAssetPath(folder);
        if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
            return null;
        return path.Replace('\\', '/');
    }

    /// <summary>
    /// 是否为文件夹下的直接子资源
    /// </summary>
    private static bool IsDirectChild(string folderPath, string assetPath)
    {
        string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        return string.Equals(parent, folderPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 类型对应后缀
    /// </summary>
    private static string GetSuffix(E_QuickAssetRenameType eType)
    {
        switch (eType)
        {
            case E_QuickAssetRenameType.Sprite:
                return "_Sprite";
            case E_QuickAssetRenameType.Prefab:
                return "_Prefab";
            case E_QuickAssetRenameType.AmClip:
                return "_AmClip";
            case E_QuickAssetRenameType.Audio:
                return "_Audio";
            case E_QuickAssetRenameType.AmCc:
                return "_AmCc";
            case E_QuickAssetRenameType.So:
                return "_So";
            case E_QuickAssetRenameType.Mat:
                return "_Mat";
            case E_QuickAssetRenameType.Mesh:
                return "_Mesh";
            case E_QuickAssetRenameType.Tex:
                return "_Tex";
            case E_QuickAssetRenameType.Font:
                return "_Font";
            case E_QuickAssetRenameType.Shader:
                return "_Shader";
            default:
                return string.Empty;
        }
    }

    /// <summary>
    /// 资产是否匹配所选类型
    /// </summary>
    private static bool IsAssetMatchType(string assetPath, E_QuickAssetRenameType eType)
    {
        Type mainType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
        if (mainType == null)
            return false;

        switch (eType)
        {
            case E_QuickAssetRenameType.Sprite:
                if (typeof(Sprite).IsAssignableFrom(mainType))
                    return true;
                return IsSpriteTexture(assetPath, mainType);
            case E_QuickAssetRenameType.Prefab:
                return assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);
            case E_QuickAssetRenameType.AmClip:
                return typeof(AnimationClip).IsAssignableFrom(mainType)
                       && assetPath.EndsWith(".anim", StringComparison.OrdinalIgnoreCase);
            case E_QuickAssetRenameType.Audio:
                return typeof(AudioClip).IsAssignableFrom(mainType);
            case E_QuickAssetRenameType.AmCc:
                return typeof(AnimatorController).IsAssignableFrom(mainType)
                       || typeof(RuntimeAnimatorController).IsAssignableFrom(mainType);
            case E_QuickAssetRenameType.So:
                if (!typeof(ScriptableObject).IsAssignableFrom(mainType))
                    return false;
                if (typeof(AnimatorController).IsAssignableFrom(mainType))
                    return false;
                if (typeof(RuntimeAnimatorController).IsAssignableFrom(mainType))
                    return false;
                if (IsFontAssetType(mainType))
                    return false;
                return true;
            case E_QuickAssetRenameType.Mat:
                return typeof(Material).IsAssignableFrom(mainType);
            case E_QuickAssetRenameType.Mesh:
                return typeof(Mesh).IsAssignableFrom(mainType)
                       && assetPath.EndsWith(".mesh", StringComparison.OrdinalIgnoreCase);
            case E_QuickAssetRenameType.Tex:
                if (!typeof(Texture).IsAssignableFrom(mainType))
                    return false;
                return !IsSpriteTexture(assetPath, mainType);
            case E_QuickAssetRenameType.Font:
                return IsFontAssetType(mainType);
            case E_QuickAssetRenameType.Shader:
                return typeof(Shader).IsAssignableFrom(mainType)
                       || assetPath.EndsWith(".shadergraph", StringComparison.OrdinalIgnoreCase);
            default:
                return false;
        }
    }

    /// <summary>
    /// 是否为 Sprite 导入模式的贴图
    /// </summary>
    private static bool IsSpriteTexture(string assetPath, Type mainType)
    {
        if (!typeof(Texture).IsAssignableFrom(mainType))
            return false;

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return false;
        return importer.textureType == TextureImporterType.Sprite;
    }

    /// <summary>
    /// 是否为字体类资产
    /// </summary>
    private static bool IsFontAssetType(Type mainType)
    {
        if (typeof(Font).IsAssignableFrom(mainType))
            return true;

        // TMP_FontAsset 等 按类型名匹配避免强依赖 TMP 程序集
        string typeName = mainType.Name;
        return typeName == "TMP_FontAsset" || typeName == "FontAsset";
    }
}
}
