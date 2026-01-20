using System;
using System.Collections.Generic;
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

        public StepEditorGraphData()
        {
            GraphId = "step_graph";
            StartNodeId = string.Empty;
            Nodes = new List<StepEditorNodeData>();
            Edges = new List<StepEditorEdgeData>();
        }

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
        public List<StepEditorActionData> Actions;

        public StepEditorNodeData()
        {
            Id = string.Empty;
            Name = string.Empty;
            Position = Vector2.zero;
            Actions = new List<StepEditorActionData>();
        }
    }

    [Serializable]
    public class StepEditorEdgeData
    {
        public string FromId;
        public string ToId;
        public int Priority;
        public string Condition;
        public string Tag;

        public StepEditorEdgeData()
        {
            FromId = string.Empty;
            ToId = string.Empty;
            Priority = 0;
            Condition = string.Empty;
            Tag = string.Empty;
        }
    }

    [Serializable]
    public class StepEditorActionData
    {
        public string TypeName;
        public List<StepEditorParameterData> Parameters;

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

        public StepEditorParameterData()
        {
            Key = string.Empty;
            Value = string.Empty;
        }
    }
}
