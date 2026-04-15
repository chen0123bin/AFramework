namespace LWStep
{
    [StepActionInfo("写入上下文", Category = "上下文", SummaryTemplate = "Set:{key}")]
    public class StepSetContextValueAction : BaseStepAction
    {
        [StepParam("key", label: "键", order: 0)]
        private string m_Key;

        [StepParam("value", label: "值", order: 1)]
        private string m_Value;

        /// <summary>
        /// 进入动作：写入上下文并结束。
        /// </summary>
        protected override void OnEnter()
        {
            WriteValue();
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
        /// 快速应用：写入上下文。
        /// </summary>
        protected override void OnApply()
        {
            WriteValue();
        }

        /// <summary>
        /// 写入上下文值，优先尝试基础类型解析。
        /// </summary>
        private void WriteValue()
        {
            StepContext context = GetContext();
            if (context == null || string.IsNullOrEmpty(m_Key))
            {
                return;
            }

            object parsedValue;
            if (StepUtility.TryParseBasicValue(m_Value, out parsedValue))
            {
                context.SetValue(m_Key, parsedValue);
                return;
            }

            context.SetValue(m_Key, m_Value ?? string.Empty);
        }
    }
}
