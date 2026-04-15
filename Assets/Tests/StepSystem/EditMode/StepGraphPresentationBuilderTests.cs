using System.Collections.Generic;
using LWStep;
using LWStep.Editor;
using LWStep.Editor.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// StepGraph 展示模型构建测试。
    /// </summary>
    public sealed class StepGraphPresentationBuilderTests
    {
        /// <summary>
        /// 构建节点展示模型时应暴露标题、副标题、徽标与动作摘要。
        /// </summary>
        [Test]
        public void BuildNodePresentation_ShouldExposeBadgesAndSummary()
        {
            StepEditorNodeData node = new StepEditorNodeData();
            node.Id = "node_start";
            node.Name = "开始";
            node.Position = Vector2.zero;
            node.Mode = StepNodeMode.Parallel;
            node.Actions.Add(CreateMoveAction("Cube"));

            StepNodePresentation presentation = StepGraphPresentationBuilder.BuildNodePresentation(
                node,
                "node_start",
                "node_start",
                StepNodeStatus.Unfinished,
                new HashSet<string> { "node_start" },
                string.Empty);

            Assert.IsNotNull(presentation);
            Assert.AreEqual("node_start", presentation.Title);
            Assert.AreEqual("开始", presentation.Subtitle);
            CollectionAssert.Contains(presentation.Badges, "Start");
            CollectionAssert.Contains(presentation.Badges, "Parallel");
            Assert.AreEqual("Move:Cube", presentation.ActionSummaries[0]);
            Assert.IsTrue(presentation.IsRunning);
            Assert.IsTrue(presentation.IsInTrail);
        }

        /// <summary>
        /// 构建连线展示模型时应暴露优先级与条件标签。
        /// </summary>
        [Test]
        public void BuildEdgePresentation_ShouldExposePriorityAndCondition()
        {
            StepEditorEdgeData edge = new StepEditorEdgeData();
            edge.FromId = "node_a";
            edge.ToId = "node_b";
            edge.Priority = 20;
            edge.ConditionKey = "mode";
            edge.ConditionComparisonType = ComparisonType.EqualTo;
            edge.ConditionValue = "A";

            StepEdgePresentation presentation = StepGraphPresentationBuilder.BuildEdgePresentation(edge);

            Assert.IsNotNull(presentation);
            Assert.AreEqual("P20 | mode EqualTo A", presentation.Label);
            Assert.IsTrue(presentation.HasCondition);
            Assert.IsFalse(presentation.HasError);
        }

        /// <summary>
        /// 创建带目标参数的移动动作数据。
        /// </summary>
        private static StepEditorActionData CreateMoveAction(string target)
        {
            StepEditorActionData action = new StepEditorActionData();
            action.TypeName = typeof(StepMoveObjectAction).FullName;
            action.SetParameterValue("target", target);
            return action;
        }
    }
}
