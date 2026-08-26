using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 资源加载与对象池管理接口
/// </summary>

namespace MieMieFrameWork.Asset
{
public interface IResourcesInterface
{
    /// <summary>初始化并订阅热更内部事件</summary>
    void Init(IHotAssets hotAssets);

    /// <summary>预克隆count个实例并回收到对象池</summary>
    void PreLoadObj(string path, int count = 1);

    /// <summary>预加载资源到缓存 不实例化</summary>
    void PreLoadResource<T>(string path) where T : UnityEngine.Object;

    /// <summary>同步加载资源 不实例化</summary>
    T LoadResource<T>(string path) where T : UnityEngine.Object;

    /// <summary>异步加载资源 不实例化</summary>
    UniTask<T> LoadResourceAsync<T>(string path, CancellationToken cancellationToken = default) where T : UnityEngine.Object;

    /// <summary>释放 LoadResource 引用 减到零卸载对应 AB</summary>
    void ReleaseAsset(string path);

    /// <summary>同步克隆预制体</summary>
    GameObject Instantiate(string path,
                           Transform parent = null,
                           Vector3 localPosition = default,
                           Vector3 localScale = default,
                           Quaternion localRotation = default);

    /// <summary>
    /// 异步克隆预制体
    /// <param name="path">资源路径</param>
    /// <param name="parent">父对象</param>
    /// <param name="localPosition">本地位置</param>
    /// <param name="localScale">本地缩放</param>
    /// <param name="localRotation">本地旋转</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步克隆的预制体</returns>
    /// </summary>
    UniTask<GameObject> InstantiateAsync(string path,
                                         Transform parent = null,
                                         Vector3 localPosition = default,
                                         Vector3 localScale = default,
                                         Quaternion localRotation = default,
                                         CancellationToken cancellationToken = default);

    /// <summary>
    /// 等待目标资源包就绪后异步克隆
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <param name="parent">父对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步克隆的预制体</returns>
    UniTask<GameObject> InstantiateWhenReadyAsync(
        string path,
        Transform parent = null,
        CancellationToken cancellationToken = default);

    /// <summary>回收或销毁GameObject destroy为true彻底销毁</summary>
    void Release(GameObject obj, bool destroy = false);

    /// <summary>卸载Texture内存</summary>
    void Release(Texture texture);

    Sprite LoadSprite(string path);
    Texture LoadTexture(string path);
    AudioClip LoadAudio(string path);
    TextAsset LoadTextAsset(string path);
    Sprite LoadAtlasSprite(string atlasPath, string spriteName);

    UniTask<Texture> LoadTextureAsync(string path, CancellationToken cancellationToken = default);

    UniTask<Sprite> LoadSpriteAsync(string path,
                                    Image image = null,
                                    bool setNativeSize = false,
                                    CancellationToken cancellationToken = default);

    /// <summary>清空所有等待中的异步任务</summary>
    void ClearAllAsyncLoadTask();

    /// <summary>清理已加载资源</summary>
    void ClearResourcesAssets(bool absoluteCleaning, bool collectGarbage = false);

    /// <summary>卸载指定资源模块</summary>
    void UnloadModule(BundleModuleEnum bundleModuleEnum, bool unloadAllLoadedObjects = true);
}
}
