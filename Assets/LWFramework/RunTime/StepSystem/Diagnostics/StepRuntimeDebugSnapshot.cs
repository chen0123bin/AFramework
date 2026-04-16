using System.Collections.Generic;

namespace LWStep
{
    /// <summary>
    /// 运行时联调快照。
    /// </summary>
    public sealed class StepRuntimeDebugSnapshot
    {
        public string CurrentNodeId { get; private set; }
        public string CurrentActionName { get; private set; }
        public List<string> TrailNodeIds { get; private set; }
        public List<string> RecentEvents { get; private set; }
        public Dictionary<string, string> ContextValues { get; private set; }

        /// <summary>
        /// 创建运行时联调快照。
        /// </summary>
        public StepRuntimeDebugSnapshot(
            string currentNodeId,
            string currentActionName,
            List<string> trailNodeIds,
            List<string> recentEvents,
            Dictionary<string, string> contextValues)
        {
            CurrentNodeId = currentNodeId ?? string.Empty;
            CurrentActionName = currentActionName ?? string.Empty;
            TrailNodeIds = trailNodeIds ?? new List<string>();
            RecentEvents = recentEvents ?? new List<string>();
            ContextValues = contextValues ?? new Dictionary<string, string>();
        }
    }
}
