using System;
using System.Collections.Generic;

namespace LWStep
{
    /// <summary>
    /// 运行时联调轨迹追踪器。
    /// </summary>
    public sealed class StepRuntimeDebugTracker
    {
        private const int MAX_RECENT_EVENT_COUNT = 16;

        private readonly List<string> m_TrailNodeIds = new List<string>();
        private readonly List<string> m_RecentEvents = new List<string>();

        /// <summary>
        /// 清空当前追踪状态。
        /// </summary>
        public void Clear()
        {
            m_TrailNodeIds.Clear();
            m_RecentEvents.Clear();
        }

        /// <summary>
        /// 记录节点进入事件并追加轨迹。
        /// </summary>
        public void RecordNodeEnter(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return;
            }

            m_TrailNodeIds.Add(nodeId);
            AppendRecentEvent("Enter:" + nodeId);
        }

        /// <summary>
        /// 记录节点离开事件。
        /// </summary>
        public void RecordNodeLeave(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return;
            }

            AppendRecentEvent("Leave:" + nodeId);
        }

        /// <summary>
        /// 记录当前动作切换事件。
        /// </summary>
        public void RecordActionChanged(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
            {
                return;
            }

            AppendRecentEvent("Action:" + actionName);
        }

        /// <summary>
        /// 记录跳转进度事件。
        /// </summary>
        public void RecordJump(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return;
            }

            AppendRecentEvent("Jump:" + nodeId);
        }

        /// <summary>
        /// 根据当前上下文与轨迹创建联调快照。
        /// </summary>
        public StepRuntimeDebugSnapshot CreateSnapshot(StepContext context, string currentNodeId, string currentActionName)
        {
            Dictionary<string, string> contextValues = new Dictionary<string, string>();
            if (context != null)
            {
                Dictionary<string, object> rawValues = context.CloneData();
                foreach (KeyValuePair<string, object> kvp in rawValues)
                {
                    Type valueType = kvp.Value != null ? kvp.Value.GetType() : null;
                    contextValues[kvp.Key] = StepUtility.ConvertToRawString(kvp.Value, valueType);
                }
            }

            return new StepRuntimeDebugSnapshot(
                currentNodeId,
                currentActionName,
                new List<string>(m_TrailNodeIds),
                new List<string>(m_RecentEvents),
                contextValues);
        }

        /// <summary>
        /// 追加最近事件并维持上限。
        /// </summary>
        private void AppendRecentEvent(string eventText)
        {
            if (string.IsNullOrEmpty(eventText))
            {
                return;
            }

            m_RecentEvents.Add(eventText);
            if (m_RecentEvents.Count <= MAX_RECENT_EVENT_COUNT)
            {
                return;
            }

            m_RecentEvents.RemoveAt(0);
        }
    }
}
