using UnityEngine;

namespace Editor.Protobuf
{
    /// <summary>
    /// EditorPrefs 封装（兼容不同 Unity 版本）
    /// </summary>
    public static class EditorPrefsHelper
    {
        public static bool HasKey(string key) => UnityEditor.EditorPrefs.HasKey(key);
        public static void DeleteKey(string key) => UnityEditor.EditorPrefs.DeleteKey(key);

        public static string GetString(string key, string defaultValue = "")
        {
            return UnityEditor.EditorPrefs.GetString(key, defaultValue);
        }

        public static void SetString(string key, string value)
        {
            UnityEditor.EditorPrefs.SetString(key, value);
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            return UnityEditor.EditorPrefs.GetInt(key, defaultValue);
        }

        public static void SetInt(string key, int value)
        {
            UnityEditor.EditorPrefs.SetInt(key, value);
        }

        public static bool GetBool(string key, bool defaultValue = false)
        {
            return UnityEditor.EditorPrefs.GetBool(key, defaultValue);
        }

        public static void SetBool(string key, bool value)
        {
            UnityEditor.EditorPrefs.SetBool(key, value);
        }
    }
}
