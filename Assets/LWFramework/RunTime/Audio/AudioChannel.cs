using LWCore;
using UnityEngine;

namespace LWAudio
{
    public class AudioChannel : IPoolGameObject
    {
        private enum FadeState
        {
            None = 0,
            FadeIn = 1,
            FadeOut = 2,
        }

        protected GameObject m_Entity;
        private AudioSource m_Source;
        private Transform m_DefaultParent;
        private bool m_IsPause = false;

        private FadeState m_FadeState = FadeState.None;
        private float m_FadeDurationSeconds = 0f;
        private float m_FadeElapsedSeconds = 0f;
        private float m_FadeStartVolume = 1f;
        private float m_FadeTargetVolume = 1f;
        private bool m_ShouldRecycle = false;

        private float m_DefaultFadeInSeconds = 0f;
        private float m_DefaultFadeOutSeconds = 0f;
        private bool m_UseFadeOnStop = false;

        /// <summary>
        /// 是否需要在管理器更新中回收（用于淡出结束后回收）。
        /// </summary>
        public bool ShouldRecycle
        {
            get { return m_ShouldRecycle; }
        }

        /// <summary>
        /// 配置通道的默认淡入淡出参数（用于后续 Stop 走淡出）。
        /// </summary>
        internal void ConfigureFade(float fadeInSeconds, float fadeOutSeconds, bool useFadeOnStop)
        {
            m_DefaultFadeInSeconds = Mathf.Max(0f, fadeInSeconds);
            m_DefaultFadeOutSeconds = Mathf.Max(0f, fadeOutSeconds);
            m_UseFadeOnStop = useFadeOnStop;
        }
        /// <summary>
        /// 当前播放的音频。
        /// </summary>
        public AudioClip AudioClip
        {
            set => m_Source.clip = value;
            get => m_Source.clip;
        }

        /// <summary>
        /// 当前通道音量。
        /// </summary>
        public float Volume
        {
            set => m_Source.volume = value;
            get => m_Source.volume;
        }

        /// <summary>
        /// 当前通道音高。
        /// </summary>
        public float Pitch
        {
            set => m_Source.pitch = value;
        }

        /// <summary>
        /// 是否循环。
        /// </summary>
        public bool Loop
        {
            set => m_Source.loop = value;
        }

        /// <summary>
        /// 设置通道父节点（用于跟随挂点）。
        /// </summary>
        public Transform Parent
        {
            set
            {
                m_Entity.transform.position = value.position;
                m_Entity.transform.parent = value;
            }
        }

        /// <summary>
        /// 设置通道世界坐标。
        /// </summary>
        public Vector3 Posi
        {
            set
            {
                m_Entity.transform.position = value;
            }
        }

        /// <summary>
        /// 是否正在播放。
        /// </summary>
        public bool IsPlay
        {
            get
            {
                if (m_Source && m_Source.isPlaying)
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
        }
        /// <summary>
        /// 是否处于暂停状态。
        /// </summary>
        public bool IsPause
        {
            get
            {
                return m_IsPause;
            }
        }

        /// <summary>
        /// 创建 GameObject 实体并绑定 AudioSource。
        /// </summary>
        public void Create(GameObject gameObject)
        {
            m_Entity = gameObject;
            m_Source = m_Entity.AddComponent<AudioSource>();
            m_DefaultParent = m_Entity.transform.parent;
            m_Source.playOnAwake = false;
            m_Source.spatialBlend = 0f;
            m_IsPause = false;
            m_FadeState = FadeState.None;
            m_ShouldRecycle = false;
            m_DefaultFadeInSeconds = 0f;
            m_DefaultFadeOutSeconds = 0f;
            m_UseFadeOnStop = false;
        }

        /// <summary>
        /// 设置为 2D 音频（位置/距离衰减等参数不生效）。
        /// </summary>
        public void Set2D()
        {
            if (m_Source)
            {
                m_Source.spatialBlend = 0f;
            }
        }

        /// <summary>
        /// 设置为 3D 音频（支持空间混合、距离衰减、Doppler 等）。
        /// </summary>
        public void Set3D(Audio3DSettings settings)
        {
            if (!m_Source)
            {
                return;
            }

            m_Source.spatialBlend = Mathf.Clamp01(settings.SpatialBlend);
            m_Source.rolloffMode = settings.RolloffMode;

            float minDistanceClamped = Mathf.Max(0f, settings.MinDistance);
            float maxDistanceClamped = Mathf.Max(minDistanceClamped, settings.MaxDistance);
            m_Source.minDistance = minDistanceClamped;
            m_Source.maxDistance = maxDistanceClamped;

            m_Source.dopplerLevel = Mathf.Max(0f, settings.DopplerLevel);
            m_Source.spread = Mathf.Clamp(settings.Spread, 0f, 360f);
        }

        /// <summary>
        /// 当前 GameObject 是否激活（层级激活）。
        /// </summary>
        public bool IsActive()
        {
            return m_Entity != null && m_Entity.activeInHierarchy;
        }

        /// <summary>
        /// 当前通道是否有效（内部 GameObject 未被销毁）。
        /// </summary>
        public bool IsValid()
        {
            return m_Entity;
        }

        /// <summary>
        /// 释放通道资源并销毁 GameObject。
        /// </summary>
        public void OnRelease()
        {
            if (m_Entity)
                GameObject.Destroy(m_Entity);
        }

        /// <summary>
        /// 设置 GameObject 激活状态（由对象池统一调用）。
        /// </summary>
        public void SetActive(bool active)
        {
            if (m_Entity)
                m_Entity.SetActive(active);
        }

        /// <summary>
        /// 回收前状态复位（由对象池调用）。
        /// </summary>
        public void OnUnSpawn()
        {
            if (m_Entity)
            {
                m_Entity.transform.position = m_DefaultParent.position;
                m_Entity.transform.parent = m_DefaultParent;
                StopPlayback();
                Volume = 1;
                AudioClip = null;
                Loop = false;
                m_IsPause = false;
                m_FadeState = FadeState.None;
                m_ShouldRecycle = false;
                m_DefaultFadeInSeconds = 0f;
                m_DefaultFadeOutSeconds = 0f;
                m_UseFadeOnStop = false;

            }
        }

        /// <summary>
        /// 取出后初始化（由对象池调用）。
        /// </summary>
        public void OnSpawn()
        {
            m_IsPause = false;
            m_FadeState = FadeState.None;
            m_ShouldRecycle = false;
            m_DefaultFadeInSeconds = 0f;
            m_DefaultFadeOutSeconds = 0f;
            m_UseFadeOnStop = false;
        }

        /// <summary>
        /// 每帧更新（由 AudioManager 调用），用于处理淡入淡出。
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (m_FadeState == FadeState.None)
            {
                return;
            }

            if (deltaTime <= 0f)
            {
                return;
            }

            m_FadeElapsedSeconds += deltaTime;

            float t = 1f;
            if (m_FadeDurationSeconds > 0f)
            {
                t = Mathf.Clamp01(m_FadeElapsedSeconds / m_FadeDurationSeconds);
            }

            float currentVolume = Mathf.Lerp(m_FadeStartVolume, m_FadeTargetVolume, t);
            Volume = currentVolume;

            if (t < 1f)
            {
                return;
            }

            if (m_FadeState == FadeState.FadeOut)
            {
                StopPlayback();
                m_ShouldRecycle = true;
            }

            m_FadeState = FadeState.None;
        }

        /// <summary>
        /// 开始播放。
        /// </summary>
        public void Play()
        {
            m_ShouldRecycle = false;
            m_Source.Play();
            m_IsPause = false;
        }

        /// <summary>
        /// 立刻停止播放，并标记在管理器更新中回收。
        /// </summary>
        public void Stop()
        {
            m_FadeState = FadeState.None;
            StopPlayback();
            m_ShouldRecycle = true;
        }

        /// <summary>
        /// 请求停止：如果配置了“Stop 走淡出”，则执行淡出；否则立刻停止并回收。
        /// </summary>
        internal void RequestStop()
        {
            if (!m_UseFadeOnStop || m_DefaultFadeOutSeconds <= 0f || m_IsPause)
            {
                Stop();
                return;
            }

            StartFadeOut(m_DefaultFadeOutSeconds);
        }

        /// <summary>
        /// 按配置的默认淡入参数淡入并播放。
        /// </summary>
        internal void PlayWithFadeIn(float targetVolume)
        {
            float targetVolumeClamped = Mathf.Max(0f, targetVolume);
            m_ShouldRecycle = false;

            if (m_DefaultFadeInSeconds <= 0f)
            {
                Volume = targetVolumeClamped;
                Play();
                return;
            }

            StartFadeIn(m_DefaultFadeInSeconds, targetVolumeClamped);
        }

        /// <summary>
        /// 开始淡入。
        /// </summary>
        private void StartFadeIn(float fadeInSeconds, float targetVolume)
        {
            m_FadeState = FadeState.FadeIn;
            m_FadeDurationSeconds = Mathf.Max(0f, fadeInSeconds);
            m_FadeElapsedSeconds = 0f;
            m_FadeStartVolume = 0f;
            m_FadeTargetVolume = Mathf.Max(0f, targetVolume);
            Volume = 0f;
            Play();
        }

        /// <summary>
        /// 开始淡出（淡出完成后会自动停止并标记回收）。
        /// </summary>
        private void StartFadeOut(float fadeOutSeconds)
        {
            m_ShouldRecycle = false;
            m_FadeState = FadeState.FadeOut;
            m_FadeDurationSeconds = Mathf.Max(0f, fadeOutSeconds);
            m_FadeElapsedSeconds = 0f;
            m_FadeStartVolume = Volume;
            m_FadeTargetVolume = 0f;
        }

        /// <summary>
        /// 仅停止 AudioSource 播放，用于回收/淡出结束。
        /// </summary>
        private void StopPlayback()
        {
            if (m_Source)
            {
                m_Source.Stop();
            }
            m_IsPause = false;
        }

        /// <summary>
        /// 暂停播放。
        /// </summary>
        public void Pause()
        {
            m_FadeState = FadeState.None;
            m_Source.Pause();
            m_IsPause = true;
        }

    }
}

