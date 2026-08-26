using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MieMieFrameWork.Asset
{
    /// <summary>
    /// MmAsset 薄门面
    /// API 贴近原 AddressableMgr 便于业务侧替换
    /// </summary>
    public static class MmAssetMgr
    {
        /// <summary>
        /// 确保框架已 Init
        /// </summary>
        private static IResourcesInterface Resources
        {
            get
            {
                MmAssetFrame.Instance.InitFrame();
                return MmAssetFrame.Instance.Resources;
            }
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        public static T LoadAsset<T>(string address) where T : Object
        {
            return Resources.LoadResource<T>(address);
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        public static UniTask<T> LoadAssetAsync<T>(string address) where T : Object
        {
            return Resources.LoadResourceAsync<T>(address);
        }

        /// <summary>
        /// 同步实例化预制体
        /// </summary>
        public static GameObject LoadGameObject(string address, Transform parent = null)
        {
            return Resources.Instantiate(address, parent);
        }

        /// <summary>
        /// 异步实例化预制体
        /// </summary>
        public static UniTask<GameObject> LoadGameObjectAsync(string address, Transform parent = null)
        {
            return Resources.InstantiateAsync(address, parent);
        }

        /// <summary>
        /// 同步实例化预制体
        /// </summary>
        public static GameObject Instantiate(string address, Transform parent = null)
        {
            return Resources.Instantiate(address, parent);
        }

        /// <summary>
        /// 异步实例化预制体
        /// </summary>
        public static UniTask<GameObject> InstantiateAsync(string address, Transform parent = null)
        {
            return Resources.InstantiateAsync(address, parent);
        }

        /// <summary>
        /// 销毁由 MmAsset 创建的实例 非自建则兜底 Destroy
        /// </summary>
        public static void DestroyObject(GameObject obj)
        {
            if (obj == null)
                return;

            Resources.Release(obj, true);
            if (obj != null)
                Object.Destroy(obj);
        }

        /// <summary>
        /// 释放 LoadAsset 引用 减到零卸载对应 AB
        /// </summary>
        public static void ReleaseAsset(string address)
        {
            Resources.ReleaseAsset(address);
        }

        /// <summary>
        /// 清空资源缓存
        /// </summary>
        public static void ClearAllCache()
        {
            Resources.ClearResourcesAssets(true, true);
        }
    }
}
