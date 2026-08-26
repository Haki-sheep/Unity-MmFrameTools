#if UNITY_EDITOR
using System.IO;
using UnityEditor;

namespace MieMieUITools.Editor
{
    /// <summary>
    /// 解析 com.hakisheep.mm-uiframe 包根与常用子路径
    /// 兼容 Assets 嵌入与 Packages file/git 引用
    /// </summary>
    public static class PackagePaths
    {
        /// <summary> 缓存的包根资产路径 </summary>
        private static string cachedRoot;

        /// <summary> 包根 如 Assets/MmUIFrameWork 或 Packages/com.hakisheep.mm-uiframe </summary>
        public static string PackageRoot
        {
            get
            {
                if (!string.IsNullOrEmpty(cachedRoot))
                    return cachedRoot;
                cachedRoot = ResolvePackageRoot();
                return cachedRoot;
            }
        }

        /// <summary> Runtime 根目录 </summary>
        public static string RuntimeRoot => $"{PackageRoot}/Runtime";

        /// <summary> StandUI 预制体根目录 </summary>
        public static string StandUIPrefabsRoot => $"{PackageRoot}/StandUIPrefabs";

        /// <summary> DOTween 预设目录 </summary>
        public static string DoTweenPresetsRoot => $"{PackageRoot}/Runtime/Widgets/DoTweenAnimExtension/Presets";

        /// <summary> 跳字系统根目录 </summary>
        public static string FloatingTextRoot => $"{PackageRoot}/Runtime/Widgets/FloatingTextSystem";

        /// <summary>
        /// 清空缓存
        /// </summary>
        public static void Invalidate()
        {
            cachedRoot = null;
        }

        /// <summary>
        /// 通过 Runtime asmdef 定位包根
        /// </summary>
        private static string ResolvePackageRoot()
        {
            string[] guidList = AssetDatabase.FindAssets("MieMieUIFrameWork.UI t:AssemblyDefinitionAsset");
            for (int i = 0; i < guidList.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guidList[i]).Replace('\\', '/');
                if (!path.EndsWith("/MieMieUIFrameWork.UI.asmdef", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                // asmdef 在包根 或 Runtime 子目录 两种布局都兼容
                string asmDir = Path.GetDirectoryName(path)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(asmDir))
                    continue;

                if (asmDir.EndsWith("/Runtime", System.StringComparison.OrdinalIgnoreCase))
                    return Path.GetDirectoryName(asmDir)?.Replace('\\', '/') ?? asmDir;

                return asmDir;
            }

            if (Directory.Exists("Packages/com.hakisheep.mm-uiframe"))
                return "Packages/com.hakisheep.mm-uiframe";
            if (Directory.Exists("Assets/MmUIFrameWork"))
                return "Assets/MmUIFrameWork";

            return "Assets/MmUIFrameWork";
        }
    }
}
#endif
