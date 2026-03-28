
namespace MieMieFrameWork.FSM
{
    public enum E_BlockBoardParme
    {
        CcGroundCheck,
        CcMovement,
        Mm_InputSystem2D,
        StateMachine
    }
    /// <summary>
    /// FSM黑板接口 - 用于状态间数据共享
    /// </summary>
    public interface I_FsmBlackboard
    {
        /// <summary>
        /// 设置数据
        /// </summary>
        public void SetValue<T>(E_BlockBoardParme key, T value);

        /// <summary>
        /// 获取数据
        /// </summary>
        public T GetValue<T>(E_BlockBoardParme key, T defaultValue = default);

        /// <summary>
        /// 检查是否存在指定键
        /// </summary>
        public bool HasKey(E_BlockBoardParme key);

        /// <summary>
        /// 移除数据
        /// </summary>
        public void RemoveValue(E_BlockBoardParme key);

        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void Clear();
    }

}
