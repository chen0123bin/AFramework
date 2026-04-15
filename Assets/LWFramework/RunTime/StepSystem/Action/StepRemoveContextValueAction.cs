namespace LWStep
{
    [StepActionInfo("移除上下文", Category = "上下文", SummaryTemplate = "Remove:{key}")]
    public class StepRemoveContextValueAction : BaseStepAction
    {
        [StepParam("key", label: "键", order: 0)]
        private string m_Key;

        /// <summary>
        /// 进入动作：删除指定键并结束。
        /// </summary>
        protected override void OnEnter()
        {
            RemoveValue();
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
        /// 快速应用：删除指定键。
        /// </summary>
        protected override void OnApply()
        {
            RemoveValue();
        }

        /// <summary>
        /// 删除上下文中的目标键。
        /// </summary>
        private void RemoveValue()
        {
            StepContext context = GetContext();
            if (context == null || string.IsNullOrEmpty(m_Key))
            {
                return;
            }

            context.RemoveKey(m_Key);
        }
    }
}
