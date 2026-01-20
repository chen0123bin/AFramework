using System.Collections.Generic;
using System.Globalization;
using LWAudio;
using LWCore;
using UnityEngine;

namespace LWStep
{
    public class StepPlayAudioAction : BaseStepAction, IStepBaselineStateAction
    {
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

        protected override void OnEnter()
        {
            ExecutePlay();
            Finish();
        }

        protected override void OnUpdate()
        {
            if (!IsFinished)
            {
                Finish();
            }
        }

        protected override void OnExit()
        {
        }

        protected override void OnApply()
        {
            ExecutePlay();
        }

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

            float volume = GetFloatParam("volume", -1f);
            bool isLoop = GetBoolParam("isLoop", false);
            float fadeInSeconds = GetFloatParam("fadeInSeconds", 0f);
            GameObject target = GetTarget();
            if (target != null)
            {
                m_LastChannel = ManagerUtility.AudioMgr.Play(clip, target.transform, isLoop, fadeInSeconds, volume);
            }
            else
            {
                m_LastChannel = ManagerUtility.AudioMgr.Play(clip, isLoop, fadeInSeconds, volume);
            }
            m_LastClip = clip;
            LWDebug.Log("步骤动作-音频播放：" + clip.name);
        }

        private AudioClip LoadClip()
        {
            if (ManagerUtility.AssetsMgr == null || !ManagerUtility.AssetsMgr.IsInitialized)
            {
                LWDebug.LogWarning("步骤动作-音频播放：AssetsMgr 未初始化");
                return null;
            }

            string clipPath = GetStringParam("clip", string.Empty);
            if (string.IsNullOrEmpty(clipPath))
            {
                LWDebug.LogWarning("步骤动作-音频播放：clip 路径为空");
                return null;
            }

            AudioClip clip = ManagerUtility.AssetsMgr.LoadAsset<AudioClip>(clipPath);
            if (clip == null)
            {
                LWDebug.LogWarning("步骤动作-音频播放：加载 clip 失败 " + clipPath);
            }
            return clip;
        }

        private GameObject GetTarget()
        {
            string targetName = GetStringParam("target", string.Empty);
            if (string.IsNullOrEmpty(targetName))
            {
                return null;
            }

            GameObject target = GameObject.Find(targetName);
            if (target == null)
            {
                LWDebug.LogWarning("步骤动作-音频播放：未找到对象 " + targetName);
            }
            return target;
        }

        private string GetStringParam(string key, string defaultValue)
        {
            Dictionary<string, string> parameters = GetParameters();
            if (parameters == null)
            {
                return defaultValue;
            }
            string value;
            if (parameters.TryGetValue(key, out value))
            {
                return value;
            }
            return defaultValue;
        }

        private float GetFloatParam(string key, float defaultValue)
        {
            string value = GetStringParam(key, string.Empty);
            float result;
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }
            return defaultValue;
        }

        private bool GetBoolParam(string key, bool defaultValue)
        {
            string value = GetStringParam(key, string.Empty);
            bool result;
            if (bool.TryParse(value, out result))
            {
                return result;
            }
            return defaultValue;
        }
    }
}
