using LWCore;
using UnityEngine;

namespace LWStep
{
    public class StepWaitMouseLeftClickAction : BaseStepAction
    {
        protected override void OnEnter()
        {
            LWDebug.Log("步骤动作-等待鼠标左键点击");
        }

        protected override void OnUpdate()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Finish();
            }
        }

        protected override void OnExit()
        {
        }

        protected override void OnApply()
        {
            Finish();
        }
    }
}
