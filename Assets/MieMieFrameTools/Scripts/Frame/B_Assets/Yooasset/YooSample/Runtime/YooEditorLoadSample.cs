using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace MieMieFrameWork.YooSample
{
    /// <summary>
    /// YooAsset 编辑器模拟加载示例
    /// </summary>
    public sealed class YooEditorLoadSample : MonoBehaviour
    {
        /// <summary>资源包名称</summary>
        [SerializeField]
        private string packageName = "YooSample";

        /// <summary>资源定位地址 未开可寻址时填资源路径</summary>
        [SerializeField]
        private string assetLocation = "Cube";

        /// <summary>当前资源包</summary>
        private ResourcePackage resourcePackage;

        /// <summary>资源句柄</summary>
        private AssetHandle assetHandle;

        /// <summary>资源实例</summary>
        private GameObject instance;

        /// <summary>
        /// 启动编辑器模拟加载
        /// </summary>
        private async UniTaskVoid Start()
        {
            bool initialized = await InitializePackage();
            if (!initialized)
                return;

            await LoadPrefab();
        }

        /// <summary>
        /// 初始化 YooAsset 编辑器模拟资源包
        /// </summary>
        private async UniTask<bool> InitializePackage()
        {
            // YooAssets 是全局资源系统入口 只做一次 创建驱动器和异步调度器
            YooAssets.Initialize();

            // Package 是一套独立的资源清单和加载环境 还没有清单 不能 Load
            if (!YooAssets.TryGetPackage(packageName, out resourcePackage))
                resourcePackage = YooAssets.CreatePackage(packageName);

            // 根据 Bundle Collector 配置生成编辑器模拟清单
            // 这里不会打出真实 AssetBundle 只写出清单到临时目录
            var buildResult = EditorSimulateBuildInvoker.Build(
                packageName,
                (int)EBundleType.VirtualAssetBundle);

            // 模拟文件系统会直接从 Unity 工程资源中读取对象
            // packageRoot 是模拟构建结果所在的清单目录
            var options = new EditorSimulateModeOptions
            {
                EditorFileSystemParameters =
                    FileSystemParameters.CreateDefaultEditorFileSystemParameters(
                        buildResult.PackageRootDirectory)
            };

            // 只挂上文件系统 此时 ActiveManifest 仍是空的
            var initializeOperation = resourcePackage.InitializePackageAsync(options);
            await initializeOperation;
            if (initializeOperation.Status != EOperationStatus.Succeeded)
            {
                Debug.LogError("YooAsset 初始化失败 " + initializeOperation.Error);
                return false;
            }

            // 从清单目录读出当前版本号 编辑器模拟就是本地那份模拟构建版本
            var versionOperation = resourcePackage.RequestPackageVersionAsync();
            await versionOperation;
            if (versionOperation.Status != EOperationStatus.Succeeded)
            {
                Debug.LogError("YooAsset 请求版本失败 " + versionOperation.Error);
                return false;
            }

            // 按版本把清单反序列化进 Package 之后才能用地址查资源
            var manifestOptions = new LoadPackageManifestOptions(versionOperation.PackageVersion, 60);
            var manifestOperation = resourcePackage.LoadPackageManifestAsync(manifestOptions);
            await manifestOperation;
            if (manifestOperation.Status != EOperationStatus.Succeeded)
            {
                Debug.LogError("YooAsset 加载清单失败 " + manifestOperation.Error);
                return false;
            }

            Debug.Log("YooAsset 编辑器模拟初始化成功 版本 " + versionOperation.PackageVersion);
            return true;
        }

        /// <summary>
        /// 加载并实例化方块预制体
        /// </summary>
        private async UniTask LoadPrefab()
        {
            // LoadAssetAsync 返回资源句柄
            // 句柄负责保存资源引用 并在最后释放引用
            assetHandle = resourcePackage.LoadAssetAsync<GameObject>(assetLocation);
            await assetHandle;

            if (assetHandle.Status != EOperationStatus.Succeeded)
            {
                Debug.LogError(
                    "YooAsset 资源加载失败 "
                    + assetLocation
                    + " "
                    + assetHandle.Error);
                return;
            }

            // 通过资源句柄实例化 Prefab
            // 这里的实例化操作和资源加载操作是两个异步操作
            var instantiateOperation = assetHandle.InstantiateAsync();
            await instantiateOperation;

            if (instantiateOperation.Status != EOperationStatus.Succeeded)
            {
                Debug.LogError(
                    "YooAsset 预制体实例化失败 "
                    + instantiateOperation.Error);
                return;
            }

            instance = instantiateOperation.Result;
            Debug.Log("YooAsset 预制体加载成功 " + instance.name);
        }

        /// <summary>
        /// 销毁实例并释放资源句柄
        /// </summary>
        private void OnDestroy()
        {
            if (instance != null)
                Destroy(instance);

            if (assetHandle != null)
                assetHandle.Release();
        }
    }
}
