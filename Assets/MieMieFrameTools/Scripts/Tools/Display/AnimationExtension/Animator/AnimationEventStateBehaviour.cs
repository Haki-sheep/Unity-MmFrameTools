namespace MieMieFrameWork.MMAnimation
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class AnimationEventInfo
    {
        public string eventName;
        public bool triggerOnce;
        [Range(0f, 1f)] public float triggerTime;

        public E_AniamtionParamType paramType = E_AniamtionParamType.None;
        public int intValue;
        public float floatValue;
        public string stringValue;
        public UnityEngine.Object objectValue;
        public bool boolValue;
        public bool isTrigger = false;

        /// <summary>
        /// 是否等待 Animator 过渡结束后再触发 适合音效特效
        /// </summary>
        public bool waitTransitionEnd = true;
    }

    public class AnimationEventStateBehaviour : StateMachineBehaviour
    {
        [SerializeField] private List<AnimationEventInfo> animationEventInfoList = new();
        private AnimationReceiver reciver;
        private float animationStartTime;
        private float previewFrameTime;
        private bool isFirstFrame = true;

        /// <summary> 按 triggerTime 排序后的下标缓存 </summary>
        private readonly List<int> sortedEventIndexList = new();

        /// <summary> 缓存对应的事件条数 </summary>
        private int sortedCacheCount = -1;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animationStartTime = stateInfo.normalizedTime;
            previewFrameTime = animationStartTime;
            isFirstFrame = true;
            reciver ??= animator.GetComponent<AnimationReceiver>();
            RebuildSortedEventIndexList(true);

            foreach (var item in animationEventInfoList)
            {
                item.isTrigger = false;
            }
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            float currentTime = stateInfo.normalizedTime;

            if (isFirstFrame)
            {
                isFirstFrame = false;
                previewFrameTime = animationStartTime;
            }

            float normalizedCurrentTime = (currentTime - animationStartTime) % 1f;
            if (normalizedCurrentTime < 0) normalizedCurrentTime += 1f;

            float normalizedPreviewTime = (previewFrameTime - animationStartTime) % 1f;
            if (normalizedPreviewTime < 0) normalizedPreviewTime += 1f;

            bool inTransition = animator.IsInTransition(layerIndex);

            RebuildSortedEventIndexList(false);
            int sortedCount = sortedEventIndexList.Count;
            for (int order = 0; order < sortedCount; order++)
            {
                var item = animationEventInfoList[sortedEventIndexList[order]];

                bool looped = normalizedCurrentTime < normalizedPreviewTime;
                if (looped && !item.triggerOnce)
                    item.isTrigger = false;

                if (item.isTrigger)
                    continue;

                if (item.waitTransitionEnd)
                {
                    if (inTransition)
                        continue;

                    if (normalizedCurrentTime >= item.triggerTime)
                    {
                        item.isTrigger = true;
                        TriggerEvent(item, normalizedCurrentTime, normalizedPreviewTime);
                    }

                    continue;
                }

                bool onTriggerPoint = normalizedPreviewTime <= item.triggerTime
                    && normalizedCurrentTime >= item.triggerTime;
                if (onTriggerPoint)
                {
                    item.isTrigger = true;
                    TriggerEvent(item, normalizedCurrentTime, normalizedPreviewTime);
                }
            }

            previewFrameTime = currentTime;
        }

        /// <summary>
        /// 重建按 triggerTime 升序的下标 列表不变则复用
        /// </summary>
        private void RebuildSortedEventIndexList(bool force)
        {
            int eventCount = animationEventInfoList.Count;
            if (!force && sortedCacheCount == eventCount && sortedEventIndexList.Count == eventCount)
                return;

            sortedEventIndexList.Clear();
            for (int i = 0; i < eventCount; i++)
                sortedEventIndexList.Add(i);

            sortedEventIndexList.Sort((leftIndex, rightIndex) =>
            {
                float leftTime = animationEventInfoList[leftIndex].triggerTime;
                float rightTime = animationEventInfoList[rightIndex].triggerTime;
                int timeCompare = leftTime.CompareTo(rightTime);
                return timeCompare != 0 ? timeCompare : leftIndex.CompareTo(rightIndex);
            });
            sortedCacheCount = eventCount;
        }

        /// <summary>
        /// 触发事件并输出调试信息
        /// </summary>
        private void TriggerEvent(AnimationEventInfo item, float normalizedCurrentTime, float normalizedPreviewTime)
        {
            if (reciver == null)
                return;

            switch (item.paramType)
            {
                case E_AniamtionParamType.None:
                    reciver.OnAnimationEventTriggered(item.eventName);
                    break;
                case E_AniamtionParamType.Int:
                    reciver.OnIntAnimationEventTriggered(item.eventName, item.intValue);
                    break;
                case E_AniamtionParamType.Float:
                    reciver.OnFloatAnimationEventTriggered(item.eventName, item.floatValue);
                    break;
                case E_AniamtionParamType.String:
                    reciver.OnStringAnimationEventTriggered(item.eventName, item.stringValue);
                    break;
                case E_AniamtionParamType.Object:
                    reciver.OnObjectAnimationEventTriggered(item.eventName, item.objectValue);
                    break;
                case E_AniamtionParamType.Bool:
                    reciver.OnBoolAnimationEventTriggered(item.eventName, item.boolValue);
                    break;
            }

#if UNITY_EDITOR
            Debug.Log($"AnimationEvent:{item.eventName} + " +
                 $"TriggerNomalizaTime:{item.triggerTime} + " +
                 $"CurrentRelativeTime:{normalizedCurrentTime} + " +
                 $"Offset:{normalizedCurrentTime - normalizedPreviewTime} + " +
                 $"WaitTransitionEnd:{item.waitTransitionEnd} + " +
                 $"ParamType:{item.paramType}");
#endif
        }
    }
}
