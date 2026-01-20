using LWCore;
using System.Collections.Generic;

namespace LWStep
{
    public class StepLogAction : BaseStepAction
    {
        protected override void OnEnter()
        {
            string message = GetMessage();
            LWDebug.Log(message);
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
            string message = GetMessage();
            LWDebug.Log(message);
        }

        private string GetMessage()
        {
            Dictionary<string, string> parameters = GetParameters();
            if (parameters == null)
            {
                return "步骤动作执行";
            }
            string message;
            if (parameters.TryGetValue("message", out message))
            {
                return message;
            }
            return "步骤动作执行";
        }
    }
}
