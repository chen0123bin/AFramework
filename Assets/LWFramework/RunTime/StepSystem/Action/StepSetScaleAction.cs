using LWCore;
using UnityEngine;

namespace LWStep
{
    [StepActionInfo("设置缩放", Category = "对象控制", SummaryTemplate = "Scale:{target}")]
    public class StepSetScaleAction : BaseTargeStepAction
    {
        [StepParam("x", label: "缩放X", order: 1)]
        private float m_X = 1f;

        [StepParam("y", label: "缩放Y", order: 2)]
        private float m_Y = 1f;

        [StepParam("z", label: "缩放Z", order: 3)]
        private float m_Z = 1f;

        /// <summary>
        /// 进入动作时写入目标缩放并立即完成。
        /// </summary>
        protected override void OnEnter()
        {
            ApplyScale();
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
        /// 快速应用时写入目标缩放。
        /// </summary>
        protected override void OnApply()
        {
            ApplyScale();
        }

        /// <summary>
        /// 将目标对象设置到指定本地缩放。
        /// </summary>
        private void ApplyScale()
        {
            if (m_Target == null)
            {
                LWDebug.LogWarning("步骤动作-设置缩放：未找到对象 " + m_TargetName);
                return;
            }

            m_Target.transform.localScale = new Vector3(m_X, m_Y, m_Z);
        }
    }
}
