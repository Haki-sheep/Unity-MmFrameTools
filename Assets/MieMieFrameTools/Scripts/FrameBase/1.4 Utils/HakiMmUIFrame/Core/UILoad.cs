using UnityEngine;

namespace MieMieFrameWork.UI
{
    /// <summary>
    /// UI 加载工具类
    /// 提供 Addressable 和 Resources 双路径加载
    /// </summary>
    public static class UILoad
    {
        /// <summary>
        /// 同步加载 UI 预制体
        /// 优先走 Addressable，失败则降级到 Resources
        /// </summary>
        /// <param name="uiName">UI 名称（Addressable 地址或 Resources 路径）</param>
        /// <returns>加载的 GameObject，未找到则返回 null</returns>
        public static GameObject AddressableLoad(string uiName)
        {
            // 先尝试 Addressable 加载
            GameObject uiPrefab = null;

            try
            {
                uiPrefab = AddressableMgr.LoadGameObject(uiName);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UILoad] Addressable 加载失败，尝试 Resources 加载: {e.Message}");
            }

            // 如果 Addressable 失败，尝试从 Resources 加载
            if (uiPrefab == null)
            {
                uiPrefab = Resources.Load<GameObject>(uiName);
                if (uiPrefab != null)
                {
                    uiPrefab = GameObject.Instantiate(uiPrefab);
                }
            }

            if (uiPrefab == null) return null;

            // 确保 UI 预制体在正确的位置和状态
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
        /// 异步加载 UI 预制体
        /// 优先走 Addressable，失败则降级到 Resources
        /// </summary>
        /// <param name="uiName">UI 名称（Addressable 地址或 Resources 路径）</param>
        /// <param name="onComplete">加载完成回调，返回加载的 GameObject</param>
        public static void AddressableLoadAsync(string uiName, System.Action<GameObject> onComplete)
        {
            // 使用 AddressableMgr 的异步方法，内部正确管理句柄
            AddressableMgr.LoadAssetAsync<GameObject>(uiName, (prefab) =>
            {
                if (prefab != null)
                {
                    RectTransform rectTransform = prefab.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.localScale = Vector3.one;
                        rectTransform.localPosition = Vector3.zero;
                        rectTransform.localRotation = Quaternion.identity;
                    }
                    onComplete?.Invoke(prefab);
                }
                else
                {
                    // Addressable 失败，降级到 Resources
                    Debug.LogWarning($"[UILoad] Addressable 异步加载失败，尝试 Resources: {uiName}");
                    GameObject uiPrefab = Resources.Load<GameObject>(uiName);
                    if (uiPrefab != null)
                    {
                        uiPrefab = GameObject.Instantiate(uiPrefab);
                        onComplete?.Invoke(uiPrefab);
                    }
                    else
                    {
                        Debug.LogError($"[UILoad] Resources 也加载失败: {uiName}");
                        onComplete?.Invoke(null);
                    }
                }
            });
        }
    }
}
