using System;
using Cysharp.Threading.Tasks;
using MieMieFrameWork.Asset;
using UnityEngine;

/// <summary>
/// 热更资源加载测试
/// 前置 资源加载类型 AssetBundle 是否热更 Hot 本机 CDN 已起服务
/// </summary>
public class HotLoad : MonoBehaviour
{
    /// <summary>热更模块</summary>
    [SerializeField]
    private BundleModuleEnum bundleModuleEnum = BundleModuleEnum.Weapon;

    /// <summary>资源别名 对应预制体名</summary>
    [SerializeField]
    private string assetAddress = "HotSphere";

    /// <summary>
    /// 启动热更模块并实例化测试预制体
    /// </summary>
    private async UniTask Start()
    {
        var cancellationToken = this.GetCancellationTokenOnDestroy();
        var progress = new Progress<AssetBootProgress>(OnBootProgress);

        try
        {
            // 纯热更模块会检版下载再加载地址表
            await MmAssetFrame.Instance.BootModule(
                bundleModuleEnum,
                progress,
                cancellationToken);

            var instance = await MmAssetFrame.Instance.Resources.InstantiateAsync(
                assetAddress,
                cancellationToken: cancellationToken);

            instance.transform.position = Vector3.zero;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    /// <summary>
    /// 输出热更启动进度
    /// </summary>
    private void OnBootProgress(AssetBootProgress progress)
    {
        Debug.Log(
            progress.Stage
            + " "
            + progress.Progress.ToString("0.00")
            + " "
            + progress.Message);
    }
}
