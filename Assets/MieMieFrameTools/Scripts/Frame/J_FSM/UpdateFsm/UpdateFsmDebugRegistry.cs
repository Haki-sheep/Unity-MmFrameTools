namespace MiMieFSM.UpdateFsm
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// UpdateFSM 调试注册表
    /// </summary>
    public static class UpdateFsmDebugRegistry
    {
        /// <summary>
        /// 调试条目
        /// </summary>
        public struct DebugEntry
        {
            /// <summary>
            /// 显示标签
            /// </summary>
            public string Label;

            /// <summary>
            /// 状态文本获取
            /// </summary>
            public Func<string> GetStateText;
        }

        /// <summary>
        /// 条目列表
        /// </summary>
        private static readonly List<DebugEntry> entryList = new();

        /// <summary>
        /// 只读条目列表
        /// </summary>
        public static IReadOnlyList<DebugEntry> EntryList => entryList;

        /// <summary>
        /// 注册调试条目
        /// </summary>
        public static void Register(string label, Func<string> getStateText)
        {
            Unregister(label);
            entryList.Add(new DebugEntry
            {
                Label = label,
                GetStateText = getStateText
            });
        }

        /// <summary>
        /// 移除调试条目
        /// </summary>
        public static void Unregister(string label)
        {
            for (int i = entryList.Count - 1; i >= 0; i--)
            {
                if (entryList[i].Label == label)
                    entryList.RemoveAt(i);
            }
        }

        /// <summary>
        /// 清空全部条目
        /// </summary>
        public static void Clear()
        {
            entryList.Clear();
        }
    }
}
