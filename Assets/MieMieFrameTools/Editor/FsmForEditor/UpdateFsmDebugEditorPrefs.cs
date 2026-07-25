namespace MieMieFrameWork.FSM.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// UpdateFSM 智能调试偏好 与运行时 UpdateFsmDebugSettings 共用 PlayerPrefs 键
    /// </summary>
    internal static class UpdateFsmDebugEditorPrefs
    {
        private const string PrefsEnabled = "MiMieFSM.UpdateFsm.SmartDebugEnabled";
        private const string PrefsFontSize = "MiMieFSM.UpdateFsm.DebugFontSize";
        private const string PrefsColorR = "MiMieFSM.UpdateFsm.DebugColorR";
        private const string PrefsColorG = "MiMieFSM.UpdateFsm.DebugColorG";
        private const string PrefsColorB = "MiMieFSM.UpdateFsm.DebugColorB";
        private const string PrefsColorA = "MiMieFSM.UpdateFsm.DebugColorA";
        private const string PrefsPosX = "MiMieFSM.UpdateFsm.DebugPosX";
        private const string PrefsPosY = "MiMieFSM.UpdateFsm.DebugPosY";
        private const string PrefsLineHeight = "MiMieFSM.UpdateFsm.DebugLineHeight";

        /// <summary>
        /// 是否启用智能调试
        /// </summary>
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(PrefsEnabled, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(PrefsEnabled, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// 字体大小
        /// </summary>
        public static int FontSize
        {
            get => PlayerPrefs.GetInt(PrefsFontSize, 18);
            set
            {
                PlayerPrefs.SetInt(PrefsFontSize, Mathf.Clamp(value, 8, 48));
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// 文本颜色
        /// </summary>
        public static Color TextColor
        {
            get
            {
                return new Color(
                    PlayerPrefs.GetFloat(PrefsColorR, 0.2f),
                    PlayerPrefs.GetFloat(PrefsColorG, 1f),
                    PlayerPrefs.GetFloat(PrefsColorB, 0.4f),
                    PlayerPrefs.GetFloat(PrefsColorA, 1f));
            }
            set
            {
                PlayerPrefs.SetFloat(PrefsColorR, value.r);
                PlayerPrefs.SetFloat(PrefsColorG, value.g);
                PlayerPrefs.SetFloat(PrefsColorB, value.b);
                PlayerPrefs.SetFloat(PrefsColorA, value.a);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// 屏幕绘制位置
        /// </summary>
        public static Vector2 ScreenPosition
        {
            get
            {
                return new Vector2(
                    PlayerPrefs.GetFloat(PrefsPosX, 12f),
                    PlayerPrefs.GetFloat(PrefsPosY, 12f));
            }
            set
            {
                PlayerPrefs.SetFloat(PrefsPosX, value.x);
                PlayerPrefs.SetFloat(PrefsPosY, value.y);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// 行高
        /// </summary>
        public static float LineHeight
        {
            get => PlayerPrefs.GetFloat(PrefsLineHeight, 24f);
            set
            {
                PlayerPrefs.SetFloat(PrefsLineHeight, Mathf.Max(12f, value));
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// 刷新运行时调试层
        /// </summary>
        public static void RefreshRuntimeOverlay()
        {
            if (!EditorApplication.isPlaying)
                return;

            Type overlayType = Type.GetType("MiMieFSM.Unity.UpdateFsmDebugOverlay, MiMieFSM.Unity");
            overlayType?.GetMethod("RefreshOverlay")?.Invoke(null, null);
        }
    }
}
