using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace LWStep
{

    /// <summary>
    /// 步骤动作基类（可继承扩展）
    /// </summary>
    public abstract class BaseTargeStepAction : BaseStepAction
    {
        [StepParam("target")]
        protected string m_TargetName;
        protected GameObject m_Target;
        public override void Enter()
        {
            m_Target = FindTarget();
            base.Enter();
        }
        /// <summary>
        /// 查找目标对象
        /// </summary>
        protected GameObject FindTarget()
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
