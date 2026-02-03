using System;
using System.Collections.Generic;

namespace LWStep
{
    /// <summary>
    /// 步骤图（DAG）
    /// </summary>
    public class StepGraph
    {
        public string StartNodeId { get; private set; }

        public string Name { get; private set; }
        private Dictionary<string, StepNode> m_Nodes;
        private List<StepEdge> m_Edges;
        private Dictionary<string, List<StepEdge>> m_OutgoingEdges;

        /// <summary>
        /// 创建步骤图
        /// </summary>
        public StepGraph(string startNodeId, string name)
        {
            StartNodeId = startNodeId;
            Name = name;
            m_Nodes = new Dictionary<string, StepNode>();
            m_Edges = new List<StepEdge>();
            m_OutgoingEdges = new Dictionary<string, List<StepEdge>>();
        }

        /// <summary>
        /// 获取图内所有节点快照（用于基线捕获与恢复）
        /// </summary>
        public List<StepNode> GetAllNodes()
        {
            List<StepNode> nodes = new List<StepNode>(m_Nodes.Count);
            foreach (KeyValuePair<string, StepNode> kvp in m_Nodes)
            {
                nodes.Add(kvp.Value);
            }
            return nodes;
        }
        /// <summary>
        /// 获取图内所有显示节点（排除隐藏节点）
        /// </summary>
        public List<StepNode> GetAllDisplayNodes()
        {
            List<StepNode> nodes = new List<StepNode>();
            //获取第一个node，并递归获取后面所有的node
            if (m_Nodes.TryGetValue(StartNodeId, out StepNode startNode))
            {
                nodes.Add(startNode);
                AddDisplayNode(startNode.Id, nodes);
            }
            return nodes;
        }
        private void AddDisplayNode(string nodeId, List<StepNode> nodes)
        {
            List<string> nextNodeIds = GetNextNodeIds(nodeId);
            if (nextNodeIds.Count == 0)
            {
                return;
            }
            string nextNodeId = nextNodeIds[0];
            StepNode node = GetNode(nextNodeId);
            if (node != null)
            {
                nodes.Add(node);
                AddDisplayNode(node.Id, nodes);
            }
        }

        public void ResetNodeStatuses()
        {
            foreach (KeyValuePair<string, StepNode> kvp in m_Nodes)
            {
                kvp.Value.ResetStatus();
            }
        }

        /// <summary>
        /// 添加节点
        /// </summary>
        public bool AddNode(StepNode node)
        {
            if (node == null)
            {
                return false;
            }
            if (m_Nodes.ContainsKey(node.Id))
            {
                return false;
            }
            m_Nodes.Add(node.Id, node);
            return true;
        }

        /// <summary>
        /// 添加连线
        /// </summary>
        public void AddEdge(StepEdge edge)
        {
            if (edge == null)
            {
                return;
            }
            m_Edges.Add(edge);
        }

        /// <summary>
        /// 构建索引
        /// </summary>
        public void BuildIndex()
        {
            m_OutgoingEdges.Clear();
            for (int i = 0; i < m_Edges.Count; i++)
            {
                StepEdge edge = m_Edges[i];
                List<StepEdge> list;
                if (!m_OutgoingEdges.TryGetValue(edge.FromId, out list))
                {
                    list = new List<StepEdge>();
                    m_OutgoingEdges.Add(edge.FromId, list);
                }
                list.Add(edge);
            }
        }

        /// <summary>
        /// 获取节点
        /// </summary>
        public StepNode GetNode(string nodeId)
        {
            StepNode node;
            if (m_Nodes.TryGetValue(nodeId, out node))
            {
                return node;
            }
            return null;
        }

        /// <summary>
        /// 获取可前进节点集合（按优先级排序）
        /// </summary>
        public List<string> GetNextNodeIds(string nodeId)
        {
            List<string> result = new List<string>();
            List<StepEdge> list;
            if (!m_OutgoingEdges.TryGetValue(nodeId, out list))
            {
                return result;
            }
            list.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            for (int i = 0; i < list.Count; i++)
            {
                result.Add(list[i].ToId);
            }
            return result;
        }
        /// <summary>
        /// 获取可前进节点集合（按优先级排序），并根据上下文和标签筛选有效节点
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public List<string> GetNextNodeIds(string nodeId, StepContext context)
        {
            List<string> result = new List<string>();
            List<StepEdge> list;
            if (!m_OutgoingEdges.TryGetValue(nodeId, out list))
            {
                return result;
            }

            List<StepEdge> candidates = new List<StepEdge>();
            for (int i = 0; i < list.Count; i++)
            {
                StepEdge edge = list[i];
                if (!edge.IsConditionMatched(context))
                {
                    continue;
                }
                candidates.Add(edge);
            }

            candidates.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            for (int i = 0; i < candidates.Count; i++)
            {
                result.Add(candidates[i].ToId);
            }
            return result;
        }

        /// <summary>
        /// 查找从起点到终点的最短路径（考虑上下文和标签）
        /// </summary>
        /// <param name="fromId"></param>
        /// <param name="toId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public List<string> FindPath(string fromId, string toId, StepContext context = null)
        {
            Queue<string> queue = new Queue<string>();
            Dictionary<string, string> prev = new Dictionary<string, string>();
            queue.Enqueue(fromId);
            prev.Add(fromId, string.Empty);

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                if (current == toId)
                {
                    break;
                }
                List<string> nextNodes = GetNextNodeIds(current, context);
                for (int i = 0; i < nextNodes.Count; i++)
                {
                    string next = nextNodes[i];
                    if (prev.ContainsKey(next))
                    {
                        continue;
                    }
                    prev.Add(next, current);
                    queue.Enqueue(next);
                }
            }

            if (!prev.ContainsKey(toId))
            {
                return null;
            }

            // 关键：从终点回溯路径
            List<string> path = new List<string>();
            string node = toId;
            while (!string.IsNullOrEmpty(node))
            {
                path.Add(node);
                node = prev[node];
            }
            path.Reverse();
            return path;
        }

        /// <summary>
        /// 校验步骤图
        /// </summary>
        public List<string> Validate()
        {
            List<string> errors = new List<string>();
            if (m_Nodes.Count == 0)
            {
                errors.Add("步骤图节点为空");
                return errors;
            }

            for (int i = 0; i < m_Edges.Count; i++)
            {
                StepEdge edge = m_Edges[i];
                if (!m_Nodes.ContainsKey(edge.FromId))
                {
                    errors.Add("连线起点不存在: " + edge.FromId);
                }
                if (!m_Nodes.ContainsKey(edge.ToId))
                {
                    errors.Add("连线终点不存在: " + edge.ToId);
                }
            }

            if (HasCycle())
            {
                errors.Add("步骤图存在环路");
            }
            return errors;
        }

        private bool HasCycle()
        {
            Dictionary<string, int> indegree = new Dictionary<string, int>();
            foreach (KeyValuePair<string, StepNode> kvp in m_Nodes)
            {
                indegree.Add(kvp.Key, 0);
            }
            for (int i = 0; i < m_Edges.Count; i++)
            {
                StepEdge edge = m_Edges[i];
                if (indegree.ContainsKey(edge.ToId))
                {
                    indegree[edge.ToId] += 1;
                }
            }

            Queue<string> queue = new Queue<string>();
            foreach (KeyValuePair<string, int> kvp in indegree)
            {
                if (kvp.Value == 0)
                {
                    queue.Enqueue(kvp.Key);
                }
            }

            int visited = 0;
            while (queue.Count > 0)
            {
                string nodeId = queue.Dequeue();
                visited += 1;
                List<StepEdge> list;
                if (!m_OutgoingEdges.TryGetValue(nodeId, out list))
                {
                    continue;
                }
                for (int i = 0; i < list.Count; i++)
                {
                    StepEdge edge = list[i];
                    indegree[edge.ToId] -= 1;
                    if (indegree[edge.ToId] == 0)
                    {
                        queue.Enqueue(edge.ToId);
                    }
                }
            }
            return visited != m_Nodes.Count;
        }
    }
}

