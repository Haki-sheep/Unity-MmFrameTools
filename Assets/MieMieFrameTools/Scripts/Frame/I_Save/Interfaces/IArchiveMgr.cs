using System.Collections.Generic;
using Game.Save;

namespace MiMieSaver
{
    /// <summary>
    /// 存档管理器核心接口
    /// </summary>
    public interface IArchiveMgr
    {
        #region 属性

        /// <summary>
        /// 存档根目录路径
        /// </summary>
        string RootPath { get; }

        #endregion

        #region 存档槽操作

        /// <summary>
        /// 获取所有存档槽的索引信息
        /// </summary>
        ISlotIndex GetAllSlotIndex();

        /// <summary>
        /// 创建一个新的存档槽
        /// </summary>
        ISlot CreatSlot(string displayerName);

        /// <summary>
        /// 切换当前使用的存档槽
        /// </summary>
        void SwitchSlot(string slotId);

        /// <summary>
        /// 删除指定的存档槽
        /// </summary>
        void DeleteSlot(string slotId);

        /// <summary>
        /// 重命名指定的存档槽
        /// </summary>
        void RenameSlot(string slotId, string newName);

        #endregion

        #region 存档读写

        /// <summary>
        /// 获取当前激活槽位的存档数据
        /// </summary>
        SaveData GetArchive();

        /// <summary>
        /// 保存存档
        /// </summary>
        void Save();

        /// <summary>
        /// 加载存档
        /// </summary>
        void Load();

        /// <summary>
        /// 判断当前激活的存档槽是否存在有效存档数据
        /// </summary>
        bool HasSaveData();

        #endregion

        #region 模块管理

        /// <summary>
        /// 注册存档模块
        /// </summary>
        void RegisterModule<T>(T module) where T : class, IArchiveModule;

        /// <summary>
        /// 按类型获取模块
        /// </summary>
        T GetModule<T>() where T : class, IArchiveModule;

        /// <summary>
        /// 按类型尝试获取模块
        /// </summary>
        bool TryGetModule<T>(out T module) where T : class, IArchiveModule;

        /// <summary>
        /// 是否已注册指定类型模块
        /// </summary>
        bool HasModule<T>() where T : class, IArchiveModule;

        /// <summary>
        /// 注销指定类型模块
        /// </summary>
        bool UnregisterModule<T>() where T : class, IArchiveModule;

        /// <summary>
        /// 获取按注册顺序排列的模块列表
        /// </summary>
        IReadOnlyList<IArchiveModule> GetModules();

        #endregion
    }
}
