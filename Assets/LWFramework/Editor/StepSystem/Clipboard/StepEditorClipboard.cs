using System.Collections.Generic;
using UnityEngine;

namespace LWStep.Editor
{
    /// <summary>
    /// 步骤图编辑器剪贴板负载数据。
    /// </summary>
    public sealed class StepEditorClipboardPayload
    {
        public List<StepEditorNodeData> Nodes;
        public List<StepEditorEdgeData> InternalEdges;

        /// <summary>
        /// 创建剪贴板负载。
        /// </summary>
        public StepEditorClipboardPayload()
        {
            Nodes = new List<StepEditorNodeData>();
            InternalEdges = new List<StepEditorEdgeData>();
        }
    }

    /// <summary>
    /// 步骤图编辑器复制粘贴工具。
    /// </summary>
    public static class StepEditorClipboard
    {
        /// <summary>
        /// 复制选中节点及其内部连线到剪贴板负载。
        /// </summary>
        public static StepEditorClipboardPayload Copy(StepEditorGraphData data, IList<string> selectedNodeIds)
        {
            StepEditorClipboardPayload payload = new StepEditorClipboardPayload();
            if (data == null || selectedNodeIds == null || selectedNodeIds.Count == 0)
            {
                return payload;
            }

            HashSet<string> selectedSet = new HashSet<string>();
            for (int i = 0; i < selectedNodeIds.Count; i++)
            {
                string nodeId = selectedNodeIds[i];
                if (string.IsNullOrEmpty(nodeId))
                {
                    continue;
                }

                if (!selectedSet.Add(nodeId))
                {
                    continue;
                }

                StepEditorNodeData node = data.GetNode(nodeId);
                if (node == null)
                {
                    continue;
                }

                payload.Nodes.Add(CloneNode(node));
            }

            if (data.Edges == null)
            {
                return payload;
            }

            for (int i = 0; i < data.Edges.Count; i++)
            {
                StepEditorEdgeData edge = data.Edges[i];
                if (edge == null || !selectedSet.Contains(edge.FromId) || !selectedSet.Contains(edge.ToId))
                {
                    continue;
                }

                payload.InternalEdges.Add(CloneEdge(edge));
            }

            return payload;
        }

        /// <summary>
        /// 将剪贴板负载粘贴到目标图数据并返回新建节点ID列表。
        /// </summary>
        public static List<string> Paste(StepEditorGraphData data, StepEditorClipboardPayload payload, Vector2 offset)
        {
            List<string> pastedNodeIds = new List<string>();
            if (data == null || payload == null || payload.Nodes == null || payload.Nodes.Count == 0)
            {
                return pastedNodeIds;
            }

            Dictionary<string, string> idMapping = new Dictionary<string, string>();
            for (int i = 0; i < payload.Nodes.Count; i++)
            {
                StepEditorNodeData sourceNode = payload.Nodes[i];
                if (sourceNode == null || string.IsNullOrEmpty(sourceNode.Id))
                {
                    continue;
                }

                string newId = GenerateCopyId(data, sourceNode.Id);
                idMapping[sourceNode.Id] = newId;

                StepEditorNodeData clonedNode = CloneNode(sourceNode);
                clonedNode.Id = newId;
                if (clonedNode.Name == sourceNode.Id)
                {
                    clonedNode.Name = newId;
                }
                clonedNode.Position = sourceNode.Position + offset;
                data.Nodes.Add(clonedNode);
                pastedNodeIds.Add(newId);
            }

            if (payload.InternalEdges == null)
            {
                return pastedNodeIds;
            }

            for (int i = 0; i < payload.InternalEdges.Count; i++)
            {
                StepEditorEdgeData sourceEdge = payload.InternalEdges[i];
                if (sourceEdge == null)
                {
                    continue;
                }

                string newFromId;
                string newToId;
                if (!idMapping.TryGetValue(sourceEdge.FromId, out newFromId) || !idMapping.TryGetValue(sourceEdge.ToId, out newToId))
                {
                    continue;
                }

                if (data.GetEdge(newFromId, newToId) != null)
                {
                    continue;
                }

                StepEditorEdgeData clonedEdge = CloneEdge(sourceEdge);
                clonedEdge.FromId = newFromId;
                clonedEdge.ToId = newToId;
                data.Edges.Add(clonedEdge);
            }

            return pastedNodeIds;
        }

        /// <summary>
        /// 深拷贝节点数据（包含动作与参数）。
        /// </summary>
        private static StepEditorNodeData CloneNode(StepEditorNodeData source)
        {
            StepEditorNodeData clone = new StepEditorNodeData();
            if (source == null)
            {
                return clone;
            }

            clone.Id = source.Id ?? string.Empty;
            clone.Name = source.Name ?? string.Empty;
            clone.Position = source.Position;
            clone.Mode = source.Mode;

            if (source.Actions != null)
            {
                for (int i = 0; i < source.Actions.Count; i++)
                {
                    clone.Actions.Add(CloneAction(source.Actions[i]));
                }
            }

            return clone;
        }

        /// <summary>
        /// 深拷贝动作数据（包含参数）。
        /// </summary>
        private static StepEditorActionData CloneAction(StepEditorActionData source)
        {
            StepEditorActionData clone = new StepEditorActionData();
            if (source == null)
            {
                return clone;
            }

            clone.TypeName = source.TypeName ?? string.Empty;
            if (source.Parameters != null)
            {
                for (int i = 0; i < source.Parameters.Count; i++)
                {
                    clone.Parameters.Add(CloneParameter(source.Parameters[i]));
                }
            }

            return clone;
        }

        /// <summary>
        /// 深拷贝参数数据。
        /// </summary>
        private static StepEditorParameterData CloneParameter(StepEditorParameterData source)
        {
            StepEditorParameterData clone = new StepEditorParameterData();
            if (source == null)
            {
                return clone;
            }

            clone.Key = source.Key ?? string.Empty;
            clone.Value = source.Value ?? string.Empty;
            return clone;
        }

        /// <summary>
        /// 深拷贝连线数据。
        /// </summary>
        private static StepEditorEdgeData CloneEdge(StepEditorEdgeData source)
        {
            StepEditorEdgeData clone = new StepEditorEdgeData();
            if (source == null)
            {
                return clone;
            }

            clone.FromId = source.FromId ?? string.Empty;
            clone.ToId = source.ToId ?? string.Empty;
            clone.Priority = source.Priority;
            clone.ConditionKey = source.ConditionKey ?? string.Empty;
            clone.ConditionComparisonType = source.ConditionComparisonType;
            clone.ConditionValue = source.ConditionValue ?? string.Empty;
            return clone;
        }

        /// <summary>
        /// 为指定原始节点ID生成唯一的 _copyN 风格节点ID。
        /// </summary>
        private static string GenerateCopyId(StepEditorGraphData data, string sourceId)
        {
            string baseId = string.IsNullOrEmpty(sourceId) ? "node" : sourceId;
            int index = 1;
            while (true)
            {
                string candidate = baseId + "_copy" + index;
                if (data.GetNode(candidate) == null)
                {
                    return candidate;
                }

                index += 1;
            }
        }
    }
}
