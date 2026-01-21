using LWCore;

namespace LWStep
{
    public class StepLogAction : BaseStepAction
    {
        [StepParam("message")]
        private string m_Message = "步骤动作执行";

        /// <summary>
        /// 进入动作：输出日志并结束
        /// </summary>
        protected override void OnEnter()
        {
            string message = GetMessage();
            LWDebug.Log(message);
            Finish();
        }

        /// <summary>
        /// 更新动作：确保完成
        /// </summary>
        protected override void OnUpdate()
        {
            if (!IsFinished)
            {
                Finish();
            }
        }

        /// <summary>
        /// 退出动作
        /// </summary>
        protected override void OnExit()
        {
        }

        /// <summary>
        /// 快速应用：输出日志
        /// </summary>
        protected override void OnApply()
        {
            string message = GetMessage();
            LWDebug.Log(message);
        }

        /// <summary>
        /// 获取日志内容
        /// </summary>
        private string GetMessage()
        {
            if (!string.IsNullOrEmpty(m_Message))
            {
                return m_Message;
            }
            return "步骤动作执行";
        }
    }
}
