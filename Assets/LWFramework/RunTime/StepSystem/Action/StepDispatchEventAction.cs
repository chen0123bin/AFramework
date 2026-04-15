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
            DispatchEventSafely();
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
        /// 快速应用：进入阶段已完成派发与结束，此处无需额外逻辑。
        /// </summary>
        protected override void OnApply()
        {
        }

        /// <summary>
        /// 安全派发无参事件：事件名为空或事件管理器缺失时跳过。
        /// </summary>
        private void DispatchEventSafely()
        {
            if (string.IsNullOrEmpty(m_EventName))
            {
                return;
            }

            IEventManager eventManager;
            if (!MainManager.Instance.TryGetManager<IEventManager>(out eventManager) || eventManager == null)
            {
                return;
            }

            eventManager.DispatchEvent(m_EventName);
        }
    }
}
