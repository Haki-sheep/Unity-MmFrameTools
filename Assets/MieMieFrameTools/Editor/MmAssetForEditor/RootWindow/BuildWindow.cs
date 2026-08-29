using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using Sirenix.Utilities;
using UnityEngine;



namespace MieMieFrameWork.Asset
{
public class BuildWindow : OdinMenuEditorWindow, MieMieFrameWork.Editor.ToolsCenter.IMieMieToolsEmbeddedWindow
{
    /// <summary>
    /// 总览页面
    /// </summary>
    [SerializeField]
    private MmAssetDashboard dashboard = new();

    /// <summary>
    /// 整包构建页面
    /// </summary>
    [SerializeField]
    private BuildBundleWindow buildBundleWindow = new();

    /// <summary>
    /// 热更构建页面
    /// </summary>
    [SerializeField]
    private BuildHotPatchWindow buildHotPatchWindow = new();

    /// <summary>
    /// 快速配置资源名称页面
    /// </summary>
    [SerializeField]
    private QuickAssetRenamePage quickAssetRenamePage = new();

    /// <summary>
    /// 打开窗口
    /// </summary>
    public static void OpenWindow()
    {
        var window = GetWindow<BuildWindow>();
        window.position = GUIHelper.GetEditorWindowRect().AlignCenter(958, 612);
        window.Show();
        window.Init();
    }

    /// <summary>
    /// 刷新已打开窗口 未打开则打开
    /// </summary>
    public static void RefreshOrOpen()
    {
        if (HasOpenInstances<BuildWindow>())
        {
            var window = GetWindow<BuildWindow>(null, false);
            window.Init();
            window.Repaint();
            return;
        }

        OpenWindow();
    }

    /// <summary>
    /// 刷新已打开的资源管线窗口
    /// </summary>
    public static void RefreshIfOpen()
    {
        if (!HasOpenInstances<BuildWindow>())
            return;

        var window = GetWindow<BuildWindow>(null, false);
        window.Init();
        window.Repaint();
    }

    /// <summary>
    /// 域重载或重新启用时同步模块列表
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        Init();
    }

    /// <summary>
    /// 构建菜单树
    /// </summary>
    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree(supportsMultiSelect: false){
            {"总览",dashboard,EditorIcons.House},
            {"构建/整包与内嵌",buildBundleWindow,EditorIcons.UnityLogo},
            {"构建/热更资源",buildHotPatchWindow,EditorIcons.UnityLogo},
            {"工具/快速配置资源名称",quickAssetRenamePage,EditorIcons.File},
            {"设置/运行时设置",BundleSettings.Instance,EditorIcons.SettingsCog},
        };
        return tree;
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public void Init()
    {
        buildBundleWindow.Init();
        buildHotPatchWindow.Init();
    }

    /// <summary>
    /// 绘制嵌入式资源管线
    /// </summary>
    public void DrawEmbeddedGUI()
    {
        OnImGUI();
    }
}
}
