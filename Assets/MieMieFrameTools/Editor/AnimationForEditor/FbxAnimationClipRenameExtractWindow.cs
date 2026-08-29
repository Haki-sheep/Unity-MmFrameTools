#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Editor.Animation
{
    /// <summary>
    /// 将 FBX 内嵌动画改名为与文件同名 并可提取为独立 AnimationClip
    /// </summary>
    public sealed class FbxAnimationClipRenameExtractWindow : EditorWindow, MieMieFrameWork.Editor.ToolsCenter.IMieMieToolsEmbeddedWindow
    {
        /// <summary> 待处理 FBX </summary>
        private readonly List<Object> fbxObjectList = new List<Object>();

        /// <summary> 提取输出目录 </summary>
        private string outputFolder = "Assets/Arts/Animations/ExtractedClips";

        /// <summary> 先改 FBX 内部 Clip 名 </summary>
        private bool renameInsideFbx = true;

        /// <summary> 再复制出独立 Clip </summary>
        private bool extractStandaloneClip = true;

        /// <summary> 多段 Clip 时用 名_01 后缀 </summary>
        private bool multiClipIndexSuffix = true;

        /// <summary> 滚动 </summary>
        private Vector2 scrollPos;

        private static void Open()
        {
            var window = GetWindow<FbxAnimationClipRenameExtractWindow>("FBX 动画改名提取");
            window.minSize = new Vector2(460f, 360f);
            window.Show();
        }

        public void DrawEmbeddedGUI()
        {
            OnGUI();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "拖入 FBX 或 Blend 模型文件\n" +
                "1 将内部动画 Clip 改名为与 FBX 文件名一致（如 Take 001 → 文件名）\n" +
                "2 可选 复制为独立 AnimationClip 到输出目录\n" +
                "同一文件含多段动画时 依次命名为 文件名 / 文件名_01 / 文件名_02",
                MessageType.Info);

            DrawDropArea();
            DrawFbxList();

            EditorGUILayout.Space(6f);
            renameInsideFbx = EditorGUILayout.Toggle("改 FBX 内部名", renameInsideFbx);
            extractStandaloneClip = EditorGUILayout.Toggle("提取独立 Clip", extractStandaloneClip);
            multiClipIndexSuffix = EditorGUILayout.Toggle("多段加序号后缀", multiClipIndexSuffix);

            EditorGUILayout.BeginHorizontal();
            outputFolder = EditorGUILayout.TextField("输出目录", outputFolder);
            if (GUILayout.Button("选文件夹", GUILayout.Width(72f)))
                TryPickOutputFolder();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10f);
            using (new EditorGUI.DisabledScope(fbxObjectList.Count == 0 || (!renameInsideFbx && !extractStandaloneClip)))
            {
                if (GUILayout.Button("处理选中 FBX", GUILayout.Height(36f)))
                    ProcessAll();
            }
        }

        /// <summary>
        /// 拖放区
        /// </summary>
        private void DrawDropArea()
        {
            Rect rect = GUILayoutUtility.GetRect(0f, 56f, GUILayout.ExpandWidth(true));
            GUI.Box(rect, "把 FBX 拖到这里 可多选", EditorStyles.helpBox);
            Event e = Event.current;
            if (!rect.Contains(e.mousePosition))
                return;

            if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (e.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                        TryAddFbx(DragAndDrop.objectReferences[i]);
                }

                e.Use();
            }
        }

        /// <summary>
        /// FBX 列表
        /// </summary>
        private void DrawFbxList()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"已加入 {fbxObjectList.Count} 个");
            using (new EditorGUI.DisabledScope(fbxObjectList.Count == 0))
            {
                if (GUILayout.Button("一键清除列表", GUILayout.Width(110f)))
                    fbxObjectList.Clear();
            }

            EditorGUILayout.EndHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(140f));
            for (int i = fbxObjectList.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();
                fbxObjectList[i] = EditorGUILayout.ObjectField(fbxObjectList[i], typeof(Object), false);
                if (GUILayout.Button("×", GUILayout.Width(24f)))
                    fbxObjectList.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            Object add = EditorGUILayout.ObjectField("追加 FBX", null, typeof(Object), false);
            if (add != null)
                TryAddFbx(add);
        }

        /// <summary>
        /// 选输出目录
        /// </summary>
        private void TryPickOutputFolder()
        {
            string abs = EditorUtility.OpenFolderPanel("选择输出目录", Application.dataPath, "");
            if (string.IsNullOrEmpty(abs))
                return;
            if (!abs.StartsWith(Application.dataPath))
            {
                EditorUtility.DisplayDialog("路径无效", "请选 Assets 下的文件夹", "确定");
                return;
            }

            outputFolder = "Assets" + abs.Substring(Application.dataPath.Length).Replace('\\', '/');
        }

        /// <summary>
        /// 加入合法 FBX
        /// </summary>
        private void TryAddFbx(Object obj)
        {
            if (obj == null)
                return;
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
                return;
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext != ".fbx" && ext != ".blend")
            {
                Debug.LogWarning($"跳过非 FBX {path}");
                return;
            }

            for (int i = 0; i < fbxObjectList.Count; i++)
            {
                if (fbxObjectList[i] == obj)
                    return;
            }

            fbxObjectList.Add(obj);
        }

        /// <summary>
        /// 批处理
        /// </summary>
        private void ProcessAll()
        {
            EnsureFolder(outputFolder);
            int ok = 0;
            int fail = 0;
            var logList = new List<string>();

            for (int i = 0; i < fbxObjectList.Count; i++)
            {
                Object obj = fbxObjectList[i];
                if (obj == null)
                    continue;
                string path = AssetDatabase.GetAssetPath(obj);
                EditorUtility.DisplayProgressBar(
                    "FBX 动画处理",
                    path,
                    (float)i / Mathf.Max(1, fbxObjectList.Count));
                if (ProcessOneFbx(path, logList))
                    ok++;
                else
                    fail++;
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "处理完成",
                $"成功 {ok} 失败 {fail}\n\n{string.Join("\n", logList)}",
                "好的");
        }

        /// <summary>
        /// 处理单个 FBX
        /// </summary>
        private bool ProcessOneFbx(string fbxPath, List<string> logList)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                logList.Add($"失败 非 Model {fbxPath}");
                return false;
            }

            string fbxName = Path.GetFileNameWithoutExtension(fbxPath);
            if (renameInsideFbx)
            {
                if (!RenameClipsInsideFbx(importer, fbxName, logList))
                    return false;
            }

            if (extractStandaloneClip)
            {
                if (!ExtractClips(fbxPath, fbxName, logList))
                    return false;
            }

            logList.Add($"完成 {fbxName}");
            return true;
        }

        /// <summary>
        /// 改 FBX 内 Clip 名并 Reimport
        /// </summary>
        private bool RenameClipsInsideFbx(ModelImporter importer, string fbxName, List<string> logList)
        {
            ModelImporterClipAnimation[] clipList = importer.clipAnimations;
            if (clipList == null || clipList.Length == 0)
                clipList = importer.defaultClipAnimations;

            if (clipList == null || clipList.Length == 0)
            {
                logList.Add($"无动画 {fbxName}");
                return false;
            }

            for (int i = 0; i < clipList.Length; i++)
                clipList[i].name = BuildClipName(fbxName, i, clipList.Length);

            importer.clipAnimations = clipList;
            importer.SaveAndReimport();
            return true;
        }

        /// <summary>
        /// 提取独立 Clip 资源
        /// </summary>
        private bool ExtractClips(string fbxPath, string fbxName, List<string> logList)
        {
            Object[] assetList = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            var sourceClipList = new List<AnimationClip>();
            for (int i = 0; i < assetList.Length; i++)
            {
                if (assetList[i] is AnimationClip clip && !IsEditorPreviewClip(clip))
                    sourceClipList.Add(clip);
            }

            if (sourceClipList.Count == 0)
            {
                logList.Add($"提取失败 无 Clip {fbxName}");
                return false;
            }

            // 按名字优先匹配 否则按顺序
            sourceClipList.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            for (int i = 0; i < sourceClipList.Count; i++)
            {
                AnimationClip source = sourceClipList[i];
                string clipName = BuildClipName(fbxName, i, sourceClipList.Count);
                // 若已在 FBX 内改过名 优先用资源当前名
                if (renameInsideFbx && !string.IsNullOrEmpty(source.name))
                    clipName = source.name;

                string outPath = AssetDatabase.GenerateUniqueAssetPath($"{outputFolder}/{clipName}.anim");
                var copy = Object.Instantiate(source);
                copy.name = clipName;
                AssetDatabase.CreateAsset(copy, outPath);
                logList.Add($"提取 {outPath}");
            }

            return true;
        }

        /// <summary>
        /// 生成 Clip 名
        /// </summary>
        private string BuildClipName(string fbxName, int index, int total)
        {
            if (total <= 1 || !multiClipIndexSuffix)
                return fbxName;
            if (index == 0)
                return fbxName;
            return $"{fbxName}_{index:00}";
        }

        /// <summary>
        /// Unity 预览用隐藏 Clip
        /// </summary>
        private static bool IsEditorPreviewClip(AnimationClip clip)
        {
            if (clip == null)
                return true;
            string n = clip.name;
            return n.StartsWith("__preview__", System.StringComparison.Ordinal)
                   || (clip.hideFlags & HideFlags.HideInHierarchy) != 0;
        }

        /// <summary>
        /// 确保文件夹
        /// </summary>
        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] partList = folderPath.Split('/');
            string current = partList[0];
            for (int i = 1; i < partList.Length; i++)
            {
                string next = current + "/" + partList[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, partList[i]);
                current = next;
            }
        }
    }
}
#endif
