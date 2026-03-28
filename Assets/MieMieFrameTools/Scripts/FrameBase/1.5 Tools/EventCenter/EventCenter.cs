namespace MieMieFrameWork
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 事件中心 - 支持枚举和字符串两种方式
    /// </summary>
    public static class EventCenter
    {
        // 事件字典 - 键为哈希值（枚举或字符串的哈希），值为Delegate委托链
        private static Dictionary<int, Delegate> eventDict = new();

        #region 辅助方法 - 获取哈希值

        /// <summary>
        /// 从枚举获取哈希值
        /// </summary>
        private static int GetHash(E_EventConstKey eventKey)
        {
            return eventKey.GetHashCode();
        }

        /// <summary>
        /// 从字符串获取哈希值
        /// </summary>
        private static int GetHash(string eventName)
        {
            return eventName.GetHashCode();
        }

        #endregion

        #region 添加事件监听 - 枚举版本

        /// <summary>
        /// 添加无参数事件监听（枚举）
        /// </summary>
        public static void AddEventListener(E_EventConstKey eventKey, Action action)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                eventDict[hash] = Delegate.Combine(existingEvent, action);
            }
            else
            {
                eventDict[hash] = action;
            }
        }

        /// <summary>
        /// 添加单参数事件监听（枚举）
        /// </summary>
        public static void AddEventListener<T>(E_EventConstKey eventKey, Action<T> action)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                eventDict[hash] = Delegate.Combine(existingEvent, action);
            }
            else
            {
                eventDict[hash] = action;
            }
        }

        /// <summary>
        /// 添加双参数事件监听（枚举）
        /// </summary>
        public static void AddEventListener<T0, T1>(E_EventConstKey eventKey, Action<T0, T1> action)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                eventDict[hash] = Delegate.Combine(existingEvent, action);
            }
            else
            {
                eventDict[hash] = action;
            }
        }

        /// <summary>
        /// 添加三参数事件监听（枚举）
        /// </summary>
        public static void AddEventListener<T0, T1, T2>(E_EventConstKey eventKey, Action<T0, T1, T2> action)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                eventDict[hash] = Delegate.Combine(existingEvent, action);
            }
            else
            {
                eventDict[hash] = action;
            }
        }

        /// <summary>
        /// 添加四参数事件监听（枚举）
        /// </summary>
        public static void AddEventListener<T0, T1, T2, T3>(E_EventConstKey eventKey, Action<T0, T1, T2, T3> action)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                eventDict[hash] = Delegate.Combine(existingEvent, action);
            }
            else
            {
                eventDict[hash] = action;
            }
        }

        /// <summary>
        /// 添加五参数事件监听（枚举）
        /// </summary>
        public static void AddEventListener<T0, T1, T2, T3, T4>(E_EventConstKey eventKey, Action<T0, T1, T2, T3, T4> action)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                eventDict[hash] = Delegate.Combine(existingEvent, action);
            }
            else
            {
                eventDict[hash] = action;
            }
        }

        #endregion

        #region 添加事件监听 - 字符串版本

        /// <summary>
        /// 添加无参数事件监听（字符串）
        /// </summary>
        public static void AddEventListener(string eventName, Action action)
        {
            int hash = GetHash(eventName);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                eventDict[hash] = Delegate.Combine(existingEvent, action);
            }
            else
            {
                eventDict[hash] = action;
            }
        }

        /// <summary>
        /// 添加单参数事件监听（字符串）
        /// </summary>
        public static void AddEventListener<T>(string eventName, Action<T> action)
        {
            int hash = GetHash(eventName);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                eventDict[hash] = Delegate.Combine(existingEvent, action);
            }
            else
            {
                eventDict[hash] = action;
            }
        }

        /// <summary>
        /// 添加双参数事件监听（字符串）
        /// </summary>
        public static void AddEventListener<T0, T1>(string eventName, Action<T0, T1> action)
        {
            int hash = GetHash(eventName);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                eventDict[hash] = Delegate.Combine(existingEvent, action);
            }
            else
            {
                eventDict[hash] = action;
            }
        }

        /// <summary>
        /// 添加三参数事件监听（字符串）
        /// </summary>
        public static void AddEventListener<T0, T1, T2>(string eventName, Action<T0, T1, T2> action)
        {
            int hash = GetHash(eventName);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                eventDict[hash] = Delegate.Combine(existingEvent, action);
            }
            else
            {
                eventDict[hash] = action;
            }
        }

        /// <summary>
        /// 添加四参数事件监听（字符串）
        /// </summary>
        public static void AddEventListener<T0, T1, T2, T3>(string eventName, Action<T0, T1, T2, T3> action)
        {
            int hash = GetHash(eventName);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                eventDict[hash] = Delegate.Combine(existingEvent, action);
            }
            else
            {
                eventDict[hash] = action;
            }
        }

        /// <summary>
        /// 添加五参数事件监听（字符串）
        /// </summary>
        public static void AddEventListener<T0, T1, T2, T3, T4>(string eventName, Action<T0, T1, T2, T3, T4> action)
        {
            int hash = GetHash(eventName);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                eventDict[hash] = Delegate.Combine(existingEvent, action);
            }
            else
            {
                eventDict[hash] = action;
            }
        }

        #endregion

        #region 触发事件 - 枚举版本

        /// <summary>
        /// 触发无参数事件（枚举）
        /// </summary>
        public static void TriggerEvent(E_EventConstKey eventKey)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var eventDelegate))
            {
                try
                {
                    (eventDelegate as Action)?.Invoke();
                }
                catch (Exception ex)
                { 
                    Debug.LogError($"触发事件 {eventKey} 失败: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// 触发单参数事件（枚举）
        /// </summary>
        public static void TriggerEvent<T>(E_EventConstKey eventKey, T arg)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var eventDelegate))
            {
                try
                {
                    (eventDelegate as Action<T>)?.Invoke(arg);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"触发事件 {eventKey} 失败: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// 触发双参数事件（枚举）
        /// </summary>
        public static void TriggerEvent<T0, T1>(E_EventConstKey eventKey, T0 arg0, T1 arg1)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var eventDelegate))
            {
                try
                {
                    (eventDelegate as Action<T0, T1>)?.Invoke(arg0, arg1);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"触发事件 {eventKey} 失败: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// 触发三参数事件（枚举）
        /// </summary>
        public static void TriggerEvent<T0, T1, T2>(E_EventConstKey eventKey, T0 arg0, T1 arg1, T2 arg2)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var eventDelegate))
            {
                try
                {
                    (eventDelegate as Action<T0, T1, T2>)?.Invoke(arg0, arg1, arg2);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"触发事件 {eventKey} 失败: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// 触发四参数事件（枚举）
        /// </summary>
        public static void TriggerEvent<T0, T1, T2, T3>(E_EventConstKey eventKey, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var eventDelegate))
            {
                try
                {
                    (eventDelegate as Action<T0, T1, T2, T3>)?.Invoke(arg0, arg1, arg2, arg3);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"触发事件 {eventKey} 失败: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// 触发五参数事件（枚举）
        /// </summary>
        public static void TriggerEvent<T0, T1, T2, T3, T4>(E_EventConstKey eventKey, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var eventDelegate))
            {
                try
                {
                    (eventDelegate as Action<T0, T1, T2, T3, T4>)?.Invoke(arg0, arg1, arg2, arg3, arg4);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"触发事件 {eventKey} 失败: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        #endregion

        #region 触发事件 - 字符串版本

        /// <summary>
        /// 触发无参数事件（字符串）
        /// </summary>
        public static void TriggerEvent(string eventName)
        {
            int hash = GetHash(eventName);
            if (eventDict.TryGetValue(hash, out var eventDelegate))
            {
                try
                {
                    (eventDelegate as Action)?.Invoke();
                }
                catch (Exception ex)
                { 
                    Debug.LogError($"触发事件 {eventName} 失败: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// 触发单参数事件（字符串）
        /// </summary>
        public static void TriggerEvent<T>(string eventName, T arg)
        {
            int hash = GetHash(eventName);
            if (eventDict.TryGetValue(hash, out var eventDelegate))
            {
                try
                {
                    (eventDelegate as Action<T>)?.Invoke(arg);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"触发事件 {eventName} 失败: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// 触发双参数事件（字符串）
        /// </summary>
        public static void TriggerEvent<T0, T1>(string eventName, T0 arg0, T1 arg1)
        {
            int hash = GetHash(eventName);
            if (eventDict.TryGetValue(hash, out var eventDelegate))
            {
                try
                {
                    (eventDelegate as Action<T0, T1>)?.Invoke(arg0, arg1);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"触发事件 {eventName} 失败: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// 触发三参数事件（字符串）
        /// </summary>
        public static void TriggerEvent<T0, T1, T2>(string eventName, T0 arg0, T1 arg1, T2 arg2)
        {
            int hash = GetHash(eventName);
            if (eventDict.TryGetValue(hash, out var eventDelegate))
            {
                try
                {
                    (eventDelegate as Action<T0, T1, T2>)?.Invoke(arg0, arg1, arg2);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"触发事件 {eventName} 失败: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// 触发四参数事件（字符串）
        /// </summary>
        public static void TriggerEvent<T0, T1, T2, T3>(string eventName, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            int hash = GetHash(eventName);
            if (eventDict.TryGetValue(hash, out var eventDelegate))
            {
                try
                {
                    (eventDelegate as Action<T0, T1, T2, T3>)?.Invoke(arg0, arg1, arg2, arg3);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"触发事件 {eventName} 失败: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// 触发五参数事件（字符串）
        /// </summary>
        public static void TriggerEvent<T0, T1, T2, T3, T4>(string eventName, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            int hash = GetHash(eventName);
            if (eventDict.TryGetValue(hash, out var eventDelegate))
            {
                try
                {
                    (eventDelegate as Action<T0, T1, T2, T3, T4>)?.Invoke(arg0, arg1, arg2, arg3, arg4);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"触发事件 {eventName} 失败: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        #endregion

        #region 删除事件监听 - 枚举版本

        /// <summary>
        /// 删除无参数事件的指定回调（枚举）
        /// </summary>
        public static void RemoveListener(E_EventConstKey eventKey, Action action)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                try
                {
                    var newEvent = Delegate.Remove(existingEvent, action);
                    if (newEvent == null)
                        eventDict.Remove(hash);
                    else
                        eventDict[hash] = newEvent; 
                }
                catch (Exception ex)
                {
                    Debug.LogError($"删除事件 {eventKey} 失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 删除单参数事件的指定回调（枚举）
        /// </summary>
        public static void RemoveListener<T>(E_EventConstKey eventKey, Action<T> action)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                try
                {
                    var newEvent = Delegate.Remove(existingEvent, action);
                    if (newEvent == null)
                        eventDict.Remove(hash);
                    else
                        eventDict[hash] = newEvent;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"删除事件 {eventKey} 失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 删除双参数事件的指定回调（枚举）
        /// </summary>
        public static void RemoveListener<T0, T1>(E_EventConstKey eventKey, Action<T0, T1> action)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                try
                {
                    var newEvent = Delegate.Remove(existingEvent, action);
                    if (newEvent == null)
                        eventDict.Remove(hash);
                    else
                        eventDict[hash] = newEvent;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"删除事件 {eventKey} 失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 删除三参数事件的指定回调（枚举）
        /// </summary>
        public static void RemoveListener<T0, T1, T2>(E_EventConstKey eventKey, Action<T0, T1, T2> action)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                try
                {
                    var newEvent = Delegate.Remove(existingEvent, action);
                    if (newEvent == null)
                        eventDict.Remove(hash);
                    else
                        eventDict[hash] = newEvent;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"删除事件 {eventKey} 失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 删除四参数事件的指定回调（枚举）
        /// </summary>
        public static void RemoveListener<T0, T1, T2, T3>(E_EventConstKey eventKey, Action<T0, T1, T2, T3> action)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                try
                {
                    var newEvent = Delegate.Remove(existingEvent, action);
                    if (newEvent == null)
                        eventDict.Remove(hash);
                    else
                        eventDict[hash] = newEvent;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"删除事件 {eventKey} 失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 删除五参数事件的指定回调（枚举）
        /// </summary>
        public static void RemoveListener<T0, T1, T2, T3, T4>(E_EventConstKey eventKey, Action<T0, T1, T2, T3, T4> action)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                try
                {
                    var newEvent = Delegate.Remove(existingEvent, action);
                    if (newEvent == null)
                        eventDict.Remove(hash);
                    else
                        eventDict[hash] = newEvent;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"删除事件 {eventKey} 失败: {ex.Message}");
                }
            }
        }

        #endregion

        #region 删除事件监听 - 字符串版本

        /// <summary>
        /// 删除无参数事件的指定回调（字符串）
        /// </summary>
        public static void RemoveListener(string eventName, Action action)
        {
            int hash = GetHash(eventName);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                try
                {
                    var newEvent = Delegate.Remove(existingEvent, action);
                    if (newEvent == null)
                        eventDict.Remove(hash);
                    else
                        eventDict[hash] = newEvent; 
                }
                catch (Exception ex)
                {
                    Debug.LogError($"删除事件 {eventName} 失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 删除单参数事件的指定回调（字符串）
        /// </summary>
        public static void RemoveListener<T>(string eventName, Action<T> action)
        {
            int hash = GetHash(eventName);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                try
                {
                    var newEvent = Delegate.Remove(existingEvent, action);
                    if (newEvent == null)
                        eventDict.Remove(hash);
                    else
                        eventDict[hash] = newEvent;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"删除事件 {eventName} 失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 删除双参数事件的指定回调（字符串）
        /// </summary>
        public static void RemoveListener<T0, T1>(string eventName, Action<T0, T1> action)
        {
            int hash = GetHash(eventName);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                try
                {
                    var newEvent = Delegate.Remove(existingEvent, action);
                    if (newEvent == null)
                        eventDict.Remove(hash);
                    else
                        eventDict[hash] = newEvent;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"删除事件 {eventName} 失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 删除三参数事件的指定回调（字符串）
        /// </summary>
        public static void RemoveListener<T0, T1, T2>(string eventName, Action<T0, T1, T2> action)
        {
            int hash = GetHash(eventName);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                try
                {
                    var newEvent = Delegate.Remove(existingEvent, action);
                    if (newEvent == null)
                        eventDict.Remove(hash);
                    else
                        eventDict[hash] = newEvent;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"删除事件 {eventName} 失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 删除四参数事件的指定回调（字符串）
        /// </summary>
        public static void RemoveListener<T0, T1, T2, T3>(string eventName, Action<T0, T1, T2, T3> action)
        {
            int hash = GetHash(eventName);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                try
                {
                    var newEvent = Delegate.Remove(existingEvent, action);
                    if (newEvent == null)
                        eventDict.Remove(hash);
                    else
                        eventDict[hash] = newEvent;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"删除事件 {eventName} 失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 删除五参数事件的指定回调（字符串）
        /// </summary>
        public static void RemoveListener<T0, T1, T2, T3, T4>(string eventName, Action<T0, T1, T2, T3, T4> action)
        {
            int hash = GetHash(eventName);
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                try
                {
                    var newEvent = Delegate.Remove(existingEvent, action);
                    if (newEvent == null)
                        eventDict.Remove(hash);
                    else
                        eventDict[hash] = newEvent;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"删除事件 {eventName} 失败: {ex.Message}");
                }
            }
        }

        #endregion

        #region 清空事件监听

        /// <summary>
        /// 移除指定事件的所有回调（枚举）
        /// </summary>
        public static void RemoveListener(E_EventConstKey eventKey)
        {
            int hash = GetHash(eventKey);
            eventDict.Remove(hash);
        }

        /// <summary>
        /// 移除指定事件的所有回调（字符串）
        /// </summary>
        public static void RemoveListener(string eventName)
        {
            int hash = GetHash(eventName);
            eventDict.Remove(hash);
        }

        /// <summary>
        /// 移除所有事件的所有回调 - 退出游戏时调用
        /// </summary>
        public static void ClearAllListeners()
        {
            eventDict.Clear();
        }

        #endregion

        #region 调试工具

        /// <summary>
        /// 获取当前注册的事件数量
        /// </summary>
        public static int GetEventCount()
        {
            return eventDict.Count;
        }

        /// <summary>
        /// 检查事件是否已注册（枚举）
        /// </summary>
        public static bool HasEvent(E_EventConstKey eventKey)
        {
            int hash = GetHash(eventKey);
            return eventDict.ContainsKey(hash);
        }

        /// <summary>
        /// 检查事件是否已注册（字符串）
        /// </summary>
        public static bool HasEvent(string eventName)
        {
            int hash = GetHash(eventName);
            return eventDict.ContainsKey(hash);
        }

        /// <summary>
        /// 获取指定事件的监听者数量（枚举）
        /// </summary>
        public static int GetListenerCount(E_EventConstKey eventKey)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var eventDelegate))
            {
                return eventDelegate.GetInvocationList().Length;
            }
            return 0;
        }

        /// <summary>
        /// 获取指定事件的监听者数量（字符串）
        /// </summary>
        public static int GetListenerCount(string eventName)
        {
            int hash = GetHash(eventName);
            if (eventDict.TryGetValue(hash, out var eventDelegate))
            {
                return eventDelegate.GetInvocationList().Length;
            }
            return 0;
        }

        #endregion
    }
}
