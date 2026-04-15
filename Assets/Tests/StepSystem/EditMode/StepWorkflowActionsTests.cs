using System.Collections.Generic;
using System.Threading;
using LWStep;
using NUnit.Framework;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// 流程控制与上下文动作测试。
    /// </summary>
    public sealed class StepWorkflowActionsTests
    {
        /// <summary>
        /// Apply 应将 value 解析为基础类型并写入上下文。
        /// </summary>
        [Test]
        public void StepSetContextValueAction_Apply_ShouldWriteParsedValue()
        {
            StepContext context = new StepContext();
            StepSetContextValueAction action = new StepSetContextValueAction();
            action.SetContext(context);
            action.SetParameters(new Dictionary<string, string>
            {
                { "key", "score" },
                { "value", "5" }
            });

            action.Apply();

            Assert.AreEqual(5, context.GetValue("score", 0));
        }

        /// <summary>
        /// Apply 应删除指定上下文键。
        /// </summary>
        [Test]
        public void StepRemoveContextValueAction_Apply_ShouldDeleteKey()
        {
            StepContext context = new StepContext();
            context.SetValue("mode", "A");
            StepRemoveContextValueAction action = new StepRemoveContextValueAction();
            action.SetContext(context);
            action.SetParameters(new Dictionary<string, string>
            {
                { "key", "mode" }
            });

            action.Apply();

            Assert.IsFalse(context.HasKey("mode"));
        }

        /// <summary>
        /// 等待动作在达到时长后应结束。
        /// </summary>
        [Test]
        public void StepWaitSecondsAction_Update_ShouldFinishAfterDuration()
        {
            StepWaitSecondsAction action = new StepWaitSecondsAction();
            action.SetParameters(new Dictionary<string, string>
            {
                { "seconds", "0.02" }
            });

            action.Enter();
            Thread.Sleep(40);
            action.Update();

            Assert.IsTrue(action.IsFinished);
        }
    }
}
