using DG.Tweening;
using UnityEngine;

namespace MieMieFrameWork.UI
{
    [CreateAssetMenu(fileName = "HoverEffectConfig", menuName = "MieMie/UI/Hover Effect Config")]
    public class HoverEffectConfig : ScriptableObject
    {
        [Header("Scale")]
        public bool scaleEnabled = true;
        [Range(1f, 2f)] public float hoverScale = 1.2f;
        public float scaleDuration = 0.2f;
        public Ease scaleEase = Ease.OutQuad;

        [Header("Color")]
        public bool colorEnabled = true;
        public Color hoverColor = Color.white;
        public float colorDuration = 0.2f;

        [Header("Move")]
        public bool moveEnabled = false;
        public float moveOffset = 5f;
        public float moveDuration = 0.2f;
        public Ease moveEase = Ease.OutQuad;
    }
}
