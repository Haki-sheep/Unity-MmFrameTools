namespace MmUIFrameWork.Core
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 所有UI的基类
    /// </summary>
    public abstract class UIDataBase
    {
        #region 属性
        public GameObject UIGameObject { get; protected set; }
        public bool UIIsShow => UICanvasGroup.alpha > 0;
        public Canvas UICanvas { get; protected set; }
        public Transform UIContent { get; protected set; }
        public CanvasGroup UICanvasGroup { get; protected set; }
        public Image UIMask { get; protected set; }
        #endregion

        #region 生命周期
        public abstract void BindGameObject(GameObject uiPrefab, Camera uiCamera);
        internal protected abstract void OnAwake();
        internal protected abstract void OnShow();
        internal protected abstract void OnHide();
        internal protected abstract void OnDestroy();
        #endregion
    }
}
