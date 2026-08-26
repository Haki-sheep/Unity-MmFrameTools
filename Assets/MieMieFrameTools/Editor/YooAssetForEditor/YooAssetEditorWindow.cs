using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Editor.YooAsset
{
    /// <summary>
    /// YooAsset 编辑器入口面板
    /// 只负责打开 YooAsset 原生工具窗口
    /// </summary>
    public sealed class YooAssetEditorWindow : EditorWindow
    {
        /// <summary>
        /// 打开 YooAsset 入口面板
        /// </summary>
        [MenuItem("Tools/MieMieFrameWork/YooAsset/资源面板")]
        public static void Open()
        {
            GetWindow<YooAssetEditorWindow>("YooAsset");
        }

        /// <summary>
        /// 绘制编辑器面板
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("YooAsset 资源管理", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "本面板只提供 YooAsset 原生工具入口 具体配置请在原生窗口中完成",
                MessageType.Info);

            DrawOpenButton("Bundle Collector", "YooAsset/Bundle Collector");
            DrawOpenButton("Bundle Builder", "YooAsset/Bundle Builder");
            DrawOpenButton("Bundle Reporter", "YooAsset/Bundle Reporter");
            DrawOpenButton("Bundle Debugger", "YooAsset/Bundle Debugger");
        }

        /// <summary>
        /// 绘制原生窗口入口按钮
        /// </summary>
        private static void DrawOpenButton(string label, string menuPath)
        {
            if (!GUILayout.Button(label, GUILayout.Height(30)))
                return;

            if (!EditorApplication.ExecuteMenuItem(menuPath))
                EditorUtility.DisplayDialog("YooAsset", "没有找到菜单 " + menuPath, "确定");
        }
    }
}
