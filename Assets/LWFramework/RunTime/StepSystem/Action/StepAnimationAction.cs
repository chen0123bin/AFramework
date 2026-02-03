using LWCore;
using UnityEngine;

namespace LWStep
{
    public class StepAnimationAction : BaseTargeStepAction, IStepBaselineStateAction
    {
        private const float NORMALIZED_TIME_EPSILON = 0.0001f;

        [StepParam("state")]
        private string m_StateName;

        [StepParam("waitForComplete")]
        private bool m_WaitForComplete = true;

        [StepParam("applyToEndPose")]
        private bool m_ApplyToEndPose = true;

        [StepParam("reverse")]
        private bool m_IsReverse;

        [StepParam("manualControl")]
        private bool m_IsManualControl;

        private Animator m_Animator;
        private int m_PlayStateHash;

        private bool m_HasOriginalAnimatorSpeed;
        private float m_OriginalAnimatorSpeed;

        private bool m_HasBaseline;
        private string m_BaselineTargetName;
        private int m_BaselineLayer;
        private int m_BaselineStateHash;
        private float m_BaselineNormalizedTime;
        private float m_BaselineSpeed;

        /// <summary>
        /// 捕获动作基线状态（用于回退恢复）
        /// </summary>
        public void CaptureBaselineState()
        {
            m_BaselineTargetName = m_TargetName;

            GameObject target = FindTarget();
            Animator animator = FindAnimator(target);
            if (animator == null)
            {
                m_HasBaseline = false;
                return;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(m_BaselineLayer);
            m_BaselineStateHash = stateInfo.fullPathHash;
            m_BaselineNormalizedTime = stateInfo.normalizedTime;
            m_BaselineSpeed = animator.speed;
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
            Animator animator = FindAnimator(target);
            if (animator == null)
            {
                return;
            }

            if (m_BaselineStateHash == 0)
            {
                return;
            }

            animator.Play(m_BaselineStateHash, m_BaselineLayer, m_BaselineNormalizedTime);
            animator.speed = m_BaselineSpeed;
            animator.Update(0f);
        }

        /// <summary>
        /// 进入动作：播放动画或触发器
        /// </summary>
        protected override void OnEnter()
        {
            m_Animator = FindAnimator(m_Target);
            if (m_Animator == null)
            {
                LWDebug.LogWarning("步骤动作-动画播放：目标对象缺少 Animator");
                Finish();
                return;
            }

            m_OriginalAnimatorSpeed = m_Animator.speed;
            m_HasOriginalAnimatorSpeed = true;


            if (string.IsNullOrEmpty(m_StateName))
            {
                LWDebug.LogWarning("步骤动作-动画播放：state 为空");
                Finish();
                return;
            }

            m_PlayStateHash = Animator.StringToHash(m_StateName);
            float startNormalizedTime = m_IsReverse ? 1f : 0f;

            m_Animator.Play(m_PlayStateHash, 0, startNormalizedTime);
            SetAnimatorPlaybackSpeed();

            LWDebug.Log("步骤动作-动画播放：播放状态 " + m_StateName);

            if (!m_WaitForComplete && !m_IsManualControl)
            {
                Finish();
            }
        }

        /// <summary>
        /// 更新动作：等待动画播放完成
        /// </summary>
        protected override void OnUpdate()
        {
            if (IsFinished)
            {
                return;
            }
            if (!m_WaitForComplete)
            {
                if (!m_IsManualControl)
                {
                    Finish();
                    return;
                }
            }


            if (m_IsManualControl)
            {
                bool isPressed = Input.GetKey(KeyCode.Space);
                SetAnimatorPlaybackSpeed(isPressed);
                if (!isPressed)
                {
                    return;
                }
            }

            if (m_Animator.IsInTransition(0))
            {
                return;
            }

            AnimatorStateInfo stateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);
            bool isSameState = stateInfo.shortNameHash == m_PlayStateHash || stateInfo.fullPathHash == m_PlayStateHash;
            if (!isSameState)
            {
                return;
            }
            Debug.Log(stateInfo.normalizedTime + "  " + m_Animator.speed);
            if (!m_IsReverse)
            {
                if (stateInfo.normalizedTime >= (1f - NORMALIZED_TIME_EPSILON))
                {
                    Finish();
                }
                return;
            }
            else
            {
                if (stateInfo.normalizedTime <= (0f + NORMALIZED_TIME_EPSILON))
                {
                    Finish();
                }
            }


        }

        /// <summary>
        /// 退出动作：清理引用
        /// </summary>
        protected override void OnExit()
        {
            if (m_Animator != null && m_HasOriginalAnimatorSpeed)
            {
                m_Animator.speed = m_OriginalAnimatorSpeed;
            }
            m_Animator = null;
        }

        /// <summary>
        /// 快速应用：将动画应用到结束姿态或直接完成
        /// </summary>
        protected override void OnApply()
        {
            if (m_Animator == null)
            {
                m_Animator = FindAnimator(m_Target);
            }

            if (string.IsNullOrEmpty(m_StateName))
            {
                return;
            }

            int stateHash = Animator.StringToHash(m_StateName);
            float normalizedTime;
            if (!m_IsReverse)
            {
                normalizedTime = m_ApplyToEndPose ? 1f : 0f;
            }
            else
            {
                normalizedTime = m_ApplyToEndPose ? 0f : 1f;
            }
            m_Animator.Play(stateHash, 0, normalizedTime);
            m_Animator.Update(0f);
        }

        /// <summary>
        /// 设置Animator播放速度：支持倒播与手动控制（按住空格播放，松开停止）
        /// </summary>
        private void SetAnimatorPlaybackSpeed()
        {
            SetAnimatorPlaybackSpeed(!m_IsManualControl);
        }

        /// <summary>
        /// 设置Animator播放速度：支持倒播与手动控制（按住空格播放，松开停止）
        /// </summary>
        private void SetAnimatorPlaybackSpeed(bool isShouldPlay)
        {
            if (m_Animator == null)
            {
                return;
            }

            float absSpeed = Mathf.Abs(m_OriginalAnimatorSpeed);
            if (absSpeed < NORMALIZED_TIME_EPSILON)
            {
                absSpeed = 1f;
            }

            float playbackSpeed;
            if (!isShouldPlay)
            {
                playbackSpeed = 0f;
            }
            else
            {
                playbackSpeed = m_IsReverse ? -absSpeed : absSpeed;
            }

            m_Animator.SetFloat("Speed", playbackSpeed);
        }

        /// <summary>
        /// 查找Animator组件（优先自身，其次子节点）
        /// </summary>
        private Animator FindAnimator(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            Animator animator = target.GetComponent<Animator>();
            if (animator != null)
            {
                return animator;
            }
            return target.GetComponentInChildren<Animator>();
        }
    }
}
