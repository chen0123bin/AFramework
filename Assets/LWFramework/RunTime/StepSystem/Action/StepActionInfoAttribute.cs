using System;

namespace LWStep
{
    /// <summary>
    /// 步骤动作展示元数据特性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class StepActionInfoAttribute : Attribute
    {
        public string DisplayName { get; private set; }
        public string Category { get; set; }
        public string SummaryTemplate { get; set; }
        public string Description { get; set; }
        public string[] Keywords { get; set; }

        /// <summary>
        /// 创建动作元数据特性。
        /// </summary>
        public StepActionInfoAttribute(string displayName)
        {
            DisplayName = displayName;
            Category = string.Empty;
            SummaryTemplate = string.Empty;
            Description = string.Empty;
            Keywords = Array.Empty<string>();
        }
    }
}
