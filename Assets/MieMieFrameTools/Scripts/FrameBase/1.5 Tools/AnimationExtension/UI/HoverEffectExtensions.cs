using UnityEngine;
using UnityEngine.UI;

namespace MieMieFrameWork.UI
{
    public static class HoverEffectExtensions
    {
        public static T AddHoverEffect<T>(this T component, HoverEffectConfig config) where T : Graphic
        {
            if (component == null || config == null) return component;

            var anim = component.gameObject.AddComponent<HoverEffectAnim>();
            anim.Init(config);

            return component;
        }
    }
}
