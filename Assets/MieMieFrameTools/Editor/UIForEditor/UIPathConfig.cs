using System;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Editor
{
    /// <summary>
    /// UI路径配置 - 存储可自定义的路径配置
    /// 1. 默认UI脚本生成路径
    /// 2. 预制体bind记录Json文件路径
    /// </summary>
    [Serializable]
    public class UIPathConfigItem
    {
        [Tooltip("预制体GUID")]
        public string prefabGuid;

        [Tooltip("预制体名称")]
        public string prefabName;

        [Tooltip("上次使用的生成脚本路径")]
        public string lastGenScriptPath;
        public UIPathConfigItem() { }

        public UIPathConfigItem(string guid, string name)
        {
            prefabGuid = guid;
            prefabName = name;
        }
    }

    /// <summary>
    /// UIPathConfig - ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "UIPathConfig", menuName = "MieMie/UI路径配置", order = 1)]
    public class UIPathConfig : ScriptableObject
    {
        [Header("=== 可自定义路径 ===")]
        [Tooltip("UI脚本的默认生成路径")]
        public string defaultUIGenScriptPath = "Assets/_Scripts/UI/";

        [Tooltip("预制体bind记录Json文件的保存路径")]
        public string prefabBindJsonPath = "Assets/MieMieFrameTools/Editor/UI/PrefabBindRecords.json";

        [Header("=== GenerateUITemp 面板资源路径 ===")]
        [Tooltip("GenerateUITemp 面板的 UXML 可视化树文件路径")]
        public string generateUITempUxmlPath = "Assets/MieMieFrameTools/Editor/UIForEditor/GenerateUITemp.uxml";

        [Tooltip("GenerateUITemp 面板的 USS 样式文件路径")]
        public string generateUITempUssPath = "Assets/MieMieFrameTools/Editor/UIForEditor/GenerateUITemp.uss";

        [Header("=== 预制体路径记录（自动维护）===")]
        [Tooltip("预制体GUID -> 路径配置的映射")]
        public UIPathConfigItem[] prefabPathRecords = Array.Empty<UIPathConfigItem>();

        // 用于运行时操作的列表
        [NonSerialized]
        public System.Collections.Generic.List<UIPathConfigItem> runtimeRecords = new();

        private void OnEnable()
        {
            // 将数组转换为列表便于操作
            runtimeRecords.Clear();
            if (prefabPathRecords != null)
            {
                runtimeRecords.AddRange(prefabPathRecords);
            }
        }

        /// <summary>
        /// 初始化默认路径
        /// </summary>
        public void InitDefaultToolPaths()
        {
            if (string.IsNullOrEmpty(defaultUIGenScriptPath))
            {
                defaultUIGenScriptPath = "Assets/_Scripts/UI/";
            }
            if (string.IsNullOrEmpty(prefabBindJsonPath))
            {
                prefabBindJsonPath = "Assets/MieMieFrameTools/Editor/UI/PrefabBindRecords.json";
            }
            if (string.IsNullOrEmpty(generateUITempUxmlPath))
            {
                generateUITempUxmlPath = "Assets/MieMieFrameTools/Editor/UIForEditor/GenerateUITemp.uxml";
            }
            if (string.IsNullOrEmpty(generateUITempUssPath))
            {
                generateUITempUssPath = "Assets/MieMieFrameTools/Editor/UIForEditor/GenerateUITemp.uss";
            }
        }

        /// <summary>
        /// 获取 GenerateUITemp UXML 路径
        /// </summary>
        public string GetGenerateUITempUxmlPath()
        {
            return string.IsNullOrEmpty(generateUITempUxmlPath)
                ? "Assets/MieMieFrameTools/Editor/UIForEditor/GenerateUITemp.uxml"
                : generateUITempUxmlPath;
        }

        /// <summary>
        /// 获取 GenerateUITemp USS 路径
        /// </summary>
        public string GetGenerateUITempUssPath()
        {
            return string.IsNullOrEmpty(generateUITempUssPath)
                ? "Assets/MieMieFrameTools/Editor/UIForEditor/GenerateUITemp.uss"
                : generateUITempUssPath;
        }

        /// <summary>
        /// 保存运行时记录到序列化数组
        /// </summary>
        public void SaveRecords()
        {
            prefabPathRecords = runtimeRecords.ToArray();
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// 根据预制体GUID获取记录
        /// </summary>
        public UIPathConfigItem GetRecordByGuid(string guid)
        {
            return runtimeRecords.Find(r => r.prefabGuid == guid);
        }

        /// <summary>
        /// 获取预制体上次使用的生成脚本路径
        /// </summary>
        public string GetLastGenScriptPath(string prefabGuid)
        {
            var record = GetRecordByGuid(prefabGuid);
            return record?.lastGenScriptPath;
        }

        /// <summary>
        /// 更新/新增预制体路径记录
        /// </summary>
        public void SetGenScriptPath(string prefabGuid, string prefabName, string genScriptPath)
        {
            var record = GetRecordByGuid(prefabGuid);
            if (record != null)
            {
                record.lastGenScriptPath = genScriptPath;
                record.prefabName = prefabName;
            }
            else
            {
                runtimeRecords.Add(new UIPathConfigItem(prefabGuid, prefabName)
                {
                    lastGenScriptPath = genScriptPath
                });
            }
            SaveRecords();
        }

        /// <summary>
        /// 清空所有预制体路径记录
        /// </summary>
        public void ClearAllRecords()
        {
            runtimeRecords.Clear();
            prefabPathRecords = Array.Empty<UIPathConfigItem>();
        }

        /// <summary>
        /// 获取默认UI脚本生成路径
        /// </summary>
        public string GetDefaultUIGenScriptPath()
        {
            return string.IsNullOrEmpty(defaultUIGenScriptPath) ? "Assets/_Scripts/UI/" : defaultUIGenScriptPath;
        }

        /// <summary>
        /// 获取预制体Bind记录Json文件路径
        /// </summary>
        public string GetPrefabBindJsonPath()
        {
            return string.IsNullOrEmpty(prefabBindJsonPath) ? "Assets/MieMieFrameTools/Editor/UI/PrefabBindRecords.json" : prefabBindJsonPath;
        }
    }
}
