using System;
using System.Collections.Generic;
using MieMieFrameWork;
using UnityEngine;

namespace MieMieFrameWork.MMAnimation
{
    public enum E_AniamtionParamType
    {
        None,
        Int,
        Float,
        String,
        Object
    }

    public class AnimationReceiver : SingletonMono<AnimationReceiver>
    {
        private List<string> animationEventList = new();
        public List<string> AnimationEventList => animationEventList;
        private int characterId;

        public int CharacterID
        {
            get => characterId;
            set
            {
                if (characterId == value) return;
                characterId = value;
            }
        }
        private string tempId;

        void OnDestroy()
        {
            foreach (var item in animationEventList)
            {
                EventCenter.RemoveListener(item);
            }
            animationEventList.Clear();
            characterId = -1;
            tempId = null;
        }


        private void RemoveNameEvent(string eventName)
        {
            if (animationEventList.Contains(eventName))
            {
                EventCenter.RemoveListener(eventName);
                animationEventList.Remove(eventName);
            }
        }
        #region 加减事件
        public void AddAnimationEvent(string eventName, Action action)
        {
            tempId = characterId + eventName;
            if (animationEventList.Contains(tempId))
            {
                Debug.LogError($"AnimationEvent {eventName} already added");
                return;
            }
            animationEventList.Add(tempId);
            EventCenter.AddEventListener(tempId, action);
        }

        public void RemoveAnimationEvent(string eventName, Action action)
        {
            tempId = characterId + eventName;
            if (!animationEventList.Contains(tempId))
            {
                Debug.LogError($"AnimationEvent {tempId} not found");
                return;
            }
            animationEventList.Remove(tempId);
            EventCenter.RemoveListener(tempId, action);
        }
        public void AddAnimationEvent<T>(string eventName, Action<T> action)
        {
            tempId = characterId + eventName;
            if (animationEventList.Contains(tempId))
            {
                Debug.LogError($"AnimationEvent {eventName} already added");
                return;
            }
            animationEventList.Add(tempId);
            EventCenter.AddEventListener(tempId, action);
        }

        public void RemoveAnimationEvent<T>(string eventName, Action<T> action)
        {
            tempId = characterId + eventName;
            if (!animationEventList.Contains(tempId))
            {
                Debug.LogError($"AnimationEvent {tempId} not found");
                return;
            }
            animationEventList.Remove(tempId);
            EventCenter.RemoveListener(tempId, action);
        }
        #endregion

        #region 触发事件 
        public void OnAnimationEventTriggered(string eventName)
        {
            EventCenter.TriggerEvent(characterId + eventName);
        }
        public void OnIntAnimationEventTriggered(string eventName, int value)
        {
            EventCenter.TriggerEvent(characterId + eventName, value);
        }
        public void OnFloatAnimationEventTriggered(string eventName, float value)
        {
            EventCenter.TriggerEvent(characterId + eventName, value);
        }
        public void OnStringAnimationEventTriggered(string eventName, string value)
        {
            EventCenter.TriggerEvent(characterId + eventName, value);
        }
        public void OnObjectAnimationEventTriggered(string eventName, object value)
        {
            EventCenter.TriggerEvent(characterId + eventName, value);
        }
        #endregion

    }
}
