using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;



namespace MieMieFrameWork.Asset
{
public partial class BuildBundleComplier
{

    #region 打包
    /// <summary>
    /// 收集并构建配置中标记为“按整个文件夹打包”的资源
    //  比如传入名字为A 路径为B的组合, 会构建出 模块名_A 指向路径B的资源包
    /// </summary>
    public static void BuildAllFolder()
    {
        if (moduleData is null || moduleData.wholePackFiles is null || moduleData.wholePackFiles.Length == 0)
            return;

        foreach (var item in moduleData.wholePackFiles)
        {
            ValidateBundleName(item.abName);
            // 将路径中的 \ 替换为 /
            var path = item.bundlePath.Replace(@"\", "/");

            // 重复路径跳过
            if (IsRepeatPath(path))
                continue;

            // 生成资源包名称
            var bundleName = GenerateBundleName(item.abName);
            // 填充容器
            allBundlePathList.Add(path);
            if (!allFolderBundleDict.ContainsKey(bundleName))
                allFolderBundleDict[bundleName] = new List<string>();

            foreach (var filePath in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                if (filePath.EndsWith(".cs") || filePath.EndsWith(".meta"))
                    continue;

                var abFilePath = filePath.Replace(@"\", "/");
                if (IsRepeatPath(abFilePath))
                    continue;

                allBundlePathList.Add(abFilePath);
                allFolderBundleDict[bundleName].Add(abFilePath);
            }
        }
    }

    /// <summary>
    /// 收集并构建根目录下各个子文件夹对应的资源包
    /// 比如 A文件夹下有 B C 文件 , B含有D文件 C含有E文件
    /// 则会构建出 模块名_B , 模块名_C 资源包, 并且会指向 D E 路径的资源
    /// </summary>
    public static void BuildRootSubFolder()
    {
        if (moduleData is null || moduleData.subFolderPacks is null || moduleData.subFolderPacks.Length == 0)
            return;

        foreach (var item in moduleData.subFolderPacks)
        {
            var subPathList = Directory.GetDirectories(item + "/");
            foreach (var subItem in subPathList)
            {
                var subPath = subItem.Replace(@"\", "/");
                var nameIndex = subPath.LastIndexOf("/") + 1;
                var bundleName = GenerateBundleName(subPath.Substring(nameIndex, subPath.Length - nameIndex));

                if (IsRepeatPath(subPath))
                    continue;

                allBundlePathList.Add(subPath);
                if (!allFolderBundleDict.ContainsKey(bundleName))
                    allFolderBundleDict[bundleName] = new List<string>();

                foreach (var filePath in Directory.GetFiles(subPath, "*", SearchOption.AllDirectories))
                {
                    if (filePath.EndsWith(".cs") || filePath.EndsWith(".meta"))
                        continue;

                    var abFilePath = filePath.Replace(@"\", "/");
                    if (IsRepeatPath(abFilePath))
                        continue;

                    allBundlePathList.Add(abFilePath);
                    if (allFolderBundleDict.ContainsKey(bundleName))
                        allFolderBundleDict[bundleName].Add(abFilePath);
                    else
                        allFolderBundleDict[bundleName] = new List<string> { abFilePath };
                }
            }
        }
    }

    /// <summary>
    /// 收集并构建所有 Prefab 相关的资源包
    /// 比如 A.prefab 依赖 B.prefab C.prefab
    /// 则会构建出 模块名_A 资源包, 并且会指向 B C 路径的资源
    /// </summary>
    public static void BuildAllPrefab()
    {
        if (moduleData is null || moduleData.prefabPacks is null || moduleData.prefabPacks.Length == 0)
            return;

        // 获取所有预制体的GUID
        var prefabGuidList = AssetDatabase.FindAssets("t:Prefab", moduleData.prefabPacks);
        foreach (var item in prefabGuidList)
        {
            var prefabPath = AssetDatabase.GUIDToAssetPath(item);
            var bundleName = GenerateBundleName(Path.GetFileNameWithoutExtension(prefabPath));

            // 保留跨包重复依赖 后续统一抽取共享包
            var dependencies = AssetDatabase.GetDependencies(prefabPath, true);
            List<string> dependencyList = new List<string>();
            foreach (var dependency in dependencies)
            {
                if (!IsBuildableAsset(dependency))
                    continue;

                if (!allBundlePathList.Contains(dependency))
                    allBundlePathList.Add(dependency);
                if (!dependencyList.Contains(dependency))
                    dependencyList.Add(dependency);
            }

            if (allPrefabBundleDict.ContainsKey(bundleName))
                allPrefabBundleDict[bundleName].AddRange(dependencyList);
            else
                allPrefabBundleDict[bundleName] = dependencyList;

        }
    }

    /// <summary>
    /// 根据前面收集到的资源路径，执行最终的 AssetBundle 构建
    /// </summary>
    public static void BuildAllAssetBundle()
    {
        ModifyAllFileBundleName();
        try
        {
            WriteAssetBundleConfig();
            AssetDatabase.Refresh();

            var builds = GetModuleAssetBundleBuilds();
            if (builds.Length == 0)
                throw new InvalidOperationException("没有可构建的模块资源包");

            var manifest = BuildPipeline.BuildAssetBundles(
                bundleOutputPath,
                builds,
                (UnityEditor.BuildAssetBundleOptions)Enum.Parse(
                    typeof(UnityEditor.BuildAssetBundleOptions),
                    BundleSettings.Instance.buildAssetBundleOptions.ToString()),
                (UnityEditor.BuildTarget)Enum.Parse(
                    typeof(UnityEditor.BuildTarget),
                    BundleSettings.Instance.buildTarget.ToString()));
            if (manifest is null)
                throw new InvalidOperationException("构建资源包失败");

            DeletAllManifestFile();
            EncryptAllAssetBundle();
            if (buildBundleType == E_EditorBuildKind.HotPatch
                && moduleData.deliveryMode != E_BundleDeliveryMode.BuiltIn)
                GeneratorHotAssets();
            GenerateBuildReport();
            Debug.Log($"构建资源包成功: {bundleOutputPath}");
        }
        finally
        {
            ModifyAllFileBundleName(true);
            // 保留 Generated AbConfig 供 Editor 短名加载 不再 DeleteAsset
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>
    /// 修改所有资源的ab包名称 (状态)
    /// </summary>
    public static void ModifyAllFileBundleName(bool clearName = false)
    {
        int i = 0;
        // 给真实的资源临时写入ab包名称 其中ab包名称是工具的Build得到的
        foreach (var item in allFolderBundleDict)
        {
            i++;
            EditorUtility.DisplayProgressBar("Modify Name", item.Key, i * 1.0f / allBundlePathList.Count);
            foreach (var path in item.Value)
            {
                var importAsset = AssetImporter.GetAtPath(path);
                if (importAsset is not null)
                {
                    importAsset.assetBundleName = clearName
                        ? ""
                        : item.Key + BundleSettings.BundleFileExtension;
                }
            }
        }

        i = 0;
        // 给预制体临时写入ab包名称
        foreach (var item in allPrefabBundleDict)
        {
            i++;
            EditorUtility.DisplayProgressBar("Modify Name", item.Key, i * 1.0f / allPrefabBundleDict.Count);
            foreach (var path in item.Value)
            {
                var importAsset = AssetImporter.GetAtPath(path);
                if (importAsset is not null)
                {
                    importAsset.assetBundleName = clearName
                        ? ""
                        : item.Key + BundleSettings.BundleFileExtension;
                }
            }
        }

        i = 0;
        // 给场景资源临时写入ab包名称
        foreach (var item in allSceneBundleDict)
        {
            i++;
            EditorUtility.DisplayProgressBar("Modify Name", item.Key, i * 1.0f / allSceneBundleDict.Count);
            foreach (var path in item.Value)
            {
                var importAsset = AssetImporter.GetAtPath(path);
                if (importAsset is not null)
                    importAsset.assetBundleName = clearName
                        ? ""
                        : item.Key + BundleSettings.BundleFileExtension;
            }
        }

        if (clearName)
        {
            // 清除掉 Json 配置文件的临时 ab 包名
            var importAsset = AssetImporter.GetAtPath(bundleConfigAssetPath);
            if (importAsset is not null)
                importAsset.assetBundleName = "";

            // 清除未使用的 ab 包名称
            AssetDatabase.RemoveUnusedAssetBundleNames();
        }
    }

  
    #endregion

    
    #region 内嵌
    /// <summary>
    /// 将资源包复制到 StreamingAssets 目录下
    /// </summary>
    public static void CopyBundleToStreamingAssets(BundleModuleData bundleModuleData, bool showTips = false)
    {
        if (bundleModuleData.deliveryMode == E_BundleDeliveryMode.HotUpdate)
        {
            Debug.LogWarning("纯热更模块不复制到 StreamingAssets");
            return;
        }

        bundleModuleEnum = Enum.Parse<BundleModuleEnum>(bundleModuleData.moduleName);
        // 获取资源包路径
        DirectoryInfo directoryInfo = new DirectoryInfo(bundleOutputPath);
        // 获取所有资源包文件
        FileInfo[] fileInfoArr = directoryInfo.GetFiles("*", SearchOption.AllDirectories);

        FileHelper.DeleteFolder(standardStreamingAssetsPath);
        Directory.CreateDirectory(standardStreamingAssetsPath);

        int i = 0;
        var builtInBundleConfig = new BuiltInBundleConfig();
        builtInBundleConfig.builtInBundleInfoList = new List<BuiltInBundleInfo>();
        // 复制资源包文件
        foreach (var item in fileInfoArr)
        {
            if (!IsModuleBundleFile(item.Name))
                continue;

            EditorUtility.DisplayProgressBar("复制文件", "Name:" + item.Name, i * 1.0f / fileInfoArr.Length);
            File.Copy(item.FullName, standardStreamingAssetsPath + item.Name);
            var builtInBundleInfo = new BuiltInBundleInfo();
            // 填充信息
            builtInBundleInfo.fileName = item.Name;
            builtInBundleInfo.md5 = MD5.GetMd5FromFile(item.FullName);
            builtInBundleInfo.size = item.Length / 1024f;
            builtInBundleConfig.builtInBundleInfoList.Add(builtInBundleInfo);
            i++;
        }
        // 转json 将json写入到内置资源路径下
        if (!Directory.Exists(builtInResourcePath))
        {
            Directory.CreateDirectory(builtInResourcePath);
        }

        var json = JsonConvert.SerializeObject(builtInBundleConfig, Formatting.Indented);
        var jsonData = Encoding.UTF8.GetBytes(json);
        // 写入一个内置资源包的信息说明文件
        string manifestName = bundleModuleEnum.ToString().ToLowerInvariant() + "_builtin.json";
        FileHelper.WriteFile(builtInResourcePath + manifestName, jsonData);
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();

        if (showTips)
        {
            EditorUtility.DisplayDialog("内嵌操作", "内置资源包构建成功", "确定");
        }

        Debug.Log("内置资源包构建成功");
    }
    #endregion
}
}
