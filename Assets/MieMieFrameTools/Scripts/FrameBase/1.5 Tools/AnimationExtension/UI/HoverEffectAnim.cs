using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MieMieFrameWork.UI
{
    public class HoverEffectAnim : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private HoverEffectConfig config;
        [SerializeField] private Graphic targetGraphic;

        private Graphic graphic;
        private Color originalColor;
        private Vector3 originalScale;
        private Vector3 originalPosition;
        private Sequence currentSeq;

        public void Init(HoverEffectConfig cfg)
        {
            config = cfg;
            if (config == null) return;

            graphic = targetGraphic ?? GetComponent<Graphic>();
            if (graphic == null) return;

            originalColor = graphic.color;
            originalScale = transform.localScale;
            originalPosition = transform.localPosition;
        }

        public void OnPointerEnter(PointerEventData _)
        {
            ModuleHub.Instance.GetManager<AudioManager>().PlayOneShotWith2DUI(AddressDef.Audio.ui切换音效);
            PlayTween(true);
        }

        public void OnPointerExit(PointerEventData _)
        {
            PlayTween(false);
        }

        private void PlayTween(bool isEnter)
        {
            if (config == null || graphic == null) return;


            currentSeq?.Kill();
            currentSeq = DOTween.Sequence();

            if (config.scaleEnabled)
            {
                float targetScale = isEnter ? originalScale.x * config.hoverScale : originalScale.x;
                currentSeq.Join(transform.DOScale(targetScale, config.scaleDuration).SetEase(config.scaleEase));
            }

            if (config.colorEnabled)
            {
                Color targetColor = isEnter ? config.hoverColor : originalColor;
                currentSeq.Join(graphic.DOColor(targetColor, config.colorDuration));
            }

            if (config.moveEnabled)
            {
                float targetY = isEnter ? originalPosition.y + config.moveOffset : originalPosition.y;
                currentSeq.Join(transform.DOLocalMoveY(targetY, config.moveDuration).SetEase(config.moveEase));
            }

            currentSeq.SetUpdate(true);
        }

        private void OnDestroy()
        {
            currentSeq?.Kill();
        }
    }
}
