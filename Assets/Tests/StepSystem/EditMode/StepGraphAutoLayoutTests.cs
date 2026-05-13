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
        /// 从左到右布局时，同层节点应按层内顺序分配 Y 坐标。
        /// </summary>
        [Test]
        public void ApplyLeftToRight_ShouldAssignYByOrderWithinLayer()
        {
            StepEditorGraphData data = new StepEditorGraphData();
            data.Nodes.Add(CreateNode("start", new Vector2(0f, 0f)));
            data.Nodes.Add(CreateNode("branch_b", new Vector2(0f, 100f)));
            data.Nodes.Add(CreateNode("branch_a", new Vector2(0f, 300f)));
            data.Edges.Add(CreateEdge("start", "branch_a"));
            data.Edges.Add(CreateEdge("start", "branch_b"));

            StepGraphAutoLayout.ApplyLeftToRight(data, 240f, 160f);

            StepEditorNodeData branchA = data.GetNode("branch_a");
            StepEditorNodeData branchB = data.GetNode("branch_b");
            Assert.IsNotNull(branchA);
            Assert.IsNotNull(branchB);
            Assert.That(branchA.Position.x, Is.EqualTo(240f));
            Assert.That(branchB.Position.x, Is.EqualTo(240f));
            Assert.That(branchB.Position.y, Is.LessThan(branchA.Position.y));
            Assert.That(branchB.Position.y, Is.EqualTo(0f));
            Assert.That(branchA.Position.y, Is.EqualTo(160f));
        }

        /// <summary>
        /// 同层节点原始 Y 相同场景下，应按节点 ID 进行稳定兜底排序。
        /// </summary>
        [Test]
        public void ApplyLeftToRight_WhenLayerYEqual_ShouldFallbackToIdOrder()
        {
            StepEditorGraphData data = new StepEditorGraphData();
            data.Nodes.Add(CreateNode("start", new Vector2(0f, 0f)));
            data.Nodes.Add(CreateNode("node_b", new Vector2(0f, 100f)));
            data.Nodes.Add(CreateNode("node_a", new Vector2(0f, 100f)));
            data.Edges.Add(CreateEdge("start", "node_b"));
            data.Edges.Add(CreateEdge("start", "node_a"));

            StepGraphAutoLayout.ApplyLeftToRight(data, 240f, 160f);

            StepEditorNodeData nodeA = data.GetNode("node_a");
            StepEditorNodeData nodeB = data.GetNode("node_b");
            Assert.IsNotNull(nodeA);
            Assert.IsNotNull(nodeB);
            Assert.That(nodeA.Position.x, Is.EqualTo(240f));
            Assert.That(nodeB.Position.x, Is.EqualTo(240f));
            Assert.That(nodeA.Position.y, Is.EqualTo(0f));
            Assert.That(nodeB.Position.y, Is.EqualTo(160f));
        }

        /// <summary>
        /// 单根双分支场景下，根节点应位于两个子节点的垂直中线附近。
        /// </summary>
        [Test]
        public void ApplyLeftToRight_ShouldCenterRootBetweenBranches()
        {
            StepEditorGraphData data = new StepEditorGraphData();
            data.StartNodeId = "start";
            data.Nodes.Add(CreateNode("start", new Vector2(0f, 0f)));
            data.Nodes.Add(CreateNode("branch_top", new Vector2(0f, 0f)));
            data.Nodes.Add(CreateNode("branch_bottom", new Vector2(0f, 300f)));
            data.Edges.Add(CreateEdge("start", "branch_top"));
            data.Edges.Add(CreateEdge("start", "branch_bottom"));

            StepGraphAutoLayout.ApplyLeftToRight(data, 240f, 160f);

            StepEditorNodeData start = data.GetNode("start");
            StepEditorNodeData branchTop = data.GetNode("branch_top");
            StepEditorNodeData branchBottom = data.GetNode("branch_bottom");

            Assert.IsNotNull(start);
            Assert.IsNotNull(branchTop);
            Assert.IsNotNull(branchBottom);

            float childrenCenterY = (branchTop.Position.y + branchBottom.Position.y) * 0.5f;
            Assert.That(start.Position.y, Is.EqualTo(childrenCenterY).Within(0.001f));
        }

        /// <summary>
        /// 双分支汇合场景下，汇合节点应位于两个父节点的垂直中线附近。
        /// </summary>
        [Test]
        public void ApplyLeftToRight_ShouldCenterMergeNodeBetweenIncomingBranches()
        {
            StepEditorGraphData data = new StepEditorGraphData();
            data.StartNodeId = "start";
            data.Nodes.Add(CreateNode("start", new Vector2(0f, 0f)));
            data.Nodes.Add(CreateNode("branch_top", new Vector2(0f, 0f)));
            data.Nodes.Add(CreateNode("branch_bottom", new Vector2(0f, 300f)));
            data.Nodes.Add(CreateNode("merge", new Vector2(0f, 0f)));
            data.Edges.Add(CreateEdge("start", "branch_top"));
            data.Edges.Add(CreateEdge("start", "branch_bottom"));
            data.Edges.Add(CreateEdge("branch_top", "merge"));
            data.Edges.Add(CreateEdge("branch_bottom", "merge"));

            StepGraphAutoLayout.ApplyLeftToRight(data, 240f, 160f);

            StepEditorNodeData merge = data.GetNode("merge");
            StepEditorNodeData branchTop = data.GetNode("branch_top");
            StepEditorNodeData branchBottom = data.GetNode("branch_bottom");

            Assert.IsNotNull(merge);
            Assert.IsNotNull(branchTop);
            Assert.IsNotNull(branchBottom);

            float parentCenterY = (branchTop.Position.y + branchBottom.Position.y) * 0.5f;
            Assert.That(merge.Position.y, Is.EqualTo(parentCenterY).Within(0.001f));
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
