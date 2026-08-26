#if UNITY_EDITOR
using MieMieUIFrameWork.Runtime;
using MieMieUIFrameWork.UI.FloatingText;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 跳字资源编辑器 Font/TMP 方案分离 数字英文中文分槽
/// </summary>
public class FloatingTextEditorWindow : EditorWindow
{
    private FloatingTextAtlasBaker.EBakeSourceMode m_SourceMode =
        FloatingTextAtlasBaker.EBakeSourceMode.TmpFontAsset;

    private Font m_DigitFont;
    private Font m_EnglishFont;
    private Font m_ChineseFont;

    private TMP_FontAsset m_DigitTmpFont;
    private TMP_FontAsset m_EnglishTmpFont;
    private TMP_FontAsset m_ChineseTmpFont;

    private int m_FontSize = 48;
    private int m_CellSize = 64;
    private int m_Columns = 16;
    private string m_Charset = FloatingTextAtlasBaker.DefaultCharset;
    private Texture2D m_CritOverride;
    private bool m_RefreshPrefab = true;
    private Vector2 m_Scroll;
    private string m_LastMessage = string.Empty;

    [MenuItem("Tools/MieMieUIFrameWork/FloatingEditor")]
    public static void Open()
    {
        var wnd = GetWindow<FloatingTextEditorWindow>();
        wnd.titleContent = new GUIContent("FloatingEditor");
        wnd.minSize = new Vector2(480f, 560f);
        wnd.Show();
    }

    private void OnGUI()
    {
        m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
        EditorGUILayout.LabelField("跳字 FloatingEditor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1 先选烘焙方案 Font 或 TMP 只启用对应槽位\n" +
            "2 数字/英文/中文严格分槽 不会拿中文字库里的数字去顶数字槽\n" +
            "3 字符按规则分流 0-9与+-.%*→数字 A-Z→英文 其余→中文\n" +
            "4 运行时拖 FloatingTextManager 预制体到场景即可\n" +
            "5 业务调用 FloatingTextManager.Instance.Play / Show",
            MessageType.Info);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("烘焙方案", EditorStyles.boldLabel);
        m_SourceMode = (FloatingTextAtlasBaker.EBakeSourceMode)EditorGUILayout.EnumPopup(
            "方案",
            m_SourceMode);

        EditorGUILayout.Space(6f);
        if (m_SourceMode == FloatingTextAtlasBaker.EBakeSourceMode.UnityFont)
        {
            EditorGUILayout.LabelField("Unity Font 三槽", EditorStyles.boldLabel);
            m_DigitFont = (Font)EditorGUILayout.ObjectField("数字 Font", m_DigitFont, typeof(Font), false);
            m_EnglishFont = (Font)EditorGUILayout.ObjectField("英文 Font", m_EnglishFont, typeof(Font), false);
            m_ChineseFont = (Font)EditorGUILayout.ObjectField("中文 Font", m_ChineseFont, typeof(Font), false);
            m_FontSize = EditorGUILayout.IntSlider("采样字号", m_FontSize, 16, 128);
        }
        else
        {
            EditorGUILayout.LabelField("TMP_FontAsset 三槽", EditorStyles.boldLabel);
            m_DigitTmpFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
                "数字 TMP", m_DigitTmpFont, typeof(TMP_FontAsset), false);
            m_EnglishTmpFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
                "英文 TMP", m_EnglishTmpFont, typeof(TMP_FontAsset), false);
            m_ChineseTmpFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
                "中文 TMP", m_ChineseTmpFont, typeof(TMP_FontAsset), false);
        }

        m_CritOverride = (Texture2D)EditorGUILayout.ObjectField(
            "暴击贴图覆盖 *", m_CritOverride, typeof(Texture2D), false);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("图集布局", EditorStyles.boldLabel);
        m_CellSize = EditorGUILayout.IntSlider("格子边长", m_CellSize, 32, 128);
        m_Columns = EditorGUILayout.IntSlider("列数", m_Columns, 8, 32);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("常用字表", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("一份总表即可 烘焙时自动按数字/英文/中文分流到对应槽", MessageType.None);
        m_Charset = EditorGUILayout.TextArea(m_Charset, GUILayout.MinHeight(80f));
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("恢复默认字表"))
        {
            m_Charset = FloatingTextAtlasBaker.DefaultCharset;
        }

        if (GUILayout.Button("仅 ASCII"))
        {
            m_Charset = "0123456789*ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6f);
        m_RefreshPrefab = EditorGUILayout.ToggleLeft("同时刷新 World/Manager 预制体", m_RefreshPrefab);

        EditorGUILayout.Space(10f);
        if (GUILayout.Button("一键烘焙 Atlas + CharMap", GUILayout.Height(36f)))
        {
            var result = FloatingTextAtlasBaker.Bake(new FloatingTextAtlasBaker.BakeRequest
            {
                SourceMode = m_SourceMode,
                DigitFont = m_DigitFont,
                EnglishFont = m_EnglishFont,
                ChineseFont = m_ChineseFont,
                DigitTmpFont = m_DigitTmpFont,
                EnglishTmpFont = m_EnglishTmpFont,
                ChineseTmpFont = m_ChineseTmpFont,
                FontSize = m_FontSize,
                CellSize = m_CellSize,
                Columns = m_Columns,
                Charset = m_Charset,
                CritOverride = m_CritOverride,
                AlsoRefreshPrefab = m_RefreshPrefab
            });
            m_LastMessage = result.Message;
            if (result.CharMap != null) EditorGUIUtility.PingObject(result.CharMap);
            Debug.Log("[FloatingEditor] " + m_LastMessage);
        }

        if (GUILayout.Button("打开生成目录"))
        {
            FloatingTextAtlasBaker.EnsureFolder(FloatingTextAtlasBaker.GeneratedFolder);
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(FloatingTextAtlasBaker.GeneratedFolder);
            if (folder != null) EditorGUIUtility.PingObject(folder);
        }

        if (!string.IsNullOrEmpty(m_LastMessage))
        {
            EditorGUILayout.HelpBox(m_LastMessage, MessageType.None);
        }

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("运行时预览", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "GPU 跳字依赖 Play 时间轴 非 Play 看不到动画\n" +
            "也可选中场景 FloatingTextManager → Inspector「测试」折叠",
            MessageType.Info);

        if (!Application.isPlaying)
        {
            if (GUILayout.Button("进入 Play 并自动预览一批", GUILayout.Height(28f)))
            {
                SessionState.SetBool(AutoPreviewSessionKey, true);
                EditorApplication.EnterPlaymode();
            }
        }
        else
        {
            if (GUILayout.Button("预览跳字 数字+短词", GUILayout.Height(28f)))
            {
                TryPreviewBurstInScene();
            }

            if (GUILayout.Button("预览普通伤害 / 暴击"))
            {
                TryPreviewDamageInScene();
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("当前生成资源", EditorStyles.boldLabel);
        DrawReadonlyAsset("Atlas", FloatingTextAtlasBaker.AtlasPath);
        DrawReadonlyAsset("CharMap", FloatingTextAtlasBaker.CharMapPath);
        DrawReadonlyAsset("Material", FloatingTextAtlasBaker.MaterialPath);
        DrawReadonlyAsset("World Prefab", FloatingTextAtlasBaker.PrefabPath);
        DrawReadonlyAsset("Manager Prefab", FloatingTextAtlasBaker.ManagerPrefabPath);
        EditorGUILayout.EndScrollView();
    }

    private const string AutoPreviewSessionKey = "FloatingText.AutoPreviewOnPlay";

    static FloatingTextEditorWindow()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
            return;
        if (!SessionState.GetBool(AutoPreviewSessionKey, false))
            return;

        SessionState.SetBool(AutoPreviewSessionKey, false);
        EditorApplication.delayCall += () =>
        {
            TryPreviewBurstInScene();
            Debug.Log("[FloatingEditor] 已自动预览一批跳字");
        };
    }

    private static void TryPreviewBurstInScene()
    {
        var manager = FindManagerInScene();
        if (manager == null)
        {
            Debug.LogWarning("[FloatingEditor] 场景中无 FloatingTextManager 请先拖入预制体");
            return;
        }

        manager.PreviewBurst();
        Selection.activeGameObject = manager.gameObject;
    }

    private static void TryPreviewDamageInScene()
    {
        var manager = FindManagerInScene();
        if (manager == null)
        {
            Debug.LogWarning("[FloatingEditor] 场景中无 FloatingTextManager");
            return;
        }

        manager.PreviewDamage();
        manager.PreviewCrit();
        Selection.activeGameObject = manager.gameObject;
    }

    private static FloatingTextManager FindManagerInScene()
    {
        if (FloatingTextManager.Instance != null)
            return FloatingTextManager.Instance;
        return Object.FindFirstObjectByType<FloatingTextManager>();
    }

    private static void DrawReadonlyAsset(string label, string path)
    {
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField(label, asset, typeof(Object), false);
        EditorGUI.EndDisabledGroup();
    }
}
#endif
