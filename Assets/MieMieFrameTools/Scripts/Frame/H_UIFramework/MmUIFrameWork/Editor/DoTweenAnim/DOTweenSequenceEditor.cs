#if UNITY_EDITOR
using DG.DOTweenEditor;
using DG.Tweening;
using MieMieUIFrameWork.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// DOTweenSequence 可视化编辑器 Preset / 内联双模式
/// </summary>
[CanEditMultipleObjects]
[CustomEditor(typeof(DOTweenSequence))]
public class DOTweenSequenceEditor : Editor
{
    /// <summary>
    /// 图标板列数
    /// </summary>
    private const int PaletteColumnCount = 4;

    /// <summary>
    /// 卡片收起高度
    /// </summary>
    private const float CardCollapsedHeight = 28f;

    /// <summary>
    /// 预设引用
    /// </summary>
    private SerializedProperty m_Preset;

    /// <summary>
    /// 序列属性
    /// </summary>
    private SerializedProperty m_Sequence;

    /// <summary>
    /// 播放目标
    /// </summary>
    private SerializedProperty m_DefaultTarget;

    /// <summary>
    /// 启动播放
    /// </summary>
    private SerializedProperty m_PlayOnAwake;

    /// <summary>
    /// 启动重置
    /// </summary>
    private SerializedProperty m_ResetOnAwake;

    /// <summary>
    /// 整段延迟
    /// </summary>
    private SerializedProperty m_Delay;

    /// <summary>
    /// 整段缓动
    /// </summary>
    private SerializedProperty m_Ease;

    /// <summary>
    /// 整段循环
    /// </summary>
    private SerializedProperty m_Loops;

    /// <summary>
    /// 循环方式
    /// </summary>
    private SerializedProperty m_LoopType;

    /// <summary>
    /// 更新模式
    /// </summary>
    private SerializedProperty m_UpdateType;

    /// <summary>
    /// 忽略时间缩放
    /// </summary>
    private SerializedProperty m_IgnoreTimeScale;

    /// <summary>
    /// 整段开始回调
    /// </summary>
    private SerializedProperty m_OnPlay;

    /// <summary>
    /// 整段更新回调
    /// </summary>
    private SerializedProperty m_OnUpdate;

    /// <summary>
    /// 整段完成回调
    /// </summary>
    private SerializedProperty m_OnComplete;

    /// <summary>
    /// 序列列表
    /// </summary>
    private ReorderableList m_SequenceList;

    /// <summary>
    /// 播放按钮
    /// </summary>
    private GUIContent m_PlayBtnContent;

    /// <summary>
    /// 倒带按钮
    /// </summary>
    private GUIContent m_RewindBtnContent;

    /// <summary>
    /// 重置按钮
    /// </summary>
    private GUIContent m_ResetBtnContent;

    /// <summary>
    /// 整段设置是否展开
    /// </summary>
    private bool m_GlobalSettingsFoldout;

    /// <summary>
    /// 整段回调是否展开
    /// </summary>
    private bool m_GlobalCallbackFoldout;

    private void OnEnable()
    {
        m_PlayBtnContent = EditorGUIUtility.TrIconContent("d_PlayButton@2x", "预览播放");
        m_RewindBtnContent = EditorGUIUtility.TrIconContent("d_preAudioAutoPlayOff@2x", "倒带再播");
        m_ResetBtnContent = EditorGUIUtility.TrIconContent("d_preAudioLoopOff@2x", "停止重置");
        m_Preset = serializedObject.FindProperty("m_Preset");
        m_Sequence = serializedObject.FindProperty("m_Sequence");
        m_DefaultTarget = serializedObject.FindProperty("DefaultTarget");
        m_PlayOnAwake = serializedObject.FindProperty("m_PlayOnAwake");
        m_ResetOnAwake = serializedObject.FindProperty("m_ResetOnAwake");
        m_Delay = serializedObject.FindProperty("m_Delay");
        m_Ease = serializedObject.FindProperty("m_Ease");
        m_Loops = serializedObject.FindProperty("m_Loops");
        m_LoopType = serializedObject.FindProperty("m_LoopType");
        m_UpdateType = serializedObject.FindProperty("m_UpdateType");
        m_IgnoreTimeScale = serializedObject.FindProperty("m_IgnoreTimeScale");
        m_OnPlay = serializedObject.FindProperty("m_OnPlay");
        m_OnUpdate = serializedObject.FindProperty("m_OnUpdate");
        m_OnComplete = serializedObject.FindProperty("m_OnComplete");

        m_SequenceList = new ReorderableList(serializedObject, m_Sequence, true, true, true, true);
        m_SequenceList.drawHeaderCallback = OnDrawSequenceHeader;
        m_SequenceList.drawElementCallback = OnDrawSequenceCard;
        m_SequenceList.elementHeightCallback = GetSequenceCardHeight;
        m_SequenceList.onAddCallback = list => AddSequenceItem(DOTweenType.DOScale);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPreviewToolbar();
        EditorGUILayout.Space(4f);

        EditorGUILayout.PropertyField(m_Preset, new GUIContent("动画预设", "拖入 SO 配方 优先于内联序列"));
        EditorGUILayout.PropertyField(m_DefaultTarget, new GUIContent("播放目标", "预设步骤无 Target 时使用 通常拖本物体组件"));

        bool usePreset = m_Preset.objectReferenceValue != null;
        if (usePreset)
        {
            DrawPresetMode();
        }
        else
        {
            DrawInlineMode();
        }

        EditorGUILayout.Space(4f);
        DrawRuntimeCallbacks();
        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 预设模式 UI
    /// </summary>
    private void DrawPresetMode()
    {
        var preset = m_Preset.objectReferenceValue as DOTweenSequencePreset;
        EditorGUILayout.HelpBox("使用预设中 序列只读 改效果请复制到内联或编辑 SO 资源", MessageType.Info);

        if (preset != null && preset.Sequence != null)
        {
            EditorGUILayout.LabelField($"步骤数 {preset.Sequence.Length}  整段延迟 {preset.Delay}s  循环 {preset.Loops}", EditorStyles.miniLabel);
            for (int i = 0; i < preset.Sequence.Length; i++)
            {
                var item = preset.Sequence[i];
                string mark = DOTweenTypeEditorUtil.GetAddTypeMark(item.AddType);
                string label = DOTweenTypeEditorUtil.GetLabel(item.AnimationType);
                EditorGUILayout.LabelField($"  {mark} {label}  {item.DurationOrSpeed:0.##}s", EditorStyles.miniLabel);
            }
        }

        if (GUILayout.Button("复制预设到内联并解除预设引用"))
        {
            Undo.RecordObject(target, "Apply DOTween Preset To Inline");
            (target as DOTweenSequence).EditorApplyPresetToInline();
            EditorUtility.SetDirty(target);
            serializedObject.Update();
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(m_PlayOnAwake, new GUIContent("Awake 播放"));
        EditorGUILayout.PropertyField(m_ResetOnAwake, new GUIContent("Awake 重置"));
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 内联模式 UI
    /// </summary>
    private void DrawInlineMode()
    {
        DrawGlobalSettings();
        EditorGUILayout.Space(6f);
        m_SequenceList.DoLayoutList();
        if (m_Sequence.arraySize == 0)
        {
            EditorGUILayout.HelpBox("序列为空 指定播放目标后点下方图标添加 或拖入动画预设", MessageType.Info);
        }

        EditorGUILayout.Space(6f);
        DrawTypePalette();

        EditorGUILayout.Space(6f);
        EditorGUI.BeginDisabledGroup(m_Sequence.arraySize == 0);
        if (GUILayout.Button("保存当前效果为预设…"))
        {
            SaveInlineAsPreset();
        }

        EditorGUI.EndDisabledGroup();
    }

    /// <summary>
    /// 将内联序列保存为 SO 预设
    /// </summary>
    private void SaveInlineAsPreset()
    {
        var seq = target as DOTweenSequence;
        if (seq == null || m_Sequence.arraySize == 0)
        {
            EditorUtility.DisplayDialog("无法保存", "内联序列为空", "确定");
            return;
        }

        string defaultDir = MieMieUITools.Editor.PackagePaths.DoTweenPresetsRoot;
        if (!AssetDatabase.IsValidFolder(defaultDir))
        {
            defaultDir = "Assets";
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "保存 DOTween 预设",
            "UI_CustomPreset",
            "asset",
            "选择保存位置",
            defaultDir);
        if (string.IsNullOrEmpty(path)) return;

        var preset = seq.EditorCreatePresetAssetInstance();
        var existing = AssetDatabase.LoadAssetAtPath<DOTweenSequencePreset>(path);
        if (existing != null)
        {
            existing.EditorSetData(
                preset.CloneSequence(),
                preset.Delay,
                preset.Ease,
                preset.Loops,
                preset.LoopType,
                preset.UpdateType,
                preset.IgnoreTimeScale);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(preset);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(existing);

            bool bindExisting = EditorUtility.DisplayDialog(
                "已覆盖",
                "预设已覆盖保存 是否让当前组件改用这份预设",
                "使用预设",
                "仅保存");
            if (bindExisting)
            {
                Undo.RecordObject(seq, "Bind Saved DOTween Preset");
                seq.EditorSetPreset(existing);
                EditorUtility.SetDirty(seq);
                serializedObject.Update();
            }

            return;
        }

        AssetDatabase.CreateAsset(preset, path);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(preset);

        bool bind = EditorUtility.DisplayDialog(
            "保存成功",
            "预设已保存 步骤里的具体 Target 已剥离 播放时用「播放目标」\n是否让当前组件改用这份预设",
            "使用预设",
            "仅保存");
        if (bind)
        {
            Undo.RecordObject(seq, "Bind Saved DOTween Preset");
            seq.EditorSetPreset(preset);
            EditorUtility.SetDirty(seq);
            serializedObject.Update();
        }
    }

    /// <summary>
    /// 运行时回调 两种模式都显示
    /// </summary>
    private void DrawRuntimeCallbacks()
    {
        m_GlobalCallbackFoldout = EditorGUILayout.Foldout(m_GlobalCallbackFoldout, "整段回调 挂在播放器上", true);
        if (!m_GlobalCallbackFoldout) return;
        EditorGUILayout.PropertyField(m_OnPlay);
        EditorGUILayout.PropertyField(m_OnUpdate);
        EditorGUILayout.PropertyField(m_OnComplete);
    }

    /// <summary>
    /// 预览工具条
    /// </summary>
    private void DrawPreviewToolbar()
    {
        if (EditorApplication.isPlaying) return;

        EditorGUILayout.BeginHorizontal();
        float btnHeight = 32f;
        if (GUILayout.Button(m_PlayBtnContent, GUILayout.Height(btnHeight), GUILayout.Width(42f)))
        {
            PlayPreview(false);
        }

        if (GUILayout.Button(m_RewindBtnContent, GUILayout.Height(btnHeight), GUILayout.Width(42f)))
        {
            PlayPreview(true);
        }

        if (GUILayout.Button(m_ResetBtnContent, GUILayout.Height(btnHeight), GUILayout.Width(42f)))
        {
            DOTweenEditorPreview.Stop(true, true);
            (target as DOTweenSequence).DOKill();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 编辑器预览播放
    /// </summary>
    private void PlayPreview(bool rewindFirst)
    {
        var seq = target as DOTweenSequence;
        var activeList = seq.GetActiveSequence();
        if (activeList == null || activeList.Length == 0)
        {
            EditorUtility.DisplayDialog("无法预览", "没有可播放的序列 请挂预设或添加内联步骤", "确定");
            return;
        }

        if (m_DefaultTarget.objectReferenceValue == null)
        {
            bool hasExplicit = false;
            for (int i = 0; i < activeList.Length; i++)
            {
                if (activeList[i].Target != null)
                {
                    hasExplicit = true;
                    break;
                }
            }

            if (!hasExplicit)
            {
                EditorUtility.DisplayDialog("无法预览", "请指定播放目标", "确定");
                return;
            }
        }

        if (DOTweenEditorPreview.isPreviewing)
        {
            DOTweenEditorPreview.Stop(true, true);
            seq.DOKill();
        }

        if (rewindFirst) seq.DORewind();
        var tween = seq.DOPlay();
        if (tween == null) return;
        DOTweenEditorPreview.PrepareTweenForPreview(tween, true, true, false);
        DOTweenEditorPreview.Start(null);
    }

    /// <summary>
    /// 整段设置折叠区 仅内联
    /// </summary>
    private void DrawGlobalSettings()
    {
        m_GlobalSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
            m_GlobalSettingsFoldout,
            "整段设置 · 循环 缓动");
        if (m_GlobalSettingsFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(m_PlayOnAwake, new GUIContent("Awake 播放"));
            EditorGUILayout.PropertyField(m_ResetOnAwake, new GUIContent("Awake 重置"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(m_Delay, new GUIContent("整段延迟"));
            EditorGUILayout.PropertyField(m_Ease, new GUIContent("整段缓动"));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(m_Loops, new GUIContent("整段循环"));
            EditorGUI.BeginDisabledGroup(m_Loops.intValue == 1);
            EditorGUILayout.PropertyField(m_LoopType, GUIContent.none);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(m_UpdateType, new GUIContent("更新模式"));
            EditorGUILayout.PropertyField(m_IgnoreTimeScale, new GUIContent("忽略 TimeScale"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    /// <summary>
    /// 序列表头
    /// </summary>
    private void OnDrawSequenceHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, "动画序列 · 卡片展开可改单步参数");
    }

    /// <summary>
    /// 卡片高度
    /// </summary>
    private float GetSequenceCardHeight(int index)
    {
        if (index < 0 || index >= m_Sequence.arraySize) return CardCollapsedHeight;
        var item = m_Sequence.GetArrayElementAtIndex(index);
        if (!item.isExpanded) return CardCollapsedHeight;
        return EditorGUI.GetPropertyHeight(item, true) + CardCollapsedHeight + 4f;
    }

    /// <summary>
    /// 绘制序列卡片
    /// </summary>
    private void OnDrawSequenceCard(Rect rect, int index, bool isActive, bool isFocused)
    {
        if (index < 0 || index >= m_Sequence.arraySize) return;
        var item = m_Sequence.GetArrayElementAtIndex(index);
        var addType = item.FindPropertyRelative("AddType");
        var tweenType = item.FindPropertyRelative("AnimationType");
        var targetProp = item.FindPropertyRelative("Target");
        var duration = item.FindPropertyRelative("DurationOrSpeed");

        var eType = (DOTweenType)tweenType.enumValueIndex;
        var eAddType = (AddType)addType.enumValueIndex;

        float line = EditorGUIUtility.singleLineHeight;
        var headerRect = new Rect(rect.x, rect.y + 2f, rect.width, line);

        var foldRect = new Rect(headerRect.x, headerRect.y, 14f, line);
        item.isExpanded = EditorGUI.Foldout(foldRect, item.isExpanded, GUIContent.none, true);

        var markRect = new Rect(foldRect.xMax + 2f, headerRect.y, 18f, line);
        var markStyle = new GUIStyle(EditorStyles.boldLabel);
        markStyle.alignment = TextAnchor.MiddleCenter;
        markStyle.normal.textColor = eAddType == AddType.Join
            ? new Color(0.35f, 0.7f, 1f)
            : new Color(0.9f, 0.75f, 0.2f);
        EditorGUI.LabelField(markRect, DOTweenTypeEditorUtil.GetAddTypeMark(eAddType), markStyle);

        var iconRect = new Rect(markRect.xMax + 2f, headerRect.y, 18f, line);
        GUI.Label(iconRect, DOTweenTypeEditorUtil.GetIconContent(eType));

        var typeRect = new Rect(iconRect.xMax + 4f, headerRect.y, 72f, line);
        EditorGUI.LabelField(typeRect, DOTweenTypeEditorUtil.GetLabel(eType), EditorStyles.boldLabel);

        string targetLabel;
        Color targetColor;
        if (targetProp.objectReferenceValue != null)
        {
            targetLabel = targetProp.objectReferenceValue.name;
            targetColor = new Color(0.4f, 0.85f, 0.5f);
        }
        else
        {
            targetLabel = "请指定 Target";
            targetColor = new Color(1f, 0.45f, 0.35f);
        }

        var targetRect = new Rect(typeRect.xMax + 4f, headerRect.y, headerRect.xMax - typeRect.xMax - 56f, line);
        var targetStyle = new GUIStyle(EditorStyles.miniLabel);
        targetStyle.normal.textColor = targetColor;
        EditorGUI.LabelField(targetRect, targetLabel, targetStyle);

        var durRect = new Rect(headerRect.xMax - 48f, headerRect.y, 48f, line);
        EditorGUI.LabelField(durRect, $"{duration.floatValue:0.##}s", EditorStyles.miniLabel);

        if (!item.isExpanded) return;

        var bodyRect = new Rect(rect.x, headerRect.yMax + 2f, rect.width, rect.height - CardCollapsedHeight);
        EditorGUI.PropertyField(bodyRect, item, GUIContent.none, true);
    }

    /// <summary>
    /// 类型图标板
    /// </summary>
    private void DrawTypePalette()
    {
        EditorGUILayout.LabelField("添加动画 · 点击写入当前播放目标", EditorStyles.boldLabel);
        var itemList = DOTweenTypeEditorUtil.PaletteItemList;
        int rowCount = Mathf.CeilToInt(itemList.Length / (float)PaletteColumnCount);
        for (int row = 0; row < rowCount; row++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < PaletteColumnCount; col++)
            {
                int index = row * PaletteColumnCount + col;
                if (index >= itemList.Length)
                {
                    GUILayout.FlexibleSpace();
                    continue;
                }

                var paletteItem = itemList[index];
                var icon = EditorGUIUtility.IconContent(paletteItem.Icon);
                if (icon == null || icon.image == null)
                    icon = EditorGUIUtility.IconContent("Animation.Play");

                var btnContent = new GUIContent(paletteItem.Label, icon.image, DOTweenTypeEditorUtil.GetLabel(paletteItem.Type));
                if (GUILayout.Button(btnContent, GUILayout.Height(36f)))
                {
                    AddSequenceItem(paletteItem.Type);
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 追加序列条目
    /// </summary>
    private void AddSequenceItem(DOTweenType eType)
    {
        if (m_DefaultTarget.objectReferenceValue == null)
        {
            EditorUtility.DisplayDialog("缺少播放目标", "请先指定「播放目标」再添加动画", "确定");
            return;
        }

        int index = m_Sequence.arraySize;
        m_Sequence.arraySize++;
        var item = m_Sequence.GetArrayElementAtIndex(index);
        item.isExpanded = true;
        item.FindPropertyRelative("AddType").enumValueIndex = (int)AddType.Append;
        item.FindPropertyRelative("AnimationType").enumValueIndex = (int)eType;

        var sourceTarget = m_DefaultTarget.objectReferenceValue as Component;
        var fixedTarget = DOTweenSequence.FixComponentForType(sourceTarget, eType);
        item.FindPropertyRelative("Target").objectReferenceValue = fixedTarget != null ? fixedTarget : sourceTarget;

        item.FindPropertyRelative("ToValue").vector4Value = DOTweenTypeEditorUtil.GetDefaultToValue(eType);
        item.FindPropertyRelative("UseToTarget").boolValue = false;
        item.FindPropertyRelative("ToTarget").objectReferenceValue = null;
        item.FindPropertyRelative("UseFromValue").boolValue = false;
        item.FindPropertyRelative("FromValue").vector4Value = Vector4.zero;
        item.FindPropertyRelative("SpeedBased").boolValue = false;
        item.FindPropertyRelative("DurationOrSpeed").floatValue = 0.3f;
        item.FindPropertyRelative("Delay").floatValue = 0f;
        item.FindPropertyRelative("UpdateType").enumValueIndex = (int)UpdateType.Normal;
        item.FindPropertyRelative("CustomEase").boolValue = false;
        item.FindPropertyRelative("Ease").enumValueIndex = (int)Ease.OutQuad;
        item.FindPropertyRelative("Loops").intValue = 1;
        item.FindPropertyRelative("LoopType").enumValueIndex = (int)LoopType.Restart;
        item.FindPropertyRelative("Snapping").boolValue = false;
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
