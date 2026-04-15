using LWStep;
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
    }
}
