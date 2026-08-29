using System.Collections.Generic;
using MessagePack;

namespace Game.Save
{
    /// <summary>
    /// 游戏存档数据
    /// </summary>
    [MessagePackObject]
    public sealed partial class SaveData
    {
        /// <summary>
        /// 存档元信息
        /// </summary>
        [Key(0)]
        public MetaSave Meta { get; set; }

        /// <summary>
        /// 玩家模块数据
        /// </summary>
        [Key(1)]
        public PlayerModuleSave Player { get; set; }

        /// <summary>
        /// 装备模块数据
        /// </summary>
        [Key(2)]
        public EquipmentModuleSave Equpment { get; set; }
    }

    /// <summary>
    /// 存档元信息
    /// </summary>
    [MessagePackObject]
    public sealed partial class MetaSave
    {
        /// <summary>
        /// 数据版本
        /// </summary>
        [Key(0)]
        public int Version { get; set; }

        /// <summary>
        /// 存档创建时间
        /// </summary>
        [Key(1)]
        public long CreatTime { get; set; }

        /// <summary>
        /// 最后存档时间
        /// </summary>
        [Key(2)]
        public long LastSaveTime { get; set; }
    }

    /// <summary>
    /// 玩家模块存档数据
    /// </summary>
    [MessagePackObject]
    public sealed partial class PlayerModuleSave
    {
        /// <summary>
        /// 玩家唯一 ID
        /// </summary>
        [Key(0)]
        public string PlayerId { get; set; }

        /// <summary>
        /// 玩家名称
        /// </summary>
        [Key(1)]
        public string PlayerName { get; set; }

        /// <summary>
        /// 创建时间戳
        /// </summary>
        [Key(2)]
        public long CreateTime { get; set; }
    }

    /// <summary>
    /// 装备模块存档数据
    /// </summary>
    [MessagePackObject]
    public sealed partial class EquipmentModuleSave
    {
        /// <summary>
        /// 装备列表
        /// </summary>
        [Key(0)]
        public List<EquipmentItem> Items { get; set; } = new List<EquipmentItem>();

        /// <summary>
        /// 当前装备方案索引
        /// </summary>
        [Key(1)]
        public int ActiveSetIndex { get; set; }
    }

    /// <summary>
    /// 装备数据
    /// </summary>
    [MessagePackObject]
    public sealed partial class EquipmentItem
    {
        /// <summary>
        /// 装备实例 ID
        /// </summary>
        [Key(0)]
        public string InstanceId { get; set; }

        /// <summary>
        /// 装备等级
        /// </summary>
        [Key(1)]
        public int Level { get; set; }

        /// <summary>
        /// 装备槽位类型
        /// </summary>
        [Key(2)]
        public int SlotType { get; set; }
    }
}
