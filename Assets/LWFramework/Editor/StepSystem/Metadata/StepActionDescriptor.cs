using System;
using System.Collections.Generic;

namespace LWStep.Editor.Metadata
{
    /// <summary>
    /// 步骤动作描述信息。
    /// </summary>
    public sealed class StepActionDescriptor
    {
        public Type ActionType;
        public string TypeName;
        public string DisplayName;
        public string Category;
        public string SummaryTemplate;
        public string Description;
        public List<string> Keywords;
        public List<StepActionParameterDescriptor> Parameters;

        /// <summary>
        /// 创建动作描述信息实例。
        /// </summary>
        public StepActionDescriptor()
        {
            TypeName = string.Empty;
            DisplayName = string.Empty;
            Category = string.Empty;
            SummaryTemplate = string.Empty;
            Description = string.Empty;
            Keywords = new List<string>();
            Parameters = new List<StepActionParameterDescriptor>();
        }
    }

    /// <summary>
    /// 步骤动作参数描述信息。
    /// </summary>
    public sealed class StepActionParameterDescriptor
    {
        public string Key;
        public string Label;
        public string TypeName;
        public int Order;
        public bool IsAdvanced;

        /// <summary>
        /// 创建动作参数描述信息实例。
        /// </summary>
        public StepActionParameterDescriptor()
        {
            Key = string.Empty;
            Label = string.Empty;
            TypeName = string.Empty;
            Order = int.MaxValue;
            IsAdvanced = false;
        }
    }
}
