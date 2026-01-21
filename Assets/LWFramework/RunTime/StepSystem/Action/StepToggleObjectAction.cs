using LWCore;
using UnityEngine;

namespace LWStep
{
    public class StepToggleObjectAction : BaseStepAction, IStepBaselineStateAction
    {
        [StepParam("target")]
        private string m_TargetName;

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
            GameObject target = FindTarget();
            if (target == null)
            {
                m_HasBaseline = false;
                m_BaselineTarget = null;
                return;
            }

            m_BaselineTarget = target;
            Renderer renderer = target.GetComponent<Renderer>();
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
            GameObject target = FindTarget();
            if (target == null)
            {
                return;
            }

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                LWDebug.LogWarning("步骤动作-物体显隐：对象缺少 Renderer " + target.name);
                return;
            }
            renderer.enabled = m_IsActive;
            LWDebug.Log("步骤动作-物体显隐：" + target.name + " -> " + m_IsActive);
        }

        /// <summary>
        /// 查找目标对象
        /// </summary>
        private GameObject FindTarget()
        {
            if (string.IsNullOrEmpty(m_TargetName))
            {
                LWDebug.LogWarning("步骤动作-物体显隐：target 为空");
                return null;
            }

            GameObject target = GameObject.Find(m_TargetName);
            if (target == null)
            {
                LWDebug.LogWarning("步骤动作-物体显隐：未找到对象 " + m_TargetName);
            }
            return target;
        }
    }
}
