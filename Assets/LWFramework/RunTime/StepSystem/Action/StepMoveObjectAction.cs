using System.Collections.Generic;
using System.Globalization;
using LWCore;
using UnityEngine;

namespace LWStep
{
    public class StepMoveObjectAction : BaseStepAction, IStepBaselineStateAction
    {
        private bool m_HasBaseline;
        private string m_BaselineTargetName;
        private GameObject m_BaselineTarget;
        private Vector3 m_BaselineLocalPosition;
        private Quaternion m_BaselineLocalRotation;
        private Vector3 m_BaselineLocalScale;

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
            Transform transform = target.transform;
            m_BaselineLocalPosition = transform.localPosition;
            m_BaselineLocalRotation = transform.localRotation;
            m_BaselineLocalScale = transform.localScale;
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
                LWDebug.LogWarning("步骤动作-物体移动：回退恢复失败，未找到对象 " + m_BaselineTargetName);
                return;
            }

            Transform transform = target.transform;
            transform.localPosition = m_BaselineLocalPosition;
            transform.localRotation = m_BaselineLocalRotation;
            transform.localScale = m_BaselineLocalScale;
        }

        protected override void OnEnter()
        {
            ExecuteMove();
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
            ExecuteMove();
        }

        private void ExecuteMove()
        {
            GameObject target = FindTarget();
            if (target == null)
            {
                return;
            }

            float x = GetFloatParam("x", 0f);
            float y = GetFloatParam("y", 0f);
            float z = GetFloatParam("z", 0f);
            bool isLocal = GetBoolParam("isLocal", false);
            Vector3 position = new Vector3(x, y, z);
            if (isLocal)
            {
                target.transform.localPosition = position;
            }
            else
            {
                target.transform.position = position;
            }
            LWDebug.Log("步骤动作-物体移动：" + target.name + " -> " + position);
        }

        private GameObject FindTarget()
        {
            string targetName = GetStringParam("target", string.Empty);
            if (string.IsNullOrEmpty(targetName))
            {
                LWDebug.LogWarning("步骤动作-物体移动：target 为空");
                return null;
            }

            GameObject target = GameObject.Find(targetName);
            if (target == null)
            {
                LWDebug.LogWarning("步骤动作-物体移动：未找到对象 " + targetName);
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

        private float GetFloatParam(string key, float defaultValue)
        {
            string value = GetStringParam(key, string.Empty);
            float result;
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                return result;
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
