namespace MieMieFrameWork
{
    using Sirenix.OdinInspector;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Video;

    #region 本地化组件类型
    public interface L_Object { }
    public class L_Text : L_Object
    {
        public string content;
    }

    [Serializable]
    public class L_Tmp : L_Object
    {
        public string content;
    }
    [Serializable]

    public class L_Sprite : L_Object
    {
        public Sprite content;
    }
    [Serializable]

    public class L_Image : L_Object
    {
        public Sprite content;
    }
    [Serializable]

    public class L_Video : L_Object
    {
        public VideoClip content;
    }
    [Serializable]
    public class L_Audio : L_Object
    {
        public AudioClip content;
    }
    #endregion

    // 语言类型枚举
    public enum E_LanuageType
    {
        ChineseSimple = 0,
        English = 1,
    }   

    [CreateAssetMenu(fileName = "LanuageSettingFile", menuName = "MieMieFrameTools/Localization/LanuageSetting")]
    public class LocalizationSetting : SerializedScriptableObject
    {
        // 简化后的双层字典结构：组合键（typeName_contentKey） -> 语言类型 -> 本地化对象
        [SerializeField]
        [DictionaryDrawerSettings(
            DisplayMode = DictionaryDisplayOptions.ExpandedFoldout,
            KeyLabel = "内容标识(类型_键值)",
            ValueLabel = "语言映射")]
        public Dictionary<string, Dictionary<E_LanuageType, L_Object>> dataBag = new Dictionary<string, Dictionary<E_LanuageType, L_Object>>();

        /// <summary>
        /// 获取本地化内容的封装方法
        /// </summary>
        /// <typeparam name="T">内容类型</typeparam>
        /// <param name="typeName">类型名称</param>
        /// <param name="contentKey">内容key</param>
        /// <param name="e_type">语言类型</param>
        /// <returns></returns>
        public T GetData<T>(string typeName, string contentKey, E_LanuageType e_type) where T : class, L_Object
        {
            string compositeKey = $"{typeName}_{contentKey}";
            if (dataBag.TryGetValue(compositeKey, out var languageDict) && 
                languageDict.TryGetValue(e_type, out var obj))
            {
                return obj as T;
            }
            return null;
        }

        /// <summary>
        /// 添加本地化内容
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <param name="contentKey">内容key</param>
        /// <param name="language">语言类型</param>
        /// <param name="content">本地化对象</param>
        public void SetData(string typeName, string contentKey, E_LanuageType language, L_Object content)
        {
            string compositeKey = $"{typeName}_{contentKey}";
            if (!dataBag.ContainsKey(compositeKey))
            {
                dataBag[compositeKey] = new Dictionary<E_LanuageType, L_Object>();
            }
            dataBag[compositeKey][language] = content;
        }

        /// <summary>
        /// 移除指定的本地化内容
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <param name="contentKey">内容key</param>
        /// <param name="language">语言类型</param>
        /// <returns></returns>
        public bool RemoveData(string typeName, string contentKey, E_LanuageType language)
        {
            string compositeKey = $"{typeName}_{contentKey}";
            if (dataBag.TryGetValue(compositeKey, out var languageDict))
            {
                bool removed = languageDict.Remove(language);
                if (languageDict.Count == 0)
                {
                    dataBag.Remove(compositeKey);
                }
                return removed;
            }
            return false;
        }

        /// <summary>
        /// 清空所有本地化数据
        /// </summary>
        public void ClearAllData()
        {
            dataBag.Clear();
        }
    }
}


