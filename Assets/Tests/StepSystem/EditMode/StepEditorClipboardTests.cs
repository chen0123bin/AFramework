using System.Collections.Generic;
using LWStep.Editor;
using NUnit.Framework;
using UnityEngine;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// StepEditorClipboard 复制粘贴测试。
    /// </summary>
    public sealed class StepEditorClipboardTests
    {
        /// <summary>
        /// 粘贴节点应克隆节点并恢复选区内部连线。
        /// </summary>
        [Test]
        public void PasteNodes_ShouldCloneNodeAndInternalEdges()
        {
            StepEditorGraphData data = new StepEditorGraphData();
            data.Nodes.Add(CreateNode("node_a", new Vector2(10f, 20f)));
            data.Nodes.Add(CreateNode("node_b", new Vector2(80f, 20f)));
            data.Edges.Add(CreateEdge("node_a", "node_b"));

            StepEditorClipboardPayload payload = StepEditorClipboard.Copy(data, new List<string> { "node_a", "node_b" });
            StepEditorClipboard.Paste(data, payload, new Vector2(40f, 20f));

            Assert.IsNotNull(data.GetNode("node_a_copy1"));
            Assert.IsNotNull(data.GetNode("node_b_copy1"));
            Assert.IsNotNull(data.GetEdge("node_a_copy1", "node_b_copy1"));
        }

        /// <summary>
        /// 创建测试节点数据。
        /// </summary>
        private static StepEditorNodeData CreateNode(string id, Vector2 position)
        {
            StepEditorNodeData node = new StepEditorNodeData();
            node.Id = id;
            node.Name = id;
            node.Position = position;
            return node;
        }

        /// <summary>
        /// 创建测试连线数据。
        /// </summary>
        private static StepEditorEdgeData CreateEdge(string fromId, string toId)
        {
            StepEditorEdgeData edge = new StepEditorEdgeData();
            edge.FromId = fromId;
            edge.ToId = toId;
            return edge;
        }
    }
}
