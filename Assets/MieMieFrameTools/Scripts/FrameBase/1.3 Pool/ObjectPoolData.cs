namespace MieMieFrameWork.Pool
{
    using System.Collections.Generic;

    public class ObjectPoolData
    {
        public Queue<object> poolQueue = new Queue<object>();

        /// <summary>
        /// 构造函数 调用即将对象放入池中
        /// </summary>
        /// <param name="obj"></param>
        public ObjectPoolData(object obj)
        {
            PushObj(obj);
        }

        public void PushObj(object obj)
        {
            poolQueue.Enqueue(obj);
        }

        // 移除无用的Transform参数，普通对象不需要parent
        public object GetObj()
        {
            if (poolQueue.Count > 0)
            {
                return poolQueue.Dequeue();
            }
            return null;
        }
    }
}