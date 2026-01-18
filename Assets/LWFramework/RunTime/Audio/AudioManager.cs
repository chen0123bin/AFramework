using LWCore;
using System.Collections.Generic;
using UnityEngine;
namespace LWAudio
{
    public struct Audio3DSettings
    {
        public float SpatialBlend;
        public AudioRolloffMode RolloffMode;
        public float MinDistance;
        public float MaxDistance;
        public float DopplerLevel;
        public float Spread;

        public static Audio3DSettings Default3D
        {
            get
            {
                Audio3DSettings settings = new Audio3DSettings();
                settings.SpatialBlend = 1f;
                settings.RolloffMode = AudioRolloffMode.Logarithmic;
                settings.MinDistance = 1f;
                settings.MaxDistance = 50f;
                settings.DopplerLevel = 0f;
                settings.Spread = 0f;
                return settings;
            }
        }
    }

    /// <summary>
    /// 音频管理器
    /// </summary>
    public class AudioManager : IAudioManager, IManager
    {
        private float m_AudioVolume = 1f;

        private GameObject m_ManagerEntity;
        private GameObjectPool<AudioChannel> m_Pool;
        private readonly List<AudioChannel> m_ActiveChannels = new List<AudioChannel>(16);

        /// <summary>
        /// 全局音量（作用于当前已激活的所有通道）。
        /// </summary>
        public float AudioVolume
        {
            set
            {
                m_AudioVolume = value;
                for (int i = 0; i < m_ActiveChannels.Count; i++)
                {
                    m_ActiveChannels[i].Volume = m_AudioVolume;
                }
            }
        }

        /// <summary>
        /// 初始化音频管理器与通道对象池。
        /// </summary>
        public void Init()
        {
            m_ManagerEntity = new GameObject("AudioManager");
            GameObject audioChannelTemp = new GameObject("AudioChannel");
            audioChannelTemp.transform.parent = m_ManagerEntity.transform;

            m_Pool = new GameObjectPool<AudioChannel>(10, audioChannelTemp);
            GameObject.DontDestroyOnLoad(m_ManagerEntity);
        }

        /// <summary>
        /// 每帧更新：回收已经播放结束且不处于暂停的通道。
        /// </summary>
        public void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = m_ActiveChannels.Count - 1; i >= 0; i--)
            {
                AudioChannel channel = m_ActiveChannels[i];
                if (channel == null)
                {
                    RemoveChannelAt(i);
                    continue;
                }

                if (!channel.IsValid())
                {
                    RemoveChannelAt(i);
                    continue;
                }

                if (channel.ShouldRecycle)
                {
                    RemoveChannelAt(i);
                    continue;
                }

                if (channel.IsPause)
                {
                    continue;
                }

                channel.Tick(deltaTime);
                if (channel.ShouldRecycle)
                {
                    RemoveChannelAt(i);
                    continue;
                }

                if (channel.IsPlay)
                {
                    continue;
                }

                RemoveChannelAt(i);
            }
        }

        /// <summary>
        /// 创建或复用一个通道，并加入到激活列表中。
        /// </summary>
        private AudioChannel CreateChannel()
        {
            AudioChannel channel = m_Pool.Spawn();
            if (channel == null)
            {
                return null;
            }

            channel.Volume = m_AudioVolume;
            m_ActiveChannels.Add(channel);
            return channel;
        }

        /// <summary>
        /// 回收指定通道（会从激活列表移除，并归还到对象池）。
        /// </summary>
        private void RemoveChannel(AudioChannel channel)
        {
            int index = m_ActiveChannels.IndexOf(channel);
            if (index >= 0)
            {
                RemoveChannelAt(index);
            }
        }

        /// <summary>
        /// 通过索引回收通道，使用 SwapBack 避免 O(n) 的中间元素搬移。
        /// </summary>
        private void RemoveChannelAt(int index)
        {
            AudioChannel channel = m_ActiveChannels[index];

            int lastIndex = m_ActiveChannels.Count - 1;
            if (index != lastIndex)
            {
                m_ActiveChannels[index] = m_ActiveChannels[lastIndex];
            }
            m_ActiveChannels.RemoveAt(lastIndex);

            if (channel != null && channel.IsValid() && !m_Pool.IsInPool(channel))
            {
                channel.Stop();
                m_Pool.Unspawn(channel);
            }
        }

        /// <summary>
        /// 播放 2D 音效。
        /// </summary>
        public AudioChannel Play(AudioClip clip, bool loop = false, float fadeInSeconds = 0f, float volume = -1)
        {
            AudioChannel channel = CreateChannel();
            if (channel == null)
            {
                return null;
            }

            channel.Set2D();
            channel.AudioClip = clip;
            channel.Loop = loop;

            float targetVolume = volume < 0f ? m_AudioVolume : volume;
            channel.ConfigureFade(fadeInSeconds, fadeInSeconds, fadeInSeconds > 0f);
            channel.PlayWithFadeIn(targetVolume);
            return channel;
        }
        /// <param name="clip"></param>
        /// <param name="emitter"></param>
        /// <returns></returns>
        public AudioChannel Play(AudioClip clip, Transform emitter, bool loop = false, float fadeInSeconds = 0f, float volume = -1, Audio3DSettings? settings = null)
        {
            AudioChannel channel = CreateChannel();
            if (channel == null)
            {
                return null;
            }

            channel.AudioClip = clip;

            Audio3DSettings audio3DSettings = settings.HasValue ? settings.Value : Audio3DSettings.Default3D;
            channel.Set3D(audio3DSettings);
            if (emitter != null)
            {
                channel.Parent = emitter;
            }
            channel.Loop = loop;

            float targetVolume = volume < 0f ? m_AudioVolume : volume;
            channel.ConfigureFade(fadeInSeconds, fadeInSeconds, fadeInSeconds > 0f);
            channel.PlayWithFadeIn(targetVolume);
            return channel;
        }


        /// <param name="clip"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public AudioChannel Play(AudioClip clip, Vector3 point, bool loop = false, float fadeInSeconds = 0f, float volume = -1, Audio3DSettings? settings = null)
        {
            AudioChannel channel = CreateChannel();
            if (channel == null)
            {
                return null;
            }

            channel.AudioClip = clip;

            Audio3DSettings audio3DSettings = settings.HasValue ? settings.Value : Audio3DSettings.Default3D;
            channel.Set3D(audio3DSettings);
            channel.Posi = point;
            channel.Loop = loop;

            float targetVolume = volume < 0f ? m_AudioVolume : volume;
            channel.ConfigureFade(fadeInSeconds, fadeInSeconds, fadeInSeconds > 0f);
            channel.PlayWithFadeIn(targetVolume);
            return channel;
        }

        /// <summary>
        /// 立刻停止并回收指定通道（不走淡出）。
        /// </summary>
        public void StopImmediate(AudioChannel audioChannel)
        {
            if (audioChannel != null)
            {
                RemoveChannel(audioChannel);
            }
        }

        /// <summary>
        /// 停止并回收指定通道（如果 Play 配置了淡入，则 Stop 会走同秒数淡出）。
        /// </summary>
        public void Stop(AudioChannel audioChannel)
        {
            if (audioChannel == null)
            {
                return;
            }

            audioChannel.RequestStop();
            if (audioChannel.ShouldRecycle)
            {
                RemoveChannel(audioChannel);
            }
        }

        /// <summary>
        /// 停止并回收所有通道（如果通道配置了淡出，会在淡出完成后回收）。
        /// </summary>
        public void StopAll()
        {
            for (int i = m_ActiveChannels.Count - 1; i >= 0; i--)
            {
                Stop(m_ActiveChannels[i]);
            }
        }

        /// <summary>
        /// 暂停指定通道。
        /// </summary>
        public void Pause(AudioChannel audioChannel)
        {
            if (audioChannel != null)
            {
                audioChannel.Pause();
            }
        }

        /// <summary>
        /// 暂停所有通道。
        /// </summary>
        public void PauseAll()
        {
            for (int i = 0; i < m_ActiveChannels.Count; i++)
            {
                Pause(m_ActiveChannels[i]);
            }
        }

        /// <summary>
        /// 恢复指定通道。
        /// </summary>
        public void Resume(AudioChannel audioChannel)
        {
            if (audioChannel != null)
            {
                audioChannel.Play();
            }
        }

        /// <summary>
        /// 恢复所有通道。
        /// </summary>
        public void ResumeAll()
        {
            for (int i = 0; i < m_ActiveChannels.Count; i++)
            {
                Resume(m_ActiveChannels[i]);
            }
        }
    }

}
