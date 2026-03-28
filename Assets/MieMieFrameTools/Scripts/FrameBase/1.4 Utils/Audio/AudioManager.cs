namespace MieMieFrameWork
{
    using Cysharp.Threading.Tasks;
    using MieMieFrameWork.Pool;
    using Sirenix.OdinInspector;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;

    public class AudioManager : MonoBehaviour, I_ManagerBase
    {
        public void Init() 
        {
            ChangeGlobalVolume();
        }

        #region 组件
        [SerializeField, LabelText("特效声音播放器")]
        private GameObject efPlayerES;

        [SerializeField, LabelText("总声音播放器")]
        private AudioSource AudioSource;
        #endregion

        #region 全局音量
        //OnValueChanged仅对编辑器下作用
        [SerializeField, Range(0, 1), OnValueChanged("ChangeGlobalVolume")]
        private float globalVolumeFactor;//全局音量乘法系数
        public float GlobalVolumeFactor
        {
            get => globalVolumeFactor;
            set
            {
                if (globalVolumeFactor == value) return;
                globalVolumeFactor = value;
                ChangeGlobalVolume();
            }
        }
        /// <summary>
        /// 改变全局音量
        /// </summary>
        private void ChangeGlobalVolume()
        {
            ChangeBgVolume();
            ChangeEffectVolume();
        }

        #endregion

        #region 背景音乐音量

        [SerializeField, Range(0, 1), OnValueChanged("ChangeBgVolume")]
        private float bgVolumeBaseNum;
        public float BgVolumeBaseNum
        {
            get => bgVolumeBaseNum; set
            {
                if (bgVolumeBaseNum == value) return;
                bgVolumeBaseNum = value;
                ChangeBgVolume();
            }
        }
        private void ChangeBgVolume()
        {
            AudioSource.volume = bgVolumeBaseNum * globalVolumeFactor;
        }

        #endregion

        #region 特效音量
        [SerializeField, Range(0, 1), OnValueChanged("ChangeEffectVolume")]
        private float effectVolumeBaseNum;
        public float EffectVolumeBaseNum
        {
            get => effectVolumeBaseNum; set
            {
                if (effectVolumeBaseNum == value) return;
                effectVolumeBaseNum = value;
                ChangeEffectVolume();
            }
        }

        //特效声List
        private List<AudioSource> efAudioList = new();
        private void ChangeEffectVolume()
        {
            //倒叙
            for (int i = efAudioList.Count - 1; i >= 0; i--)
            {
                if (efAudioList[i] != null)
                {
                    SetEffectAudioPlay(efAudioList[i]);
                }
                else
                {
                    efAudioList.RemoveAt(i);
                }
            }
        }
        #endregion

        #region 一键功能
        //静音
        [SerializeField]
        [OnValueChanged("OnSelectMute")]
        private bool isMute;
        public bool IsMute
        {
            get => isMute;
            set
            {
                if (isMute == value) return;
                isMute = value;

                OnSelectMute();
            }
        }
        private void OnSelectMute()
        {
            AudioSource.mute = IsMute;
            ChangeEffectVolume();
        }

        //循环
        [SerializeField]
        [OnValueChanged("OnSelectLoop")]
        private bool isLoop;
        public bool IsLoop
        {
            get => isLoop; set
            {
                if (isLoop == value) return;
                isLoop = value;
                OnSelectLoop();
            }
        }

        private void OnSelectLoop()
        {
            AudioSource.loop = IsLoop;
        }

        //暂停
        [SerializeField]
        [OnValueChanged("OnIsPause")]
        private bool isPause;
        public bool IsPause
        {
            get => isPause; set
            {
                if (isPause == value) return;
                isPause = value;

                OnIsPause();
            }
        }

        private void OnIsPause()
        {

            if (isPause == true) AudioSource.Pause();
            else AudioSource.UnPause();

            //音效同步设置
            ChangeEffectVolume();
        }


        #endregion

        /// <summary>
        /// 设置特效音效播放器属性
        /// </summary>
        /// <param name="efAudioSource"></param>
        /// <param name="spatial">空间的</param>
        private void SetEffectAudioPlay(AudioSource efAudioSource, float spatial = 0)
        {
            efAudioSource.mute = isMute;
            // 同步特效音量 = 特效基准 * 全局系数
            efAudioSource.volume = effectVolumeBaseNum * globalVolumeFactor;

            if (spatial != 0)
            {
                efAudioSource.spatialBlend = spatial;
            }
            if (IsPause)
                efAudioSource.Pause();
            else
                efAudioSource.UnPause();
        }

        #region 背景音乐控制

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="audioClip"></param>
        /// <param name="needLoop">是否循环</param>
        /// <param name="volume">默认音量</param>
        public void PlayerBgAudio(AudioClip audioClip, bool needLoop = true, float volume = -1)
        {
            this.AudioSource.clip = audioClip;
            this.IsLoop = needLoop;
            if (volume != -1) BgVolumeBaseNum = volume;
            AudioSource.Play();
        }
        //重载方法 可以音乐路径加载
        public void PlayerBgAudio(string path, bool needLoop = true, float volume = -1)
        {
            AudioClip clip = AddressableMgr.LoadAsset<AudioClip>(path);
            PlayerBgAudio(clip, needLoop, volume);
        }
        #endregion


        #region 特效音乐控制
        [SerializeField, LabelText("特效音乐根节点")]
        private Transform EffectClipRoot;

        /// <summary>
        /// 得到特效声音组件
        /// </summary>
        /// <param name="is3d"></param>
        /// <returns></returns>
        private AudioSource GetEfAudio(bool is3d)
        {
            if (EffectClipRoot == null)
            {
                EffectClipRoot = this.transform.Find("EffectRoot");
            }

            //获取特效播放器上的音源组件
            AudioSource ef = ModuleHub.Instance.GetManager<PoolManager>().GetGameObj<AudioSource>(efPlayerES, EffectClipRoot);
            try
            {
                if (ef != null)
                {
                    SetEffectAudioPlay(ef, is3d ? 1 : 0);
                    efAudioList.Add(ef);
                    return ef;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("获取特效声音组件失败: " + e.Message);
            }
            return null;
        }

        //TODO:控制Clip停止播放 用某一个容器去管理 在需要的时候停止

        /// <summary>
        /// 播放一次特性声 跟随组件位置
        /// </summary>
        /// <param name="clip">音效片段</param>
        /// <param name="component">挂载组件</param>
        /// <param name="volumeScale">音量 0-1</param>
        /// <param name="is3d">是否3D</param>
        /// <param name="callBack">回调函数-在音乐播放完成后执行</param>
        /// <param name="callBackTime">回调函数在音乐播放完成后执行的延迟时间</param>
        public void PlayOneShot(AudioClip clip,
            float volumeScale = 1,
            bool is3d = true,
            Component component = null,
            UnityAction callBack = null,
            float callBackTime = 0)
        {
            // 初始化特效声音播放器
            AudioSource audioSource = GetEfAudio(is3d);
            //如果特效声音播放器都没有则直接返回
            if (audioSource == null) return;

            if (component != null)
            {
                audioSource.transform.SetParent(component.transform);
                audioSource.transform.localPosition = Vector3.zero;
            }
            else
            {
                audioSource.transform.position = this.transform.position;
            }

            // 播放一次音效
            audioSource.PlayOneShot(clip, volumeScale);
            // 播放器回收以及回调函数
            RecycleAudioPlay(audioSource, clip, callBack, callBackTime);
        }


        /// <summary>
        /// 播放一次特效声 路径加载
        /// </summary>
        /// <param name="clipPath">音效路径</param>
        /// <param name="component">挂载组件</param>
        /// <param name="volumeScale">音量 0-1</param>
        /// <param name="is3d">是否3D</param>
        /// <param name="callBack">回调函数-在音乐播放完成后执行</param>
        /// <param name="callBacKTime">回调函数在音乐播放完成后执行的延迟时间</param>
        public void PlayOneShot(string clipPath, Component component, float volumeScale = 1, bool is3d = true, UnityAction callBack = null, float callBacKTime = 0)
        {
            AudioClip audioClip = AddressableMgr.LoadAsset<AudioClip>(clipPath);

            if (audioClip != null) PlayOneShot(audioClip, volumeScale, is3d, component, callBack, callBacKTime);
        }


        /// <summary>
        /// 回收播放器
        /// </summary>
        private void RecycleAudioPlay(AudioSource audioSource, AudioClip clip, UnityAction callBak, float time)
        {
            DoRecycleAudioPlay(audioSource, clip, callBak, time).Forget();
        }

        private async UniTaskVoid DoRecycleAudioPlay(AudioSource audioSource, AudioClip clip, UnityAction callBak, float time)
        {
            //等待音乐播放完成
            await UniTask.WaitForSeconds(clip.length);

            if (audioSource != null)
            {
                //如果播放器在特效声音列表中，则移除
                if (efAudioList.Contains(audioSource))
                    efAudioList.Remove(audioSource);
                audioSource.PushGameObjectToPool();
                //等待回调时间后执行回调
                await UniTask.Delay(
                        TimeSpan.FromSeconds(time), ignoreTimeScale: true).
                            ContinueWith(() => callBak?.Invoke()
                    );

            }
        }

        #endregion


    }

}