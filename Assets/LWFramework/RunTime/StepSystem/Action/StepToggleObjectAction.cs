using LWCore;
using UnityEngine;

namespace LWStep
{
    public class StepToggleObjectAction : BaseTargeStepAction, IStepBaselineStateAction
    {

        [StepParam("isActive")]
        private bool m_IsActive = true;

        private bool m_HasBaseline;
        private string m_BaselineTargetName;
        private GameObject m_BaselineTarget;
        private bool m_BaselineHasRenderer;
        private bool m_BaselineRendererEnabled;

        /// <summary>
        /// 捕获动作基线状态（用于回退恢复）
        /// </summary>
        public void CaptureBaselineState()
        {
            m_BaselineTargetName = m_TargetName;
            m_Target = FindTarget();
            if (m_Target == null)
            {
                m_HasBaseline = false;
                m_BaselineTarget = null;
                return;
            }

            m_BaselineTarget = m_Target;
            Renderer renderer = m_Target.GetComponent<Renderer>();
            m_BaselineHasRenderer = renderer != null;
            m_BaselineRendererEnabled = renderer != null && renderer.enabled;
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

            GameObject target = m_BaselineTarget != null ? m_BaselineTarget : GameObject.Find(m_BaselineTargetName);
            if (target == null)
            {
                LWDebug.LogWarning("步骤动作-物体显隐：回退恢复失败，未找到对象 " + m_BaselineTargetName);
                return;
            }

            if (!m_BaselineHasRenderer)
            {
                return;
            }

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                LWDebug.LogWarning("步骤动作-物体显隐：回退恢复失败，对象缺少 Renderer " + target.name);
                return;
            }

            renderer.enabled = m_BaselineRendererEnabled;
        }

        /// <summary>
        /// 进入动作：执行显隐并结束
        /// </summary>
        protected override void OnEnter()
        {
            ExecuteToggle();
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
        }

        /// <summary>
        /// 快速应用：执行显隐
        /// </summary>
        protected override void OnApply()
        {
            ExecuteToggle();
        }

        /// <summary>
        /// 执行显隐逻辑
        /// </summary>
        private void ExecuteToggle()
        {
            if (m_Target == null)
            {
                return;
            }

            Renderer renderer = m_Target.GetComponent<Renderer>();
            if (renderer == null)
            {
                LWDebug.LogWarning("步骤动作-物体显隐：对象缺少 Renderer " + m_Target.name);
                return;
            }
            renderer.enabled = m_IsActive;
            LWDebug.Log("步骤动作-物体显隐：" + m_Target.name + " -> " + m_IsActive);
            GetContext().SetValue(m_TargetName + "-" + "isActive", m_IsActive);
        }


    }
}
