using LWCore;

namespace LWStep
{
    [StepActionInfo("派发事件", Category = "流程控制", SummaryTemplate = "Event:{eventName}")]
    public class StepDispatchEventAction : BaseStepAction
    {
        [StepParam("eventName", label: "事件名", order: 0)]
        private string m_EventName;

        /// <summary>
        /// 进入动作：派发事件并结束。
        /// </summary>
        protected override void OnEnter()
        {
            DispatchEvent();
            Finish();
        }

        /// <summary>
        /// 更新动作：该动作在 Enter 时已完成。
        /// </summary>
        protected override void OnUpdate()
        {
        }

        /// <summary>
        /// 退出动作。
        /// </summary>
        protected override void OnExit()
        {
        }

        /// <summary>
        /// 快速应用：派发事件。
        /// </summary>
        protected override void OnApply()
        {
            DispatchEvent();
        }

        /// <summary>
        /// 通过事件管理器派发无参事件。
        /// </summary>
        private void DispatchEvent()
        {
            if (string.IsNullOrEmpty(m_EventName))
            {
                return;
            }

            if (ManagerUtility.EventMgr == null)
            {
                return;
            }

            ManagerUtility.EventMgr.DispatchEvent(m_EventName);
        }
    }
}
