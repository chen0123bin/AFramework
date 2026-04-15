using LWCore;
using UnityEngine;

namespace LWStep
{
    [StepActionInfo("设置父节点", Category = "对象控制", SummaryTemplate = "Parent:{target}")]
    public class StepSetParentAction : BaseTargeStepAction
    {
        [StepParam("parent", label: "父节点", order: 1)]
        private string m_ParentName;

        [StepParam("worldPositionStays", label: "保持世界坐标", order: 2)]
        private bool m_WorldPositionStays = true;

        /// <summary>
        /// 进入动作时设置父节点并立即完成。
        /// </summary>
        protected override void OnEnter()
        {
            ApplyParent();
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
        /// 快速应用时设置父节点。
        /// </summary>
        protected override void OnApply()
        {
            ApplyParent();
        }

        /// <summary>
        /// 将目标对象设置到指定父节点下。
        /// </summary>
        private void ApplyParent()
        {
            if (m_Target == null)
            {
                LWDebug.LogWarning("步骤动作-设置父节点：未找到对象 " + m_TargetName);
                return;
            }

            Transform parentTransform = ResolveParentTransform();
            m_Target.transform.SetParent(parentTransform, m_WorldPositionStays);
        }

        /// <summary>
        /// 解析父节点名称对应的 Transform；名称为空时返回空表示挂到根节点。
        /// </summary>
        private Transform ResolveParentTransform()
        {
            if (string.IsNullOrEmpty(m_ParentName))
            {
                return null;
            }

            GameObject parentObject = GameObject.Find(m_ParentName);
            if (parentObject == null)
            {
                LWDebug.LogWarning("步骤动作-设置父节点：未找到父对象 " + m_ParentName);
                return null;
            }

            return parentObject.transform;
        }
    }
}
