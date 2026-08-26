using UnityEngine;

/// <summary>
/// 对象池缓存项
/// 包装已克隆GameObject及其元数据
/// </summary>

namespace MieMieFrameWork.Asset
{
public class CacheObject
{
    /// <summary>预制体路径CRC 标识对象类型</summary>
    public uint crc;
    /// <summary>预制体资源路径</summary>
    public string path;
    /// <summary>场景实例唯一标识 EntityId</summary>
    public EntityId entityId;
    /// <summary>克隆出来的GameObject实例</summary>
    public GameObject obj;
    /// <summary>是否已经回收到对象池</summary>
    public bool isPooled;

    /// <summary>重置字段 回收到类对象池前调用</summary>
    public void Release()
    {
        obj = null;
        path = null;
        entityId = EntityId.None;
        crc = 0;
        isPooled = false;
    }
}
}
