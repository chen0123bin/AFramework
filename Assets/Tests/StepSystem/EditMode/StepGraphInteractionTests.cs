using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LWStep;
using LWStep.Editor;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// StepGraph 节点交互测试。
    /// </summary>
    public sealed class StepGraphInteractionTests
    {
        /// <summary>
        /// 节点视图重建时应恢复已保存的折叠状态。
        /// </summary>
        [Test]
        public void StepNodeView_WhenCreatedWithCollapsedData_ShouldRestoreCollapsedState()
        {
            StepEditorNodeData node = new StepEditorNodeData();
            node.Id = "node_collapsed";
            node.IsCollapsed = true;

            StepNodeView view = new StepNodeView(node);

            Assert.IsFalse(view.expanded);
        }

        /// <summary>
        /// 切换折叠状态时应同步回写编辑器节点数据。
        /// </summary>
        [Test]
        public void StepNodeView_ToggleCollapsed_ShouldSyncNodeData()
        {
            StepEditorNodeData node = new StepEditorNodeData();
            node.Id = "node_toggle";

            StepNodeView view = new StepNodeView(node);
            MethodInfo toggleMethod = typeof(StepNodeView).GetMethod("ToggleCollapsed", BindingFlags.Instance | BindingFlags.Public);

            Assert.IsNotNull(toggleMethod);
            toggleMethod.Invoke(view, null);

            Assert.IsTrue(node.IsCollapsed);
            Assert.IsFalse(view.expanded);
        }

        /// <summary>
        /// 图节点端口应挂载拖拽连线所需的连接器，并只返回合法目标端口。
        /// </summary>
        [Test]
        public void StepGraphView_NodePorts_ShouldSupportDragConnection()
        {
            StepGraphView graphView = CreateGraphView();
            StepNodeView fromView = graphView.AddNode(new Vector2(0.0f, 0.0f));
            StepNodeView toView = graphView.AddNode(new Vector2(240.0f, 0.0f));

            FieldInfo inputConnectorField = typeof(StepNodeView).GetField("m_InputEdgeConnector", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo outputConnectorField = typeof(StepNodeView).GetField("m_OutputEdgeConnector", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(inputConnectorField);
            Assert.IsNotNull(outputConnectorField);
            Assert.IsNotNull(inputConnectorField.GetValue(toView));
            Assert.IsNotNull(outputConnectorField.GetValue(fromView));

            List<Port> compatiblePorts = graphView.GetCompatiblePorts(fromView.OutputPort, null);
            CollectionAssert.Contains(compatiblePorts, toView.InputPort);
            CollectionAssert.DoesNotContain(compatiblePorts, fromView.InputPort);
        }

        /// <summary>
        /// 框选后的选中节点应支持批量折叠。
        /// </summary>
        [Test]
        public void StepGraphView_CollapseSelectedNodes_ShouldCollapseAllSelectedNodes()
        {
            StepGraphView graphView = CreateGraphView();
            StepNodeView firstView = graphView.AddNode(new Vector2(0.0f, 0.0f));
            StepNodeView secondView = graphView.AddNode(new Vector2(240.0f, 0.0f));

            graphView.AddToSelection(firstView);
            graphView.AddToSelection(secondView);

            int changedCount = graphView.CollapseSelectedNodes();

            Assert.AreEqual(2, changedCount);
            Assert.IsTrue(firstView.Data.IsCollapsed);
            Assert.IsTrue(secondView.Data.IsCollapsed);
            Assert.IsFalse(firstView.expanded);
            Assert.IsFalse(secondView.expanded);
        }

        /// <summary>
        /// 已折叠的选中节点应支持批量展开。
        /// </summary>
        [Test]
        public void StepGraphView_ExpandSelectedNodes_ShouldExpandAllSelectedNodes()
        {
            StepGraphView graphView = CreateGraphView();
            StepNodeView firstView = graphView.AddNode(new Vector2(0.0f, 0.0f));
            StepNodeView secondView = graphView.AddNode(new Vector2(240.0f, 0.0f));
            firstView.ToggleCollapsed();
            secondView.ToggleCollapsed();

            graphView.AddToSelection(firstView);
            graphView.AddToSelection(secondView);

            int changedCount = graphView.ExpandSelectedNodes();

            Assert.AreEqual(2, changedCount);
            Assert.IsFalse(firstView.Data.IsCollapsed);
            Assert.IsFalse(secondView.Data.IsCollapsed);
            Assert.IsTrue(firstView.expanded);
            Assert.IsTrue(secondView.expanded);
        }

        /// <summary>
        /// 全图批量折叠应作用于当前图中的所有节点。
        /// </summary>
        [Test]
        public void StepGraphView_CollapseAllNodes_ShouldCollapseEveryNode()
        {
            StepGraphView graphView = CreateGraphView();
            StepNodeView firstView = graphView.AddNode(new Vector2(0.0f, 0.0f));
            StepNodeView secondView = graphView.AddNode(new Vector2(240.0f, 0.0f));
            StepNodeView thirdView = graphView.AddNode(new Vector2(480.0f, 0.0f));

            int changedCount = graphView.CollapseAllNodes();

            Assert.AreEqual(3, changedCount);
            Assert.IsTrue(firstView.Data.IsCollapsed);
            Assert.IsTrue(secondView.Data.IsCollapsed);
            Assert.IsTrue(thirdView.Data.IsCollapsed);
            Assert.IsFalse(firstView.expanded);
            Assert.IsFalse(secondView.expanded);
            Assert.IsFalse(thirdView.expanded);
        }

        /// <summary>
        /// 全图批量展开应恢复当前图中的所有节点。
        /// </summary>
        [Test]
        public void StepGraphView_ExpandAllNodes_ShouldExpandEveryNode()
        {
            StepGraphView graphView = CreateGraphView();
            StepNodeView firstView = graphView.AddNode(new Vector2(0.0f, 0.0f));
            StepNodeView secondView = graphView.AddNode(new Vector2(240.0f, 0.0f));
            StepNodeView thirdView = graphView.AddNode(new Vector2(480.0f, 0.0f));
            graphView.CollapseAllNodes();

            int changedCount = graphView.ExpandAllNodes();

            Assert.AreEqual(3, changedCount);
            Assert.IsFalse(firstView.Data.IsCollapsed);
            Assert.IsFalse(secondView.Data.IsCollapsed);
            Assert.IsFalse(thirdView.Data.IsCollapsed);
            Assert.IsTrue(firstView.expanded);
            Assert.IsTrue(secondView.expanded);
            Assert.IsTrue(thirdView.expanded);
        }

        /// <summary>
        /// 图视图应在节点加入 Graph 后再同步折叠态，避免导入时端口行被错误隐藏。
        /// </summary>
        [Test]
        public void StepGraphView_ShouldSyncCollapsedStateAfterNodeAdded()
        {
            MethodInfo syncCollapsedState = typeof(StepNodeView).GetMethod("SyncCollapsedStateFromData", BindingFlags.Instance | BindingFlags.Public);

            Assert.IsNotNull(syncCollapsedState);

            string source = ReadStepGraphViewSource();
            StringAssert.Contains("AddElement(nodeView);", source);
            StringAssert.Contains("nodeView.SyncCollapsedStateFromData();", source);

            string nodeViewSource = ReadStepNodeViewSource();
            StringAssert.Contains("if (expanded == targetExpanded)", nodeViewSource);
        }

        /// <summary>
        /// 创建测试用图视图。
        /// </summary>
        private static StepGraphView CreateGraphView()
        {
            StepEditorGraphData data = new StepEditorGraphData();
            return new StepGraphView(data);
        }

        /// <summary>
        /// 读取 StepGraphView 源码文本，用于轻量契约验证。
        /// </summary>
        private static string ReadStepGraphViewSource()
        {
            string root = TestContext.CurrentContext.TestDirectory;
            for (int i = 0; i < 8; i++)
            {
                string candidate = Path.Combine(root, "Assets", "LWFramework", "Editor", "StepSystem", "GraphView", "StepGraphView.cs");
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }

                DirectoryInfo parent = Directory.GetParent(root);
                if (parent == null)
                {
                    break;
                }

                root = parent.FullName;
            }

            Assert.Fail("未找到 StepGraphView.cs，无法执行契约验证。");
            return string.Empty;
        }

        /// <summary>
        /// 读取 StepNodeView 源码文本，用于轻量契约验证。
        /// </summary>
        private static string ReadStepNodeViewSource()
        {
            string root = TestContext.CurrentContext.TestDirectory;
            for (int i = 0; i < 8; i++)
            {
                string candidate = Path.Combine(root, "Assets", "LWFramework", "Editor", "StepSystem", "GraphView", "StepNodeView.cs");
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }

                DirectoryInfo parent = Directory.GetParent(root);
                if (parent == null)
                {
                    break;
                }

                root = parent.FullName;
            }

            Assert.Fail("未找到 StepNodeView.cs，无法执行契约验证。");
            return string.Empty;
        }
    }
}
