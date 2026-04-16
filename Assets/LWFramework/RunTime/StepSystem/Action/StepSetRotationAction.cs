using LWCore;
using UnityEngine;

namespace LWStep
{
    [StepActionInfo("设置旋转", Category = "对象控制", SummaryTemplate = "Rot:{target}")]
    public class StepSetRotationAction : BaseTargeStepAction
    {
        [StepParam("x", label: "目标X", order: 1)]
        private float m_X;

        [StepParam("y", label: "目标Y", order: 2)]
        private float m_Y;

        [StepParam("z", label: "目标Z", order: 3)]
        private float m_Z;

        [StepParam("isLocal", label: "本地旋转", order: 4)]
        private bool m_IsLocal;

        /// <summary>
        /// 进入动作时写入目标旋转并立即完成。
        /// </summary>
        protected override void OnEnter()
        {
            ApplyRotation();
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
        /// 快速应用时写入目标旋转。
        /// </summary>
        protected override void OnApply()
        {
            ApplyRotation();
        }

        /// <summary>
        /// 将目标对象设置到指定欧拉角旋转。
        /// </summary>
        private void ApplyRotation()
        {
            if (m_Target == null)
            {
                LWDebug.LogWarning("步骤动作-设置旋转：未找到对象 " + m_TargetName);
                return;
            }

            Quaternion rotation = Quaternion.Euler(m_X, m_Y, m_Z);
            if (m_IsLocal)
            {
                m_Target.transform.localRotation = rotation;
                return;
            }

            m_Target.transform.rotation = rotation;
        }
    }
}
