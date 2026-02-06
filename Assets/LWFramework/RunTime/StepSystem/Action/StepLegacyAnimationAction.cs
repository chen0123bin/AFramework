using LWCore;
using UnityEngine;

namespace LWStep
{
    public class StepLegacyAnimationAction : BaseTargeStepAction, IStepBaselineStateAction
    {
        private const float NORMALIZED_TIME_EPSILON = 0.0001f;

        [StepParam("state")]
        private string m_StateName;

        [StepParam("reverse")]
        private bool m_IsReverse;

        [StepParam("manualSpeedKey")]
        private string m_ManualSpeedKey;

        private Animation m_Animation;
        private AnimationState m_PlayState;

        private bool m_HasOriginalSpeed;
        private float m_OriginalSpeed;

        private bool m_HasBaseline;
        private string m_BaselineTargetName;
        private bool m_BaselineIsPlaying;
        private string m_BaselineStateName;
        private float m_BaselineTime;
        private float m_BaselineSpeed;

        /// <summary>
        /// 捕获动作基线状态（用于回退恢复）
        /// </summary>
        public void CaptureBaselineState()
        {
            m_BaselineTargetName = m_TargetName;

            GameObject target = FindTarget();
            Animation animation = FindAnimation(target);
            if (animation == null)
            {
                m_HasBaseline = false;
                return;
            }

            string playingStateName = GetFirstPlayingStateName(animation);
            AnimationState playingState = animation[playingStateName];
            m_BaselineIsPlaying = true;
            m_BaselineStateName = playingStateName;
            m_BaselineTime = playingState.time;
            m_BaselineSpeed = playingState.speed;
            m_HasBaseline = true;
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

            GameObject target = null;
            if (!string.IsNullOrEmpty(m_BaselineTargetName))
            {
                target = GameObject.Find(m_BaselineTargetName);
            }
            Animation animation = FindAnimation(target);

            if (!m_BaselineIsPlaying)
            {
                animation.Stop();
                return;
            }
            AnimationState state = animation[m_BaselineStateName];
            if (state == null)
            {
                animation.Stop();
                return;
            }

            animation.Play(m_BaselineStateName);
            state.time = m_BaselineTime;
            state.speed = m_BaselineSpeed;
            animation.Sample();
        }

        /// <summary>
        /// 进入动作：播放旧版 Animation 动画
        /// </summary>
        protected override void OnEnter()
        {
            m_Animation = FindAnimation(m_Target);
            if (m_Animation == null)
            {
                LWDebug.LogWarning("步骤动作-旧版动画播放：目标对象缺少 Animation");
                Finish();
                return;
            }

            if (string.IsNullOrEmpty(m_StateName))
            {
                LWDebug.LogWarning("步骤动作-旧版动画播放：state 为空");
                Finish();
                return;
            }

            m_PlayState = m_Animation[m_StateName];
            if (m_PlayState == null)
            {
                LWDebug.LogWarning("步骤动作-旧版动画播放：未找到动画片段 " + m_StateName);
                Finish();
                return;
            }

            m_OriginalSpeed = m_PlayState.speed;
            m_HasOriginalSpeed = true;

            float startTime = m_IsReverse ? Mathf.Max(m_PlayState.length - NORMALIZED_TIME_EPSILON, 0f) : 0f;
            m_Animation.Play(m_StateName);
            m_PlayState.time = startTime;

            ApplyPlaybackSpeed();
            m_Animation.Sample();

            LWDebug.Log("步骤动作-旧版动画播放：播放状态 " + m_StateName);

        }

        /// <summary>
        /// 是否启用手动速度控制
        /// </summary>
        private bool IsManualControl()
        {
            return !m_ManualSpeedKey.IsEmpty();
        }

        /// <summary>
        /// 更新动作：等待旧版 Animation 播放完成
        /// </summary>
        protected override void OnUpdate()
        {
            if (IsFinished)
            {
                return;
            }

            ApplyPlaybackSpeed();

            if (!m_IsReverse)
            {
                if (m_PlayState.normalizedTime >= (1f - NORMALIZED_TIME_EPSILON))
                {
                    Finish();
                }
                return;
            }

            if (m_PlayState.normalizedTime <= (0f + NORMALIZED_TIME_EPSILON))
            {
                Finish();
            }
        }

        /// <summary>
        /// 退出动作：恢复旧版 Animation 状态
        /// </summary>
        protected override void OnExit()
        {
            if (m_PlayState != null && m_HasOriginalSpeed)
            {
                m_PlayState.speed = m_OriginalSpeed;
            }

            m_Animation = null;
            m_PlayState = null;
        }

        /// <summary>
        /// 快速应用：将旧版 Animation 应用到目标姿态
        /// </summary>
        protected override void OnApply()
        {
            if (m_Animation == null)
            {
                m_Animation = FindAnimation(m_Target);
            }
            if (m_Animation == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(m_StateName))
            {
                return;
            }

            AnimationState state = m_Animation[m_StateName];
            if (state == null)
            {
                return;
            }

            m_Animation.Play(m_StateName);
            state.normalizedTime = !m_IsReverse ? 1f : 0f;
            state.speed = 0f;
            m_Animation.Sample();
        }

        /// <summary>
        /// 应用旧版 Animation 播放速度：支持反向与上下文手动速度
        /// </summary>
        private void ApplyPlaybackSpeed()
        {

            float playbackSpeed;
            if (IsManualControl())
            {
                playbackSpeed = GetContext().GetValue<float>(m_ManualSpeedKey);
            }
            else
            {
                float absSpeed = Mathf.Abs(m_OriginalSpeed);
                if (absSpeed < NORMALIZED_TIME_EPSILON)
                {
                    absSpeed = 1f;
                }
                playbackSpeed = m_IsReverse ? -absSpeed : absSpeed;
            }

            if (Mathf.Abs(m_PlayState.speed - playbackSpeed) > NORMALIZED_TIME_EPSILON)
            {
                m_PlayState.speed = playbackSpeed;
            }

            if (!m_Animation.IsPlaying(m_StateName))
            {
                m_Animation.Play(m_StateName);
            }
        }

        /// <summary>
        /// 查找 Animation 组件（优先自身，其次子节点）
        /// </summary>
        private Animation FindAnimation(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            Animation animation = target.GetComponent<Animation>();
            if (animation != null)
            {
                return animation;
            }

            return target.GetComponentInChildren<Animation>();
        }

        /// <summary>
        /// 获取当前第一个正在播放的 AnimationState 名称
        /// </summary>
        private string GetFirstPlayingStateName(Animation animation)
        {
            if (animation == null)
            {
                return string.Empty;
            }

            foreach (AnimationState state in animation)
            {
                if (state == null)
                {
                    continue;
                }

                if (animation.IsPlaying(state.name))
                {
                    return state.name;
                }
            }

            return string.Empty;
        }
    }
}
