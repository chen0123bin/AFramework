using DG.Tweening;
using LWCore;
using UnityEngine;

namespace LWStep
{
    public class StepRotateObjectAction : BaseTargeStepAction, IStepBaselineStateAction
    {
        [StepParam("x")]
        private float m_X;

        [StepParam("y")]
        private float m_Y;

        [StepParam("z")]
        private float m_Z;

        [StepParam("isLocal")]
        private bool m_IsLocal;

        [StepParam("rotateTime")]
        private float m_RotateTime;

        private Vector3 m_TargetRotation;
        private bool m_HasBaseline;
        private string m_BaselineTargetName;
        private GameObject m_BaselineTarget;
        private Vector3 m_BaselineLocalRotation;

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
            m_BaselineLocalRotation = transform.localEulerAngles;
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
                LWDebug.LogWarning("步骤动作-物体旋转：回退恢复失败，未找到对象 " + m_BaselineTargetName);
                return;
            }

            Transform transform = target.transform;
            transform.localEulerAngles = m_BaselineLocalRotation;
        }

        /// <summary>
        /// 进入动作：执行旋转并等待完成
        /// </summary>
        protected override void OnEnter()
        {
            if (m_Target == null)
            {
                Finish();
                return;
            }

            m_TargetRotation = new Vector3(m_X, m_Y, m_Z);
            ExecuteRotate();
        }

        /// <summary>
        /// 更新动作：等待动画完成
        /// </summary>
        protected override void OnUpdate()
        {
        }

        /// <summary>
        /// 退出动作：停止未完成的 Tween
        /// </summary>
        protected override void OnExit()
        {
            if (m_Target != null)
            {
                m_Target.transform.DOKill();
            }
        }

        /// <summary>
        /// 快速应用：直接写入目标旋转
        /// </summary>
        protected override void OnApply()
        {
            if (m_Target == null)
            {
                LWDebug.LogWarning("步骤动作-物体旋转：快速应用失败，未找到对象 " + m_TargetName);
                return;
            }

            if (m_IsLocal)
            {
                m_Target.transform.localEulerAngles = m_TargetRotation;
            }
            else
            {
                m_Target.transform.eulerAngles = m_TargetRotation;
            }
        }

        /// <summary>
        /// 执行旋转逻辑
        /// </summary>
        private void ExecuteRotate()
        {
            if (m_IsLocal)
            {
                m_Target.transform.DOLocalRotate(m_TargetRotation, m_RotateTime).OnComplete(() =>
                {
                    Finish();
                });
            }
            else
            {
                m_Target.transform.DORotate(m_TargetRotation, m_RotateTime).OnComplete(() =>
                {
                    Finish();
                });
            }

            LWDebug.Log("步骤动作-物体旋转：" + m_Target.name + " -> " + m_TargetRotation);
        }
    }
}
