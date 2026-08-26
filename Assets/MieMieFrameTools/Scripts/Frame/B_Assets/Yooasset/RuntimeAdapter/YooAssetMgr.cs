using UnityEngine;
using YooAsset;

namespace MieMieFrameWork.Asset
{
    /// <summary>
    /// YooAsset 运行时门面
    /// 负责初始化全局系统和访问资源包
    /// </summary>
    public static class YooAssetMgr
    {
        /// <summary>当前默认资源包</summary>
        public static ResourcePackage DefaultPackage { get; private set; }

        /// <summary>
        /// 初始化 YooAsset 并创建默认资源包
        /// </summary>
        public static ResourcePackage Initialize(string packageName)
        {
            if (!YooAssets.IsInitialized)
                YooAssets.Initialize();

            if (!YooAssets.TryGetPackage(packageName, out ResourcePackage package))
                package = YooAssets.CreatePackage(packageName);

            DefaultPackage = package;
            return package;
        }

        /// <summary>
        /// 获取默认资源包
        /// </summary>
        public static ResourcePackage GetDefaultPackage()
        {
            return DefaultPackage;
        }

        /// <summary>
        /// 异步加载资源并返回资源句柄
        /// </summary>
        public static AssetHandle LoadAssetAsync<T>(
            string location,
            uint priority = 0) where T : Object
        {
            return DefaultPackage.LoadAssetAsync<T>(location, priority);
        }

        /// <summary>
        /// 同步加载资源并返回资源句柄
        /// </summary>
        public static AssetHandle LoadAsset<T>(string location) where T : Object
        {
            return DefaultPackage.LoadAssetSync<T>(location);
        }

        /// <summary>
        /// 释放资源句柄
        /// </summary>
        public static void Release(AssetHandle assetHandle)
        {
            assetHandle?.Release();
        }
    }
}
