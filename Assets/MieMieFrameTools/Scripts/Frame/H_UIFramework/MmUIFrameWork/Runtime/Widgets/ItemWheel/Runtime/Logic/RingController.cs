using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个扇环交互与选中动画 不含物品数据
/// </summary>
public class RingController : MonoBehaviour, IRingBehaviour
{
    [SerializeField] private float scaleUpFactor = 1.08f;
    [SerializeField] private float tweenDuration = 0.22f;
    [SerializeField] private float backEaseAmplitude = 0.2f;
    [SerializeField] private Color highlightColor = new Color(1f, 0.92f, 0.65f, 1f);

    private Image itemIcon;
    private RingDraw ringDraw;

    private Vector3 originalScale;
    private Color originalRingColor;

    private Tween scaleTween;
    private Tween colorTween;

    private void Awake()
    {
        ringDraw = GetComponent<RingDraw>();
        originalScale = transform.localScale;

        if (ringDraw != null)
            originalRingColor = ringDraw.color;
    }

    private void Start()
    {
        itemIcon = GetComponentInChildren<Image>();
    }

    private void OnDestroy()
    {
        scaleTween?.Kill();
        colorTween?.Kill();
    }

    /// <summary>
    /// 设置扇区图标
    /// </summary>
    public void SetItemIcon(Sprite icon)
    {
        if (itemIcon == null)
            itemIcon = GetComponentInChildren<Image>();
        if (itemIcon != null)
            itemIcon.sprite = icon;
    }

    public void OnEnter()
    {
        OnEnterAnimation();
    }

    public void OnExit()
    {
        OnExitAnimation();
    }

    /// <summary>
    /// 移入动画 外扩缩放与高亮
    /// </summary>
    private void OnEnterAnimation()
    {
        KillActiveTween();

        scaleTween = transform
            .DOScale(originalScale * scaleUpFactor, tweenDuration)
            .SetEase(Ease.OutBack, backEaseAmplitude)
            .SetUpdate(true);

        if (ringDraw != null)
        {
            colorTween = ringDraw
                .DOColor(highlightColor, tweenDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }
    }

    /// <summary>
    /// 移出动画 缩回与还原颜色
    /// </summary>
    private void OnExitAnimation()
    {
        KillActiveTween();

        scaleTween = transform
            .DOScale(originalScale, tweenDuration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);

        if (ringDraw != null)
        {
            colorTween = ringDraw
                .DOColor(originalRingColor, tweenDuration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }
    }

    /// <summary>
    /// 清理当前扇环上的补间
    /// </summary>
    private void KillActiveTween()
    {
        scaleTween?.Kill();
        colorTween?.Kill();
        scaleTween = null;
        colorTween = null;
    }
}
