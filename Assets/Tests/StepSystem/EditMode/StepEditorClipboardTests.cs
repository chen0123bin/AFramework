using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LWStep.Editor;
using LWStep;
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

            string copiedAId = FindSingleNodeIdByPrefix(data, "node_a_copy");
            string copiedBId = FindSingleNodeIdByPrefix(data, "node_b_copy");
            Assert.IsNotNull(data.GetNode(copiedAId));
            Assert.IsNotNull(data.GetNode(copiedBId));
            Assert.IsNotNull(data.GetEdge(copiedAId, copiedBId));
        }

        /// <summary>
        /// 粘贴时应仅恢复选区内部连线，不应带入外部连线。
        /// </summary>
        [Test]
        public void PasteNodes_ShouldNotCloneExternalEdges()
        {
            StepEditorGraphData data = new StepEditorGraphData();
            data.Nodes.Add(CreateNode("node_a", new Vector2(0f, 0f)));
            data.Nodes.Add(CreateNode("node_b", new Vector2(80f, 0f)));
            data.Nodes.Add(CreateNode("node_c", new Vector2(160f, 0f)));
            data.Edges.Add(CreateEdge("node_a", "node_b")); // 内部连线
            data.Edges.Add(CreateEdge("node_b", "node_c")); // 选区到外部
            data.Edges.Add(CreateEdge("node_c", "node_a")); // 外部到选区

            StepEditorClipboardPayload payload = StepEditorClipboard.Copy(data, new List<string> { "node_a", "node_b" });
            StepEditorClipboard.Paste(data, payload, new Vector2(40f, 20f));

            string copiedAId = FindSingleNodeIdByPrefix(data, "node_a_copy");
            string copiedBId = FindSingleNodeIdByPrefix(data, "node_b_copy");
            Assert.IsNotNull(data.GetEdge(copiedAId, copiedBId));
            Assert.IsNull(data.GetEdge(copiedBId, "node_c"));
            Assert.IsNull(data.GetEdge("node_c", copiedAId));
        }

        /// <summary>
        /// 复制后修改原始动作和参数时，粘贴结果不应被联动污染。
        /// </summary>
        [Test]
        public void CopyThenMutateSource_ShouldKeepPayloadAndPastedActionParameterIsolated()
        {
            StepEditorGraphData data = new StepEditorGraphData();
            StepEditorNodeData nodeA = CreateNode("node_a", new Vector2(10f, 20f));
            StepEditorNodeData nodeB = CreateNode("node_b", new Vector2(80f, 20f));
            nodeA.Actions.Add(CreateAction("type_before", "key_before", "value_before"));
            data.Nodes.Add(nodeA);
            data.Nodes.Add(nodeB);
            data.Edges.Add(CreateEdge("node_a", "node_b"));

            StepEditorClipboardPayload payload = StepEditorClipboard.Copy(data, new List<string> { "node_a", "node_b" });

            nodeA.Actions[0].TypeName = "type_after";
            nodeA.Actions[0].Parameters[0].Key = "key_after";
            nodeA.Actions[0].Parameters[0].Value = "value_after";

            StepEditorClipboard.Paste(data, payload, new Vector2(40f, 20f));
            string copiedAId = FindSingleNodeIdByPrefix(data, "node_a_copy");
            StepEditorNodeData copiedNodeA = data.GetNode(copiedAId);
            Assert.IsNotNull(copiedNodeA);
            Assert.That(copiedNodeA.Actions.Count, Is.EqualTo(1));
            Assert.That(copiedNodeA.Actions[0].TypeName, Is.EqualTo("type_before"));
            Assert.That(copiedNodeA.Actions[0].Parameters.Count, Is.EqualTo(1));
            Assert.That(copiedNodeA.Actions[0].Parameters[0].Key, Is.EqualTo("key_before"));
            Assert.That(copiedNodeA.Actions[0].Parameters[0].Value, Is.EqualTo("value_before"));
        }

        /// <summary>
        /// 动作类型展示文本应走“分类/显示名”而非裸类型名。
        /// </summary>
        [Test]
        public void DrawActionTypeSelector_ShouldUseCategoryAndDisplayNameContract()
        {
            MethodInfo displayNameMethod = typeof(StepEditorWindow).GetMethod("BuildActionTypeDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(displayNameMethod);

            object result = displayNameMethod.Invoke(null, new object[] { typeof(StepLogAction) });
            string displayName = result as string;
            Assert.AreEqual("调试/输出日志", displayName);
            Assert.AreNotEqual(typeof(StepLogAction).FullName, displayName);
        }

        /// <summary>
        /// 节点动作 Inspector 应包含上移/下移按钮接线。
        /// </summary>
        [Test]
        public void DrawNodeInspector_ShouldContainMoveButtonsContract()
        {
            string source = ReadStepEditorWindowSource();
            StringAssert.Contains("GUILayout.Button(\"上移\")", source);
            StringAssert.Contains("GUILayout.Button(\"下移\")", source);
        }

        /// <summary>
        /// 重复与自动布局应接入刷新与选中恢复链路。
        /// </summary>
        [Test]
        public void DuplicateAndAutoLayout_ShouldWireRefreshAndSelectionContracts()
        {
            MethodInfo duplicateMethod = typeof(StepEditorWindow).GetMethod("OnDuplicateSelection", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo autoLayoutMethod = typeof(StepEditorWindow).GetMethod("OnAutoLayout", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo getSelectedMethod = typeof(StepGraphView).GetMethod("GetSelectedNodeIds", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo selectNodesMethod = typeof(StepGraphView).GetMethod("SelectNodes", BindingFlags.Public | BindingFlags.Instance);

            Assert.IsNotNull(duplicateMethod);
            Assert.IsNotNull(autoLayoutMethod);
            Assert.IsNotNull(getSelectedMethod);
            Assert.IsNotNull(selectNodesMethod);

            string source = ReadStepEditorWindowSource();
            StringAssert.Contains("m_GraphView.RebuildView();", source);
            StringAssert.Contains("m_GraphView.SelectNodes(pastedNodeIds);", source);
            StringAssert.Contains("StepGraphAutoLayout.ApplyLeftToRight(m_Data, 240f, 160f);", source);
        }

        /// <summary>
        /// 读取 StepEditorWindow 源码文本，用于轻量契约验证。
        /// </summary>
        private static string ReadStepEditorWindowSource()
        {
            string root = TestContext.CurrentContext.TestDirectory;
            for (int i = 0; i < 8; i++)
            {
                string candidate = Path.Combine(root, "Assets", "LWFramework", "Editor", "StepSystem", "StepEditorWindow.cs");
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

            Assert.Fail("未找到 StepEditorWindow.cs，无法执行契约验证。");
            return string.Empty;
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

        /// <summary>
        /// 创建动作与单参数数据。
        /// </summary>
        private static StepEditorActionData CreateAction(string typeName, string key, string value)
        {
            StepEditorActionData action = new StepEditorActionData();
            action.TypeName = typeName;
            StepEditorParameterData parameter = new StepEditorParameterData();
            parameter.Key = key;
            parameter.Value = value;
            action.Parameters.Add(parameter);
            return action;
        }

        /// <summary>
        /// 按节点 ID 前缀查找唯一匹配节点。
        /// </summary>
        private static string FindSingleNodeIdByPrefix(StepEditorGraphData data, string prefix)
        {
            List<string> ids = new List<string>();
            for (int i = 0; i < data.Nodes.Count; i++)
            {
                StepEditorNodeData node = data.Nodes[i];
                if (node == null || string.IsNullOrEmpty(node.Id))
                {
                    continue;
                }
                if (!node.Id.StartsWith(prefix))
                {
                    continue;
                }

                ids.Add(node.Id);
            }

            Assert.That(ids.Count, Is.EqualTo(1), "前缀 " + prefix + " 匹配节点数应为 1。");
            return ids[0];
        }
    }
}
