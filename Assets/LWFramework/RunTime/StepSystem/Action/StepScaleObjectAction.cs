using DG.Tweening;
using LWCore;
using UnityEngine;

namespace LWStep
{
    public class StepScaleObjectAction : BaseTargeStepAction, IStepBaselineStateAction
    {
        [StepParam("x")]
        private float m_X;

        [StepParam("y")]
        private float m_Y;

        [StepParam("z")]
        private float m_Z;

        [StepParam("scaleTime")]
        private float m_ScaleTime;

        private Vector3 m_TargetScale;
        private bool m_HasBaseline;
        private string m_BaselineTargetName;
        private GameObject m_BaselineTarget;
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
                LWDebug.LogWarning("步骤动作-物体缩放：回退恢复失败，未找到对象 " + m_BaselineTargetName);
                return;
            }

            Transform transform = target.transform;
            transform.localScale = m_BaselineLocalScale;
        }

        /// <summary>
        /// 进入动作：执行缩放并等待完成
        /// </summary>
        protected override void OnEnter()
        {
            if (m_Target == null)
            {
                Finish();
                return;
            }

            m_TargetScale = new Vector3(m_X, m_Y, m_Z);
            ExecuteScale();
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
        /// 快速应用：直接写入目标缩放
        /// </summary>
        protected override void OnApply()
        {
            if (m_Target == null)
            {
                LWDebug.LogWarning("步骤动作-物体缩放：快速应用失败，未找到对象 " + m_TargetName);
                return;
            }

            m_Target.transform.localScale = m_TargetScale;
        }

        /// <summary>
        /// 执行缩放逻辑
        /// </summary>
        private void ExecuteScale()
        {
            m_Target.transform.DOScale(m_TargetScale, m_ScaleTime).OnComplete(() =>
            {
                Finish();
            });

            LWDebug.Log("步骤动作-物体缩放：" + m_Target.name + " -> " + m_TargetScale);
        }
    }
}
