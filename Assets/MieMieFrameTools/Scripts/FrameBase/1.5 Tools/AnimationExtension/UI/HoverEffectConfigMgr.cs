namespace MieMieFrameWork.UI
{
    using Sirenix.OdinInspector;

    using UnityEngine;
    public class HoverEffectConfigMgr :MonoBehaviour
    {
        [SerializeField,LabelText("悬浮效果配置")] 
        public HoverEffectConfig hoverEffectSetting;
    }
}