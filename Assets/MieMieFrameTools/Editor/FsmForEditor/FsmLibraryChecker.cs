namespace MieMieFrameWork.FSM.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// FSM 库安装检测
    /// </summary>
    public static class FsmLibraryChecker
    {
        /// <summary>
        /// 框架内置核心是否可用
        /// </summary>
        public static bool IsCoreInstalled =>
            Type.GetType("MiMieFSM.UpdateFsm.StateMachine, MieMieFrameWork.Runtime") != null;

        /// <summary>
        /// 绘制未安装提示
        /// </summary>
        public static void DrawNotInstalledHelpBox()
        {
            EditorGUILayout.HelpBox(
                "未检测到框架内置 MiMieFSM 核心\n" +
                "请确认 J_FSM 核心源码已导入并完成脚本编译",
                MessageType.Warning);
        }

        /// <summary>
        /// 绘制顶部安装状态
        /// </summary>
        public static bool DrawInstallGate()
        {
            if (IsCoreInstalled)
            {
                EditorGUILayout.HelpBox("框架内置 MiMieFSM 核心已就绪", MessageType.Info);
                return true;
            }

            DrawNotInstalledHelpBox();
            return false;
        }
    }
}
