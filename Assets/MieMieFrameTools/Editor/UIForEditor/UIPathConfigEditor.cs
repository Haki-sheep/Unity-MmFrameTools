using UnityEngine;
using UnityEditor;

namespace MieMieFrameWork.Editor
{
    /// <summary>
    /// UI路径配置编辑器窗口
    /// 用于：
    /// 1. 配置默认UI脚本生成路径
    /// 2. 配置预制体bind记录Json文件路径
    /// 3. 查看/清除预制体路径记录
    /// </summary>
    public class UIPathConfigEditor : EditorWindow
    {
        private UIPathConfig config;

        [MenuItem("Tools/UI/UIPathConfigEditor")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<UIPathConfigEditor>("UI路径配置");
            wnd.minSize = new Vector2(500, 300);
        }

        private void OnEnable()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            config = AssetDatabase.LoadAssetAtPath<UIPathConfig>(
                "Assets/MieMieFrameTools/FrameSettings/UIPathConfig.asset");

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<UIPathConfig>();

                string dir = "Assets/MieMieFrameTools/FrameSettings";
                if (!AssetDatabase.IsValidFolder(dir))
                    AssetDatabase.CreateFolder("Assets/MieMieFrameTools", "FrameSettings");

                AssetDatabase.CreateAsset(config, "Assets/MieMieFrameTools/FrameSettings/UIPathConfig.asset");
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("提示", "已自动创建 UIPathConfig.asset", "确定");
            }
            else
            {
                config.InitDefaultToolPaths();
            }
        }

        private void OnGUI()
        {
            if (config == null)
            {
                LoadConfig();
                return;
            }

            EditorGUILayout.Space(5);
            DrawHeader();
            EditorGUILayout.Space(5);
            DrawPathSettings();
            EditorGUILayout.Space(10);
            DrawRecordSection();
            EditorGUILayout.Space(10);
            DrawFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("UI 路径配置面板", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "【UI脚本生成路径】生成UI脚本时默认填入的路径\n" +
                "【Bind记录Json路径】预制体bind映射关系的保存文件路径",
                MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void DrawPathSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("可自定义路径", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("UI脚本默认生成路径:", GUILayout.Width(150));
            config.defaultUIGenScriptPath = EditorGUILayout.TextField(config.defaultUIGenScriptPath);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Bind记录Json路径:", GUILayout.Width(150));
            config.prefabBindJsonPath = EditorGUILayout.TextField(config.prefabBindJsonPath);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("GenerateUITemp UXML:", GUILayout.Width(150));
            config.generateUITempUxmlPath = EditorGUILayout.TextField(config.generateUITempUxmlPath);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("GenerateUITemp USS:", GUILayout.Width(150));
            config.generateUITempUssPath = EditorGUILayout.TextField(config.generateUITempUssPath);
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(config);
            }

            EditorGUILayout.Space(5);
            if (GUILayout.Button("保存配置", GUILayout.Width(100)))
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("提示", "配置已保存", "确定");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRecordSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("预制体路径记录", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField($"共 {config.runtimeRecords.Count} 条记录", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("清空全部记录", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("确认清空", "确定要清空所有预制体路径记录吗？", "确定", "取消"))
                {
                    config.ClearAllRecords();
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssets();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(3);

            if (config.runtimeRecords.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无记录，生成UI脚本时会自动记录预制体→路径映射", MessageType.None);
            }
            else
            {
                foreach (var record in config.runtimeRecords)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.textArea);
                    EditorGUILayout.LabelField($"预制体: {record.prefabName}", EditorStyles.boldLabel, GUILayout.Width(150));
                    EditorGUILayout.LabelField($"路径: {record.lastGenScriptPath}", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(2);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("配置文件路径:", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("选中配置文件", GUILayout.Width(110)))
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }
            if (GUILayout.Button("刷新", GUILayout.Width(50)))
            {
                LoadConfig();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
