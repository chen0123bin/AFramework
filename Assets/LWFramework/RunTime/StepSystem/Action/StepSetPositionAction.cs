using LWCore;
using UnityEngine;

namespace LWStep
{
    [StepActionInfo("设置位置", Category = "对象控制", SummaryTemplate = "Pos:{target}")]
    public class StepSetPositionAction : BaseTargeStepAction
    {
        [StepParam("x", label: "目标X", order: 1)]
        private float m_X;

        [StepParam("y", label: "目标Y", order: 2)]
        private float m_Y;

        [StepParam("z", label: "目标Z", order: 3)]
        private float m_Z;

        [StepParam("isLocal", label: "本地坐标", order: 4)]
        private bool m_IsLocal;

        /// <summary>
        /// 进入动作时写入目标位置并立即完成。
        /// </summary>
        protected override void OnEnter()
        {
            ApplyPosition();
            Finish();
        }

        /// <summary>
        /// 更新动作：该动作为瞬时动作，无需额外更新。
        /// </summary>
        protected override void OnUpdate()
        {
        }

        /// <summary>
        /// 退出动作：无需额外清理。
        /// </summary>
        protected override void OnExit()
        {
        }

        /// <summary>
        /// 快速应用时写入目标位置。
        /// </summary>
        protected override void OnApply()
        {
            ApplyPosition();
        }

        /// <summary>
        /// 将目标对象设置到指定位置。
        /// </summary>
        private void ApplyPosition()
        {
            if (m_Target == null)
            {
                LWDebug.LogWarning("步骤动作-设置位置：未找到对象 " + m_TargetName);
                return;
            }

            Vector3 targetPosition = new Vector3(m_X, m_Y, m_Z);
            if (m_IsLocal)
            {
                m_Target.transform.localPosition = targetPosition;
                return;
            }

            m_Target.transform.position = targetPosition;
        }
    }
}
