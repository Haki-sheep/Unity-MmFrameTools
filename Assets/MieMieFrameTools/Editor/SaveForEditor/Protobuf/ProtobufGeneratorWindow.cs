// Assets/Editor/Protobuf/ProtobufGeneratorWindow.cs
// UI Toolkit 主窗口

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Protobuf
{
    public class ProtobufGeneratorWindow : EditorWindow
    {
        private TextField _protocPathField;
        private TextField _protoPathField;
        private TextField _outputPathField;
        private TextField _namespaceField;
        private Label _protocStatusLabel;
        private Label _protoCountLabel;
        private ListView _protoListView;
        private TextField _logTextField;
        private ScrollView _logScrollView;
        private List<string> _protoFiles = new();

        public static void ShowWindow()
        {
            var window = GetWindow<ProtobufGeneratorWindow>(false, "Protobuf 生成器", true);
            window.minSize = new Vector2(520, 560);
            window.Show();
        }

        private void OnEnable()
        {
            ProtobufSettingsStore.EnsureLoaded();
            CreateUI();
            RefreshProtoList();
        }

        private void OnDisable()
        {
            SaveSettingsToJson();
        }

        private void CreateUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/MieMieFrameTools/Editor/SaveForEditor/Protobuf/UI/ProtobufGeneratorWindow.uxml");
            visualTree.CloneTree(rootVisualElement);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/MieMieFrameTools/Editor/SaveForEditor/Protobuf/UI/ProtobufGeneratorWindow.uss");
            rootVisualElement.styleSheets.Add(styleSheet);

            rootVisualElement.style.flexGrow = 1;
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.minHeight = 0;

            var data = ProtobufSettingsStore.Data;

            _protocPathField = rootVisualElement.Q<TextField>("protocPath");
            _protoPathField = rootVisualElement.Q<TextField>("protoPath");
            _outputPathField = rootVisualElement.Q<TextField>("outputPath");
            _namespaceField = rootVisualElement.Q<TextField>("namespace");
            _protocStatusLabel = rootVisualElement.Q<Label>("protocStatus");
            _protoCountLabel = rootVisualElement.Q<Label>("protoCount");
            _protoListView = rootVisualElement.Q<ListView>("protoList");
            _logTextField = rootVisualElement.Q<TextField>("logText");
            _logScrollView = rootVisualElement.Q<ScrollView>("logScroll");
            _logTextField.multiline = true;
            _logTextField.isReadOnly = true;

            var protoScrollView = _protoListView.Q<ScrollView>();
            if (protoScrollView != null)
                protoScrollView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            _logScrollView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;

            _protocPathField.value = data.protocPath;
            _protoPathField.value = data.protoDirectory;
            _outputPathField.value = data.outputDirectory;
            _namespaceField.value = data.csharpNamespace;

            rootVisualElement.Q<Button>("openDownloadLinks").clicked += () => ProtobufDownloadWindow.Open();

            rootVisualElement.Q<Button>("browseProtoc").clicked += OnBrowseProtoc;
            rootVisualElement.Q<Button>("resetProtoc").clicked += OnResetProtoc;
            rootVisualElement.Q<Button>("openProto").clicked += OnOpenProtoFolder;
            rootVisualElement.Q<Button>("browseProto").clicked += OnBrowseProto;
            rootVisualElement.Q<Button>("openOutput").clicked += OnOpenOutputFolder;
            rootVisualElement.Q<Button>("browseOutput").clicked += OnBrowseOutput;

            rootVisualElement.Q<Button>("copyLog").clicked += OnCopyLog;
            rootVisualElement.Q<Button>("clearLog").clicked += OnClearLog;
            rootVisualElement.Q<Button>("compileAll").clicked += OnCompileAll;
            rootVisualElement.Q<Button>("compileSelected").clicked += OnCompileSelected;

            void BindSave(TextField field, Action<string> apply)
            {
                field.RegisterValueChangedCallback(evt =>
                {
                    apply(evt.newValue);
                    SaveSettingsToJson();
                });
            }

            _protocPathField.RegisterValueChangedCallback(evt =>
            {
                ProtobufSettingsStore.Data.protocPath = evt.newValue;
                SaveSettingsToJson();
                SyncProtocGeneratorPath();
                UpdateProtocStatus();
            });
            BindSave(_protoPathField, v => ProtobufSettingsStore.Data.protoDirectory = v);
            BindSave(_outputPathField, v => ProtobufSettingsStore.Data.outputDirectory = v);
            BindSave(_namespaceField, v => ProtobufSettingsStore.Data.csharpNamespace = v);

            _protoListView.makeItem = () => new Label();
            _protoListView.bindItem = (element, index) =>
            {
                if (element is Label label && index < _protoFiles.Count)
                    label.text = _protoFiles[index];
            };
            _protoListView.selectionType = SelectionType.Multiple;
            _protoListView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            _protoListView.fixedItemHeight = 22f;

            _protoPathField.RegisterValueChangedCallback(_ => RefreshProtoList());

            SyncProtocGeneratorPath();
            UpdateProtocStatus();
        }

        private void SaveSettingsToJson()
        {
            ProtobufSettingsStore.EnsureLoaded();
            var d = ProtobufSettingsStore.Data;
            d.protocPath = _protocPathField?.value ?? d.protocPath;
            d.protoDirectory = _protoPathField?.value ?? d.protoDirectory;
            d.outputDirectory = _outputPathField?.value ?? d.outputDirectory;
            d.csharpNamespace = _namespaceField?.value ?? d.csharpNamespace;
            ProtobufSettingsStore.Save();
        }

        private void SyncProtocGeneratorPath()
        {
            if (_protocPathField != null)
                ProtocGenerator.ProtocPath = _protocPathField.value;
        }

        private void UpdateProtocStatus()
        {
            string exe = _protocPathField?.value ?? "";
            bool ok = !string.IsNullOrEmpty(exe) && File.Exists(exe);
            if (ok)
            {
                _protocStatusLabel.text = "状态: 就绪";
                _protocStatusLabel.AddToClassList("success");
                _protocStatusLabel.RemoveFromClassList("error");
            }
            else
            {
                _protocStatusLabel.text = "状态: 未找到 protoc";
                _protocStatusLabel.AddToClassList("error");
                _protocStatusLabel.RemoveFromClassList("success");
            }
        }

        private void RefreshProtoList()
        {
            _protoFiles.Clear();
            string protoDir = _protoPathField.value;

            if (Directory.Exists(protoDir))
            {
                _protoFiles.AddRange(Directory.GetFiles(protoDir, "*.proto", SearchOption.AllDirectories));
            }

            _protoListView.itemsSource = _protoFiles;
            _protoListView.Rebuild();

            _protoCountLabel.text = $"共 {_protoFiles.Count} 个 proto 文件";
        }

        private void OnBrowseProtoc()
        {
            string path = EditorUtility.OpenFilePanel("选择 protoc 编译器", "", "exe");
            if (!string.IsNullOrEmpty(path))
            {
                _protocPathField.value = path;
                ProtobufSettingsStore.Data.protocPath = path;
                ProtobufSettingsStore.Save();
                SyncProtocGeneratorPath();
                UpdateProtocStatus();
            }
        }

        private void OnResetProtoc()
        {
            string def = ProtocGenerator.DefaultProtocPath;
            _protocPathField.value = def;
            ProtobufSettingsStore.Data.protocPath = def;
            ProtobufSettingsStore.Save();
            SyncProtocGeneratorPath();
            UpdateProtocStatus();
        }

        private void OnOpenProtoFolder()
        {
            string p = _protoPathField.value;
            if (string.IsNullOrEmpty(p) || !Directory.Exists(p))
            {
                EditorUtility.DisplayDialog("打开目录", "proto 目录不存在或为空。", "确定");
                return;
            }

            EditorUtility.RevealInFinder(p);
        }

        private void OnBrowseProto()
        {
            string path = EditorUtility.OpenFolderPanel("选择 proto 目录", _protoPathField.value, "");
            if (!string.IsNullOrEmpty(path))
            {
                _protoPathField.value = path;
                ProtobufSettingsStore.Data.protoDirectory = path;
                ProtobufSettingsStore.Save();
            }
        }

        private void OnOpenOutputFolder()
        {
            string p = _outputPathField.value;
            if (string.IsNullOrEmpty(p) || !Directory.Exists(p))
            {
                EditorUtility.DisplayDialog("打开目录", "输出目录不存在或为空。", "确定");
                return;
            }

            EditorUtility.RevealInFinder(p);
        }

        private void OnBrowseOutput()
        {
            string path = EditorUtility.OpenFolderPanel("选择输出目录", _outputPathField.value, "");
            if (!string.IsNullOrEmpty(path))
            {
                _outputPathField.value = path;
                ProtobufSettingsStore.Data.outputDirectory = path;
                ProtobufSettingsStore.Save();
            }
        }

        private void OnCopyLog()
        {
            string text = _logTextField?.value ?? string.Empty;
            EditorGUIUtility.systemCopyBuffer = text;
            if (string.IsNullOrEmpty(text))
                EditorUtility.DisplayDialog("复制日志", "当前没有日志内容。", "确定");
            else
                Debug.Log($"[Protobuf] 已复制 {text.Length} 字符到剪贴板");
        }

        private void OnClearLog()
        {
            _logTextField.value = string.Empty;
        }

        private void OnCompileAll()
        {
            if (_protoFiles.Count == 0)
            {
                AddLog("[Warning] 没有找到 proto 文件");
                return;
            }

            CompileProtoFiles(_protoFiles);
        }

        private void OnCompileSelected()
        {
            var selected = _protoListView.selectedIndices;
            if (selected == null || !selected.Any())
            {
                AddLog("[Warning] 请先选择要编译的 proto 文件");
                return;
            }

            var selectedFiles = new List<string>();
            foreach (var index in selected)
            {
                if (index >= 0 && index < _protoFiles.Count)
                    selectedFiles.Add(_protoFiles[index]);
            }

            CompileProtoFiles(selectedFiles);
        }

        private void CompileProtoFiles(List<string> files)
        {
            SyncProtocGeneratorPath();
            if (!ProtocGenerator.IsConfigured)
            {
                AddLog("[Error] protoc 路径无效，请检查「编译器路径」");
                return;
            }

            AddLog("========== 开始编译 ==========");

            string outputDir = _outputPathField.value;
            string @namespace = _namespaceField.value;
            int successCount = 0;

            foreach (var protoPath in files)
            {
                bool success = ProtocGenerator.Compile(protoPath, outputDir, @namespace, AddLog);
                if (success) successCount++;
            }

            AddLog($"========== 编译完成: {successCount}/{files.Count} ==========");

            AssetDatabase.Refresh();
        }

        private void AddLog(string message)
        {
            if (_logTextField == null) return;
            _logTextField.value = string.IsNullOrEmpty(_logTextField.value)
                ? message
                : _logTextField.value + "\n" + message;
        }
    }
}
