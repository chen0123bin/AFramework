using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using LWStep;
using LWStep.Editor;
using NUnit.Framework;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// 运行时联调轨迹追踪器测试。
    /// </summary>
    public sealed class StepRuntimeDebugTrackerTests
    {
        /// <summary>
        /// 创建快照时应暴露轨迹、当前动作、最近事件与上下文值。
        /// </summary>
        [Test]
        public void CreateSnapshot_ShouldExposeTrailCurrentActionAndEvents()
        {
            StepContext context = new StepContext();
            StepRuntimeDebugTracker tracker = new StepRuntimeDebugTracker();

            context.SetValue("mode", "A");
            context.SetValue("score", 5);
            tracker.RecordNodeEnter("node_start");
            tracker.RecordActionChanged("StepMoveObjectAction");
            tracker.RecordNodeEnter("node_middle");
            tracker.RecordJump("node_end");

            StepRuntimeDebugSnapshot snapshot = tracker.CreateSnapshot(context, "node_middle", "StepMoveObjectAction");

            Assert.AreEqual("node_middle", snapshot.CurrentNodeId);
            Assert.AreEqual("StepMoveObjectAction", snapshot.CurrentActionName);
            CollectionAssert.Contains(snapshot.TrailNodeIds, "node_start");
            CollectionAssert.Contains(snapshot.TrailNodeIds, "node_middle");
            Assert.AreEqual("A", snapshot.ContextValues["mode"]);
            Assert.AreEqual("5", snapshot.ContextValues["score"]);
            Assert.AreEqual("Jump:node_end", snapshot.RecentEvents[snapshot.RecentEvents.Count - 1]);
        }

        /// <summary>
        /// 调试面板缺少 ContextJson 段时不应把整段头文本当作上下文 JSON 回写。
        /// </summary>
        [Test]
        public void ExtractContextJsonText_WithoutContextSection_ShouldReturnEmpty()
        {
            object window = RuntimeHelpers.GetUninitializedObject(typeof(StepEditorWindow));
            MethodInfo extractMethod = typeof(StepEditorWindow).GetMethod("ExtractContextJsonText", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(extractMethod);

            string panelText = "CurrentNode: node_middle\nCurrentAction: StepMoveObjectAction";
            string contextJson = extractMethod.Invoke(window, new object[] { panelText }) as string;

            Assert.AreEqual(string.Empty, contextJson);
        }

        /// <summary>
        /// 命中历史回跳分支时也应记录 Jump 事件，保持 JumpTo 语义一致。
        /// </summary>
        [Test]
        public void GetRuntimeDebugSnapshot_AfterJumpToHistoryNode_ShouldContainJumpEvent()
        {
            StepManager stepManager = CreateStepManagerWithLinearGraph();

            stepManager.Start("debug_graph");
            stepManager.Forward();
            stepManager.Forward();
            stepManager.JumpTo("node_middle");

            StepRuntimeDebugSnapshot snapshot = stepManager.GetRuntimeDebugSnapshot();

            Assert.AreEqual("node_middle", snapshot.CurrentNodeId);
            CollectionAssert.Contains(snapshot.RecentEvents, "Jump:node_middle");
        }

        /// <summary>
        /// 创建线性三节点图并注入到 StepManager，便于覆盖运行时跳转行为。
        /// </summary>
        private static StepManager CreateStepManagerWithLinearGraph()
        {
            StepManager stepManager = new StepManager();
            stepManager.Init();

            StepGraph graph = new StepGraph("node_start", "debug_graph");
            graph.AddNode(new StepNode("node_start", "开始"));
            graph.AddNode(new StepNode("node_middle", "中间"));
            graph.AddNode(new StepNode("node_end", "结束"));
            graph.AddEdge(new StepEdge("node_start", "node_middle", 0, string.Empty, ComparisonType.EqualTo, string.Empty));
            graph.AddEdge(new StepEdge("node_middle", "node_end", 0, string.Empty, ComparisonType.EqualTo, string.Empty));
            graph.BuildIndex();

            FieldInfo graphsField = typeof(StepManager).GetField("m_Graphs", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(graphsField);

            Dictionary<string, StepGraph> graphs = graphsField.GetValue(stepManager) as Dictionary<string, StepGraph>;
            Assert.IsNotNull(graphs);
            graphs.Add(graph.Name, graph);

            return stepManager;
        }
    }
}
