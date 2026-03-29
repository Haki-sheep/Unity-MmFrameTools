using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MieMieFrameWork;

namespace MieMieFrameWork.Editor
{
    /// <summary>
    /// 在工程内自动查找 UIPathConfig、GenerateUITemp 资源与 Bind 记录路径，避免硬编码 Assets/... 前缀随工程结构变化失效。
    /// </summary>
    public static class UIPathConfigLocator
    {
        public const string PrefabBindJsonFileName = "PrefabBindRecords.json";
        private const string GenerateUITempBaseName = "GenerateUITemp";

        /// <summary>
        /// 查找工程中第一个 UIPathConfig（优先名为 UIPathConfig.asset）。
        /// </summary>
        public static UIPathConfig FindUIPathConfig()
        {
            UIPathConfig fallback = null;
            foreach (var guid in AssetDatabase.FindAssets("t:UIPathConfig"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var cfg = AssetDatabase.LoadAssetAtPath<UIPathConfig>(path);
                if (cfg == null) continue;
                if (path.EndsWith("/UIPathConfig.asset", StringComparison.OrdinalIgnoreCase))
                    return cfg;
                fallback ??= cfg;
            }
            return fallback;
        }

        /// <summary>
        /// 在工程中查找 FrameSetting（优先名为 FrameSetting.asset），避免硬编码 Assets/MieMieFrameTools/... 路径在脚本迁移到 _Scripts 等目录后失效。
        /// </summary>
        public static FrameSetting FindFrameSetting()
        {
            FrameSetting fallback = null;
            foreach (var guid in AssetDatabase.FindAssets("t:FrameSetting"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var setting = AssetDatabase.LoadAssetAtPath<FrameSetting>(path);
                if (setting == null) continue;
                if (path.EndsWith("/FrameSetting.asset", StringComparison.OrdinalIgnoreCase))
                    return setting;
                fallback ??= setting;
            }

            return fallback;
        }

        /// <summary>
        /// 获取或创建 UIPathConfig；创建目录优先使用已存在的 FrameSetting 所在文件夹，其次根据 GenerateUITemp.uxml / 脚本位置推断 MieMieFrameTools 根目录。
        /// </summary>
        public static UIPathConfig LoadOrCreateUIPathConfig()
        {
            var existing = FindUIPathConfig();
            if (existing != null)
                return existing;

            if (!TryGetFrameSettingsDirectory(out string frameSettingsDir))
            {
                Debug.LogError(
                    "[UIPathConfigLocator] 无法推断 FrameSettings 目录：未找到 FrameSetting.asset、GenerateUITemp.uxml 或 UIPathConfig 脚本。请手动创建 UIPathConfig。");
                return null;
            }

            if (!AssetDatabase.IsValidFolder(frameSettingsDir))
            {
                string parent = Path.GetDirectoryName(frameSettingsDir)?.Replace('\\', '/');
                string leaf = Path.GetFileName(frameSettingsDir);
                if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                    AssetDatabase.CreateFolder(parent, leaf);
            }

            string assetPath = $"{frameSettingsDir}/UIPathConfig.asset";
            var config = ScriptableObject.CreateInstance<UIPathConfig>();
            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[UIPathConfigLocator] 已创建 {assetPath}");
            return config;
        }

        /// <summary>
        /// 在工程中按文件名/类型解析三条工具路径并写回 config（可选仅修正无效路径）。
        /// </summary>
        /// <returns>实际被修改的字段数量。</returns>
        public static int ApplyAutoDetectedPaths(UIPathConfig config, bool overwriteExisting = false)
        {
            if (config == null) return 0;
            int changed = 0;

            if (TryFindPrefabBindJsonPath(out string jsonPath) &&
                ShouldUpdatePath(config.prefabBindJsonPath, jsonPath, overwriteExisting))
            {
                config.prefabBindJsonPath = jsonPath;
                changed++;
            }

            if (TryFindGenerateUITempUxmlPath(out string uxmlPath) &&
                ShouldUpdatePath(config.generateUITempUxmlPath, uxmlPath, overwriteExisting))
            {
                config.generateUITempUxmlPath = uxmlPath;
                changed++;
            }

            if (TryFindGenerateUITempUssPath(out string ussPath) &&
                ShouldUpdatePath(config.generateUITempUssPath, ussPath, overwriteExisting))
            {
                config.generateUITempUssPath = ussPath;
                changed++;
            }

            if (changed > 0)
                EditorUtility.SetDirty(config);
            return changed;
        }

        /// <summary>
        /// 若当前配置无法加载 UXML，则自动扫描并保存后重试。
        /// </summary>
        public static bool EnsureGenerateUITempPaths(UIPathConfig config)
        {
            if (config == null) return false;
            string uxml = config.GetGenerateUITempUxmlPath();
            if (AssetExistsAtPath<VisualTreeAsset>(uxml))
                return true;

            int n = ApplyAutoDetectedPaths(config, overwriteExisting: true);
            if (n > 0)
                AssetDatabase.SaveAssets();
            uxml = config.GetGenerateUITempUxmlPath();
            return AssetExistsAtPath<VisualTreeAsset>(uxml);
        }

        public static bool AssetExistsAtPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null;
        }

        public static bool AssetExistsAtPath<T>(string assetPath) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            return AssetDatabase.LoadAssetAtPath<T>(assetPath) != null;
        }

        private static bool ShouldUpdatePath(string current, string detected, bool overwriteExisting)
        {
            if (string.IsNullOrEmpty(detected)) return false;
            if (overwriteExisting) return !string.Equals(current, detected, StringComparison.OrdinalIgnoreCase);
            return string.IsNullOrEmpty(current) || !AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(current);
        }

        public static bool TryFindPrefabBindJsonPath(out string assetPath)
        {
            foreach (var guid in AssetDatabase.FindAssets($"{Path.GetFileNameWithoutExtension(PrefabBindJsonFileName)}"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith(PrefabBindJsonFileName, StringComparison.OrdinalIgnoreCase))
                {
                    assetPath = p;
                    return true;
                }
            }

            if (TryGetMieMieFrameToolsRoot(out string root))
            {
                string suggested = $"{root}/Editor/UI/{PrefabBindJsonFileName}";
                assetPath = suggested;
                return true;
            }

            assetPath = null;
            return false;
        }

        public static bool TryFindGenerateUITempUxmlPath(out string assetPath)
        {
            return TryPickBestPath(
                AssetDatabase.FindAssets($"{GenerateUITempBaseName} t:VisualTreeAsset"),
                p => p.EndsWith($"{GenerateUITempBaseName}.uxml", StringComparison.OrdinalIgnoreCase),
                subPathMustContain: "UIForEditor",
                out assetPath);
        }

        public static bool TryFindGenerateUITempUssPath(out string assetPath)
        {
            return TryPickBestPath(
                AssetDatabase.FindAssets($"{GenerateUITempBaseName} t:StyleSheet"),
                p => p.EndsWith($"{GenerateUITempBaseName}.uss", StringComparison.OrdinalIgnoreCase),
                subPathMustContain: "UIForEditor",
                out assetPath);
        }

        private static bool TryPickBestPath(string[] guids, Func<string, bool> pathFilter, string subPathMustContain,
            out string assetPath)
        {
            string best = null;
            foreach (var guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (!pathFilter(p)) continue;
                if (p.IndexOf(subPathMustContain, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    assetPath = p;
                    return true;
                }
                best ??= p;
            }
            if (best != null)
            {
                assetPath = best;
                return true;
            }
            assetPath = null;
            return false;
        }

        public static bool TryGetMieMieFrameToolsRoot(out string root)
        {
            if (TryFindGenerateUITempUxmlPath(out string uxml))
            {
                root = GetMieMieFrameToolsRootFromUIForEditorUxml(uxml);
                return !string.IsNullOrEmpty(root);
            }

            if (TryGetUIPathConfigScriptDirectory(out string scriptDir))
            {
                // .../Editor/UIForEditor -> root is parent of Editor
                string uiForEditor = scriptDir.Replace('\\', '/').TrimEnd('/');
                string editor = Path.GetDirectoryName(uiForEditor)?.Replace('\\', '/');
                root = string.IsNullOrEmpty(editor) ? null : Path.GetDirectoryName(editor)?.Replace('\\', '/');
                return !string.IsNullOrEmpty(root);
            }

            root = null;
            return false;
        }

        private static string GetMieMieFrameToolsRootFromUIForEditorUxml(string uxmlPath)
        {
            string uiForEditor = Path.GetDirectoryName(uxmlPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(uiForEditor)) return null;
            string editor = Path.GetDirectoryName(uiForEditor)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(editor)) return null;
            return Path.GetDirectoryName(editor)?.Replace('\\', '/');
        }

        private static bool TryGetUIPathConfigScriptDirectory(out string directory)
        {
            foreach (var guid in AssetDatabase.FindAssets("UIPathConfig t:MonoScript"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith("/UIPathConfig.cs", StringComparison.OrdinalIgnoreCase))
                {
                    directory = Path.GetDirectoryName(p);
                    return !string.IsNullOrEmpty(directory);
                }
            }
            directory = null;
            return false;
        }

        private static bool TryGetFrameSettingsDirectory(out string frameSettingsDir)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:FrameSetting"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                string dir = Path.GetDirectoryName(p)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(dir))
                {
                    frameSettingsDir = dir;
                    return true;
                }
            }

            if (TryGetMieMieFrameToolsRoot(out string root))
            {
                frameSettingsDir = $"{root}/FrameSettings";
                return true;
            }

            frameSettingsDir = null;
            return false;
        }
    }
}
