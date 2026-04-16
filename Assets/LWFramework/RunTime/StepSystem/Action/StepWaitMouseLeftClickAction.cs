using LWCore;
using UnityEngine;

namespace LWStep
{
    [StepActionInfo("等待鼠标左键", Category = "流程控制", SummaryTemplate = "Wait:MouseLeftClick")]
    public class StepWaitMouseLeftClickAction : BaseStepAction
    {
        /// <summary>
        /// 进入动作：输出提示日志。
        /// </summary>
        protected override void OnEnter()
        {
            LWDebug.Log("步骤动作-等待鼠标左键点击");
        }

        /// <summary>
        /// 更新动作：检测鼠标左键点击后完成。
        /// </summary>
        protected override void OnUpdate()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Finish();
            }
        }

        /// <summary>
        /// 退出动作。
        /// </summary>
        protected override void OnExit()
        {
        }

        /// <summary>
        /// 快速应用：直接完成动作。
        /// </summary>
        protected override void OnApply()
        {
            Finish();
        }
    }
}
