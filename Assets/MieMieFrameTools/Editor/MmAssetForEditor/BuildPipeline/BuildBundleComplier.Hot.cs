using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Asset
{
    public partial class BuildBundleComplier
    {
        /// <summary>
        /// 生成热更资源包
        /// </summary>
        public static void GeneratorHotAssets()
        {
            FileHelper.DeleteFolder(hotPatchOutputPath);
            Directory.CreateDirectory(hotPatchOutputPath);

            var fileInfoArr = Directory.GetFiles(bundleOutputPath);
            int i = 0;
            foreach (var item in fileInfoArr)
            {
                if (!IsModuleBundleFile(Path.GetFileName(item)))
                    continue;

                i++;
                EditorUtility.DisplayProgressBar("复制文件", "Name:" + Path.GetFileName(item), i * 1.0f / fileInfoArr.Length);
                var disPath = hotPatchOutputPath + Path.GetFileName(item);
                File.Copy(item, disPath);
            }
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
            GeneratorHotAssetsManifest();
            Debug.Log("热更资源包构建成功");
        }

        /// <summary>
        /// 生成热更资源包清单
        /// </summary>
        public static void GeneratorHotAssetsManifest()
        {

            // 设置热更资源包清单信息
            var manifest = new HotAssetsManifest();
            manifest.updateNotice = updateNotice;
            manifest.minClientVersion = BundleSettings.Instance.minimumClientVersion;
            manifest.resourceVersion = hotPatchVersion.ToString();
            manifest.downloadUrl = BundleSettings.Instance.downloadUrl.Trim().TrimEnd('/') +
                                "/HotAssets/" + bundleModuleEnum.ToString().ToLowerInvariant()
                                + "/" + hotPatchVersion
                                + "/" + BundleSettings.Instance.buildTarget;

            // 设置热更资源包信息
            var hotAssetPatch = new HotAssetsPatch();
            hotAssetPatch.version = hotPatchVersion.ToString();


            // 访问热更文件夹获取其下所有文件 然后填充到热更资源包信息中
            var directoryInfo = new DirectoryInfo(hotPatchOutputPath);
            var fileInfoList = directoryInfo.GetFiles();
            foreach (var item in fileInfoList)
            {
                if (!IsModuleBundleFile(item.Name))
                    continue;

                var fileInfo = new HotFileInfo();
                fileInfo.abName = item.Name;
                fileInfo.md5 = MD5.GetMd5FromFile(item.FullName);
                fileInfo.size = item.Length / 1024f;
                fileInfo.sizeBytes = item.Length;
                hotAssetPatch.fileList.Add(fileInfo);
            }

            // 更新清单补丁列表
            manifest.patchList.Add(hotAssetPatch);

            // 将清单信息写入到json文件中
            var json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
            var jsonData = Encoding.UTF8.GetBytes(json);
            Directory.CreateDirectory(hotManifestOutputPath);
            FileHelper.WriteFile(
                Path.Combine(hotManifestOutputPath, "hot_manifest.json"),
                jsonData);
        }
    }
}