using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 自动生成模块枚举与交付方式查询
/// </summary>

namespace MieMieFrameWork.Asset
{
public class BundleEnumCreator
{
    /// <summary>
    /// 模块枚举脚本路径
    /// </summary>
    private static string bundleModuleEnumFilePath =
             MmAssetPaths.ConfigDiskPath + "/BundleModuleEnum.cs";

    /// <summary>
    /// 模块交付方式脚本路径
    /// </summary>
    private static string bundleModuleDeliveryFilePath =
             MmAssetPaths.ConfigDiskPath + "/BundleModuleDelivery.cs";

    [MenuItem("Tools/MieMieFrameWork/MmAsset/生成模块枚举")]
    public static void GenerateBundleModuleEnum()
    {
        List<BundleModuleData> moduleList = BuildBundleConfigura.Instance.bundleModuleDataList;
        Directory.CreateDirectory(MmAssetPaths.ConfigDiskPath);
        WriteModuleEnum(moduleList);
        WriteModuleDelivery(moduleList);
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 写入模块枚举
    /// </summary>
    private static void WriteModuleEnum(List<BundleModuleData> moduleList)
    {
        using var writer = new StreamWriter(
            bundleModuleEnumFilePath,
            false,
            new UTF8Encoding(false));
        writer.WriteLine("// 此脚本由 MmAsset 自动生成 请勿手动修改");
        writer.WriteLine("namespace MieMieFrameWork.Asset");
        writer.WriteLine("{");
        writer.WriteLine("public enum BundleModuleEnum");
        writer.WriteLine("{");
        writer.WriteLine("    None,");
        if (moduleList != null)
        {
            foreach (var moduleData in moduleList)
                writer.WriteLine("    " + moduleData.moduleName + ",");
        }
        writer.WriteLine("}");
        writer.WriteLine("}");
    }

    /// <summary>
    /// 写入模块交付方式查询
    /// </summary>
    private static void WriteModuleDelivery(List<BundleModuleData> moduleList)
    {
        using var writer = new StreamWriter(
            bundleModuleDeliveryFilePath,
            false,
            new UTF8Encoding(false));
        writer.WriteLine("// 此脚本由 MmAsset 自动生成 请勿手动修改");
        writer.WriteLine("namespace MieMieFrameWork.Asset");
        writer.WriteLine("{");
        writer.WriteLine("/// <summary>");
        writer.WriteLine("/// 模块交付方式运行时查询");
        writer.WriteLine("/// </summary>");
        writer.WriteLine("public static class BundleModuleDelivery");
        writer.WriteLine("{");
        writer.WriteLine("    /// <summary>");
        writer.WriteLine("    /// 获取模块交付方式");
        writer.WriteLine("    /// </summary>");
        writer.WriteLine("    public static E_BundleDeliveryMode Get(BundleModuleEnum bundleModuleEnum)");
        writer.WriteLine("    {");
        writer.WriteLine("        switch (bundleModuleEnum)");
        writer.WriteLine("        {");
        if (moduleList != null)
        {
            foreach (var moduleData in moduleList)
            {
                writer.WriteLine("            case BundleModuleEnum." + moduleData.moduleName + ":");
                writer.WriteLine("                return E_BundleDeliveryMode." + moduleData.deliveryMode + ";");
            }
        }
        writer.WriteLine("            default:");
        writer.WriteLine("                return E_BundleDeliveryMode.Hybrid;");
        writer.WriteLine("        }");
        writer.WriteLine("    }");
        writer.WriteLine();
        writer.WriteLine("    /// <summary>");
        writer.WriteLine("    /// 是否需要提取随包资源");
        writer.WriteLine("    /// </summary>");
        writer.WriteLine("    public static bool NeedExtract(BundleModuleEnum bundleModuleEnum)");
        writer.WriteLine("    {");
        writer.WriteLine("        var eDeliveryMode = Get(bundleModuleEnum);");
        writer.WriteLine("        return eDeliveryMode == E_BundleDeliveryMode.BuiltIn");
        writer.WriteLine("               || eDeliveryMode == E_BundleDeliveryMode.Hybrid;");
        writer.WriteLine("    }");
        writer.WriteLine();
        writer.WriteLine("    /// <summary>");
        writer.WriteLine("    /// 是否需要执行热更");
        writer.WriteLine("    /// </summary>");
        writer.WriteLine("    public static bool NeedHotUpdate(BundleModuleEnum bundleModuleEnum)");
        writer.WriteLine("    {");
        writer.WriteLine("        var eDeliveryMode = Get(bundleModuleEnum);");
        writer.WriteLine("        return eDeliveryMode == E_BundleDeliveryMode.HotUpdate");
        writer.WriteLine("               || eDeliveryMode == E_BundleDeliveryMode.Hybrid;");
        writer.WriteLine("    }");
        writer.WriteLine("}");
        writer.WriteLine("}");
    }
}
}
