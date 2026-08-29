using System;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Editor.ToolsCenter
{
    /// <summary>
    /// 工具中枢页面契约
    /// </summary>
    public interface IMieMieToolsPage
    {
        /// <summary>
        /// 菜单树路径
        /// </summary>
        string MenuPath { get; }

        /// <summary>
        /// 页面标题
        /// </summary>
        string Title { get; }

        /// <summary>
        /// 页面描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 页面打开回调
        /// </summary>
        void OnOpen();

        /// <summary>
        /// 页面关闭回调
        /// </summary>
        void OnClose();

        /// <summary>
        /// 绘制页面内容
        /// </summary>
        void DrawGUI();
    }

    /// <summary>
    /// 可嵌入工具窗口契约
    /// </summary>
    public interface IMieMieToolsEmbeddedWindow
    {
        /// <summary>
        /// 绘制嵌入式工具界面
        /// </summary>
        void DrawEmbeddedGUI();
    }

    /// <summary>
    /// 工具中枢页面基类
    /// </summary>
    public abstract class MieMieToolsPage : IMieMieToolsPage
    {
        /// <summary>
        /// 页面菜单路径
        /// </summary>
        private readonly string PageMenuPath;

        /// <summary>
        /// 页面标题文本
        /// </summary>
        private readonly string PageTitle;

        /// <summary>
        /// 页面描述文本
        /// </summary>
        private readonly string PageDescription;

        public string MenuPath => PageMenuPath;

        public string Title => PageTitle;

        public string Description => PageDescription;

        protected MieMieToolsPage(string menuPath, string title, string description)
        {
            PageMenuPath = menuPath;
            PageTitle = title;
            PageDescription = description;
        }

        /// <summary>
        /// 页面打开回调
        /// </summary>
        public virtual void OnOpen()
        {
        }

        /// <summary>
        /// 页面关闭回调
        /// </summary>
        public virtual void OnClose()
        {
        }

        /// <summary>
        /// 绘制页面内容
        /// </summary>
        public abstract void DrawGUI();

        /// <summary>
        /// 绘制页面标题
        /// </summary>
        protected void DrawPageTitle()
        {
            EditorGUILayout.LabelField(Title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(Description, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(8f);
        }
    }

    /// <summary>
    /// 编辑器窗口嵌入页面
    /// </summary>
    public sealed class MieMieToolsEmbeddedPage<TWindow> : MieMieToolsPage
        where TWindow : EditorWindow, IMieMieToolsEmbeddedWindow
    {
        /// <summary>
        /// 嵌入工具窗口实例
        /// </summary>
        private TWindow ToolWindow;

        /// <summary>
        /// 创建嵌入工具页面
        /// </summary>
        public MieMieToolsEmbeddedPage(
            string menuPath,
            string title,
            string description)
            : base(menuPath, title, description)
        {
        }

        /// <summary>
        /// 创建嵌入工具窗口
        /// </summary>
        public override void OnOpen()
        {
            if (ToolWindow != null)
                return;

            ToolWindow = ScriptableObject.CreateInstance<TWindow>();
            ToolWindow.hideFlags = HideFlags.HideAndDontSave;
        }

        /// <summary>
        /// 销毁嵌入工具窗口
        /// </summary>
        public override void OnClose()
        {
            if (ToolWindow == null)
                return;

            UnityEngine.Object.DestroyImmediate(ToolWindow);
            ToolWindow = null;
        }

        /// <summary>
        /// 绘制嵌入工具页面
        /// </summary>
        public override void DrawGUI()
        {
            DrawPageTitle();
            if (ToolWindow == null)
            {
                OnOpen();
            }

            ToolWindow.DrawEmbeddedGUI();
        }
    }

    /// <summary>
    /// 静态工具操作页面
    /// </summary>
    public sealed class MieMieToolsActionPage : MieMieToolsPage
    {
        /// <summary>
        /// 页面操作委托
        /// </summary>
        private readonly Action Action;

        /// <summary>
        /// 最近一次操作提示
        /// </summary>
        private string LastMessage;

        public MieMieToolsActionPage(
            string menuPath,
            string title,
            string description,
            Action action)
            : base(menuPath, title, description)
        {
            Action = action;
        }

        /// <summary>
        /// 绘制静态工具页面
        /// </summary>
        public override void DrawGUI()
        {
            DrawPageTitle();
            EditorGUILayout.HelpBox("该操作已迁移到工具中枢 执行后会调用原有工具逻辑", MessageType.Info);
            EditorGUILayout.Space(8f);

            if (GUILayout.Button("执行操作", GUILayout.Height(32f)))
            {
                Action();
                LastMessage = "操作已执行";
            }

            if (!string.IsNullOrEmpty(LastMessage))
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.HelpBox(LastMessage, MessageType.None);
            }
        }
    }

    /// <summary>
    /// 工具中枢首页
    /// </summary>
    public sealed class MieMieToolsHomePage : MieMieToolsPage
    {
        public MieMieToolsHomePage()
            : base("首页", "MieMie 工具中枢", "统一管理项目内散落的编辑器工具")
        {
        }

        /// <summary>
        /// 绘制工具中枢首页
        /// </summary>
        public override void DrawGUI()
        {
            DrawPageTitle();
            if (GUILayout.Button("打开模块中心", GUILayout.Height(30f)))
                EditorApplication.ExecuteMenuItem("Tools/MieMieFrameWork/模块中心");
        }
    }
}
