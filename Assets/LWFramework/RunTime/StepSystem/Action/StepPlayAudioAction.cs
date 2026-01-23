using LWAudio;
using LWCore;
using UnityEngine;

namespace LWStep
{
    public class StepPlayAudioAction : BaseTargeStepAction, IStepBaselineStateAction
    {
        [StepParam("clip")]
        private string m_ClipPath;

        [StepParam("volume")]
        private float m_Volume = -1f;

        [StepParam("isLoop")]
        private bool m_IsLoop;

        [StepParam("fadeInSeconds")]
        private float m_FadeInSeconds;

        private bool m_HasBaseline;
        private AudioChannel m_LastChannel;
        private AudioClip m_LastClip;

        /// <summary>
        /// 捕获动作基线状态（用于回退恢复）
        /// </summary>
        public void CaptureBaselineState()
        {
            m_HasBaseline = true;
            m_LastChannel = null;
            m_LastClip = null;
        }

        /// <summary>
        /// 恢复动作基线状态（用于回退恢复）
        /// </summary>
        public void RestoreBaselineState()
        {
            if (!m_HasBaseline)
            {
                return;
            }

            if (m_LastChannel == null)
            {
                return;
            }

            if (ManagerUtility.AudioMgr == null)
            {
                m_LastChannel = null;
                m_LastClip = null;
                return;
            }

            bool shouldStop = true;
            if (m_LastClip != null && m_LastChannel.IsValid())
            {
                if (m_LastChannel.AudioClip != m_LastClip)
                {
                    shouldStop = false;
                }
            }
            if (shouldStop)
            {
                ManagerUtility.AudioMgr.StopImmediate(m_LastChannel);
            }

            m_LastChannel = null;
            m_LastClip = null;
        }

        /// <summary>
        /// 进入动作：执行播放并结束
        /// </summary>
        protected override void OnEnter()
        {
            ExecutePlay();
            // 如果是循环播放，且通道正在播放，直接结束
            if (m_LastChannel != null && m_LastChannel.IsPlay && m_IsLoop)
            {
                Finish();
            }
        }

        /// <summary>
        /// 更新动作：确保完成
        /// </summary>
        protected override void OnUpdate()
        {
            if (!IsFinished && m_LastChannel != null && !m_LastChannel.IsPlay)
            {
                Finish();
            }
        }

        /// <summary>
        /// 退出动作
        /// </summary>
        protected override void OnExit()
        {
            ManagerUtility.AssetsMgr.Release(m_ClipPath);
            Debug.Log("步骤动作-音频播放：退出!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        }

        /// <summary>
        /// 快速应用：执行播放
        /// </summary>
        protected override void OnApply()
        {
            //ExecutePlay();
            // 如果不是循环播放，且通道正在播放，直接结束
            if (m_LastChannel != null && m_LastChannel.IsPlay && !m_IsLoop)
            {
                ManagerUtility.AudioMgr.StopImmediate(m_LastChannel);
            }
        }

        /// <summary>
        /// 执行播放逻辑
        /// </summary>
        private void ExecutePlay()
        {
            AudioClip clip = LoadClip();
            if (clip == null)
            {
                return;
            }
            if (m_LastChannel != null && m_LastChannel.IsPlay)
            {
                return;
            }

            m_LastChannel = ManagerUtility.AudioMgr.Play(clip, m_Target?.transform, m_IsLoop, m_FadeInSeconds, m_Volume);
            m_LastClip = clip;
            LWDebug.Log("步骤动作-音频播放：" + clip.name);
        }

        /// <summary>
        /// 加载音频资源
        /// </summary>
        private AudioClip LoadClip()
        {
            AudioClip clip = ManagerUtility.AssetsMgr.LoadAsset<AudioClip>(m_ClipPath);
            if (clip == null)
            {
                LWDebug.LogWarning("步骤动作-音频播放：加载 clip 失败 " + m_ClipPath);
            }
            return clip;
        }


    }
}
