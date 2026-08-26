using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI;

namespace DG.Tweening
{
    /// <summary>
    /// 本程序集无法引用 Plugins 下 DOTweenModuleUI 故在此补齐 UI 扩展
    /// </summary>
    internal static class DOTweenUIExtensions
    {
        /// <summary>
        /// CanvasGroup 透明度
        /// </summary>
        public static TweenerCore<float, float, FloatOptions> DOFade(this CanvasGroup target, float endValue, float duration)
        {
            var t = DOTween.To(() => target.alpha, x => target.alpha = x, endValue, duration);
            t.SetTarget(target);
            return t;
        }

        /// <summary>
        /// Graphic 颜色
        /// </summary>
        public static TweenerCore<Color, Color, ColorOptions> DOColor(this Graphic target, Color endValue, float duration)
        {
            var t = DOTween.To(() => target.color, x => target.color = x, endValue, duration);
            t.SetTarget(target);
            return t;
        }

        /// <summary>
        /// Graphic 透明度
        /// </summary>
        public static TweenerCore<Color, Color, ColorOptions> DOFade(this Graphic target, float endValue, float duration)
        {
            var t = DOTween.ToAlpha(() => target.color, x => target.color = x, endValue, duration);
            t.SetTarget(target);
            return t;
        }

        /// <summary>
        /// Image 填充量
        /// </summary>
        public static TweenerCore<float, float, FloatOptions> DOFillAmount(this Image target, float endValue, float duration)
        {
            if (endValue > 1f) endValue = 1f;
            else if (endValue < 0f) endValue = 0f;
            var t = DOTween.To(() => target.fillAmount, x => target.fillAmount = x, endValue, duration);
            t.SetTarget(target);
            return t;
        }

        /// <summary>
        /// Slider 数值
        /// </summary>
        public static TweenerCore<float, float, FloatOptions> DOValue(this Slider target, float endValue, float duration, bool snapping = false)
        {
            var t = DOTween.To(() => target.value, x => target.value = x, endValue, duration);
            t.SetOptions(snapping).SetTarget(target);
            return t;
        }

        /// <summary>
        /// RectTransform 锚点位置
        /// </summary>
        public static TweenerCore<Vector2, Vector2, VectorOptions> DOAnchorPos(this RectTransform target, Vector2 endValue, float duration, bool snapping = false)
        {
            var t = DOTween.To(() => target.anchoredPosition, x => target.anchoredPosition = x, endValue, duration);
            t.SetOptions(snapping).SetTarget(target);
            return t;
        }

        /// <summary>
        /// RectTransform 锚点 X
        /// </summary>
        public static TweenerCore<Vector2, Vector2, VectorOptions> DOAnchorPosX(this RectTransform target, float endValue, float duration, bool snapping = false)
        {
            var t = DOTween.To(() => target.anchoredPosition, x => target.anchoredPosition = x, new Vector2(endValue, 0f), duration);
            t.SetOptions(AxisConstraint.X, snapping).SetTarget(target);
            return t;
        }

        /// <summary>
        /// RectTransform 锚点 Y
        /// </summary>
        public static TweenerCore<Vector2, Vector2, VectorOptions> DOAnchorPosY(this RectTransform target, float endValue, float duration, bool snapping = false)
        {
            var t = DOTween.To(() => target.anchoredPosition, x => target.anchoredPosition = x, new Vector2(0f, endValue), duration);
            t.SetOptions(AxisConstraint.Y, snapping).SetTarget(target);
            return t;
        }

        /// <summary>
        /// RectTransform 锚点 3D
        /// </summary>
        public static TweenerCore<Vector3, Vector3, VectorOptions> DOAnchorPos3D(this RectTransform target, Vector3 endValue, float duration, bool snapping = false)
        {
            var t = DOTween.To(() => target.anchoredPosition3D, x => target.anchoredPosition3D = x, endValue, duration);
            t.SetOptions(snapping).SetTarget(target);
            return t;
        }

        /// <summary>
        /// RectTransform sizeDelta
        /// </summary>
        public static TweenerCore<Vector2, Vector2, VectorOptions> DOSizeDelta(this RectTransform target, Vector2 endValue, float duration, bool snapping = false)
        {
            var t = DOTween.To(() => target.sizeDelta, x => target.sizeDelta = x, endValue, duration);
            t.SetOptions(snapping).SetTarget(target);
            return t;
        }

        /// <summary>
        /// LayoutElement 弹性尺寸
        /// </summary>
        public static TweenerCore<Vector2, Vector2, VectorOptions> DOFlexibleSize(this LayoutElement target, Vector2 endValue, float duration, bool snapping = false)
        {
            var t = DOTween.To(
                () => new Vector2(target.flexibleWidth, target.flexibleHeight),
                x =>
                {
                    target.flexibleWidth = x.x;
                    target.flexibleHeight = x.y;
                },
                endValue,
                duration);
            t.SetOptions(snapping).SetTarget(target);
            return t;
        }

        /// <summary>
        /// LayoutElement 最小尺寸
        /// </summary>
        public static TweenerCore<Vector2, Vector2, VectorOptions> DOMinSize(this LayoutElement target, Vector2 endValue, float duration, bool snapping = false)
        {
            var t = DOTween.To(
                () => new Vector2(target.minWidth, target.minHeight),
                x =>
                {
                    target.minWidth = x.x;
                    target.minHeight = x.y;
                },
                endValue,
                duration);
            t.SetOptions(snapping).SetTarget(target);
            return t;
        }

        /// <summary>
        /// LayoutElement 首选尺寸
        /// </summary>
        public static TweenerCore<Vector2, Vector2, VectorOptions> DOPreferredSize(this LayoutElement target, Vector2 endValue, float duration, bool snapping = false)
        {
            var t = DOTween.To(
                () => new Vector2(target.preferredWidth, target.preferredHeight),
                x =>
                {
                    target.preferredWidth = x.x;
                    target.preferredHeight = x.y;
                },
                endValue,
                duration);
            t.SetOptions(snapping).SetTarget(target);
            return t;
        }
    }
}
