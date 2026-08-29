using PrimeTween;
using UnityEngine;

namespace MieMieFrameWork.ProgrammedAnimation.Tween
{
    /// <summary>
    /// 项目统一补间动画入口
    /// </summary>
    public static class MmTween
    {
        /// <summary>
        /// 创建目标缩放动画
        /// </summary>
        public static PrimeTween.Tween ScaleTo(
            Transform target,
            Vector3 endScale,
            float duration,
            Easing ease = default,
            bool useUnscaledTime = true)
        {
            return PrimeTween.Tween.Scale(
                target,
                endScale,
                duration,
                ease: ease,
                useUnscaledTime: useUnscaledTime);
        }

        /// <summary>
        /// 创建目标透明度动画
        /// </summary>
        public static PrimeTween.Tween FadeTo(
            CanvasGroup target,
            float endAlpha,
            float duration,
            Easing ease = default,
            bool useUnscaledTime = true)
        {
            return PrimeTween.Tween.Alpha(
                target,
                endAlpha,
                duration,
                ease: ease,
                useUnscaledTime: useUnscaledTime);
        }

        /// <summary>
        /// 创建目标缩放进入动画
        /// </summary>
        public static PrimeTween.Tween ScaleIn(
            Transform target,
            Vector3 endScale,
            float duration,
            Easing ease = default,
            bool useUnscaledTime = true)
        {
            target.localScale = Vector3.zero;
            return ScaleTo(target, endScale, duration, ease, useUnscaledTime);
        }

        /// <summary>
        /// 创建目标淡入动画
        /// </summary>
        public static PrimeTween.Tween FadeIn(
            CanvasGroup target,
            float duration,
            Easing ease = default,
            bool useUnscaledTime = true)
        {
            target.alpha = 0f;
            return FadeTo(target, 1f, duration, ease, useUnscaledTime);
        }

        /// <summary>
        /// 创建目标淡出动画
        /// </summary>
        public static PrimeTween.Tween FadeOut(
            CanvasGroup target,
            float duration,
            Easing ease = default,
            bool useUnscaledTime = true)
        {
            return FadeTo(target, 0f, duration, ease, useUnscaledTime);
        }
    }
}
