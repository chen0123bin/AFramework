using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using LWCore;
using UnityEngine;

namespace LWStep
{
    public class StepMoveObjectAction : BaseTargeStepAction, IStepBaselineStateAction
    {

        [StepParam("x")]
        private float m_X;

        [StepParam("y")]
        private float m_Y;

        [StepParam("z")]
        private float m_Z;

        [StepParam("isLocal")]
        private bool m_IsLocal;
        [StepParam("moveTime")]
        private float m_MoveTime;

        private Vector3 targetPosition;
        private bool m_HasBaseline;
        private string m_BaselineTargetName;
        private GameObject m_BaselineTarget;
        private Vector3 m_BaselineLocalPosition;


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

        }

        /// <summary>
        /// 进入动作：执行移动并结束
        /// </summary>
        protected override void OnEnter()
        {
            targetPosition = new Vector3(m_X, m_Y, m_Z);
            ExecuteMove();
            //Finish();
        }

        /// <summary>
        /// 更新动作：确保完成
        /// </summary>
        protected override void OnUpdate()
        {
            if (!IsFinished)
            {
                //Finish();
            }
        }

        /// <summary>
        /// 退出动作
        /// </summary>
        protected override void OnExit()
        {
            m_Target.transform.DOKill();
        }

        /// <summary>
        /// 快速应用：执行移动
        /// </summary>
        protected override void OnApply()
        {
            if (m_Target == null)
            {
                LWDebug.LogWarning("步骤动作-物体移动：快速应用失败，未找到对象 " + m_TargetName);
                return;
            }
            if (m_IsLocal)
            {
                m_Target.transform.localPosition = targetPosition;
            }
            else
            {
                m_Target.transform.position = targetPosition;
            }
        }

        /// <summary>
        /// 执行移动逻辑
        /// </summary>
        private void ExecuteMove()
        {


            if (m_IsLocal)
            {
                m_Target.transform.DOLocalMove(targetPosition, m_MoveTime).OnComplete(() =>
                {
                    Finish();
                });
            }
            else
            {
                m_Target.transform.DOMove(targetPosition, m_MoveTime).OnComplete(() =>
                {
                    Finish();
                });
            }
            LWDebug.Log("步骤动作-物体移动：" + m_Target.name + " -> " + targetPosition);
        }


    }
}
