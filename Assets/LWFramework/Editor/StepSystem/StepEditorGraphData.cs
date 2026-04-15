using System;
using System.Collections.Generic;
using LWStep;
using UnityEngine;

namespace LWStep.Editor
{
    [Serializable]
    public class StepEditorGraphData
    {
        public string GraphId;
        public string StartNodeId;
        public List<StepEditorNodeData> Nodes;
        public List<StepEditorEdgeData> Edges;

        /// <summary>
        /// 创建步骤图编辑器数据
        /// </summary>
        public StepEditorGraphData()
        {
            GraphId = string.Empty;
            StartNodeId = string.Empty;
            Nodes = new List<StepEditorNodeData>();
            Edges = new List<StepEditorEdgeData>();
        }

        /// <summary>
        /// 根据节点ID获取节点数据
        /// </summary>
        public StepEditorNodeData GetNode(string nodeId)
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i].Id == nodeId)
                {
                    return Nodes[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 根据起点与终点ID获取连线数据
        /// </summary>
        public StepEditorEdgeData GetEdge(string fromId, string toId)
        {
            for (int i = 0; i < Edges.Count; i++)
            {
                StepEditorEdgeData edge = Edges[i];
                if (edge.FromId == fromId && edge.ToId == toId)
                {
                    return edge;
                }
            }
            return null;
        }
    }

    [Serializable]
    public class StepEditorNodeData
    {
        public string Id;
        public string Name;
        public Vector2 Position;
        public StepNodeMode Mode;
        public List<StepEditorActionData> Actions;

        /// <summary>
        /// 创建节点数据
        /// </summary>
        public StepEditorNodeData()
        {
            Id = string.Empty;
            Name = string.Empty;
            Position = Vector2.zero;
            Mode = StepNodeMode.Serial;
            Actions = new List<StepEditorActionData>();
        }
    }

    [Serializable]
    public class StepEditorEdgeData
    {
        public string FromId;
        public string ToId;
        public int Priority;
        public string ConditionKey;
        public ComparisonType ConditionComparisonType;
        public string ConditionValue;

        /// <summary>
        /// 创建连线数据
        /// </summary>
        public StepEditorEdgeData()
        {
            FromId = string.Empty;
            ToId = string.Empty;
            Priority = 0;
            ConditionKey = string.Empty;
            ConditionComparisonType = ComparisonType.EqualTo;
            ConditionValue = string.Empty;
        }
    }

    [Serializable]
    public class StepEditorActionData
    {
        public string TypeName;
        public List<StepEditorParameterData> Parameters;

        /// <summary>
        /// 创建动作数据
        /// </summary>
        public StepEditorActionData()
        {
            TypeName = string.Empty;
            Parameters = new List<StepEditorParameterData>();
        }

        /// <summary>
        /// 根据参数键读取参数值，未命中时返回默认值。
        /// </summary>
        public string GetParameterValue(string key, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(key))
            {
                return defaultValue;
            }

            for (int i = 0; i < Parameters.Count; i++)
            {
                StepEditorParameterData parameter = Parameters[i];
                if (parameter == null)
                {
                    continue;
                }

                if (string.Equals(parameter.Key, key, StringComparison.Ordinal))
                {
                    return parameter.Value ?? string.Empty;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// 设置参数值；若参数已存在则覆盖并清理同键重复项，不存在则新增。
        /// </summary>
        public void SetParameterValue(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            int firstMatchedIndex = -1;
            for (int i = 0; i < Parameters.Count; i++)
            {
                StepEditorParameterData parameter = Parameters[i];
                if (parameter == null)
                {
                    continue;
                }

                if (!string.Equals(parameter.Key, key, StringComparison.Ordinal))
                {
                    continue;
                }

                if (firstMatchedIndex < 0)
                {
                    firstMatchedIndex = i;
                    parameter.Value = value ?? string.Empty;
                    continue;
                }

                Parameters.RemoveAt(i);
                i--;
            }

            if (firstMatchedIndex < 0)
            {
                StepEditorParameterData newParameter = new StepEditorParameterData();
                newParameter.Key = key;
                newParameter.Value = value ?? string.Empty;
                Parameters.Add(newParameter);
            }
        }
    }

    [Serializable]
    public class StepEditorParameterData
    {
        public string Key;
        public string Value;

        /// <summary>
        /// 创建参数数据
        /// </summary>
        public StepEditorParameterData()
        {
            Key = string.Empty;
            Value = string.Empty;
        }
    }
}
