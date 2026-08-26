using System;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;


namespace MieMieFrameWork.Asset
{
public class BuildHotPatchWindow : BundleBehaviour
{
    [HideInInspector][SerializeField] private string appVersion = "0.0.0";
    [HideInInspector][SerializeField] private string hotPatchVersion = "1.0.0";
    [HideInInspector][SerializeField] private string remoteHotPatchVersion = "1.0.1";
    [HideInInspector][SerializeField] private string patchNotice = "• 修复已知问题\n• 优化资源加载";
    private Vector2 patchNoticeScroll;

    public override void OnGUI()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
        base.OnGUI();
        DrawVersionsInfo();
        EditorGUILayout.EndVertical();
    }
    #region 绘制
    /// <summary>
    /// 绘制左按钮
    /// </summary>
    public override void DrawLeftButton()
    {
        if (GUILayout.Button("打包热更", GUILayout.Height(28), GUILayout.MinWidth(88)))
            BuildHot();
    }

    /// <summary>
    /// 绘制右按钮
    /// </summary>
    public override void DrawRightButton()
    {
        if (GUILayout.Button("上传资源", GUILayout.Height(28), GUILayout.MinWidth(88)))
            UploadHotPatch();
    }

    /// <summary>
    /// 绘制版本信息
    /// </summary>
    private void DrawVersionsInfo()
    {
        GUILayout.Space(14);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

        DrawVersionHeader();
        GUILayout.Space(6);

        appVersion = DrawVersionRow(
            new GUIContent(
                "母包版本",
                "写入清单 minClientVersion 必须小于等于 Player Settings 的 Version 当前工程是 "
                + Application.version),
            appVersion,
            new Color(0.45f, 0.65f, 0.95f));
        hotPatchVersion = DrawVersionRow(
            new GUIContent("本地热更", "本次资源补丁版本 与 App 版本无关"),
            hotPatchVersion,
            new Color(0.45f, 0.82f, 0.5f));
        DrawVersionRowReadOnly(
            new GUIContent("服务器", "CDN 上当前已发布的最新热更版本，上传/拉取后自动更新"),
            string.IsNullOrEmpty(remoteHotPatchVersion) ? "未拉取" : remoteHotPatchVersion,
            new Color(0.95f, 0.72f, 0.35f));

        EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
        DrawPatchNotice();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制版本头
    /// </summary>
    private void DrawVersionHeader()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("版本信息", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        DrawSyncStatusBadge();
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制版本号行
    /// </summary>
    private static string DrawVersionRow(GUIContent label, string value, Color accent)
    {
        EditorGUILayout.BeginHorizontal(GUILayout.Height(26));

        var barRect = EditorGUILayout.GetControlRect(false, GUILayout.Width(3), GUILayout.Height(22));
        EditorGUI.DrawRect(barRect, accent);

        EditorGUILayout.LabelField(label, GUILayout.Width(64));
        value = EditorGUILayout.TextField(value);

        EditorGUILayout.EndHorizontal();
        return value;
    }

    /// <summary>
    /// 绘制版本号只读行
    /// </summary>
    private static void DrawVersionRowReadOnly(GUIContent label, string value, Color accent)
    {
        EditorGUILayout.BeginHorizontal(GUILayout.Height(26));

        var barRect = EditorGUILayout.GetControlRect(false, GUILayout.Width(3), GUILayout.Height(22));
        EditorGUI.DrawRect(barRect, accent);

        EditorGUILayout.LabelField(label, GUILayout.Width(64));

        var style = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
        EditorGUILayout.LabelField(value, style);

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制热更公告
    /// </summary>
    private void DrawPatchNotice()
    {
        EditorGUILayout.LabelField("热更公告", EditorStyles.boldLabel);
        GUILayout.Space(4);

        const float minViewHeight = 88f;
        var textStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };

        Rect scrollRect = GUILayoutUtility.GetRect(
            0,
            10000,
            GUILayout.ExpandHeight(true),
            GUILayout.MinHeight(minViewHeight),
            GUILayout.ExpandWidth(true));

        float contentWidth = scrollRect.width - 16f;
        float contentHeight = Mathf.Max(
            textStyle.CalcHeight(new GUIContent(patchNotice ?? string.Empty), contentWidth),
            scrollRect.height);

        patchNoticeScroll = GUI.BeginScrollView(
            scrollRect,
            patchNoticeScroll,
            new Rect(0, 0, contentWidth, contentHeight));

        patchNotice = EditorGUI.TextArea(
            new Rect(0, 0, contentWidth, contentHeight),
            patchNotice,
            textStyle);

        GUI.EndScrollView();

        EditorGUILayout.HelpBox(
            "母包版本 = 最低客户端版本 需 <= Player Settings/Version（当前 "
            + Application.version
            + "）\n本地热更 = 资源补丁版本 升这个即可",
            MessageType.Info);
    }

    /// <summary>
    /// 绘制同步状态徽章
    /// </summary>
    private void DrawSyncStatusBadge()
    {
        var (label, color) = GetSyncStatus();
        var prev = GUI.backgroundColor;
        GUI.backgroundColor = color;
        GUILayout.Label(label, EditorStyles.miniButton, GUILayout.Height(20), GUILayout.MinWidth(64));
        GUI.backgroundColor = prev;
    }
    #endregion

    #region 逻辑
    /// <summary>
    /// 打包热更
    /// </summary>
    public  void BuildHot()
    {
        if (CollectSelectedModuleNameList().Count == 0)
        {
            EditorUtility.DisplayDialog("MmAsset", "没有勾选要打包的模块", "确定");
            return;
        }

        if (!EnsureDiagnosticsPassed())
            return;
        if (!Version.TryParse(hotPatchVersion, out _))
        {
            EditorUtility.DisplayDialog("MmAsset", "热更版本必须使用数字版本格式", "确定");
            return;
        }
        if (!Version.TryParse(appVersion, out _))
        {
            EditorUtility.DisplayDialog("MmAsset", "母包版本必须使用数字版本格式", "确定");
            return;
        }
        BundleSettings.Instance.minimumClientVersion = appVersion;
        EditorUtility.SetDirty(BundleSettings.Instance);

        EditorApplication.delayCall += () =>
        {
            foreach (var moduleData in moduleDataList)
            {
                if (moduleData.isBuild)
                {
                    BuildBundleComplier.BuildAsseetBundle(moduleData,
                                                          E_EditorBuildKind.HotPatch,
                                                          hotPatchVersion,
                                                          patchNotice);
                }
            }
        };
    }

    /// <summary>
    /// 上传资源到服务器
    /// </summary>
    public async void UploadHotPatch()
    {
        try
        {
            await MmAssetCIBuild.UploadHotPatchAsync(
                hotPatchVersion,
                MmAssetCIBuild.GetSelectedModules());
            remoteHotPatchVersion = hotPatchVersion;
            EditorUtility.DisplayDialog("MmAsset", "热更资源上传完成", "确定");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("MmAsset", "上传失败\n" + exception.Message, "确定");
        }
    }

    /// <summary>
    /// 获取同步状态
    /// </summary>
    /// <returns></returns>
    private (string label, Color color) GetSyncStatus()
    {
        if (string.IsNullOrEmpty(remoteHotPatchVersion))
            return ("待拉取", new Color(0.55f, 0.55f, 0.55f));

        int cmp = CompareVersion(hotPatchVersion, remoteHotPatchVersion);
        return cmp switch
        {
            < 0 => ("可更新", new Color(0.95f, 0.65f, 0.25f)),
            > 0 => ("领先", new Color(0.45f, 0.7f, 0.95f)),
            _ => ("已同步", new Color(0.45f, 0.82f, 0.45f)),
        };
    }

    /// <summary>
    /// 比较版本号
    /// </summary>
    /// <param name="local"></param>
    /// <param name="remote"></param>
    /// <returns></returns>
    private static int CompareVersion(string local, string remote)
    {
        if (Version.TryParse(local, out var localVer) && Version.TryParse(remote, out var remoteVer))
            return localVer.CompareTo(remoteVer);
        return string.Compare(local, remote, StringComparison.Ordinal);
    }

    #endregion
}
}
