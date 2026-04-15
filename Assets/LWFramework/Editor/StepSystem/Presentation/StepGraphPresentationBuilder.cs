using System;
using System.Collections.Generic;
using LWStep.Editor.Metadata;

namespace LWStep.Editor.Presentation
{
    /// <summary>
    /// 步骤图展示模型构建器。
    /// </summary>
    public static class StepGraphPresentationBuilder
    {
        private const int MAX_ACTION_SUMMARY_COUNT = 3;

        /// <summary>
        /// 基于节点编辑器数据构建节点展示模型。
        /// </summary>
        public static StepNodePresentation BuildNodePresentation(
            StepEditorNodeData nodeData,
            string startNodeId,
            string runtimeNodeId,
            StepNodeStatus status,
            ISet<string> runtimeTrailNodeIds,
            string currentActionName)
        {
            StepNodePresentation presentation = new StepNodePresentation();
            if (nodeData == null)
            {
                presentation.HasError = true;
                return presentation;
            }

            presentation.Title = nodeData.Id ?? string.Empty;
            presentation.Subtitle = nodeData.Name ?? string.Empty;
            presentation.CurrentActionName = currentActionName ?? string.Empty;
            presentation.IsRunning = string.Equals(runtimeNodeId, nodeData.Id, StringComparison.Ordinal)
                || status == StepNodeStatus.Running;
            presentation.IsCompleted = !presentation.IsRunning && status == StepNodeStatus.Completed;
            presentation.IsInTrail = runtimeTrailNodeIds != null && runtimeTrailNodeIds.Contains(nodeData.Id);

            if (string.Equals(startNodeId, nodeData.Id, StringComparison.Ordinal))
            {
                presentation.Badges.Add("Start");
            }

            presentation.Badges.Add(nodeData.Mode == StepNodeMode.Parallel ? "Parallel" : "Serial");
            AppendActionSummaries(nodeData.Actions, presentation.ActionSummaries);
            return presentation;
        }

        /// <summary>
        /// 基于连线编辑器数据构建连线展示模型。
        /// </summary>
        public static StepEdgePresentation BuildEdgePresentation(StepEditorEdgeData edgeData)
        {
            StepEdgePresentation presentation = new StepEdgePresentation();
            if (edgeData == null)
            {
                presentation.HasError = true;
                return presentation;
            }

            presentation.HasCondition = !string.IsNullOrEmpty(edgeData.ConditionKey);
            presentation.Label = "P" + edgeData.Priority;
            if (presentation.HasCondition)
            {
                presentation.Label += " | " + edgeData.ConditionKey + " " + edgeData.ConditionComparisonType + " " + edgeData.ConditionValue;
            }

            return presentation;
        }

        /// <summary>
        /// 将动作摘要按上限写入展示模型列表。
        /// </summary>
        private static void AppendActionSummaries(List<StepEditorActionData> actions, List<string> summaries)
        {
            if (summaries == null || actions == null || actions.Count == 0)
            {
                return;
            }

            int summaryCount = Math.Min(actions.Count, MAX_ACTION_SUMMARY_COUNT);
            for (int i = 0; i < summaryCount; i++)
            {
                StepEditorActionData action = actions[i];
                if (action == null)
                {
                    summaries.Add(string.Empty);
                    continue;
                }

                summaries.Add(StepActionDescriptorRegistry.BuildSummary(action.TypeName, action.Parameters));
            }

            int remainingCount = actions.Count - MAX_ACTION_SUMMARY_COUNT;
            if (remainingCount > 0)
            {
                summaries.Add("+" + remainingCount);
            }
        }
    }
}
