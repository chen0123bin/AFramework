using LWStep;
using LWStep.Editor;
using NUnit.Framework;
using UnityEngine;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// StepSystem XML 导入导出归一化回归测试。
    /// </summary>
    public sealed class StepXmlRoundTripTests
    {
        /// <summary>
        /// 导入 XML 时应读取 graph.id，兼容 action 内联参数属性，并兼容缺失 collapsed 属性。
        /// </summary>
        [Test]
        public void LoadFromText_ShouldReadGraphIdAndInlineActionAttributes()
        {
            string xmlText = "<graph id=\"demo_graph\" start=\"node_start\"><nodes><node id=\"node_start\" name=\"开始\" x=\"0\" y=\"0\"><actions><action type=\"LWStep.StepLogAction\" message=\"hello\" /></actions></node></nodes><edges /></graph>";

            StepEditorGraphData data = StepXmlImporter.LoadFromText(xmlText);

            Assert.IsNotNull(data);
            Assert.AreEqual("demo_graph", data.GraphId);
            Assert.AreEqual("hello", data.Nodes[0].Actions[0].GetParameterValue("message"));
            Assert.IsFalse(data.Nodes[0].IsCollapsed);
        }

        /// <summary>
        /// 导出 XML 时应总是以 param 节点写出动作参数。
        /// </summary>
        [Test]
        public void ExportToText_ShouldAlwaysWriteParamNodes()
        {
            StepEditorGraphData data = new StepEditorGraphData();
            data.GraphId = "demo_graph";
            data.StartNodeId = "node_start";
            data.Nodes.Add(CreateNodeWithLogAction("node_start", "hello"));

            string xmlText = StepXmlExporter.ExportToText(data);

            StringAssert.Contains("<graph id=\"demo_graph\" start=\"node_start\">", xmlText);
            StringAssert.Contains("<param key=\"message\" value=\"hello\" />", xmlText);
            StringAssert.DoesNotContain("message=\"hello\"", xmlText);
        }

        /// <summary>
        /// action 同时存在内联属性与 param 同键时，应以 param 为准。
        /// </summary>
        [Test]
        public void LoadFromText_WhenInlineAndParamHaveSameKey_ShouldUseParamValue()
        {
            string xmlText = "<graph id=\"demo_graph\" start=\"node_start\"><nodes><node id=\"node_start\" name=\"开始\" x=\"0\" y=\"0\"><actions><action type=\"LWStep.StepLogAction\" message=\"inline\"><param key=\"message\" value=\"from_param\" /></action></actions></node></nodes><edges /></graph>";

            StepEditorGraphData data = StepXmlImporter.LoadFromText(xmlText);

            Assert.IsNotNull(data);
            Assert.AreEqual("from_param", data.Nodes[0].Actions[0].GetParameterValue("message"));
        }

        /// <summary>
        /// 历史脏数据存在重复参数键时，归一化后导出不应包含重复 param。
        /// </summary>
        [Test]
        public void ExportToText_WhenDuplicateKeysExist_ShouldNormalizeToSingleParam()
        {
            StepEditorGraphData data = new StepEditorGraphData();
            data.GraphId = "demo_graph";
            data.StartNodeId = "node_start";

            StepEditorNodeData node = new StepEditorNodeData();
            node.Id = "node_start";
            node.Name = "node_start";
            node.Position = Vector2.zero;

            StepEditorActionData action = new StepEditorActionData();
            action.TypeName = typeof(StepLogAction).FullName;
            action.Parameters.Add(CreateParameter("message", "legacy_a"));
            action.Parameters.Add(CreateParameter("message", "legacy_b"));
            action.SetParameterValue("message", "hello");
            node.Actions.Add(action);
            data.Nodes.Add(node);

            string xmlText = StepXmlExporter.ExportToText(data);

            StringAssert.Contains("<param key=\"message\" value=\"hello\" />", xmlText);
            Assert.AreEqual(1, CountOccurrences(xmlText, "<param key=\"message\""));
        }

        /// <summary>
        /// 导出再导入后应恢复节点折叠状态。
        /// </summary>
        [Test]
        public void ExportAndLoadCollapsed_ShouldRoundTripNodeCollapsedState()
        {
            StepEditorGraphData data = new StepEditorGraphData();
            data.GraphId = "demo_graph";
            data.StartNodeId = "node_a";

            StepEditorNodeData nodeA = CreateNodeWithLogAction("node_a", "A");
            nodeA.IsCollapsed = true;
            data.Nodes.Add(nodeA);

            StepEditorNodeData nodeB = CreateNodeWithLogAction("node_b", "B");
            nodeB.IsCollapsed = false;
            data.Nodes.Add(nodeB);

            string xmlText = StepXmlExporter.ExportToText(data);

            StringAssert.Contains("collapsed=\"true\"", xmlText);
            StringAssert.Contains("collapsed=\"false\"", xmlText);

            StepEditorGraphData loadedData = StepXmlImporter.LoadFromText(xmlText);

            Assert.IsNotNull(loadedData);
            Assert.IsTrue(loadedData.GetNode("node_a").IsCollapsed);
            Assert.IsFalse(loadedData.GetNode("node_b").IsCollapsed);
        }

        /// <summary>
        /// 创建包含日志动作的节点数据。
        /// </summary>
        private static StepEditorNodeData CreateNodeWithLogAction(string nodeId, string message)
        {
            StepEditorNodeData node = new StepEditorNodeData();
            node.Id = nodeId;
            node.Name = nodeId;
            node.Position = Vector2.zero;

            StepEditorActionData action = new StepEditorActionData();
            action.TypeName = typeof(StepLogAction).FullName;
            action.SetParameterValue("message", message);
            node.Actions.Add(action);
            return node;
        }

        /// <summary>
        /// 创建测试参数数据。
        /// </summary>
        private static StepEditorParameterData CreateParameter(string key, string value)
        {
            StepEditorParameterData parameter = new StepEditorParameterData();
            parameter.Key = key;
            parameter.Value = value;
            return parameter;
        }

        /// <summary>
        /// 统计字符串中目标片段出现次数。
        /// </summary>
        private static int CountOccurrences(string text, string token)
        {
            int count = 0;
            int index = 0;
            while (index >= 0)
            {
                index = text.IndexOf(token, index, System.StringComparison.Ordinal);
                if (index < 0)
                {
                    break;
                }
                count++;
                index += token.Length;
            }
            return count;
        }
    }
}
