using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MmAsset 命令行构建与 CDN 上传入口
/// </summary>

namespace MieMieFrameWork.Asset
{
public static class MmAssetCIBuild
{
    /// <summary>
    /// Unity 命令行构建入口
    /// </summary>
    public static void BuildFromCommandLine()
    {
        if (!MmAssetDiagnostics.ValidateProject())
            throw new InvalidOperationException("MmAsset 自检未通过");

        string buildKind = GetCommandLineArgument("-mmAssetKind", "full");
        string resourceVersion = GetCommandLineArgument("-mmAssetVersion", "1.0.0");
        string targetName = GetCommandLineArgument(
            "-mmAssetTarget",
            BundleSettings.Instance.buildTarget.ToString());
        bool upload = bool.TryParse(
            GetCommandLineArgument("-mmAssetUpload", "false"),
            out bool uploadValue)
            && uploadValue;
        if (!Enum.TryParse(targetName, true, out E_BuildTarget eBuildTarget))
            throw new ArgumentException("不支持的目标平台 " + targetName);
        BundleSettings.Instance.buildTarget = eBuildTarget;

        var eBuildKind = string.Equals(buildKind, "hot", StringComparison.OrdinalIgnoreCase)
            ? E_EditorBuildKind.HotPatch
            : E_EditorBuildKind.AssetBundle;
        var selectedModuleList = GetSelectedModules();
        foreach (var moduleData in selectedModuleList)
        {
            BuildBundleComplier.BuildAsseetBundle(
                moduleData,
                eBuildKind,
                resourceVersion,
                "CI Build");
        }

        if (upload && eBuildKind == E_EditorBuildKind.HotPatch)
            UploadHotPatchAsync(resourceVersion, selectedModuleList).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 上传已构建热更资源
    /// </summary>
    public static async Task UploadHotPatchAsync(
        string resourceVersion,
        IReadOnlyList<BundleModuleData> moduleDataList)
    {
        string uploadUrl = Environment.GetEnvironmentVariable("MMASSET_UPLOAD_URL");
        if (string.IsNullOrWhiteSpace(uploadUrl))
            throw new InvalidOperationException("缺少环境变量 MMASSET_UPLOAD_URL");

        string uploadToken = Environment.GetEnvironmentVariable("MMASSET_UPLOAD_TOKEN");
        using var httpClient = new HttpClient();
        if (!string.IsNullOrWhiteSpace(uploadToken))
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", uploadToken);

        foreach (var moduleData in moduleDataList)
        {
            if (!moduleData.isBuild || moduleData.deliveryMode == E_BundleDeliveryMode.BuiltIn)
                continue;

            string moduleName = moduleData.moduleName.ToLowerInvariant();
            string platformName = BundleSettings.Instance.buildTarget.ToString();
            string moduleRoot = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "BuildOutput",
                "Hot",
                moduleName));
            string patchRoot = Path.Combine(moduleRoot, resourceVersion, platformName);
            foreach (string filePath in Directory.GetFiles(patchRoot))
            {
                string remotePath = uploadUrl.TrimEnd('/')
                                    + "/HotAssets/"
                                    + moduleName
                                    + "/"
                                    + resourceVersion
                                    + "/"
                                    + platformName
                                    + "/"
                                    + Path.GetFileName(filePath);
                await PutFileAsync(httpClient, filePath, remotePath);
            }

            string manifestPath = Path.Combine(moduleRoot, "hot_manifest.json");
            string manifestUrl = uploadUrl.TrimEnd('/')
                                 + "/HotAssets/"
                                 + moduleName
                                 + "/hot_manifest.json";
            await PutFileAsync(httpClient, manifestPath, manifestUrl);
        }
    }

    /// <summary>
    /// 获取当前选中的构建模块
    /// </summary>
    public static List<BundleModuleData> GetSelectedModules()
    {
        return BuildBundleConfigura.Instance.bundleModuleDataList.FindAll(data => data.isBuild);
    }

    /// <summary>
    /// HTTP PUT 上传单文件
    /// </summary>
    private static async Task PutFileAsync(
        HttpClient httpClient,
        string filePath,
        string remoteUrl)
    {
        using var fileStream = File.OpenRead(filePath);
        using var content = new StreamContent(fileStream);
        using var response = await httpClient.PutAsync(remoteUrl, content);
        response.EnsureSuccessStatusCode();
        Debug.Log("[MmAsset] 上传完成 " + remoteUrl);
    }

    /// <summary>
    /// 获取命令行参数
    /// </summary>
    private static string GetCommandLineArgument(
        string argumentName,
        string defaultValue)
    {
        var argumentList = Environment.GetCommandLineArgs();
        for (int argumentIndex = 0; argumentIndex < argumentList.Length - 1; argumentIndex++)
        {
            if (string.Equals(argumentList[argumentIndex], argumentName, StringComparison.OrdinalIgnoreCase))
                return argumentList[argumentIndex + 1];
        }
        return defaultValue;
    }
}
}
