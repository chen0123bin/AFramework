using System.Collections.Generic;
using UnityEngine;

namespace LWStep.Editor
{
    /// <summary>
    /// 步骤图自动布局工具。
    /// </summary>
    public static class StepGraphAutoLayout
    {
        private const int ORDERING_PASS_COUNT = 2;
        private const int CENTERING_PASS_COUNT = 3;

        /// <summary>
        /// 按“从左到右”规则自动布局节点：层级决定 X，层内顺序和层中心共同决定 Y。
        /// </summary>
        public static void ApplyLeftToRight(StepEditorGraphData data, float horizontalSpacing, float verticalSpacing)
        {
            if (data == null || data.Nodes == null || data.Nodes.Count == 0)
            {
                return;
            }

            Dictionary<string, StepEditorNodeData> nodeMap = BuildNodeMap(data.Nodes);
            Dictionary<string, List<string>> outgoing = BuildOutgoingMap(data.Edges, nodeMap);
            Dictionary<string, List<string>> incoming = BuildIncomingMap(outgoing);
            Dictionary<string, int> indegree = BuildIndegreeMap(nodeMap, outgoing);
            Dictionary<string, int> layerByNode = ResolveLayerByNode(nodeMap, outgoing, indegree);
            Dictionary<int, List<StepEditorNodeData>> layers = BuildLayerGroups(data.Nodes, layerByNode);

            SortLayersByConnectivity(layers, incoming, outgoing);
            PositionLayers(layers, horizontalSpacing, verticalSpacing);
            CenterLayers(layers, nodeMap, incoming, outgoing);
            NormalizeVerticalOrigin(data.Nodes);
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
        /// 根据出边邻接表反推入边邻接表。
        /// </summary>
        private static Dictionary<string, List<string>> BuildIncomingMap(Dictionary<string, List<string>> outgoing)
        {
            Dictionary<string, List<string>> incoming = new Dictionary<string, List<string>>();
            foreach (KeyValuePair<string, List<string>> pair in outgoing)
            {
                string fromId = pair.Key;
                List<string> toList = pair.Value;
                for (int i = 0; i < toList.Count; i++)
                {
                    string toId = toList[i];
                    List<string> fromList;
                    if (!incoming.TryGetValue(toId, out fromList))
                    {
                        fromList = new List<string>();
                        incoming.Add(toId, fromList);
                    }

                    if (!fromList.Contains(fromId))
                    {
                        fromList.Add(fromId);
                    }
                }
            }

            return incoming;
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
        /// 按父子连通关系优化层内顺序，尽量让分支阅读顺序稳定且少交叉。
        /// </summary>
        private static void SortLayersByConnectivity(
            Dictionary<int, List<StepEditorNodeData>> layers,
            Dictionary<string, List<string>> incoming,
            Dictionary<string, List<string>> outgoing)
        {
            List<int> layerIds = GetSortedLayerIds(layers);
            for (int i = 0; i < layerIds.Count; i++)
            {
                layers[layerIds[i]].Sort(CompareNodeInLayer);
            }

            for (int pass = 0; pass < ORDERING_PASS_COUNT; pass++)
            {
                Dictionary<string, int> previousOrder = RebuildLayerOrderIndex(layers);
                for (int i = 1; i < layerIds.Count; i++)
                {
                    SortLayerByNeighborOrder(layers[layerIds[i]], incoming, previousOrder);
                }

                previousOrder = RebuildLayerOrderIndex(layers);
                for (int i = layerIds.Count - 2; i >= 0; i--)
                {
                    SortLayerByNeighborOrder(layers[layerIds[i]], outgoing, previousOrder);
                }
            }
        }

        /// <summary>
        /// 根据邻居层内顺序对当前层节点排序。
        /// </summary>
        private static void SortLayerByNeighborOrder(
            List<StepEditorNodeData> nodesInLayer,
            Dictionary<string, List<string>> neighborMap,
            Dictionary<string, int> previousOrder)
        {
            nodesInLayer.Sort((a, b) => CompareNodeByNeighborOrder(a, b, neighborMap, previousOrder));
        }

        /// <summary>
        /// 按给定排序后的层数据写入基础坐标。
        /// </summary>
        private static void PositionLayers(Dictionary<int, List<StepEditorNodeData>> layers, float horizontalSpacing, float verticalSpacing)
        {
            List<int> layerIds = GetSortedLayerIds(layers);
            for (int i = 0; i < layerIds.Count; i++)
            {
                int layerId = layerIds[i];
                List<StepEditorNodeData> nodesInLayer = layers[layerId];
                for (int j = 0; j < nodesInLayer.Count; j++)
                {
                    StepEditorNodeData node = nodesInLayer[j];
                    node.Position = new Vector2(layerId * horizontalSpacing, j * verticalSpacing);
                }
            }
        }

        /// <summary>
        /// 让每一层整体向相邻层的重心对齐，使主链与汇合点更居中。
        /// </summary>
        private static void CenterLayers(
            Dictionary<int, List<StepEditorNodeData>> layers,
            Dictionary<string, StepEditorNodeData> nodeMap,
            Dictionary<string, List<string>> incoming,
            Dictionary<string, List<string>> outgoing)
        {
            List<int> layerIds = GetSortedLayerIds(layers);
            for (int pass = 0; pass < CENTERING_PASS_COUNT; pass++)
            {
                for (int i = 0; i < layerIds.Count; i++)
                {
                    List<StepEditorNodeData> nodesInLayer = layers[layerIds[i]];
                    float desiredCenterY;
                    if (!TryResolveLayerCenterY(nodesInLayer, nodeMap, incoming, outgoing, out desiredCenterY))
                    {
                        continue;
                    }

                    float currentCenterY = GetLayerCenterY(nodesInLayer);
                    ShiftLayer(nodesInLayer, desiredCenterY - currentCenterY);
                }
            }
        }

        /// <summary>
        /// 将整体布局上移到从 Y=0 开始，便于保持稳定视图范围。
        /// </summary>
        private static void NormalizeVerticalOrigin(List<StepEditorNodeData> nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return;
            }

            float minY = float.MaxValue;
            for (int i = 0; i < nodes.Count; i++)
            {
                StepEditorNodeData node = nodes[i];
                if (node == null)
                {
                    continue;
                }

                if (node.Position.y < minY)
                {
                    minY = node.Position.y;
                }
            }

            if (minY == float.MaxValue || Mathf.Approximately(minY, 0f))
            {
                return;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                StepEditorNodeData node = nodes[i];
                if (node == null)
                {
                    continue;
                }

                node.Position = new Vector2(node.Position.x, node.Position.y - minY);
            }
        }

        /// <summary>
        /// 获取排序后的层级 ID 列表。
        /// </summary>
        private static List<int> GetSortedLayerIds(Dictionary<int, List<StepEditorNodeData>> layers)
        {
            List<int> layerIds = new List<int>(layers.Keys);
            layerIds.Sort();
            return layerIds;
        }

        /// <summary>
        /// 重建“节点 ID -> 层内顺序”索引。
        /// </summary>
        private static Dictionary<string, int> RebuildLayerOrderIndex(Dictionary<int, List<StepEditorNodeData>> layers)
        {
            Dictionary<string, int> orderIndex = new Dictionary<string, int>();
            foreach (KeyValuePair<int, List<StepEditorNodeData>> pair in layers)
            {
                List<StepEditorNodeData> nodesInLayer = pair.Value;
                for (int i = 0; i < nodesInLayer.Count; i++)
                {
                    StepEditorNodeData node = nodesInLayer[i];
                    if (node == null || string.IsNullOrEmpty(node.Id))
                    {
                        continue;
                    }

                    orderIndex[node.Id] = i;
                }
            }

            return orderIndex;
        }

        /// <summary>
        /// 按邻居平均顺序比较节点，未命中时回退到既有顺序和原始位置。
        /// </summary>
        private static int CompareNodeByNeighborOrder(
            StepEditorNodeData a,
            StepEditorNodeData b,
            Dictionary<string, List<string>> neighborMap,
            Dictionary<string, int> previousOrder)
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

            float aNeighborOrder;
            bool hasANeighborOrder = TryGetAverageNeighborOrder(a.Id, neighborMap, previousOrder, out aNeighborOrder);
            float bNeighborOrder;
            bool hasBNeighborOrder = TryGetAverageNeighborOrder(b.Id, neighborMap, previousOrder, out bNeighborOrder);

            if (hasANeighborOrder && hasBNeighborOrder)
            {
                int neighborCompare = aNeighborOrder.CompareTo(bNeighborOrder);
                if (neighborCompare != 0)
                {
                    return neighborCompare;
                }
            }
            else if (hasANeighborOrder != hasBNeighborOrder)
            {
                return hasANeighborOrder ? -1 : 1;
            }

            int aPreviousOrder = previousOrder.ContainsKey(a.Id) ? previousOrder[a.Id] : int.MaxValue;
            int bPreviousOrder = previousOrder.ContainsKey(b.Id) ? previousOrder[b.Id] : int.MaxValue;
            int previousCompare = aPreviousOrder.CompareTo(bPreviousOrder);
            if (previousCompare != 0)
            {
                return previousCompare;
            }

            return CompareNodeInLayer(a, b);
        }

        /// <summary>
        /// 尝试计算节点邻居在各自层内顺序的平均值。
        /// </summary>
        private static bool TryGetAverageNeighborOrder(
            string nodeId,
            Dictionary<string, List<string>> neighborMap,
            Dictionary<string, int> orderIndex,
            out float averageOrder)
        {
            averageOrder = 0f;
            if (string.IsNullOrEmpty(nodeId) || neighborMap == null || orderIndex == null)
            {
                return false;
            }

            List<string> neighborIds;
            if (!neighborMap.TryGetValue(nodeId, out neighborIds) || neighborIds == null || neighborIds.Count == 0)
            {
                return false;
            }

            float sum = 0f;
            int count = 0;
            for (int i = 0; i < neighborIds.Count; i++)
            {
                int order;
                if (!orderIndex.TryGetValue(neighborIds[i], out order))
                {
                    continue;
                }

                sum += order;
                count += 1;
            }

            if (count == 0)
            {
                return false;
            }

            averageOrder = sum / count;
            return true;
        }

        /// <summary>
        /// 尝试计算当前层应对齐到的垂直中心。
        /// </summary>
        private static bool TryResolveLayerCenterY(
            List<StepEditorNodeData> nodesInLayer,
            Dictionary<string, StepEditorNodeData> nodeMap,
            Dictionary<string, List<string>> incoming,
            Dictionary<string, List<string>> outgoing,
            out float centerY)
        {
            centerY = 0f;
            if (nodesInLayer == null || nodesInLayer.Count == 0)
            {
                return false;
            }

            float sumY = 0f;
            int count = 0;
            for (int i = 0; i < nodesInLayer.Count; i++)
            {
                StepEditorNodeData node = nodesInLayer[i];
                if (node == null || string.IsNullOrEmpty(node.Id))
                {
                    continue;
                }

                CollectNeighborY(node.Id, incoming, nodeMap, ref sumY, ref count);
                CollectNeighborY(node.Id, outgoing, nodeMap, ref sumY, ref count);
            }

            if (count == 0)
            {
                return false;
            }

            centerY = sumY / count;
            return true;
        }

        /// <summary>
        /// 累加邻接节点的 Y 坐标。
        /// </summary>
        private static void CollectNeighborY(
            string nodeId,
            Dictionary<string, List<string>> neighborMap,
            Dictionary<string, StepEditorNodeData> nodeMap,
            ref float sumY,
            ref int count)
        {
            if (string.IsNullOrEmpty(nodeId) || neighborMap == null || nodeMap == null)
            {
                return;
            }

            List<string> neighborIds;
            if (!neighborMap.TryGetValue(nodeId, out neighborIds) || neighborIds == null)
            {
                return;
            }

            for (int i = 0; i < neighborIds.Count; i++)
            {
                StepEditorNodeData neighborNode;
                if (!nodeMap.TryGetValue(neighborIds[i], out neighborNode) || neighborNode == null)
                {
                    continue;
                }

                sumY += neighborNode.Position.y;
                count += 1;
            }
        }

        /// <summary>
        /// 获取当前层的几何中心 Y。
        /// </summary>
        private static float GetLayerCenterY(List<StepEditorNodeData> nodesInLayer)
        {
            if (nodesInLayer == null || nodesInLayer.Count == 0)
            {
                return 0f;
            }

            float firstY = nodesInLayer[0].Position.y;
            float lastY = nodesInLayer[nodesInLayer.Count - 1].Position.y;
            return (firstY + lastY) * 0.5f;
        }

        /// <summary>
        /// 按给定偏移量整体平移一层节点。
        /// </summary>
        private static void ShiftLayer(List<StepEditorNodeData> nodesInLayer, float offsetY)
        {
            if (nodesInLayer == null || Mathf.Approximately(offsetY, 0f))
            {
                return;
            }

            for (int i = 0; i < nodesInLayer.Count; i++)
            {
                StepEditorNodeData node = nodesInLayer[i];
                if (node == null)
                {
                    continue;
                }

                node.Position = new Vector2(node.Position.x, node.Position.y + offsetY);
            }
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
