namespace MieMieFrameWork.Pool
{
    using System;

    /// <summary>
    /// 对象池配置规则
    /// 替代运行时 HashSet 配置，改用命名规则 + [Pool] 特性判断
    /// </summary>
    public static class PoolDef
    {
        /// <summary>
        /// 判断类型是否应该使用对象池
        /// 判断优先级：
        /// 1. 有 [Pool] 特性 → 进池
        /// 2. 类名以 "Window" 结尾 → 进池（UI 窗口自动进池）
        /// 3. 类名以 "Pool" 开头 → 进池
        /// 4. 其他 → 不进池
        /// </summary>
        public static bool ShouldUsePool<T>() where T : class
        {
            return ShouldUsePool(typeof(T));
        }

        public static bool ShouldUsePool(Type type)
        {
            // 1. [Pool] 特性优先
            if (HasPoolAttribute(type))
            {
                return true;
            }

            // 2. 命名规则兜底
            string name = type.Name;
            if (name.EndsWith("Window") || name.EndsWith("UI"))
            {
                return true;
            }

            // 3. Pool 前缀
            if (name.StartsWith("Pool"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查类型是否有 [Pool] 特性
        /// </summary>
        private static bool HasPoolAttribute(Type type)
        {
            return type.IsDefined(typeof(PoolAttribute), false);
        }
    }
}
