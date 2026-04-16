using System.Collections.Generic;
using UnityEngine;

namespace LWStep.Editor
{
    /// <summary>
    /// 步骤图自动布局工具。
    /// </summary>
    public static class StepGraphAutoLayout
    {
        /// <summary>
        /// 按“从左到右”规则自动布局节点：层级决定 X，层内序号决定 Y。
        /// </summary>
        public static void ApplyLeftToRight(StepEditorGraphData data, float horizontalSpacing, float verticalSpacing)
        {
            if (data == null || data.Nodes == null || data.Nodes.Count == 0)
            {
                return;
            }

            Dictionary<string, StepEditorNodeData> nodeMap = BuildNodeMap(data.Nodes);
            Dictionary<string, List<string>> outgoing = BuildOutgoingMap(data.Edges, nodeMap);
            Dictionary<string, int> indegree = BuildIndegreeMap(nodeMap, outgoing);
            Dictionary<string, int> layerByNode = ResolveLayerByNode(nodeMap, outgoing, indegree);
            Dictionary<int, List<StepEditorNodeData>> layers = BuildLayerGroups(data.Nodes, layerByNode);

            foreach (KeyValuePair<int, List<StepEditorNodeData>> pair in layers)
            {
                List<StepEditorNodeData> nodesInLayer = pair.Value;
                nodesInLayer.Sort(CompareNodeInLayer);
                for (int i = 0; i < nodesInLayer.Count; i++)
                {
                    StepEditorNodeData node = nodesInLayer[i];
                    node.Position = new Vector2(pair.Key * horizontalSpacing, i * verticalSpacing);
                }
            }
        }

        /// <summary>
        /// 构建节点 ID 到节点数据的映射。
        /// </summary>
        private static Dictionary<string, StepEditorNodeData> BuildNodeMap(List<StepEditorNodeData> nodes)
        {
            Dictionary<string, StepEditorNodeData> map = new Dictionary<string, StepEditorNodeData>();
            for (int i = 0; i < nodes.Count; i++)
            {
                StepEditorNodeData node = nodes[i];
                if (node == null || string.IsNullOrEmpty(node.Id) || map.ContainsKey(node.Id))
                {
                    continue;
                }

                map.Add(node.Id, node);
            }

            return map;
        }

        /// <summary>
        /// 构建出边邻接表。
        /// </summary>
        private static Dictionary<string, List<string>> BuildOutgoingMap(List<StepEditorEdgeData> edges, Dictionary<string, StepEditorNodeData> nodeMap)
        {
            Dictionary<string, List<string>> outgoing = new Dictionary<string, List<string>>();
            if (edges == null)
            {
                return outgoing;
            }

            for (int i = 0; i < edges.Count; i++)
            {
                StepEditorEdgeData edge = edges[i];
                if (edge == null || !nodeMap.ContainsKey(edge.FromId) || !nodeMap.ContainsKey(edge.ToId))
                {
                    continue;
                }

                List<string> toList;
                if (!outgoing.TryGetValue(edge.FromId, out toList))
                {
                    toList = new List<string>();
                    outgoing.Add(edge.FromId, toList);
                }

                if (!toList.Contains(edge.ToId))
                {
                    toList.Add(edge.ToId);
                }
            }

            return outgoing;
        }

        /// <summary>
        /// 基于邻接表计算每个节点的入度。
        /// </summary>
        private static Dictionary<string, int> BuildIndegreeMap(Dictionary<string, StepEditorNodeData> nodeMap, Dictionary<string, List<string>> outgoing)
        {
            Dictionary<string, int> indegree = new Dictionary<string, int>();
            foreach (KeyValuePair<string, StepEditorNodeData> pair in nodeMap)
            {
                indegree[pair.Key] = 0;
            }

            foreach (KeyValuePair<string, List<string>> pair in outgoing)
            {
                List<string> toList = pair.Value;
                for (int i = 0; i < toList.Count; i++)
                {
                    string toId = toList[i];
                    indegree[toId] = indegree[toId] + 1;
                }
            }

            return indegree;
        }

        /// <summary>
        /// 通过拓扑层级计算每个节点所属的横向层。
        /// </summary>
        private static Dictionary<string, int> ResolveLayerByNode(Dictionary<string, StepEditorNodeData> nodeMap, Dictionary<string, List<string>> outgoing, Dictionary<string, int> indegree)
        {
            Dictionary<string, int> layerByNode = new Dictionary<string, int>();
            Queue<string> queue = new Queue<string>();

            foreach (KeyValuePair<string, int> pair in indegree)
            {
                if (pair.Value == 0)
                {
                    queue.Enqueue(pair.Key);
                    layerByNode[pair.Key] = 0;
                }
            }

            while (queue.Count > 0)
            {
                string fromId = queue.Dequeue();
                int fromLayer = layerByNode[fromId];

                List<string> toList;
                if (!outgoing.TryGetValue(fromId, out toList))
                {
                    continue;
                }

                for (int i = 0; i < toList.Count; i++)
                {
                    string toId = toList[i];
                    int nextLayer = fromLayer + 1;
                    int existedLayer;
                    if (!layerByNode.TryGetValue(toId, out existedLayer) || nextLayer > existedLayer)
                    {
                        layerByNode[toId] = nextLayer;
                    }

                    indegree[toId] = indegree[toId] - 1;
                    if (indegree[toId] == 0)
                    {
                        queue.Enqueue(toId);
                    }
                }
            }

            foreach (KeyValuePair<string, StepEditorNodeData> pair in nodeMap)
            {
                if (!layerByNode.ContainsKey(pair.Key))
                {
                    layerByNode[pair.Key] = 0;
                }
            }

            return layerByNode;
        }

        /// <summary>
        /// 按层级聚合节点列表。
        /// </summary>
        private static Dictionary<int, List<StepEditorNodeData>> BuildLayerGroups(List<StepEditorNodeData> nodes, Dictionary<string, int> layerByNode)
        {
            Dictionary<int, List<StepEditorNodeData>> layers = new Dictionary<int, List<StepEditorNodeData>>();
            for (int i = 0; i < nodes.Count; i++)
            {
                StepEditorNodeData node = nodes[i];
                if (node == null || string.IsNullOrEmpty(node.Id))
                {
                    continue;
                }

                int layer;
                if (!layerByNode.TryGetValue(node.Id, out layer))
                {
                    layer = 0;
                }

                List<StepEditorNodeData> list;
                if (!layers.TryGetValue(layer, out list))
                {
                    list = new List<StepEditorNodeData>();
                    layers.Add(layer, list);
                }

                list.Add(node);
            }

            return layers;
        }

        /// <summary>
        /// 比较同层节点顺序，优先保持原有纵向顺序。
        /// </summary>
        private static int CompareNodeInLayer(StepEditorNodeData a, StepEditorNodeData b)
        {
            if (ReferenceEquals(a, b))
            {
                return 0;
            }
            if (a == null)
            {
                return 1;
            }
            if (b == null)
            {
                return -1;
            }

            int yCompare = a.Position.y.CompareTo(b.Position.y);
            if (yCompare != 0)
            {
                return yCompare;
            }

            return string.CompareOrdinal(a.Id ?? string.Empty, b.Id ?? string.Empty);
        }
    }
}
