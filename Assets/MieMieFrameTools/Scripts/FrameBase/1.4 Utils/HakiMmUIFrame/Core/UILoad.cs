using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;

namespace MieMieFrameWork.UI
{
    /// <summary>
    /// 专门为加载UI的类
    /// </summary>

    public static class UILoad
    {
        public static GameObject AddressableLoad(string uiName)
        {
            // 先尝试Addressable加载
            GameObject uiPrefab = null;

            try
            {
                uiPrefab = AddressableMgr.LoadGameObject(uiName);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"UI_Load: Addressable加载失败，尝试Resources加载: {e.Message}");
            }

            // 如果Addressable失败，尝试从Resources加载
            if (uiPrefab == null)
            {
                uiPrefab = Resources.Load<GameObject>(uiName);
                if (uiPrefab != null)
                {
                    uiPrefab = GameObject.Instantiate(uiPrefab);
                }
            }

            if (uiPrefab == null) return null;

            // 确保UI预制体在正确的位置和状态
            RectTransform rectTransform = uiPrefab.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
                rectTransform.localPosition = Vector3.zero;
                rectTransform.localRotation = Quaternion.identity;
            }

            return uiPrefab;
        }

        /// <summary>
        /// 异步加载UI预制体
        /// </summary>
        /// <param name="uiName">UI名称</param>
        /// <param name="onComplete">加载完成回调，返回加载的GameObject</param>
        public static void AddressableLoadAsync(string uiName, System.Action<GameObject> onComplete)
        {
            Addressables.LoadAssetAsync<GameObject>(uiName).Completed += (AsyncOperationHandle<GameObject> handle) =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject uiPrefab = handle.Result;

                    RectTransform rectTransform = uiPrefab.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.localScale = Vector3.one;
                        rectTransform.localPosition = Vector3.zero;
                        rectTransform.localRotation = Quaternion.identity;
                    }

                    onComplete?.Invoke(uiPrefab);
                }
                else
                {
                    Debug.LogError($"UI_Load: 异步加载UI失败: {uiName}, Error: {handle.OperationException}");
                    onComplete?.Invoke(null);
                }
            };
        }
    }
}