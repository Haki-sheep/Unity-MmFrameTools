using Cysharp.Threading.Tasks;
using MieMieFrameWork.Asset;
using UnityEngine;

namespace MmUIFrameWork.Core
{
    /// <summary>
    /// UI 加载工具类 直接走框架 MmAssetMgr
    /// </summary>
    public static class UILoad
    {
        /// <summary>
        /// 同步加载 UI 预制体 窗口名须在 assetAliasList 注册短名
        /// </summary>
        public static GameObject AddressableLoad(string uiName)
        {
            GameObject uiPrefab = MmAssetMgr.LoadGameObject(uiName);
            if (uiPrefab == null)
            {
                Debug.LogError($"[UILoad.Load] 加载失败: {uiName}");
                return null;
            }

            NormalizeRect(uiPrefab);
            return uiPrefab;
        }

        /// <summary>
        /// 异步加载 UI 预制体 窗口名须在 assetAliasList 注册短名
        /// </summary>
        public static async UniTask<GameObject> AddressableLoadAsync(string uiName, Transform parent = null)
        {
            GameObject uiPrefab = await MmAssetMgr.LoadGameObjectAsync(uiName, parent);
            if (uiPrefab == null)
            {
                Debug.LogError($"[UILoad.LoadAsync] 加载失败: {uiName}");
                return null;
            }

            NormalizeRect(uiPrefab);
            return uiPrefab;
        }

        /// <summary>
        /// 释放 UI 实例
        /// </summary>
        public static void Release(GameObject uiInstance)
        {
            MmAssetMgr.DestroyObject(uiInstance);
        }

        /// <summary>
        /// 归一化 RectTransform
        /// </summary>
        private static void NormalizeRect(GameObject uiPrefab)
        {
            RectTransform rectTransform = uiPrefab.GetComponent<RectTransform>();
            if (rectTransform == null)
                return;

            rectTransform.localScale = Vector3.one;
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localRotation = Quaternion.identity;
        }
    }
}
