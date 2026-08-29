using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace MieMieFrameWork.Editor.DataForEditor
{
    /// <summary>
    /// Luban 工作台项目配置
    /// </summary>
    [Serializable]
    internal sealed class LubanWorkbenchSettings
    {
        /// <summary>
        /// Excel 配置表目录
        /// </summary>
        public string DataTableDirectoryPath = "DataTables";

        /// <summary>
        /// Luban 生成脚本路径
        /// </summary>
        public string GenerateScriptPath = "DataTables/gen.bat";

        /// <summary>
        /// 可选校验脚本路径
        /// </summary>
        public string ValidateScriptPath = "DataTables/check.bat";

        /// <summary>
        /// 生成成功后是否刷新 Unity 资源
        /// </summary>
        public bool RefreshAssetsAfterGenerate = true;
    }

    /// <summary>
    /// Luban 工作台配置存储
    /// </summary>
    internal static class LubanWorkbenchSettingsStore
    {
        /// <summary>
        /// 项目配置文件相对路径
        /// </summary>
        private const string SettingsRelativePath = "ProjectSettings/MieMieLubanWorkbench.json";

        /// <summary>
        /// 读取项目配置
        /// </summary>
        public static LubanWorkbenchSettings Load()
        {
            string settingsPath = LubanPathUtility.ResolveProjectPath(SettingsRelativePath);
            if (!File.Exists(settingsPath))
                return new LubanWorkbenchSettings();

            string json = File.ReadAllText(settingsPath, Encoding.UTF8);
            var settings = JsonUtility.FromJson<LubanWorkbenchSettings>(json);
            return settings ?? new LubanWorkbenchSettings();
        }

        /// <summary>
        /// 保存项目配置
        /// </summary>
        /// <param name="settings">需要保存的工作台配置</param>
        public static void Save(LubanWorkbenchSettings settings)
        {
            string settingsPath = LubanPathUtility.ResolveProjectPath(SettingsRelativePath);
            string json = JsonUtility.ToJson(settings, true);
            File.WriteAllText(settingsPath, json, Encoding.UTF8);
        }
    }

    /// <summary>
    /// Luban 工作台路径工具
    /// </summary>
    internal static class LubanPathUtility
    {
        public static string ProjectRootPath => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        /// <summary>
        /// 将项目路径或绝对路径转换为绝对路径
        /// </summary>
        /// <param name="path">项目路径或绝对路径</param>
        public static string ResolveProjectPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            string normalizedPath = path.Trim();
            if (Path.IsPathRooted(normalizedPath))
                return Path.GetFullPath(normalizedPath);

            return Path.GetFullPath(Path.Combine(ProjectRootPath, normalizedPath));
        }

        /// <summary>
        /// 将项目内绝对路径转换为项目相对路径
        /// </summary>
        /// <param name="path">需要转换的绝对路径</param>
        public static string ToProjectRelativePath(string path)
        {
            string fullPath = Path.GetFullPath(path);
            string projectRootWithSeparator = ProjectRootPath.TrimEnd(Path.DirectorySeparatorChar)
                                              + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(projectRootWithSeparator, StringComparison.OrdinalIgnoreCase))
                return fullPath;

            return fullPath.Substring(projectRootWithSeparator.Length).Replace('\\', '/');
        }
    }
}
