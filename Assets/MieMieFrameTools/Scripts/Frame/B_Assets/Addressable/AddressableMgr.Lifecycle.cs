namespace MieMieFrameWork.AddressableAsset
{
    using UnityEngine;
    using UnityEngine.AddressableAssets;

    /// <summary>
    /// 生命周期管理
    /// </summary>
    public static partial class AddressableMgr
    {
        /// <summary>
        /// 释放 LoadAsset 产生的引用 减到零会卸载资源
        /// </summary>
        public static void ReleaseAsset(string address) => state.ReleaseReference(address);

        /// <summary>
        /// 销毁物体 先尝试 ReleaseInstance 失败则 Destroy
        /// </summary>
        public static void DestroyObject(GameObject obj)
        {
            if (obj == null)
                return;
            if (Addressables.ReleaseInstance(obj))
                return;
            Object.Destroy(obj);
        }

        /// <summary>
        /// 切换场景时调用 释放资源缓存和批量句柄 场景里物体请先 DestroyObject
        /// </summary>
        public static void ClearAllCache() => state.ClearAll();
    }
}
