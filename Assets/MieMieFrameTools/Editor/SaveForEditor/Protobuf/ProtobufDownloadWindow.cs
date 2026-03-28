using UnityEditor;
using UnityEngine;

namespace Editor.Protobuf
{
    /// <summary>
    /// protoc / NuGet 依赖下载说明（与项目外「下载地址.txt」一致）
    /// </summary>
    public class ProtobufDownloadWindow : EditorWindow
    {
        private const string UrlProtocZip =
            "https://github.com/protocolbuffers/protobuf/releases/download/v34.1/protoc-34.1-win64.zip";

        private const string UrlGoogleProtobufNuGet =
            "https://www.nuget.org/packages/Google.Protobuf/34.1.0";

        private const string UrlUnsafeNuGet =
            "https://www.nuget.org/packages/System.Runtime.CompilerServices.Unsafe/6.0.0";

        public static void Open() 
        { 
            var w = GetWindow<ProtobufDownloadWindow>(true, "Protobuf 依赖下载", true);
            w.minSize = new Vector2(420, 260);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("1. Windows 64 位 protoc（生成 C# 代码）", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(UrlProtocZip, GUILayout.Height(18));
            if (GUILayout.Button("在浏览器中打开"))
                Application.OpenURL(UrlProtocZip);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("2. Google.Protobuf（Unity 运行库，主 DLL）", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(UrlGoogleProtobufNuGet, GUILayout.Height(18));
            if (GUILayout.Button("在浏览器中打开"))
                Application.OpenURL(UrlGoogleProtobufNuGet);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("3. System.Runtime.CompilerServices.Unsafe（依赖）", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(UrlUnsafeNuGet, GUILayout.Height(18));
            if (GUILayout.Button("在浏览器中打开"))
                Application.OpenURL(UrlUnsafeNuGet);

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "DLL 请把NuGet 转 Zpi格式 解压后复制到 Assets/Plugins。具体路径可在「Protobuf 生成器」窗口中备忘。",
                MessageType.Info);

            EditorGUILayout.Space(6);
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("打开 Protobuf 生成器", GUILayout.Height(30)))
            {
                ProtobufGeneratorWindow.ShowWindow();
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
