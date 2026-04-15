using LWCore;
using UnityEngine;

namespace LWStep
{
    [StepActionInfo("设置激活状态", Category = "对象控制", SummaryTemplate = "Active:{target}")]
    public class StepSetActiveAction : BaseTargeStepAction
    {
        [StepParam("active", label: "是否激活", order: 1)]
        private bool m_Active = true;

        /// <summary>
        /// 进入动作时写入对象激活状态并立即完成。
        /// </summary>
        protected override void OnEnter()
        {
            ApplyActiveState();
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
        /// 快速应用时写入对象激活状态。
        /// </summary>
        protected override void OnApply()
        {
            ApplyActiveState();
        }

        /// <summary>
        /// 将目标对象设置为指定激活状态。
        /// </summary>
        private void ApplyActiveState()
        {
            if (m_Target == null)
            {
                LWDebug.LogWarning("步骤动作-设置激活状态：未找到对象 " + m_TargetName);
                return;
            }

            m_Target.SetActive(m_Active);
        }
    }
}
