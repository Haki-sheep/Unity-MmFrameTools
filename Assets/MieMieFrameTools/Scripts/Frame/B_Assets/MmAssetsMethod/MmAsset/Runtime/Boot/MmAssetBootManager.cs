using Cysharp.Threading.Tasks;
using UnityEngine;
using static MieMieFrameWork.ModuleHub;

namespace MieMieFrameWork.Asset
{
    /// <summary>
    /// MmAsset 启动管理器
    /// 在其它依赖资源的 Manager 之前 Boot 内嵌模块
    /// </summary>
    [ManagerAttribute(0)]
    public sealed class MmAssetBootManager : IManagerBase
    {
        /// <summary>
        /// 初始化并启动全部随包内置模块
        /// </summary>
        public void Init()
        {
            // TODO 网络 Manager 插入时 order 放在本 Boot 之后
            MmAssetFrame.Instance.InitFrame();
#if UNITY_EDITOR
            // Editor 优先同步灌入 Generated 地址表 避免后续 Manager 抢跑空表
            if (BundleSettings.Instance.loadAssetType == E_LoadAssetType.Editor)
                LoadEditorModuleConfigs();
#endif
            BootAsync().Forget();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 同步灌入全部模块的 Generated 地址表
        /// </summary>
        private static void LoadEditorModuleConfigs()
        {
            foreach (BundleModuleEnum moduleEnum in System.Enum.GetValues(typeof(BundleModuleEnum)))
            {
                if (moduleEnum == BundleModuleEnum.None)
                    continue;
                AssetBundleManager.Instance.LoadAssetBundleConfig(moduleEnum);
            }
        }
#endif

        /// <summary>
        /// 异步完整 Boot 全部需要随包解压的模块
        /// </summary>
        private async UniTaskVoid BootAsync()
        {
            foreach (BundleModuleEnum moduleEnum in System.Enum.GetValues(typeof(BundleModuleEnum)))
            {
                if (moduleEnum == BundleModuleEnum.None)
                    continue;
                if (!BundleModuleDelivery.NeedExtract(moduleEnum))
                    continue;
                await MmAssetFrame.Instance.BootModule(moduleEnum);
            }
        }
    }
}
