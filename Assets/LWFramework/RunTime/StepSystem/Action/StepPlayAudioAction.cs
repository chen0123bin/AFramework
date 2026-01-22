using LWAudio;
using LWCore;
using UnityEngine;

namespace LWStep
{
    public class StepPlayAudioAction : BaseStepAction, IStepBaselineStateAction
    {
        [StepParam("clip")]
        private string m_ClipPath;

        [StepParam("target")]
        private string m_TargetName;

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
            Finish();
        }

        /// <summary>
        /// 更新动作：确保完成
        /// </summary>
        protected override void OnUpdate()
        {
            if (!IsFinished)
            {
                Finish();
            }
        }

        /// <summary>
        /// 退出动作
        /// </summary>
        protected override void OnExit()
        {
            if (m_LastChannel != null && m_LastChannel.IsPlay)
            {
                //ManagerUtility.AudioMgr.StopImmediate(m_LastChannel);
            }
        }

        /// <summary>
        /// 快速应用：执行播放
        /// </summary>
        protected override void OnApply()
        {
            ExecutePlay();
        }

        /// <summary>
        /// 执行播放逻辑
        /// </summary>
        private void ExecutePlay()
        {
            if (ManagerUtility.AudioMgr == null)
            {
                LWDebug.LogWarning("步骤动作-音频播放：AudioMgr 未初始化");
                return;
            }

            AudioClip clip = LoadClip();
            if (clip == null)
            {
                return;
            }

            GameObject target = GetTarget();
            if (target != null)
            {
                m_LastChannel = ManagerUtility.AudioMgr.Play(clip, target.transform, m_IsLoop, m_FadeInSeconds, m_Volume);
            }
            else
            {
                m_LastChannel = ManagerUtility.AudioMgr.Play(clip, m_IsLoop, m_FadeInSeconds, m_Volume);
            }
            m_LastClip = clip;
            LWDebug.Log("步骤动作-音频播放：" + clip.name);
        }

        /// <summary>
        /// 加载音频资源
        /// </summary>
        private AudioClip LoadClip()
        {
            if (ManagerUtility.AssetsMgr == null || !ManagerUtility.AssetsMgr.IsInitialized)
            {
                LWDebug.LogWarning("步骤动作-音频播放：AssetsMgr 未初始化");
                return null;
            }

            if (string.IsNullOrEmpty(m_ClipPath))
            {
                LWDebug.LogWarning("步骤动作-音频播放：clip 路径为空");
                return null;
            }

            AudioClip clip = ManagerUtility.AssetsMgr.LoadAsset<AudioClip>(m_ClipPath);
            if (clip == null)
            {
                LWDebug.LogWarning("步骤动作-音频播放：加载 clip 失败 " + m_ClipPath);
            }
            return clip;
        }

        /// <summary>
        /// 获取可选的目标对象（用于3D音频跟随）
        /// </summary>
        private GameObject GetTarget()
        {
            if (string.IsNullOrEmpty(m_TargetName))
            {
                return null;
            }

            GameObject target = GameObject.Find(m_TargetName);
            if (target == null)
            {
                LWDebug.LogWarning("步骤动作-音频播放：未找到对象 " + m_TargetName);
            }
            return target;
        }
    }
}
