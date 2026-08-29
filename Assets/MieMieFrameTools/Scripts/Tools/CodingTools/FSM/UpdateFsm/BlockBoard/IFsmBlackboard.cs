namespace MiMieFSM.UpdateFsm
{
    /// <summary>
    /// FSM 黑板接口
    /// </summary>
    public interface IFsmBlackboard
    {
        /// <summary>
        /// 设置数据
        /// </summary>
        void SetValue<T>(EBlockBoardParme key, T value);

        /// <summary>
        /// 获取数据
        /// </summary>
        T GetValue<T>(EBlockBoardParme key, T defaultValue = default);

        /// <summary>
        /// 检查是否存在指定键
        /// </summary>
        bool HasKey(EBlockBoardParme key);

        /// <summary>
        /// 移除数据
        /// </summary>
        void RemoveValue(EBlockBoardParme key);

        /// <summary>
        /// 清空所有数据
        /// </summary>
        void Clear();
    }
}
