namespace LWStep.Editor.Presentation
{
    /// <summary>
    /// 步骤连线展示模型。
    /// </summary>
    public sealed class StepEdgePresentation
    {
        public string Label;
        public bool HasCondition;
        public bool HasError;

        /// <summary>
        /// 创建连线展示模型。
        /// </summary>
        public StepEdgePresentation()
        {
            Label = string.Empty;
        }
    }
}
