using System;
using System.Collections.Generic;
using MieMieFrameWork;
using MieMieFrameWork.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI模板生成器编辑器窗口
/// 用于快速生成UI预制体对应的脚本模板
/// </summary>
public class GenerateUITemp : EditorWindow
{
    /// <summary>
    /// UXML可视化树模板
    /// </summary>
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    /// <summary>
    /// 根可视化元素
    /// </summary>
    private VisualElement root;

    // BaseInfoElement 基础信息区控件
    private TextField classNameField;
    private UnityEditor.UIElements.ObjectField prefabFieldGameObject;
    private TextField checkPathField;
    private Button selectButton;
    private Button defaultPathButton;

    // 映射表区域控件
    private ScrollView mappingScrollView;
    private Button addMappingButton;
    private Button resetMappingButton;
    private Button setNewDefaultButton;
    private Button copyMappingButton;
    private Button saveMappingButton;
    private Button creatButton;

    // 生成脚本相关
    private string className;
    private GameObject prefab;

    /// <summary>
    /// 编辑器菜单入口
    /// </summary>
    [MenuItem("Tools/UI/GenerateUITemp")]
    public static void ShowExample()
    {
        GenerateUITemp wnd = GetWindow<GenerateUITemp>();
        wnd.titleContent = new GUIContent("GenerateUITemp");
    }

    /// <summary>
    /// 创建编辑器GUI
    /// </summary>
    public void CreateGUI()
    {
        root = rootVisualElement;
        if (m_VisualTreeAsset == null)
        {
            var config = AssetDatabase.LoadAssetAtPath<UIPathConfig>(
                "Assets/MieMieFrameTools/FrameSettings/UIPathConfig.asset");
            string uxmlPath = config?.GetGenerateUITempUxmlPath()
                ?? "Assets/MieMieFrameTools/Editor/UIForEditor/GenerateUITemp.uxml";

            m_VisualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (m_VisualTreeAsset == null)
            {
                Debug.LogError($"GenerateUITemp.uxml 路径不正确: {uxmlPath}");
                return;
            }
        }
        root.Add(m_VisualTreeAsset.CloneTree());

        InitBaseInfoElement();
        InitMappingSection();
    }

    #region 基础信息栏

    private void InitBaseInfoElement()
    {
        className = string.Empty;
        prefab = null;

        classNameField = root.Q<TextField>("ClassName");
        prefabFieldGameObject = root.Q<UnityEditor.UIElements.ObjectField>("PrefabGameObject");
        checkPathField = root.Q<TextField>("CheckPath");
        selectButton = root.Q<Button>("SelectButton");
        defaultPathButton = root.Q<Button>("DefaultPahtButton");

        prefabFieldGameObject.RegisterValueChangedCallback(OnPrefabFieldGameObjectChanged);
        defaultPathButton.RegisterCallback<ClickEvent>(OnDefaultPathButtonClicked);
        selectButton.RegisterCallback<ClickEvent>(OnSelectButtonClicked);
    }

    private void OnPrefabFieldGameObjectChanged(ChangeEvent<UnityEngine.Object> evt)
    {
        if (evt.newValue == null)
        {
            if (classNameField != null)
                classNameField.value = default(string);
            return;
        }

        if (classNameField != null)
            className = classNameField.value = evt.newValue.name;
        else
            Debug.LogError("classNameField 为空");

        prefab = evt.newValue as GameObject;

        if (prefab != null)
        {
            string lastPath = GetRecordedPathForPrefab(prefab);
            if (!string.IsNullOrEmpty(lastPath))
            {
                checkPathField.value = lastPath;
                Debug.Log($"[GenerateUITemp] 已自动填入上次生成路径: {prefab.name} -> {lastPath}");
            }
        }
    }

    private void OnDefaultPathButtonClicked(ClickEvent evt)
    {
        checkPathField.value = GenerateUITool.GetDefaultUIGenScriptPath();
    }

    private void OnSelectButtonClicked(ClickEvent evt)
    {
        string path = EditorUtility.OpenFolderPanel("选择生成文件夹", checkPathField.value, "");
        if (!string.IsNullOrEmpty(path))
        {
            checkPathField.value = path;
            if (prefab != null)
            {
                RecordPrefabPath(prefab, path);
            }
        }
    }

    private string GetRecordedPathForPrefab(GameObject prefab)
    {
        if (prefab == null) return null;
        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(prefabPath)) return null;
        string guid = AssetDatabase.AssetPathToGUID(prefabPath);
        var config = AssetDatabase.LoadAssetAtPath<UIPathConfig>(
            "Assets/MieMieFrameTools/FrameSettings/UIPathConfig.asset");
        return config?.GetLastGenScriptPath(guid);
    }

    private void RecordPrefabPath(GameObject prefab, string generatePath)
    {
        if (prefab == null || string.IsNullOrEmpty(generatePath)) return;
        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(prefabPath)) return;
        string guid = AssetDatabase.AssetPathToGUID(prefabPath);

        string configPath = "Assets/MieMieFrameTools/FrameSettings/UIPathConfig.asset";
        var config = AssetDatabase.LoadAssetAtPath<UIPathConfig>(configPath);

        if (config == null)
        {
            if (!System.IO.Directory.Exists("Assets/MieMieFrameTools/FrameSettings"))
                System.IO.Directory.CreateDirectory("Assets/MieMieFrameTools/FrameSettings");

            config = ScriptableObject.CreateInstance<UIPathConfig>();
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GenerateUITemp] 自动创建 UIPathConfig: {configPath}");
        }

        config.SetGenScriptPath(guid, prefab.name, generatePath);
    }

    #endregion

    #region 映射表区域

    private void InitMappingSection()
    {
        mappingScrollView = root.Q<ScrollView>("MappingScrollView");
        addMappingButton = root.Q<Button>("AddMappingButton");
        resetMappingButton = root.Q<Button>("ResetMappingButton");
        setNewDefaultButton = root.Q<Button>("SetNewDefaultButton");
        copyMappingButton = root.Q<Button>("CopyMappingButton");
        saveMappingButton = root.Q<Button>("SaveMappingButton");
        creatButton = root.Q<Button>("CreatButton");

        addMappingButton.RegisterCallback<ClickEvent>(OnAddMappingClicked);
        resetMappingButton.RegisterCallback<ClickEvent>(OnResetMappingClicked);
        setNewDefaultButton.RegisterCallback<ClickEvent>(OnSetNewDefaultClicked);
        copyMappingButton.RegisterCallback<ClickEvent>(OnCopyMappingClicked);
        saveMappingButton.RegisterCallback<ClickEvent>(OnSaveMappingClicked);
        creatButton.RegisterCallback<ClickEvent>(OnCreatButtonClicked);

        RefreshMappingTable(); 
    } 

    private void RefreshMappingTable()
    {
        if (mappingScrollView == null) return;

        mappingScrollView.Clear();
        var config = GetFrameSetting();
        if (config == null || config.PrefixToComponentTypeMap == null) return;

        foreach (var pair in config.PrefixToComponentTypeMap)
        {
            AddMappingRow(pair.Prefix, pair.ComponentType);
        }
    }

    private void AddMappingRow(string prefixValue, string typeValue)
    {
        var row = new VisualElement();
        row.AddToClassList("mapping-row");

        var prefixField = new TextField();
        prefixField.AddToClassList("mapping-prefix-field");
        prefixField.value = prefixValue ?? string.Empty;
        prefixField.RegisterValueChangedCallback(_ => MarkMappingDirty());

        var arrowLabel = new Label("→");
        arrowLabel.AddToClassList("mapping-arrow");

        var typeField = new TextField();
        typeField.AddToClassList("mapping-type-field");
        typeField.value = typeValue ?? string.Empty;
        typeField.RegisterValueChangedCallback(_ => MarkMappingDirty());

        var deleteBtn = new Button(() => OnDeleteMappingRow(row));
        deleteBtn.AddToClassList("mapping-delete-btn");
        deleteBtn.text = "删除";

        row.Add(prefixField);
        row.Add(arrowLabel);
        row.Add(typeField);
        row.Add(deleteBtn);
        mappingScrollView.Add(row);
    }

    private void MarkMappingDirty()
    {
        saveMappingButton.style.backgroundColor = new Color(0.3f, 0.8f, 0.3f, 0.5f);
    }

    private void OnAddMappingClicked(ClickEvent evt)
    {
        AddMappingRow("NewPrefix", "ComponentType");
        MarkMappingDirty();
    }

    private void OnDeleteMappingRow(VisualElement row)
    {
        mappingScrollView.Remove(row);
        MarkMappingDirty();
    }

    private void OnResetMappingClicked(ClickEvent evt)
    {
        var config = GetFrameSetting();
        if (config == null) return;

        if (!EditorUtility.DisplayDialog("确认重置", "确定要将映射表重置为默认？", "确定", "取消"))
            return;

        config.PrefixToComponentTypeMap.Clear();
        foreach (var kvp in GenerateUITool.DefaultPrefixToTypeMap)
        {
            config.PrefixToComponentTypeMap.Add(new PrefixComponentPair { Prefix = kvp.Key, ComponentType = kvp.Value });
        }

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        GenerateUITool.RefreshPrefixToTypeMap();
        RefreshMappingTable();
        EditorUtility.DisplayDialog("提示", "映射表已重置为默认", "确定");
    }

    private void OnSetNewDefaultClicked(ClickEvent evt)
    {
        if (!EditorUtility.DisplayDialog("确认", "设置当前映射表为新默认？", "确定", "取消"))
            return;

        var config = GetFrameSetting();
        if (config == null) return;

        // 先保存当前UI数据到config
        SaveMappingToConfig(config);
        EditorUtility.DisplayDialog("提示", "已将当前映射设为新默认", "确定");
    }

    private void OnCopyMappingClicked(ClickEvent evt)
    {
        var config = GetFrameSetting();
        if (config == null) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var pair in config.PrefixToComponentTypeMap)
        {
            sb.AppendLine($"{pair.Prefix} -> {pair.ComponentType}");
        }

        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        EditorUtility.DisplayDialog("提示", "映射表已复制到剪切板", "确定");
    }

    private void OnSaveMappingClicked(ClickEvent evt)
    {
        var config = GetFrameSetting();
        if (config == null) return;

        SaveMappingToConfig(config);
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        GenerateUITool.RefreshPrefixToTypeMap();

        saveMappingButton.style.backgroundColor = Color.clear;
        EditorUtility.DisplayDialog("提示", "映射表已保存", "确定");
    }

    private void SaveMappingToConfig(FrameSetting config)
    {
        config.PrefixToComponentTypeMap.Clear();

        foreach (var child in mappingScrollView.Children())
        {
            // 必须用 className 查询；Q(".xxx") 会把字符串当成 name，永远匹配不到，导致保存成空表
            var prefixField = child.Q<TextField>(className: "mapping-prefix-field");
            var typeField = child.Q<TextField>(className: "mapping-type-field");

            if (prefixField == null || typeField == null) continue;

            string prefix = prefixField.value.Trim();
            string type = typeField.value.Trim();

            if (string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(type)) continue;

            config.PrefixToComponentTypeMap.Add(new PrefixComponentPair { Prefix = prefix, ComponentType = type });
        }
    }

    private void OnCreatButtonClicked(ClickEvent evt)
    {
        if (string.IsNullOrEmpty(className))
        {
            EditorUtility.DisplayDialog("错误", "请输入类名", "确定");
            return;
        }

        if (prefab == null)
        {
            EditorUtility.DisplayDialog("错误", "请拖入UI预制体", "确定");
            return;
        }

        if (string.IsNullOrEmpty(checkPathField.value))
        {
            EditorUtility.DisplayDialog("错误", "请选择生成路径", "确定");
            return;
        }

        // 生成前先刷新映射表（确保最新）
        var config = GetFrameSetting();
        if (config != null)
        {
            SaveMappingToConfig(config);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            GenerateUITool.RefreshPrefixToTypeMap();
        }

        RecordPrefabPath(prefab, checkPathField.value);

        GenerateUITool.GenerateUITemplates(
            className,
            prefab,
            checkPathField.value
        );
    }

    private static FrameSetting GetFrameSetting()
    {
        return AssetDatabase.LoadAssetAtPath<FrameSetting>(
            "Assets/MieMieFrameTools/FrameSettings/FrameSetting.asset");
    }

    #endregion
}
