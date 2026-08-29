namespace MiMieFSM.UpdateFsm
{
    using System.Collections.Generic;

    /// <summary>
    /// FSM 黑板默认实现
    /// </summary>
    public class FsmBlackboard : IFsmBlackboard
    {
        /// <summary>
        /// 黑板数据字典
        /// </summary>
        private readonly Dictionary<EBlockBoardParme, object> valueDict = new();

        /// <summary>
        /// 设置数据
        /// </summary>
        public void SetValue<T>(EBlockBoardParme key, T value)
        {
            valueDict[key] = value;
        }

        /// <summary>
        /// 获取数据
        /// </summary>
        public T GetValue<T>(EBlockBoardParme key, T defaultValue = default)
        {
            if (!valueDict.TryGetValue(key, out object value))
                return defaultValue;

            if (value is T typedValue)
                return typedValue;

            return defaultValue;
        }

        /// <summary>
        /// 检查是否存在指定键
        /// </summary>
        public bool HasKey(EBlockBoardParme key)
        {
            return valueDict.ContainsKey(key);
        }

        /// <summary>
        /// 移除数据
        /// </summary>
        public void RemoveValue(EBlockBoardParme key)
        {
            valueDict.Remove(key);
        }

        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void Clear()
        {
            valueDict.Clear();
        }
    }
}
