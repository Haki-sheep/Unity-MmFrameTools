using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MieMieFrameWork.Editor.ToolsCenter;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Editor.DataForEditor
{
    /// <summary>
    /// Luban 数据工作台
    /// </summary>
    public sealed class LubanWorkbenchWindow : EditorWindow, IMieMieToolsEmbeddedWindow
    {
        /// <summary>
        /// 支持打开的配置表扩展名集合
        /// </summary>
        private static readonly HashSet<string> ExcelExtensionHashList = new(StringComparer.OrdinalIgnoreCase)
        {
            ".xlsx",
            ".xls",
            ".xlsm",
            ".csv"
        };

        /// <summary>
        /// 工作台项目配置
        /// </summary>
        private LubanWorkbenchSettings Settings;

        /// <summary>
        /// 脚本异步执行器
        /// </summary>
        private LubanProcessRunner ProcessRunner;

        /// <summary>
        /// 扫描到的配置表绝对路径列表
        /// </summary>
        private readonly List<string> ExcelFileList = new();

        /// <summary>
        /// 页面主滚动位置
        /// </summary>
        private Vector2 MainScrollPosition;

        /// <summary>
        /// 配置表列表滚动位置
        /// </summary>
        private Vector2 ExcelScrollPosition;

        /// <summary>
        /// 日志滚动位置
        /// </summary>
        private Vector2 LogScrollPosition;

        /// <summary>
        /// 配置表搜索文本
        /// </summary>
        private string SearchText = string.Empty;

        /// <summary>
        /// 当前配置是否尚未保存
        /// </summary>
        private bool SettingsDirty;

        /// <summary>
        /// 当前任务成功后是否刷新 Unity
        /// </summary>
        private bool RefreshAfterRun;

        /// <summary>
        /// 最近一次工作台状态
        /// </summary>
        private string LastStatus = "等待执行";

        /// <summary>
        /// 创建工作台状态
        /// </summary>
        private void OnEnable()
        {
            Settings = LubanWorkbenchSettingsStore.Load();
            ProcessRunner = new LubanProcessRunner();
            ProcessRunner.StateChanged += Repaint;
            ProcessRunner.Completed += OnProcessCompleted;
            RefreshExcelFileList();
        }

        /// <summary>
        /// 释放工作台状态
        /// </summary>
        private void OnDisable()
        {
            if (ProcessRunner == null)
                return;

            ProcessRunner.StateChanged -= Repaint;
            ProcessRunner.Completed -= OnProcessCompleted;
            ProcessRunner.Dispose();
            ProcessRunner = null;
        }

        /// <summary>
        /// 绘制独立编辑器窗口
        /// </summary>
        private void OnGUI()
        {
            DrawEmbeddedGUI();
        }

        /// <summary>
        /// 绘制工具中枢嵌入页面
        /// </summary>
        public void DrawEmbeddedGUI()
        {
            MainScrollPosition = EditorGUILayout.BeginScrollView(MainScrollPosition);
            DrawSettingsPanel();
            EditorGUILayout.Space(8f);
            DrawActionPanel();
            EditorGUILayout.Space(8f);
            DrawExcelPanel();
            EditorGUILayout.Space(8f);
            DrawLogPanel();
            EditorGUILayout.EndScrollView();
        }

        #region Settings

        /// <summary>
        /// 绘制工作台配置面板
        /// </summary>
        private void DrawSettingsPanel()
        {
            EditorGUILayout.LabelField("项目配置", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "路径支持项目相对路径与绝对路径 配置仅决定工作台如何找到 Excel 与脚本 Luban 参数继续由 bat 管理",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            Settings.DataTableDirectoryPath = DrawFolderPathField(
                "配置表目录",
                Settings.DataTableDirectoryPath);
            Settings.GenerateScriptPath = DrawScriptPathField(
                "生成脚本",
                Settings.GenerateScriptPath);
            Settings.ValidateScriptPath = DrawScriptPathField(
                "校验脚本 可选",
                Settings.ValidateScriptPath);
            Settings.RefreshAssetsAfterGenerate = EditorGUILayout.Toggle(
                "生成成功后刷新 Unity",
                Settings.RefreshAssetsAfterGenerate);
            if (EditorGUI.EndChangeCheck())
                SettingsDirty = true;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!SettingsDirty || ProcessRunner.IsRunning))
                {
                    if (GUILayout.Button("保存配置", GUILayout.Width(90f)))
                        SaveSettings();
                }

                if (GUILayout.Button("重新载入", GUILayout.Width(90f)))
                    ReloadSettings();

                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(
                    SettingsDirty ? "有未保存修改" : "配置已保存",
                    EditorStyles.miniLabel,
                    GUILayout.Width(100f));
            }
        }

        /// <summary>
        /// 绘制目录路径字段
        /// </summary>
        /// <param name="label">字段标签</param>
        /// <param name="path">当前路径</param>
        private static string DrawFolderPathField(string label, string path)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string nextPath = EditorGUILayout.TextField(label, path);
                if (!GUILayout.Button("选择", GUILayout.Width(56f)))
                    return nextPath;

                string currentPath = LubanPathUtility.ResolveProjectPath(nextPath);
                string startDirectory = Directory.Exists(currentPath)
                    ? currentPath
                    : LubanPathUtility.ProjectRootPath;
                string selectedPath = EditorUtility.OpenFolderPanel("选择配置表目录", startDirectory, string.Empty);
                return string.IsNullOrEmpty(selectedPath)
                    ? nextPath
                    : LubanPathUtility.ToProjectRelativePath(selectedPath);
            }
        }

        /// <summary>
        /// 绘制脚本路径字段
        /// </summary>
        /// <param name="label">字段标签</param>
        /// <param name="path">当前路径</param>
        private static string DrawScriptPathField(string label, string path)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string nextPath = EditorGUILayout.TextField(label, path);
                if (!GUILayout.Button("选择", GUILayout.Width(56f)))
                    return nextPath;

                string currentPath = LubanPathUtility.ResolveProjectPath(nextPath);
                string startDirectory = File.Exists(currentPath)
                    ? Path.GetDirectoryName(currentPath)
                    : LubanPathUtility.ProjectRootPath;
                string selectedPath = EditorUtility.OpenFilePanel("选择 Luban 脚本", startDirectory, string.Empty);
                return string.IsNullOrEmpty(selectedPath)
                    ? nextPath
                    : LubanPathUtility.ToProjectRelativePath(selectedPath);
            }
        }

        /// <summary>
        /// 保存当前工作台配置
        /// </summary>
        private void SaveSettings()
        {
            LubanWorkbenchSettingsStore.Save(Settings);
            SettingsDirty = false;
            LastStatus = "工作台配置已保存";
            RefreshExcelFileList();
        }

        /// <summary>
        /// 重新载入工作台配置
        /// </summary>
        private void ReloadSettings()
        {
            Settings = LubanWorkbenchSettingsStore.Load();
            SettingsDirty = false;
            LastStatus = "工作台配置已重新载入";
            RefreshExcelFileList();
        }

        #endregion

        #region Actions

        /// <summary>
        /// 绘制脚本操作面板
        /// </summary>
        private void DrawActionPanel()
        {
            EditorGUILayout.LabelField("Luban 操作", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(LastStatus, EditorStyles.helpBox);

            string generateScriptPath = LubanPathUtility.ResolveProjectPath(Settings.GenerateScriptPath);
            string validateScriptPath = LubanPathUtility.ResolveProjectPath(Settings.ValidateScriptPath);
            bool canGenerate = File.Exists(generateScriptPath);
            bool canValidate = File.Exists(validateScriptPath);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(ProcessRunner.IsRunning || !canGenerate))
                {
                    if (GUILayout.Button("一键生成", GUILayout.Height(28f)))
                        RunScript(generateScriptPath, Settings.RefreshAssetsAfterGenerate);
                }

                using (new EditorGUI.DisabledScope(ProcessRunner.IsRunning || !canValidate))
                {
                    if (GUILayout.Button("仅校验", GUILayout.Height(28f)))
                        RunScript(validateScriptPath, false);
                }

                using (new EditorGUI.DisabledScope(!ProcessRunner.IsRunning))
                {
                    if (GUILayout.Button("中止", GUILayout.Width(72f), GUILayout.Height(28f)))
                        ProcessRunner.Cancel();
                }
            }

            if (!canGenerate)
                EditorGUILayout.HelpBox($"未找到生成脚本 {generateScriptPath}", MessageType.Warning);

            if (!string.IsNullOrWhiteSpace(Settings.ValidateScriptPath) && !canValidate)
                EditorGUILayout.HelpBox($"未找到校验脚本 {validateScriptPath}", MessageType.Warning);
        }

        /// <summary>
        /// 启动 Luban 脚本
        /// </summary>
        /// <param name="scriptPath">脚本绝对路径</param>
        /// <param name="refreshAfterRun">成功后是否刷新 Unity</param>
        private void RunScript(string scriptPath, bool refreshAfterRun)
        {
            if (SettingsDirty)
                SaveSettings();

            try
            {
                RefreshAfterRun = refreshAfterRun;
                LastStatus = $"正在执行 {Path.GetFileName(scriptPath)}";
                ProcessRunner.Start(scriptPath);
            }
            catch (Exception exception)
            {
                RefreshAfterRun = false;
                LastStatus = exception.Message;
                Debug.LogException(exception);
            }
        }

        /// <summary>
        /// 处理 Luban 脚本完成事件
        /// </summary>
        /// <param name="successful">脚本是否成功退出</param>
        private void OnProcessCompleted(bool successful)
        {
            LastStatus = successful ? "Luban 脚本执行成功" : "Luban 脚本执行失败";
            if (successful && RefreshAfterRun)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                LastStatus = "Luban 生成成功 Unity 资源已刷新";
            }

            RefreshAfterRun = false;
        }

        #endregion

        #region Excel

        /// <summary>
        /// 绘制配置表面板
        /// </summary>
        private void DrawExcelPanel()
        {
            EditorGUILayout.LabelField("配置表", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                SearchText = EditorGUILayout.TextField("搜索", SearchText);
                if (GUILayout.Button("刷新列表", GUILayout.Width(80f)))
                    RefreshExcelFileList();

                string dataTableDirectoryPath = LubanPathUtility.ResolveProjectPath(Settings.DataTableDirectoryPath);
                using (new EditorGUI.DisabledScope(!Directory.Exists(dataTableDirectoryPath)))
                {
                    if (GUILayout.Button("打开目录", GUILayout.Width(80f)))
                        EditorUtility.RevealInFinder(dataTableDirectoryPath);
                }
            }

            string dataRootPath = LubanPathUtility.ResolveProjectPath(Settings.DataTableDirectoryPath);
            if (!Directory.Exists(dataRootPath))
            {
                EditorGUILayout.HelpBox($"未找到配置表目录 {dataRootPath}", MessageType.Warning);
                return;
            }

            var displayFileList = ExcelFileList
                .Where(path => string.IsNullOrWhiteSpace(SearchText)
                               || path.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            EditorGUILayout.LabelField($"已找到 {displayFileList.Count} 个配置表", EditorStyles.miniLabel);
            ExcelScrollPosition = EditorGUILayout.BeginScrollView(
                ExcelScrollPosition,
                EditorStyles.helpBox,
                GUILayout.MinHeight(150f),
                GUILayout.MaxHeight(280f));
            for (int i = 0; i < displayFileList.Count; i++)
                DrawExcelFile(dataRootPath, displayFileList[i]);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制单个配置表入口
        /// </summary>
        /// <param name="dataRootPath">配置表根目录</param>
        /// <param name="filePath">配置表绝对路径</param>
        private static void DrawExcelFile(string dataRootPath, string filePath)
        {
            string relativePath = filePath.Substring(dataRootPath.TrimEnd(Path.DirectorySeparatorChar).Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(relativePath, EditorStyles.miniLabel);
                if (GUILayout.Button("打开", GUILayout.Width(56f)))
                    EditorUtility.OpenWithDefaultApp(filePath);
                if (GUILayout.Button("定位", GUILayout.Width(56f)))
                    EditorUtility.RevealInFinder(filePath);
            }
        }

        /// <summary>
        /// 重新扫描配置表目录
        /// </summary>
        private void RefreshExcelFileList()
        {
            ExcelFileList.Clear();
            string dataRootPath = LubanPathUtility.ResolveProjectPath(Settings.DataTableDirectoryPath);
            if (!Directory.Exists(dataRootPath))
                return;

            var filePathList = Directory.GetFiles(dataRootPath, "*.*", SearchOption.AllDirectories)
                .Where(path => !Path.GetFileName(path).StartsWith("~$", StringComparison.Ordinal))
                .Where(path => ExcelExtensionHashList.Contains(Path.GetExtension(path)))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
            ExcelFileList.AddRange(filePathList);
        }

        #endregion

        #region Log

        /// <summary>
        /// 绘制脚本日志面板
        /// </summary>
        private void DrawLogPanel()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("执行日志", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("清空", GUILayout.Width(56f)))
                    ProcessRunner.ClearLog();
            }

            LogScrollPosition = EditorGUILayout.BeginScrollView(
                LogScrollPosition,
                EditorStyles.helpBox,
                GUILayout.MinHeight(160f));
            EditorGUILayout.SelectableLabel(
                ProcessRunner.Log,
                EditorStyles.textArea,
                GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        #endregion
    }
}
