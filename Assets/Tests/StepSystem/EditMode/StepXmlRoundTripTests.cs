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
        /// 导入 XML 时应读取 graph.id 且兼容 action 内联参数属性。
        /// </summary>
        [Test]
        public void LoadFromText_ShouldReadGraphIdAndInlineActionAttributes()
        {
            string xmlText = "<graph id=\"demo_graph\" start=\"node_start\"><nodes><node id=\"node_start\" name=\"开始\" x=\"0\" y=\"0\"><actions><action type=\"LWStep.StepLogAction\" message=\"hello\" /></actions></node></nodes><edges /></graph>";

            StepEditorGraphData data = StepXmlImporter.LoadFromText(xmlText);

            Assert.IsNotNull(data);
            Assert.AreEqual("demo_graph", data.GraphId);
            Assert.AreEqual("hello", data.Nodes[0].Actions[0].GetParameterValue("message"));
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
    }
}
