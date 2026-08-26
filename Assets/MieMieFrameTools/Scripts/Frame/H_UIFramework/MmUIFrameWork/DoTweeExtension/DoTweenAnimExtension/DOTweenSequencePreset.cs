using DG.Tweening;
using UnityEngine;
namespace MieMieUIFrameWork.Runtime
{
    
    /// <summary>
    /// DOTween 序列预制配方 不含具体 UI 引用
    /// </summary>
    [CreateAssetMenu(fileName = "DOTweenSequencePreset", menuName = "MmUI/DoTween Sequence Preset", order = 100)]
    public class DOTweenSequencePreset : ScriptableObject
    {
        [Tooltip("序列步骤 Target 应为空 由播放器上的播放目标填充")]
        [SerializeField]
        private SequenceAnimation[] m_Sequence;
    
        [SerializeField]
        private float m_Delay;
    
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
    
        public SequenceAnimation[] Sequence => m_Sequence;
    
        public float Delay => m_Delay;
    
        public Ease Ease => m_Ease;
    
        public int Loops => m_Loops;
    
        public LoopType LoopType => m_LoopType;
    
        public UpdateType UpdateType => m_UpdateType;
    
        public bool IgnoreTimeScale => m_IgnoreTimeScale;
    
        /// <summary>
        /// 编辑器工厂写入数据
        /// </summary>
        public void EditorSetData(
            SequenceAnimation[] sequenceList,
            float delay,
            Ease eEase,
            int loops,
            LoopType eLoopType,
            UpdateType eUpdateType,
            bool ignoreTimeScale)
        {
            m_Sequence = sequenceList;
            m_Delay = delay;
            m_Ease = eEase;
            m_Loops = loops;
            m_LoopType = eLoopType;
            m_UpdateType = eUpdateType;
            m_IgnoreTimeScale = ignoreTimeScale;
        }
    
        /// <summary>
        /// 复制序列步骤到新数组 供内联编辑
        /// </summary>
        public SequenceAnimation[] CloneSequence()
        {
            if (m_Sequence == null || m_Sequence.Length == 0) return new SequenceAnimation[0];
    
            var cloneList = new SequenceAnimation[m_Sequence.Length];
            for (int i = 0; i < m_Sequence.Length; i++)
            {
                cloneList[i] = CloneItem(m_Sequence[i]);
            }
    
            return cloneList;
        }
    
        /// <summary>
        /// 复制单条步骤
        /// </summary>
        public static SequenceAnimation CloneItem(SequenceAnimation source)
        {
            if (source == null) return new SequenceAnimation();
            return new SequenceAnimation
            {
                AddType = source.AddType,
                AnimationType = source.AnimationType,
                Target = null,
                ToValue = source.ToValue,
                UseToTarget = false,
                ToTarget = null,
                UseFromValue = source.UseFromValue,
                FromValue = source.FromValue,
                SpeedBased = source.SpeedBased,
                DurationOrSpeed = source.DurationOrSpeed,
                Delay = source.Delay,
                UpdateType = source.UpdateType,
                CustomEase = source.CustomEase,
                EaseCurve = source.EaseCurve,
                Ease = source.Ease,
                Loops = source.Loops,
                LoopType = source.LoopType,
                Snapping = source.Snapping
            };
        }
    }
    
}