using System.Collections.Generic;
using MessagePack;

namespace MiMieSaver
{
    /// <summary>
    /// 存档槽索引数据
    /// </summary>
    [MessagePackObject]
    public sealed partial class SlotsIndexData
    {
        /// <summary>
        /// 当前槽位 ID
        /// </summary>
        [Key(0)]
        public string CurrentSlotId { get; set; }

        /// <summary>
        /// 所有槽位
        /// </summary>
        [Key(1)]
        public List<SlotData> SlotList { get; set; } = new List<SlotData>();
    }
}
