using Game.Save;

namespace MiMieSaver
{
    /// <summary>
    /// 存档模块接口
    /// </summary>
    public interface IArchiveModule
    {
        #region 属性

        /// <summary>
        /// 模块名称
        /// </summary>
        string ModuleName { get; }

        #endregion

        #region 存档读写

        /// <summary>
        /// 创建新存档时的初始化
        /// </summary>
        void CreateArchive(SaveData saveData);

        /// <summary>
        /// 从存档读取数据
        /// </summary>
        void FromArchive(SaveData saveData);

        /// <summary>
        /// 写入存档数据
        /// </summary>
        void ToArchive(SaveData saveData);

        #endregion
    }
}
