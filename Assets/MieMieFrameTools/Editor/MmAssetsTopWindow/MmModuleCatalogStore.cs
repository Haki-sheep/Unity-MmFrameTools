using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Editor.MmAssets
{
    public static class MmModuleCatalogStore
    {
        /// <summary>
        /// 模块清单文件路径
        /// </summary>
        public static string CatalogFilePath =>
            Path.Combine(Application.dataPath, "MieMieFrameTools/Editor/MmAssetsTopWindow/MmModuleCatalog.json");

        /// <summary>
        /// 当前模块清单数据
        /// </summary>
        private static MmModuleCatalogData catalog;

        /// <summary>
        /// 获取当前模块清单
        /// </summary>
        public static MmModuleCatalogData Catalog
        {
            get
            {
                EnsureLoaded();
                return catalog;
            }
        }

        /// <summary>
        /// 确保模块清单已经加载
        /// </summary>
        public static void EnsureLoaded()
        {
            if (catalog != null)
                return;

            if (!File.Exists(CatalogFilePath))
            {
                catalog = new MmModuleCatalogData();
                return;
            }

            try
            {
                string json = File.ReadAllText(CatalogFilePath);
                catalog = JsonConvert.DeserializeObject<MmModuleCatalogData>(json) ?? new MmModuleCatalogData();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MmModuleCatalog] 读取清单失败 {exception.Message}");
                catalog = new MmModuleCatalogData();
            }
        }

        /// <summary>
        /// 清除当前模块清单缓存
        /// </summary>
        public static void Invalidate()
        {
            catalog = null;
        }

        /// <summary>
        /// 检查模块是否存在于当前项目
        /// </summary>
        public static bool IsInstalled(MmModuleEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.installCheckPath))
                return false;

            string normalizedPath = entry.installCheckPath.Replace('\\', '/');
            if (normalizedPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                string packageName = ExtractPackageName(normalizedPath);
                if (IsPackageInManifest(packageName) && IsUpmPackageResolved(packageName))
                    return true;
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, normalizedPath));
            if (File.Exists(fullPath) || Directory.Exists(fullPath))
                return true;

            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(normalizedPath) != null;
        }

        private static string ExtractPackageName(string normalizedPath)
        {
            const string packagePrefix = "Packages/";
            int packageNameStart = packagePrefix.Length;
            int packageNameEnd = normalizedPath.IndexOf('/', packageNameStart);
            if (packageNameEnd < 0)
                return string.Empty;

            return normalizedPath.Substring(packageNameStart, packageNameEnd - packageNameStart);
        }

        private static bool IsPackageInManifest(string packageName)
        {
            string manifestFilePath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Packages/manifest.json"));
            if (string.IsNullOrWhiteSpace(packageName) || !File.Exists(manifestFilePath))
                return false;

            try
            {
                JObject manifest = JObject.Parse(File.ReadAllText(manifestFilePath));
                return manifest["dependencies"]?[packageName] != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsUpmPackageResolved(string packageName)
        {
            string virtualPath = $"Packages/{packageName}/package.json";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(virtualPath) != null)
                return true;

            string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            string localPath = Path.Combine(projectRoot, "Packages", packageName);
            if (Directory.Exists(localPath))
                return true;

            string cacheDirectory = Path.Combine(projectRoot, "Library", "PackageCache");
            if (!Directory.Exists(cacheDirectory))
                return false;

            foreach (string directory in Directory.GetDirectories(cacheDirectory, packageName + "@*"))
            {
                if (File.Exists(Path.Combine(directory, "package.json")))
                    return true;
            }

            return false;
        }
    }
}
