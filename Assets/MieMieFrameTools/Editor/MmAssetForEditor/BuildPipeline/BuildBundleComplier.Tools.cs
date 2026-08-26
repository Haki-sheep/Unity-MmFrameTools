using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;


namespace MieMieFrameWork.Asset
{
public partial class BuildBundleComplier
{

    /// <summary>
    /// 初始化本次构建参数，并清空上一次构建收集到的缓存数据
    /// </summary>
    /// <param name="moduleData">模块构建配置</param>
    /// <param name="buildBundleType">构建类型</param>
    /// <param name="hotPatchVersion">热更新版本号</param>
    /// <param name="updateNotice">热更新公告内容</param>
    public static void InitPack(BundleModuleData moduleData,
                                        E_EditorBuildKind buildBundleType,
                                        string hotPatchVersion = "1.0.0",
                                        string updateNotice = "")
    {

        allBundlePathList.Clear();
        allFolderBundleDict.Clear();
        allPrefabBundleDict.Clear();
        allSceneBundleDict.Clear();
        sharedDependencyPathList.Clear();

        BuildBundleComplier.moduleData = moduleData;
        BuildBundleComplier.buildBundleType = buildBundleType;
        BuildBundleComplier.hotPatchVersion = hotPatchVersion;
        BuildBundleComplier.updateNotice = updateNotice;

        // 模块名称转枚举
        bundleModuleEnum = Enum.Parse<BundleModuleEnum>(moduleData.moduleName);

        // 清理并准备 AB 输出目录
        PrepareOutputDirectory();
    }

    /// <summary>
    /// 准备 AB 输出目录
    /// </summary>
    private static void PrepareOutputDirectory()
    {
        // 清理并准备 AB 输出目录
        var outputDir = bundleOutputPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        FileHelper.DeleteFolder(outputDir);
        Directory.CreateDirectory(outputDir);
        Directory.CreateDirectory(Path.GetDirectoryName(bundleConfigFilePath));
    }

    /// <summary>
    /// 判断给定的资源路径是否已经存在于缓存中
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>是否重复</returns>
    public static bool IsRepeatPath(string path)
    {

        foreach (var item in allBundlePathList)
        {
            if (string.Equals(item, path, StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }


    /// <summary>
    /// 根据资源路径生成资源包名称
    /// 格式：xx模块_xx路径
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>资源包名称</returns>
    public static string GenerateBundleName(string abName)
    {
        ValidateBundleName(abName);
        return bundleModuleEnum.ToString().ToLowerInvariant()
               + "_"
               + abName.ToLowerInvariant();
    }

    /// <summary>
    /// 是否属于当前模块的资源包（排除 Unity manifest 包、Package 自带 AB 等）
    /// </summary>
    private static bool IsModuleBundleFile(string fileName)
    {
        // 排除 Unity manifest 包(忽略大小写)
        if (fileName.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase))
            return false;

        // 必须是约定 AB 后缀 避免旧 .unity 误入
        if (!fileName.EndsWith(BundleSettings.BundleFileExtension, StringComparison.OrdinalIgnoreCase))
            return false;

        // 如果文件名是配置文件名，则属于当前模块的资源包(忽略大小写)
        if (string.Equals(fileName, bundleConfigBundleName, StringComparison.OrdinalIgnoreCase))
            return true;

        // 如果文件名以模块名开头，则属于当前模块的资源包(忽略大小写)
        string prefix = bundleModuleEnum.ToString().ToLowerInvariant() + "_";
        return fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 获取当前模块的资源包构建列表    
    /// </summary>
    /// <returns></returns>
    private static AssetBundleBuild[] GetModuleAssetBundleBuilds()
    {
        var builds = new List<AssetBundleBuild>();
        foreach (var bundleName in AssetDatabase.GetAllAssetBundleNames())
        {
            if (!IsModuleBundleFile(bundleName))
                continue;

            builds.Add(new AssetBundleBuild
            {
                assetBundleName = bundleName,
                assetNames = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName)
            });
        }

        return builds.ToArray();
    }

    /// <summary>
    /// 删除所有 manifest 文件
    /// </summary>
    public static void DeletAllManifestFile()
    {
        foreach (var file in Directory.GetFiles(bundleOutputPath))
        {
            if (file.EndsWith(".manifest") || !IsModuleBundleFile(Path.GetFileName(file)))
                File.Delete(file);
        }
    }

    /// <summary>
    /// 加密所有资源包
    /// </summary>
    public static void EncryptAllAssetBundle()
    {
        if (!BundleSettings.Instance.bundleEncryptToggle.isEncrypt)
            return;

        DirectoryInfo directoryInfo = new DirectoryInfo(bundleOutputPath);
        FileInfo[] fileInfoArr = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
        for (int i = 0; i < fileInfoArr.Length; i++)
        {
            if (BundleSettings.Instance.bundleEncryptToggle.encryptionScope == E_BundleEncryptionScope.ConfigOnly
                && !string.Equals(fileInfoArr[i].Name, bundleConfigBundleName, StringComparison.OrdinalIgnoreCase))
                continue;
            EditorUtility.DisplayProgressBar("加密文件", "Name:" + fileInfoArr[i].Name, i * 1.0f / fileInfoArr.Length);
            AES.AESFileEncrypt(
                fileInfoArr[i].FullName,
                BundleSettings.Instance.bundleEncryptToggle.encryptKey);
        }
    }

    /// <summary>
    /// 生成一整个模块的打包配置文件
    /// </summary>
    public static void WriteAssetBundleConfig()
    {
        var bundleConfig = new BundleConfig();
        bundleConfig.bundleInfoList = new();

        // 反向字典：资源路径 -> 资源包名称
        Dictionary<string, string> allBundleFilePathDict = new();
        // 别名唯一性检查字典
        Dictionary<string, string> aliasPathDict = new(StringComparer.OrdinalIgnoreCase);
        // CRC 碰撞检查字典
        Dictionary<uint, string> crcPathDict = new();

        // 获取所有 assetBundleName
        string[] allBundleArr = AssetDatabase.GetAllAssetBundleNames();

        // 从每一类 ab 包中获取其下的所有资源
        foreach (var bundleName in allBundleArr)
        {
            if (!IsModuleBundleFile(bundleName))
                continue;

            string[] bundleFileArr = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
            // 存储每一个资源对应的包名称
            foreach (var filePath in bundleFileArr)
            {
                if (!filePath.EndsWith(".cs"))
                    allBundleFilePathDict[filePath] = bundleName;
            }
        }

        // 计算 AssetBundle 数据，生成配置文件
        foreach (var item in allBundleFilePathDict)
        {
            // 获取资源路径
            var filepath = item.Key;
            if (filepath.EndsWith(".cs"))
                continue;

            var bundleInfo = new BundleInfo();
            bundleInfo.path = filepath;
            bundleInfo.alias = GenerateAssetAlias(filepath);
            bundleInfo.bundleName = item.Value;
            bundleInfo.assetName = Path.GetFileName(filepath);
            bundleInfo.crc = Crc32.GetCrc32(filepath);
            bundleInfo.dependencielist = new List<string>();
            if (aliasPathDict.TryGetValue(bundleInfo.alias, out var existingPath)
                && !string.Equals(existingPath, filepath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("资源别名重复 " + bundleInfo.alias);
            aliasPathDict[bundleInfo.alias] = filepath;
            if (crcPathDict.TryGetValue(bundleInfo.crc, out var collisionPath)
                && !string.Equals(collisionPath, filepath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "资源 CRC 冲突 " + collisionPath + " " + filepath);
            crcPathDict[bundleInfo.crc] = filepath;

            // 获取具体资源的依赖项
            string[] depence = AssetDatabase.GetDependencies(filepath);
            foreach (var dePath in depence)
            {
                // 排除自身和脚本
                if (!dePath.Equals(filepath) && !dePath.EndsWith(".cs"))
                {
                    // 排除重复依赖项
                    if (allBundleFilePathDict.TryGetValue(dePath, out var assetBundleName) &&
                        !bundleInfo.dependencielist.Contains(assetBundleName))
                    {
                        bundleInfo.dependencielist.Add(assetBundleName);
                    }
                }
            }

            // 将所有资源包信息添加到总配置列表之中
            bundleConfig.bundleInfoList.Add(bundleInfo);
        }

        // 将配置文件写入 Json 并保存到指定路径
        string json = JsonConvert.SerializeObject(bundleConfig, Formatting.Indented);
        File.WriteAllText(bundleConfigFilePath, json);
        AssetDatabase.Refresh();

        var importer = AssetImporter.GetAtPath(bundleConfigAssetPath);
        if (importer is not null)
            importer.assetBundleName = bundleConfigBundleName;
    }

    /// <summary>
    /// 生成资源地址别名
    /// 对齐 AA 主地址为完整路径 短名仅来自 assetAliasList 显式注册
    /// </summary>
    private static string GenerateAssetAlias(string assetPath)
    {
        if (moduleData.shaderVariantCollection != null
            && string.Equals(
                AssetDatabase.GetAssetPath(moduleData.shaderVariantCollection),
                assetPath,
                StringComparison.OrdinalIgnoreCase))
            return "__shader_variants_" + bundleModuleEnum.ToString().ToLowerInvariant();

        if (moduleData.assetAliasList != null)
        {
            foreach (var aliasInfo in moduleData.assetAliasList)
            {
                if (aliasInfo.asset == null)
                    continue;
                string targetPath = AssetDatabase.GetAssetPath(aliasInfo.asset);
                if (string.Equals(targetPath, assetPath, StringComparison.OrdinalIgnoreCase))
                    return aliasInfo.alias;
            }
        }

        // 默认别名 = 完整 Assets 路径 永不因文件名撞车 短名必须显式注册
        return assetPath.Replace('\\', '/');
    }
}
}
