using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Asset
{
    public enum E_EditorBuildKind
    {
        AssetBundle,
        HotPatch,
    }

    public partial class BuildBundleComplier
    {
        // 当前正在构建的模块配置与构建参数
        private static BundleModuleData moduleData;
        private static string updateNotice;
        private static string hotPatchVersion;

        // 当前正在构建的构建类型
        private static E_EditorBuildKind buildBundleType;
        // 当前正在构建的模块枚举
        private static BundleModuleEnum bundleModuleEnum;

        // 所有待构建的资源包路径
        private static List<string> allBundlePathList = new List<string>();
        // 文件夹资源包映射：目录路径 -> 该目录下需要打包的资源
        private static Dictionary<string, List<string>> allFolderBundleDict = new();
        // Prefab 资源包映射：Prefab 路径 -> 依赖或关联资源列表
        private static Dictionary<string, List<string>> allPrefabBundleDict = new();
        // 场景资源包映射：场景名称 -> 依赖或关联资源列表
        private static Dictionary<string, List<string>> allSceneBundleDict = new();
        // 自动抽取到共享包的资源路径列表
        private static List<string> sharedDependencyPathList = new();


        /// <summary>
        /// 构建资源包的统一入口
        /// </summary>
        /// <param name="moduleData">模块构建配置</param>
        /// <param name="buildBundleType">构建类型，例如完整 AssetBundle 或热更新包</param> 
        /// <param name="hotPatchVersion">热更新版本号</param>
        /// <param name="updateNotice">热更新公告内容</param>
        public static void BuildAsseetBundle(BundleModuleData moduleData,
                                            E_EditorBuildKind buildBundleType,
                                            string hotPatchVersion = "1.0.0",
                                            string updateNotice = "")
        {
            InitPack(moduleData, buildBundleType, hotPatchVersion, updateNotice);
            BuildAllFolder();
            BuildRootSubFolder();
            BuildAllPrefab();
            BuildAllScene();
            ExtractSharedDependencies();
            BuildAllAssetBundle();
        }

        /// <summary>
        /// 仅导出模块地址表 不打 AB
        /// Editor 短名加载依赖 Generated 下的 AbConfig
        /// </summary>
        public static void ExportAddressTableOnly(BundleModuleData moduleData)
        {
            InitExportAddressTable(moduleData);
            BuildAllFolder();
            BuildRootSubFolder();
            BuildAllPrefab();
            BuildAllScene();
            ExtractSharedDependencies();
            try
            {
                ModifyAllFileBundleName();
                WriteAssetBundleConfig();
                Debug.Log("地址表已导出 " + bundleConfigFilePath);
            }
            finally
            {
                ModifyAllFileBundleName(true);
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// 初始化仅导出地址表参数 不清空已有 AB 输出目录
        /// </summary>
        private static void InitExportAddressTable(BundleModuleData moduleData)
        {
            allBundlePathList.Clear();
            allFolderBundleDict.Clear();
            allPrefabBundleDict.Clear();
            allSceneBundleDict.Clear();
            sharedDependencyPathList.Clear();

            BuildBundleComplier.moduleData = moduleData;
            buildBundleType = E_EditorBuildKind.AssetBundle;
            hotPatchVersion = "1.0.0";
            updateNotice = "";
            bundleModuleEnum = Enum.Parse<BundleModuleEnum>(moduleData.moduleName);
            Directory.CreateDirectory(MmAssetPaths.GeneratedDiskPath);
        }
    }

}
