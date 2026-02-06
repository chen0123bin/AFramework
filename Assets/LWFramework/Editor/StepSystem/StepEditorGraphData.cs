using System;
using System.Collections.Generic;
using LWStep;
using UnityEngine;

namespace LWStep.Editor
{
    [Serializable]
    public class StepEditorGraphData
    {
        public string StartNodeId;
        public List<StepEditorNodeData> Nodes;
        public List<StepEditorEdgeData> Edges;

        /// <summary>
        /// 创建步骤图编辑器数据
        /// </summary>
        public StepEditorGraphData()
        {
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
