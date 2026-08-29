using System.Collections.Generic;

namespace MiMieSaver
{
    /// <summary>
    /// 单个存档槽信息接口
    /// </summary>
    public interface ISlot
    {
        /// <summary>
        /// 存档槽唯一 ID
        /// </summary>
        string SlotId { get; }

        /// <summary>
        /// 存档槽显示名称
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 创建时间戳
        /// </summary>
        long CreateTime { get; }

        /// <summary>
        /// 最后保存时间戳
        /// </summary>
        long LastSaveTime { get; }
    }

    /// <summary>
    /// 存档槽目录只读视图
    /// </summary>
    public interface ISlotIndex
    {
        /// <summary>
        /// 当前选中槽位
        /// </summary>
        ISlot CurrentSlot { get; }

        /// <summary>
        /// 所有槽位列表
        /// </summary>
        IReadOnlyList<ISlot> Slots { get; }

        /// <summary>
        /// 槽位数量
        /// </summary>
        int Count { get; }
    }
}
