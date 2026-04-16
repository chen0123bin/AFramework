using LWCore;
using UnityEngine;

namespace LWStep
{
    [StepActionInfo("销毁对象", Category = "对象控制", SummaryTemplate = "Destroy:{target}")]
    public class StepDestroyTargetAction : BaseTargeStepAction
    {
        /// <summary>
        /// 进入动作时销毁目标对象并立即完成。
        /// </summary>
        protected override void OnEnter()
        {
            DestroyTarget();
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
        /// 快速应用时销毁目标对象。
        /// </summary>
        protected override void OnApply()
        {
            DestroyTarget();
        }

        /// <summary>
        /// 按当前运行环境销毁目标对象。
        /// </summary>
        private void DestroyTarget()
        {
            if (m_Target == null)
            {
                LWDebug.LogWarning("步骤动作-销毁对象：未找到对象 " + m_TargetName);
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(m_Target);
                return;
            }

            Object.DestroyImmediate(m_Target);
        }
    }
}
