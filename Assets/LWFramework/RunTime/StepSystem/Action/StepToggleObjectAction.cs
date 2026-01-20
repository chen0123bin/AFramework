using System.Collections.Generic;
using LWCore;
using UnityEngine;

namespace LWStep
{
    public class StepToggleObjectAction : BaseStepAction, IStepBaselineStateAction
    {
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
            m_BaselineTargetName = GetStringParam("target", string.Empty);
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

        protected override void OnEnter()
        {
            ExecuteToggle();
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
            ExecuteToggle();
        }

        private void ExecuteToggle()
        {
            GameObject target = FindTarget();
            if (target == null)
            {
                return;
            }

            bool isActive = GetBoolParam("isActive", true);
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                LWDebug.LogWarning("步骤动作-物体显隐：对象缺少 Renderer " + target.name);
                return;
            }
            renderer.enabled = isActive;
            LWDebug.Log("步骤动作-物体显隐：" + target.name + " -> " + isActive);
        }

        private GameObject FindTarget()
        {
            string targetName = GetStringParam("target", string.Empty);
            if (string.IsNullOrEmpty(targetName))
            {
                LWDebug.LogWarning("步骤动作-物体显隐：target 为空");
                return null;
            }

            GameObject target = GameObject.Find(targetName);
            if (target == null)
            {
                LWDebug.LogWarning("步骤动作-物体显隐：未找到对象 " + targetName);
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
