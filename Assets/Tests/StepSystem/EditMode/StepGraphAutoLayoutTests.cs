using LWStep.Editor;
using NUnit.Framework;
using UnityEngine;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// StepGraphAutoLayout 自动布局测试。
    /// </summary>
    public sealed class StepGraphAutoLayoutTests
    {
        /// <summary>
        /// 从左到右布局应让主路径节点的 X 坐标递增。
        /// </summary>
        [Test]
        public void ApplyLeftToRight_ShouldIncreaseXAlongMainPath()
        {
            StepEditorGraphData data = new StepEditorGraphData();
            data.Nodes.Add(CreateNode("start", Vector2.zero));
            data.Nodes.Add(CreateNode("middle", Vector2.zero));
            data.Nodes.Add(CreateNode("end", Vector2.zero));
            data.Edges.Add(CreateEdge("start", "middle"));
            data.Edges.Add(CreateEdge("middle", "end"));

            StepGraphAutoLayout.ApplyLeftToRight(data, 240f, 160f);

            float startX = data.GetNode("start").Position.x;
            float middleX = data.GetNode("middle").Position.x;
            float endX = data.GetNode("end").Position.x;

            Assert.That(startX, Is.LessThan(middleX));
            Assert.That(middleX, Is.LessThan(endX));
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
