using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;


namespace MieMieFrameWork.Asset
{
public enum E_Platform
{
    Windows,
    MacOS,
    Linux,
    Android,
    iOS,
}
[Serializable]
public class BundleBehaviour
{
    /// <summary>
    /// 当前活跃平台
    /// </summary>
    protected E_Platform CurrentPlatform => (E_BuildTarget)Enum.Parse(typeof(E_BuildTarget), BundleSettings.Instance.buildTarget.ToString()) switch
    {
        E_BuildTarget.Android => E_Platform.Android,
        E_BuildTarget.iOS => E_Platform.iOS,
        E_BuildTarget.StandaloneOSX => E_Platform.MacOS,
        E_BuildTarget.StandaloneLinux => E_Platform.Linux,
        E_BuildTarget.StandaloneWindows64 => E_Platform.Windows,
        _ => E_Platform.Windows,
    };

    /// <summary>
    /// 配置列表
    /// </summary>
    protected List<BundleModuleData> moduleDataList = new();

    private const int ModulesPerRow = 6;

    public virtual void Init()
    {
        RefreshModuleList();
    }

    /// <summary>
    /// 从配置资产同步模块列表
    /// </summary>
    protected void RefreshModuleList()
    {
        var config = BuildBundleConfigura.Instance;
        moduleDataList = config != null
            ? config.bundleModuleDataList
            : new List<BundleModuleData>();
    }

    [OnInspectorGUI]
    public virtual void OnGUI()
    {
        // 枚举重生会触发域重载 列表字段不序列化 每次绘制前从资产同步
        RefreshModuleList();
        DrawToolbar();

        GUILayout.Space(10);

        if (moduleDataList == null || moduleDataList.Count == 0)
        {
            EditorGUILayout.HelpBox("暂无模块，点击上方「添加模块」创建。", MessageType.Info);
            return;
        }

        DrawToggles();
    }

    #region 工具栏
    /// <summary>
    /// 绘制工具栏
    /// </summary>
    protected virtual void DrawToolbar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        {
            DrawAddMoudleButton();
            GUILayout.Space(4);
            DrawSelectButtons();

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField(
                new GUIContent($"当前平台: {CurrentPlatform}", EditorUserBuildSettings.activeBuildTarget.ToString()),
                EditorStyles.boldLabel,
                GUILayout.Height(28));

            GUILayout.FlexibleSpace();

            DrawLeftButton();
            GUILayout.Space(8);
            DrawRightButton();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 全选 / 反选
    /// </summary>
    protected virtual void DrawSelectButtons()
    {
        if (GUILayout.Button("全选", GUILayout.Height(28), GUILayout.MinWidth(52)))
            SetAllModulesSelected(true);

        if (GUILayout.Button("反选", GUILayout.Height(28), GUILayout.MinWidth(52)))
            InvertModuleSelection();
    }

    /// <summary>
    /// 全选模块
    /// </summary>
    /// <param name="selected"></param>
    protected void SetAllModulesSelected(bool selected)
    {
        if (moduleDataList == null || moduleDataList.Count == 0)
            return;

        foreach (var moduleData in moduleDataList)
            moduleData.isBuild = selected;

        EditorUtility.SetDirty(BuildBundleConfigura.Instance);
    }

    /// <summary>
    /// 反选模块
    /// </summary>
    protected void InvertModuleSelection()
    {
        if (moduleDataList == null || moduleDataList.Count == 0)
            return;

        foreach (var moduleData in moduleDataList)
            moduleData.isBuild = !moduleData.isBuild;

        EditorUtility.SetDirty(BuildBundleConfigura.Instance);
    }

    #endregion

    #region 模块绘制与点击响应

    /// <summary>
    /// 模块绘制与点击响应
    /// </summary>
    public virtual void DrawToggles()
    {
        for (int i = 0; i < moduleDataList.Count; i++)
        {
            if (i % ModulesPerRow == 0)
                GUILayout.BeginHorizontal();

            DrawModuleToggle(moduleDataList[i]);

            if (i % ModulesPerRow == ModulesPerRow - 1 || i == moduleDataList.Count - 1)
                GUILayout.EndHorizontal();
        }
    }

    private void DrawModuleToggle(BundleModuleData moduleData)
    {
        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = moduleData.isBuild
            ? new Color(0.65f, 0.85f, 1f)
            : prevBg;

        Rect rect = GUILayoutUtility.GetRect(140, 104, GUILayout.Width(140), GUILayout.Height(104));
        GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

        bool isDoubleClick = Event.current.type == EventType.MouseDown && Event.current.clickCount == 2
            && rect.Contains(Event.current.mousePosition);

        if (isDoubleClick)
        {
            moduleData.isBuild = true;
            EditorUtility.SetDirty(BuildBundleConfigura.Instance);
            OnModuleDoubleClick(moduleData);
            Event.current.Use();
        }

        var toggleRect = new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 24f);
        bool selected = GUI.Toggle(toggleRect, moduleData.isBuild, moduleData.moduleName, EditorStyles.miniButton);
        var modeRect = new Rect(rect.x + 10f, rect.y + 40f, rect.width - 20f, 18f);
        GUI.Label(modeRect, "交付  " + moduleData.deliveryMode, EditorStyles.miniLabel);
        var sharedRect = new Rect(rect.x + 10f, rect.y + 62f, rect.width - 20f, 18f);
        GUI.Label(
            sharedRect,
            moduleData.autoExtractSharedDependencies
                ? "共享依赖  自动"
                : "共享依赖  关闭",
            EditorStyles.miniLabel);
        var hintRect = new Rect(rect.x + 10f, rect.y + 82f, rect.width - 20f, 16f);
        GUI.Label(hintRect, "双击编辑", EditorStyles.centeredGreyMiniLabel);
        GUI.backgroundColor = prevBg;

        if (!isDoubleClick && selected != moduleData.isBuild)
        {
            moduleData.isBuild = selected;
            EditorUtility.SetDirty(BuildBundleConfigura.Instance);
        }
    }

    /// <summary>
    /// 模块双击回调
    /// </summary>
    protected virtual void OnModuleDoubleClick(BundleModuleData moduleData)
    {
        BundleModuleConfigWindow.ShowWindow(moduleData.moduleName);
    }
    #endregion

    #region 父类默认行为
    /// <summary>
    /// 左侧按钮
    /// </summary>
    public virtual void DrawLeftButton(){}

    /// <summary>
    /// 右侧按钮
    /// </summary>
    public virtual void DrawRightButton(){}

    /// <summary>
    /// 添加模块按钮
    /// </summary>
    public virtual void DrawAddMoudleButton()
    {
        if (GUILayout.Button("+ 添加模块", GUILayout.Height(28), GUILayout.MinWidth(108)))
        {
            AddMoudle();
        }
    }

    /// <summary>
    /// 添加模块
    /// </summary>
    public virtual void AddMoudle()
    {
        BundleModuleConfigWindow.ShowWindow("新模块", isCreate: true);
    }

    /// <summary>
    /// 自检未通过时弹出含模块名的错误对话框
    /// </summary>
    protected bool EnsureDiagnosticsPassed()
    {
        var errorList = new List<string>();
        var warningList = new List<string>();
        if (MmAssetDiagnostics.ValidateProject(errorList, warningList))
            return true;
        EditorUtility.DisplayDialog(
            "MmAsset",
            MmAssetDiagnostics.FormatFailDialog(errorList, warningList),
            "确定");
        return false;
    }

    /// <summary>
    /// 收集当前勾选模块名称
    /// </summary>
    protected List<string> CollectSelectedModuleNameList()
    {
        var nameList = new List<string>();
        if (moduleDataList == null)
            return nameList;
        foreach (var moduleData in moduleDataList)
        {
            if (moduleData.isBuild)
                nameList.Add(moduleData.moduleName);
        }
        return nameList;
    }

    #endregion

}

}
