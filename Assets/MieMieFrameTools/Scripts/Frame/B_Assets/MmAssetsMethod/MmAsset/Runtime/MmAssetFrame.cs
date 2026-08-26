using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using MieMieFrameWork;
using UnityEngine;

namespace MieMieFrameWork.Asset
{
    /// <summary>
    /// 资源框架入口
    /// 热更 解压 资源加载统一访问点
    /// </summary>
    public partial class MmAssetFrame : SingletonMono<MmAssetFrame>
    {
        private IHotAssets hotAssets;
        public IHotAssets HotAssets => hotAssets;

        /// <summary>
        /// 随包资源层
        /// </summary>
        private IBuiltInAssets builtInAssets;
        public IBuiltInAssets BuiltInAssets => builtInAssets;

        /// <summary>
        /// 资源加载层
        /// </summary>
        private IResourcesInterface resources;
        public IResourcesInterface Resources => resources;

        /// <summary>对象池回收节点 Release时实例挂到这里隐藏</summary>
        private static Transform recyclObjRoot;
        public static Transform RecyclObjRoot => recyclObjRoot;

        protected override bool DontDestroyOnLoadEnabled => true;

        /// <summary>
        /// 初始化各模块 热更或加载前调用
        /// </summary>
        public void InitFrame()
        {
            // 创建全局回收根节点
            if (recyclObjRoot == null)
            {
                var rootObj = new GameObject("RecyclObjRoot");
                DontDestroyOnLoad(rootObj);
                recyclObjRoot = rootObj.transform;
            }

            hotAssets ??= new HotAssetsManager();
            builtInAssets ??= new AssetsDeCompressManager();
            resources ??= new ResourceManager();
            resources.Init(hotAssets);
        }

        /// <summary>
        /// 启动指定资源模块
        /// BuiltIn 只解压加载配置 HotUpdate 只热更加载配置 Hybrid 全流程
        /// </summary>
        public async UniTask BootModule(
            BundleModuleEnum bundleModuleEnum,
            IProgress<AssetBootProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            InitFrame();

            if (BundleModuleDelivery.NeedExtract(bundleModuleEnum))
                await builtInAssets.ExtractAsync(bundleModuleEnum, progress, cancellationToken);

            if (BundleModuleDelivery.NeedHotUpdate(bundleModuleEnum))
                await hotAssets.HotAssetsAsync(bundleModuleEnum, progress, cancellationToken);

            progress?.Report(new AssetBootProgress(
                bundleModuleEnum,
                EAssetBootStage.LoadConfig,
                0f,
                0f,
                0f,
                "加载资源地址表"));
            bool configLoaded = await AssetBundleManager.Instance.LoadAssetBundleConfigAsync(
                bundleModuleEnum,
                cancellationToken);
#if UNITY_EDITOR
            bool allowEditorWithoutConfig = BundleSettings.Instance.loadAssetType == E_LoadAssetType.Editor;
#else
            bool allowEditorWithoutConfig = false;
#endif
            if (!configLoaded && !allowEditorWithoutConfig)
                throw new FileNotFoundException("资源地址表不存在 " + bundleModuleEnum);

            string shaderVariantAlias = "__shader_variants_"
                                        + BundleSettings.Instance.GetModuleFolderName(bundleModuleEnum);
            if (AssetBundleManager.Instance.TryGetBundleItem(shaderVariantAlias, out _))
            {
                var shaderVariantCollection = await resources.LoadResourceAsync<ShaderVariantCollection>(
                    shaderVariantAlias,
                    cancellationToken);
                shaderVariantCollection?.WarmUp();
            }

            progress?.Report(new AssetBootProgress(
                bundleModuleEnum,
                EAssetBootStage.Completed,
                1f,
                0f,
                0f,
                "资源模块启动完成"));
        }
    }
}
