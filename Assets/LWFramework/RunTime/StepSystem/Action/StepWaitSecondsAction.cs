using System;

namespace LWStep
{
    [StepActionInfo("等待秒数", Category = "流程控制", SummaryTemplate = "Wait:{seconds}s")]
    public class StepWaitSecondsAction : BaseStepAction
    {
        [StepParam("seconds", label: "等待秒数", order: 0)]
        private float m_Seconds;

        private DateTime m_EndTimeUtc;

        /// <summary>
        /// 进入动作：记录截止时间。
        /// </summary>
        protected override void OnEnter()
        {
            m_EndTimeUtc = DateTime.UtcNow.AddSeconds(m_Seconds);
            if (m_Seconds <= 0f)
            {
                Finish();
            }
        }

        /// <summary>
        /// 更新动作：到达截止时间后结束。
        /// </summary>
        protected override void OnUpdate()
        {
            if (DateTime.UtcNow >= m_EndTimeUtc)
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
        /// 快速应用：直接结束动作。
        /// </summary>
        protected override void OnApply()
        {
            Finish();
        }
    }
}
