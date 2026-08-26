using System.Collections.Generic;
using UnityEngine;


namespace MieMieFrameWork.Asset
{
public class ClassObjectPool<T> where T : class, new()
{
    protected Stack<T> pool = new();
    protected int maxCount = 100;

    public ClassObjectPool(int maxCount)
    {
        this.maxCount = maxCount;
        for (int i = 0; i < maxCount; i++)
        {
            pool.Push(new T());
        }
    }

    /// <summary>
    /// 获取对象
    /// </summary>
    /// <returns></returns>
    public T Spawn()
    {
        if (pool.Count > 0)
        {
            return pool.Pop();
        }
        return new T();
    }

    /// <summary>
    /// 回收对象
    /// </summary>
    /// <param name="item"></param>
    public void Recycle(T item)
    {
        if(item == null)
        {
            Debug.LogError("回收对象为空");
            return;
        }
        if (pool.Count < maxCount)
        {
            pool.Push(item);
        }
        else
        {
            Debug.LogWarning("对象池已满，无法回收对象");
        }
    }
}
}
