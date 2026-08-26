using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace MieMieUIFrameWork.Runtime
{
    using Component = UnityEngine.Component;
    
    [Serializable]
    public enum DOTweenType
    {
        [InspectorName("移动")]
        DOMove,
        [InspectorName("移动X")]
        DOMoveX,
        [InspectorName("移动Y")]
        DOMoveY,
        [InspectorName("移动Z")]
        DOMoveZ,
    
        [InspectorName("本地移动")]
        DOLocalMove,
        [InspectorName("本地移动X")]
        DOLocalMoveX,
        [InspectorName("本地移动Y")]
        DOLocalMoveY,
        [InspectorName("本地移动Z")]
        DOLocalMoveZ,
    
        [InspectorName("缩放")]
        DOScale,
        [InspectorName("缩放X")]
        DOScaleX,
        [InspectorName("缩放Y")]
        DOScaleY,
        [InspectorName("缩放Z")]
        DOScaleZ,
    
        [InspectorName("旋转")]
        DORotate,
        [InspectorName("本地旋转")]
        DOLocalRotate,
    
        [InspectorName("锚点移动")]
        DOAnchorPos,
        [InspectorName("锚点移动X")]
        DOAnchorPosX,
        [InspectorName("锚点移动Y")]
        DOAnchorPosY,
        [InspectorName("锚点移动Z")]
        DOAnchorPosZ,
        [InspectorName("锚点3D移动")]
        DOAnchorPos3D,
    
        [InspectorName("颜色渐变")]
        DOColor,
        [InspectorName("透明度渐变")]
        DOFade,
        [InspectorName("CanvasGroup透明度")]
        DOCanvasGroupFade,
        [InspectorName("填充渐变")]
        DOFillAmount,
        [InspectorName("弹性尺寸")]
        DOFlexibleSize,
        [InspectorName("最小尺寸")]
        DOMinSize,
        [InspectorName("首选尺寸")]
        DOPreferredSize,
        [InspectorName("尺寸变化")]
        DOSizeDelta,
        [InspectorName("数值渐变")]
        DOValue
    }
    
    [Serializable]
    public enum AddType
    {
        [InspectorName("追加")]
        Append,
        [InspectorName("并行")]
        Join
    }
    
    [Serializable]
    public class SequenceAnimation
    {
        [InspectorName("添加方式"), Tooltip("追加：等待前一个完成 | 并行：与前一个同时开始")]
        public AddType AddType = AddType.Append;
    
        [InspectorName("动画类型"), Tooltip("移动、缩放、旋转、透明度等")]
        public DOTweenType AnimationType = DOTweenType.DOMove;
    
        [InspectorName("目标物体"), Tooltip("应用动画的目标物体")]
        public Component Target = null;
    
        [InspectorName("目标值"), Tooltip("动画结束时的目标值")]
        public Vector4 ToValue = Vector4.zero;
    
        [InspectorName("使用目标"), Tooltip("使用另一个物体作为终点")]
        public bool UseToTarget = false;
        [InspectorName("终点目标"), Tooltip("作为终点的目标物体")]
        public Component ToTarget = null;
    
        [InspectorName("使用起始值"), Tooltip("播放前先重置到起始值")]
        public bool UseFromValue = false;
        [InspectorName("起始值"), Tooltip("动画开始前的初始值")]
        public Vector4 FromValue = Vector4.zero;
    
        [InspectorName("速度模式"), Tooltip("使用速度计算时间，而非直接设置时间")]
        public bool SpeedBased = false;
    
        [InspectorName("持续时间"), Tooltip("动画持续时间（秒）")]
        public float DurationOrSpeed = 1;
    
        [InspectorName("延迟"), Tooltip("等待多久后开始此动画")]
        public float Delay = 0;
    
        [InspectorName("更新类型"), Tooltip("Normal：正常更新 | Late：LateUpdate更新 | Fixed：物理更新")]
        public UpdateType UpdateType = UpdateType.Normal;
    
        [InspectorName("自定义曲线"), Tooltip("使用自定义曲线而非预设缓动")]
        public bool CustomEase = false;
    
        [InspectorName("缓动曲线"), Tooltip("自定义的缓动曲线")]
        public AnimationCurve EaseCurve;
    
        [InspectorName("缓动曲线"), Tooltip("动画的缓动曲线类型")]
        public Ease Ease = Ease.OutQuad;
    
        [InspectorName("循环次数"), Tooltip("动画循环的次数")]
        public int Loops = 1;
    
        [InspectorName("循环类型"), Tooltip("Restart：重新开始 | Yoyo：来回播放 | Incremental：累加")]
        public LoopType LoopType = LoopType.Restart;
    
        [InspectorName("整数对齐"), Tooltip("像素对齐，UI动画建议开启")]
        public bool Snapping = false;
    
        [InspectorName("开始时回调"), Tooltip("动画开始时触发")]
        public UnityEvent OnPlay = null;
    
        [InspectorName("更新时回调"), Tooltip("动画每帧更新时触发")]
        public UnityEvent OnUpdate = null;
    
        [InspectorName("完成时回调"), Tooltip("动画完成时触发")]
        public UnityEvent OnComplete = null;
    
        /// <summary>
        /// 创建 Tween
        /// </summary>
        public Tween CreateTween(Component target, bool reverse)
        {
            Component effectiveTarget = target != null ? target : Target;
            return SequenceAnimationTweenBuilder.Create(this, effectiveTarget, reverse);
        }
    }
    
    /// <summary>
    /// DOTween 序列播放器 可挂 Preset 或内联序列
    /// </summary>
    public class DOTweenSequence : MonoBehaviour
    {
        [SerializeField]
        private DOTweenSequencePreset m_Preset;
    
        [HideInInspector]
        [SerializeField]
        public SequenceAnimation[] m_Sequence;
    
        [Tooltip("播放目标 预设步骤 Target 为空时使用")]
        public Component DefaultTarget = null;
    
        public Dictionary<int, Component> RuntimeTargetOverrides = null;
    
        [SerializeField]
        private bool m_PlayOnAwake = false;
    
        [SerializeField]
        private bool m_ResetOnAwake = false;
    
        [SerializeField]
        private float m_Delay = 0;
    
        [SerializeField]
        private Ease m_Ease = Ease.OutQuad;
    
        [SerializeField]
        private int m_Loops = 1;
    
        [SerializeField]
        private LoopType m_LoopType = LoopType.Restart;
    
        [SerializeField]
        private UpdateType m_UpdateType = UpdateType.Normal;
    
        [SerializeField]
        private bool m_IgnoreTimeScale = true;
    
        [SerializeField]
        private UnityEvent m_OnPlay = null;
    
        [SerializeField]
        private UnityEvent m_OnUpdate = null;
    
        [SerializeField]
        private UnityEvent m_OnComplete = null;
    
        public DOTweenSequencePreset Preset => m_Preset;
    
        private void Awake()
        {
            if (m_PlayOnAwake)
            {
                DOPlay();
            }
            else if (m_ResetOnAwake)
            {
                ResetToFromValue();
            }
        }
    
        private void OnDestroy()
        {
            DOKill();
        }
    
        /// <summary>
        /// 当前生效序列 Preset 优先
        /// </summary>
        public SequenceAnimation[] GetActiveSequence()
        {
            if (m_Preset != null && m_Preset.Sequence != null && m_Preset.Sequence.Length > 0)
                return m_Preset.Sequence;
            return m_Sequence;
        }
    
        /// <summary>
        /// 是否使用预设
        /// </summary>
        public bool HasPreset => m_Preset != null;
    
        /// <summary>
        /// 重置所有子序列起始值
        /// </summary>
        private void ResetToFromValue()
        {
            var sequenceList = GetActiveSequence();
            if (sequenceList == null) return;
    
            for (int i = 0; i < sequenceList.Length; i++)
            {
                SequenceAnimation item = sequenceList[i];
                if (!item.UseFromValue) continue;
    
                Component targetCom = ResolveTarget(item, i);
                if (targetCom == null) continue;
    
                SequenceAnimationResetApplier.Apply(targetCom, item.AnimationType, item.FromValue);
            }
        }
    
        /// <summary>
        /// 创建完整序列 Tween
        /// </summary>
        private Tween CreateTween(Component runtimeTarget, bool reverse = false)
        {
            var sequenceList = GetActiveSequence();
            if (sequenceList == null || sequenceList.Length == 0) return null;
    
            Sequence sequence = DOTween.Sequence();
            if (reverse)
            {
                for (int i = sequenceList.Length - 1; i >= 0; i--)
                {
                    AppendSequenceItem(sequence, sequenceList, i, runtimeTarget, reverse);
                }
            }
            else
            {
                for (int i = 0; i < sequenceList.Length; i++)
                {
                    AppendSequenceItem(sequence, sequenceList, i, runtimeTarget, reverse);
                }
            }
    
            ApplySequenceSettings(sequence);
            return sequence;
        }
    
        /// <summary>
        /// 创建指定索引范围的 Tween
        /// </summary>
        private Tween CreateTween(Component runtimeTarget, bool reverse, int startIndex, int endIndex)
        {
            Sequence sequence = DOTween.Sequence();
            var sequenceList = GetActiveSequence();
            if (sequenceList == null || sequenceList.Length == 0) return sequence;
    
            int clampedStart = Mathf.Clamp(startIndex, 0, sequenceList.Length - 1);
            int clampedEnd = Mathf.Clamp(endIndex, 0, sequenceList.Length - 1);
            if (clampedStart > clampedEnd) return sequence;
    
            for (int i = clampedStart; i <= clampedEnd; i++)
            {
                AppendSequenceItem(sequence, sequenceList, i, runtimeTarget, reverse);
            }
    
            ApplySequenceSettings(sequence);
            return sequence;
        }
    
        /// <summary>
        /// 向序列追加单条动画
        /// </summary>
        private void AppendSequenceItem(Sequence sequence, SequenceAnimation[] sequenceList, int index, Component runtimeTarget, bool reverse)
        {
            SequenceAnimation item = sequenceList[index];
            Component target = ResolveTarget(item, index, runtimeTarget);
            Tween tweener = item.CreateTween(target, reverse);
            if (tweener == null)
            {
                Debug.LogErrorFormat(
                    "Tweener is null. Index:{0}, Animation Type:{1}, Component Type:{2}",
                    index,
                    item.AnimationType,
                    target == null ? "null" : target.GetType().Name);
                return;
            }
    
            bool ignoreTimeScale = m_Preset != null ? m_Preset.IgnoreTimeScale : m_IgnoreTimeScale;
            tweener.SetUpdate(!ignoreTimeScale);
            if (item.AddType == AddType.Append) sequence.Append(tweener);
            else sequence.Join(tweener);
        }
    
        /// <summary>
        /// 应用序列级通用配置
        /// </summary>
        private void ApplySequenceSettings(Sequence sequence)
        {
            if (m_Preset != null)
            {
                sequence.SetUpdate(m_Preset.UpdateType);
                sequence.SetDelay(m_Preset.Delay);
                sequence.SetEase(m_Preset.Ease);
                if (m_Preset.Loops > 1) sequence.SetLoops(m_Preset.Loops, m_Preset.LoopType);
            }
            else
            {
                sequence.SetUpdate(m_UpdateType);
                sequence.SetDelay(m_Delay);
                sequence.SetEase(m_Ease);
                if (m_Loops > 1) sequence.SetLoops(m_Loops, m_LoopType);
            }
    
            sequence.OnStart(() => m_OnPlay?.Invoke());
            sequence.OnUpdate(() => m_OnUpdate?.Invoke());
            sequence.OnComplete(() => m_OnComplete?.Invoke());
        }
    
        public Tween DOPlay(bool recyle = true)
        {
            DOKill();
            Tween tween = CreateTween(null, false);
            if (tween == null) return null;
            tween.SetAutoKill(recyle);
            tween.Play();
            return tween;
        }
    
        /// <summary>
        /// 使用指定的运行时 Target 播放动画
        /// </summary>
        public Tween DOPlay(Component target, bool recyle = true)
        {
            DOTween.Kill(target);
            Tween tween = CreateTween(target, false);
            if (tween == null) return null;
            tween.SetAutoKill(true);
            tween.Play();
            return tween;
        }
    
        public void DORewind()
        {
            DORewind(null);
        }
    
        /// <summary>
        /// 使用指定的运行时 Target 重置动画
        /// </summary>
        public void DORewind(Component target)
        {
            DOTween.Kill(target);
            Tween tween = CreateTween(target, true);
            if (tween == null) return;
            tween.SetAutoKill(true);
            tween.Play();
        }
    
        /// <summary>
        /// 使用指定的运行时 Target 播放指定索引范围的动画
        /// </summary>
        public Tween DOPlay(Component target, int startIndex, int endIndex)
        {
            DOTween.Kill(target);
            Tween tween = CreateTween(target, false, startIndex, endIndex);
            if (tween == null) return null;
            tween.SetAutoKill(true);
            tween.Play();
            return tween;
        }
    
        /// <summary>
        /// 使用指定的运行时 Target 重置指定索引范围的动画
        /// </summary>
        public void DORewind(Component target, int startIndex, int endIndex)
        {
            DOTween.Kill(target);
            Tween tween = CreateTween(target, true, startIndex, endIndex);
            if (tween == null) return;
            tween.SetAutoKill(true);
            tween.Play();
        }
    
        public void DOKill()
        {
            if (DefaultTarget != null) DOTween.Kill(DefaultTarget);
            DOTween.Kill(this);
        }
    
        /// <summary>
        /// 解析目标 步骤 Target 优先 否则播放目标 再按类型纠正组件
        /// </summary>
        private Component ResolveTarget(SequenceAnimation item, int index)
        {
            if (RuntimeTargetOverrides != null && RuntimeTargetOverrides.TryGetValue(index, out Component overrideTarget) && overrideTarget != null)
            {
                return FixComponentForType(overrideTarget, item.AnimationType) ?? overrideTarget;
            }
    
            Component source = item.Target != null ? item.Target : DefaultTarget;
            if (source == null) return null;
            return FixComponentForType(source, item.AnimationType) ?? source;
        }
    
        /// <summary>
        /// 解析目标 运行时 Target 优先级最高
        /// </summary>
        private Component ResolveTarget(SequenceAnimation item, int index, Component runtimeTarget)
        {
            if (runtimeTarget != null)
                return FixComponentForType(runtimeTarget, item.AnimationType) ?? runtimeTarget;
            return ResolveTarget(item, index);
        }
    
        /// <summary>
        /// 按动画类型纠正到合适组件
        /// </summary>
        public static Component FixComponentForType(Component source, DOTweenType eType)
        {
            if (source == null) return null;
            switch (eType)
            {
                case DOTweenType.DOMove:
                case DOTweenType.DOMoveX:
                case DOTweenType.DOMoveY:
                case DOTweenType.DOMoveZ:
                case DOTweenType.DOLocalMove:
                case DOTweenType.DOLocalMoveX:
                case DOTweenType.DOLocalMoveY:
                case DOTweenType.DOLocalMoveZ:
                case DOTweenType.DOScale:
                case DOTweenType.DOScaleX:
                case DOTweenType.DOScaleY:
                case DOTweenType.DOScaleZ:
                case DOTweenType.DORotate:
                case DOTweenType.DOLocalRotate:
                    return source.GetComponent<Transform>();
                case DOTweenType.DOAnchorPos:
                case DOTweenType.DOAnchorPosX:
                case DOTweenType.DOAnchorPosY:
                case DOTweenType.DOAnchorPosZ:
                case DOTweenType.DOAnchorPos3D:
                case DOTweenType.DOSizeDelta:
                    return source.GetComponent<RectTransform>();
                case DOTweenType.DOColor:
                case DOTweenType.DOFade:
                    return source.GetComponent<Graphic>();
                case DOTweenType.DOCanvasGroupFade:
                    return source.GetComponent<CanvasGroup>();
                case DOTweenType.DOFillAmount:
                    return source.GetComponent<Image>();
                case DOTweenType.DOFlexibleSize:
                case DOTweenType.DOMinSize:
                case DOTweenType.DOPreferredSize:
                    return source.GetComponent<LayoutElement>();
                case DOTweenType.DOValue:
                    return source.GetComponent<Slider>();
                default:
                    return source;
            }
        }
    
    #if UNITY_EDITOR
        /// <summary>
        /// 编辑器 复制预设到内联并清空预设引用
        /// </summary>
        public void EditorApplyPresetToInline()
        {
            if (m_Preset == null) return;
            m_Sequence = m_Preset.CloneSequence();
            m_Delay = m_Preset.Delay;
            m_Ease = m_Preset.Ease;
            m_Loops = m_Preset.Loops;
            m_LoopType = m_Preset.LoopType;
            m_UpdateType = m_Preset.UpdateType;
            m_IgnoreTimeScale = m_Preset.IgnoreTimeScale;
            m_Preset = null;
        }
    
        /// <summary>
        /// 编辑器设置预设引用
        /// </summary>
        public void EditorSetPreset(DOTweenSequencePreset preset)
        {
            m_Preset = preset;
        }
    
        /// <summary>
        /// 从当前内联配置构建可保存的预设实例 Target 已剥离
        /// </summary>
        public DOTweenSequencePreset EditorCreatePresetAssetInstance()
        {
            var preset = ScriptableObject.CreateInstance<DOTweenSequencePreset>();
            SequenceAnimation[] cloneList = null;
            if (m_Sequence != null && m_Sequence.Length > 0)
            {
                cloneList = new SequenceAnimation[m_Sequence.Length];
                for (int i = 0; i < m_Sequence.Length; i++)
                {
                    cloneList[i] = DOTweenSequencePreset.CloneItem(m_Sequence[i]);
                }
            }
            else
            {
                cloneList = new SequenceAnimation[0];
            }
    
            preset.EditorSetData(cloneList, m_Delay, m_Ease, m_Loops, m_LoopType, m_UpdateType, m_IgnoreTimeScale);
            return preset;
        }
    #endif
    }
    
}