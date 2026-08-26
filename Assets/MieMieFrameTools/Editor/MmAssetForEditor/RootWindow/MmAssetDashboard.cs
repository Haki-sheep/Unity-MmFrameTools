using System;
using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MmAsset 编辑器总览页
/// </summary>

namespace MieMieFrameWork.Asset
{
[Serializable]
public sealed class MmAssetDashboard
{
    /// <summary>
    /// 绘制总览
    /// </summary>
    [OnInspectorGUI]
    private void DrawDashboard()
    {
        EditorGUILayout.LabelField("MmAsset 资源管线", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "随包层  热更层  加解密层  加载层\n"
            + "构建产物统一输出到 BuildOutput  运行时数据统一落到 persistentDataPath/MmAsset",
            MessageType.Info);

        GUILayout.Space(8f);
        DrawStatusRow("运行时加载", "CRC 与别名 O1 索引  异步 AB  对象池  模块卸载");
        DrawStatusRow("随包资源", "StreamingAssets 内嵌  移动端按 MD5 提取");
        DrawStatusRow("热更资源", "断点续传  重试  MD5 校验  旧包清理");
        DrawStatusRow("构建管线", "共享依赖自动抽包  Scene  Shader  报告");

        GUILayout.Space(12f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("运行自检", GUILayout.Height(32f)))
            MmAssetDiagnostics.ValidateProject();
        if (GUILayout.Button("打开构建产物", GUILayout.Height(32f)))
            RevealPath(GetBuildOutputPath());
        if (GUILayout.Button("打开 StreamingAssets", GUILayout.Height(32f)))
            RevealPath(Application.streamingAssetsPath);
        if (GUILayout.Button("打开持久化目录", GUILayout.Height(32f)))
            RevealPath(GetPersistentAssetsPath());
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制状态行
    /// </summary>
    private static void DrawStatusRow(string title, string content)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel, GUILayout.Width(88f));
        EditorGUILayout.LabelField(content);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 打开指定目录
    /// </summary>
    private static void RevealPath(string path)
    {
        Directory.CreateDirectory(path);
        EditorUtility.RevealInFinder(path);
    }

    /// <summary>
    /// 获取构建产物目录
    /// </summary>
    private static string GetBuildOutputPath()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "BuildOutput"));
    }

    /// <summary>
    /// 获取运行时持久化资源目录
    /// </summary>
    private static string GetPersistentAssetsPath()
    {
        return Path.Combine(Application.persistentDataPath, "MmAsset");
    }
}
}
