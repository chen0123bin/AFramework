using System.Collections.Generic;
using System.Globalization;
using LWCore;
using UnityEngine;

namespace LWStep
{
    public class StepMoveObjectAction : BaseStepAction, IStepBaselineStateAction
    {
        [StepParam("target")]
        private string m_TargetName;

        [StepParam("x")]
        private float m_X;

        [StepParam("y")]
        private float m_Y;

        [StepParam("z")]
        private float m_Z;

        [StepParam("isLocal")]
        private bool m_IsLocal;

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
            m_BaselineTargetName = m_TargetName;
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

        /// <summary>
        /// 进入动作：执行移动并结束
        /// </summary>
        protected override void OnEnter()
        {
            ExecuteMove();
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
        /// 快速应用：执行移动
        /// </summary>
        protected override void OnApply()
        {
            ExecuteMove();
        }

        /// <summary>
        /// 执行移动逻辑
        /// </summary>
        private void ExecuteMove()
        {
            GameObject target = FindTarget();
            if (target == null)
            {
                return;
            }

            Vector3 position = new Vector3(m_X, m_Y, m_Z);
            if (m_IsLocal)
            {
                target.transform.localPosition = position;
            }
            else
            {
                target.transform.position = position;
            }
            LWDebug.Log("步骤动作-物体移动：" + target.name + " -> " + position);
        }

        /// <summary>
        /// 查找目标对象
        /// </summary>
        private GameObject FindTarget()
        {
            if (string.IsNullOrEmpty(m_TargetName))
            {
                LWDebug.LogWarning("步骤动作-物体移动：target 为空");
                return null;
            }

            GameObject target = GameObject.Find(m_TargetName);
            if (target == null)
            {
                LWDebug.LogWarning("步骤动作-物体移动：未找到对象 " + m_TargetName);
            }
            return target;
        }
    }
}
