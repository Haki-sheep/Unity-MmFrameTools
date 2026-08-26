using System;
using Cysharp.Threading.Tasks;
using MieMieFrameWork.Asset;
using UnityEngine;

/// <summary>
/// 随包资源加载测试
/// 前置 资源加载类型 AssetBundle 是否热更 NotHot 已打包并内嵌
/// </summary>
public class Load : MonoBehaviour
{
    /// <summary>要启动的随包模块</summary>
    [SerializeField]
    private BundleModuleEnum bundleModuleEnum = BundleModuleEnum.Player;

    /// <summary>资源别名或完整 Assets 路径</summary>
    [SerializeField]
    private string assetAddress = "BuildInCube";

    /// <summary>
    /// 启动随包模块并实例化测试预制体
    /// </summary>
    private async UniTask Start()
    {
        // 获取取消令牌
        var cancellationToken = this.GetCancellationTokenOnDestroy();

        // 启动随包模块
        await MmAssetFrame.Instance.BootModule(
            bundleModuleEnum,
            cancellationToken: cancellationToken);

        // 实例化预制体
        var instance = await MmAssetFrame.Instance.Resources.InstantiateAsync(
            assetAddress,
            cancellationToken: cancellationToken);

        // 设置位置
        instance.transform.position = Vector3.zero;
    }


}
