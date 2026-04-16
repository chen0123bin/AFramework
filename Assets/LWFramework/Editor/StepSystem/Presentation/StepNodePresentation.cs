using System.Collections.Generic;

namespace LWStep.Editor.Presentation
{
    /// <summary>
    /// 步骤节点展示模型。
    /// </summary>
    public sealed class StepNodePresentation
    {
        public string Title;
        public string Subtitle;
        public List<string> Badges;
        public List<string> ActionSummaries;
        public bool IsRunning;
        public bool IsCompleted;
        public bool HasWarning;
        public bool HasError;
        public bool IsInTrail;
        public string CurrentActionName;

        /// <summary>
        /// 创建节点展示模型并初始化集合字段。
        /// </summary>
        public StepNodePresentation()
        {
            Title = string.Empty;
            Subtitle = string.Empty;
            Badges = new List<string>();
            ActionSummaries = new List<string>();
            CurrentActionName = string.Empty;
        }
    }
}
