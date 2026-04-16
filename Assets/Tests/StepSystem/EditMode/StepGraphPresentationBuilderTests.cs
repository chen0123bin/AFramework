using System;
using System.Collections.Generic;
using System.Reflection;
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
        /// 动作超过三条时应截断摘要并追加剩余数量标记。
        /// </summary>
        [Test]
        public void BuildNodePresentation_WhenActionsExceedThree_ShouldAppendRemainingCount()
        {
            StepEditorNodeData node = new StepEditorNodeData();
            node.Id = "node_many";
            node.Name = "多动作";
            node.Position = Vector2.zero;
            node.Mode = StepNodeMode.Serial;
            node.Actions.Add(CreateMoveAction("CubeA"));
            node.Actions.Add(CreateMoveAction("CubeB"));
            node.Actions.Add(CreateMoveAction("CubeC"));
            node.Actions.Add(CreateMoveAction("CubeD"));
            node.Actions.Add(CreateMoveAction("CubeE"));

            StepNodePresentation presentation = StepGraphPresentationBuilder.BuildNodePresentation(
                node,
                string.Empty,
                string.Empty,
                StepNodeStatus.Unfinished,
                null,
                string.Empty);

            Assert.IsNotNull(presentation);
            Assert.AreEqual(4, presentation.ActionSummaries.Count);
            Assert.AreEqual("Move:CubeA", presentation.ActionSummaries[0]);
            Assert.AreEqual("Move:CubeB", presentation.ActionSummaries[1]);
            Assert.AreEqual("Move:CubeC", presentation.ActionSummaries[2]);
            Assert.AreEqual("+2", presentation.ActionSummaries[3]);
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
        /// 无条件连线应只显示优先级标签。
        /// </summary>
        [Test]
        public void BuildEdgePresentation_WithoutCondition_ShouldUsePriorityOnlyLabel()
        {
            StepEditorEdgeData edge = new StepEditorEdgeData();
            edge.FromId = "node_a";
            edge.ToId = "node_b";
            edge.Priority = 7;
            edge.ConditionKey = string.Empty;
            edge.ConditionValue = string.Empty;

            StepEdgePresentation presentation = StepGraphPresentationBuilder.BuildEdgePresentation(edge);

            Assert.IsNotNull(presentation);
            Assert.AreEqual("P7", presentation.Label);
            Assert.IsFalse(presentation.HasCondition);
            Assert.IsFalse(presentation.HasError);
        }

        /// <summary>
        /// 节点视图应暴露展示绑定 API 与任务要求的渲染字段。
        /// </summary>
        [Test]
        public void StepNodeView_ShouldExposePresentationBindingSurface()
        {
            MethodInfo bindPresentation = typeof(StepNodeView).GetMethod("BindPresentation", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo subtitleField = typeof(StepNodeView).GetField("m_SubtitleLabel", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo badgeContainerField = typeof(StepNodeView).GetField("m_BadgeContainer", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo summaryContainerField = typeof(StepNodeView).GetField("m_SummaryContainer", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(bindPresentation);
            Assert.AreEqual(typeof(void), bindPresentation.ReturnType);
            Assert.AreEqual(1, bindPresentation.GetParameters().Length);
            Assert.AreEqual(typeof(StepNodePresentation), bindPresentation.GetParameters()[0].ParameterType);
            Assert.IsNotNull(subtitleField);
            Assert.AreEqual("Label", subtitleField.FieldType.Name);
            Assert.IsNotNull(badgeContainerField);
            Assert.AreEqual("VisualElement", badgeContainerField.FieldType.Name);
            Assert.IsNotNull(summaryContainerField);
            Assert.AreEqual("VisualElement", summaryContainerField.FieldType.Name);
        }

        /// <summary>
        /// 图视图应暴露运行时轨迹入口与任务要求的渲染字段。
        /// </summary>
        [Test]
        public void StepGraphView_ShouldExposeRuntimeTrailAndEdgeRefreshSurface()
        {
            MethodInfo setRuntimeTrail = typeof(StepGraphView).GetMethod("SetRuntimeTrail", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo updateEdgeView = typeof(StepGraphView).GetMethod("UpdateEdgeView", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo runtimeTrailField = typeof(StepGraphView).GetField("m_RuntimeTrailNodeIds", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(setRuntimeTrail);
            Assert.AreEqual(typeof(void), setRuntimeTrail.ReturnType);
            Assert.AreEqual(1, setRuntimeTrail.GetParameters().Length);
            Assert.AreEqual(typeof(List<string>), setRuntimeTrail.GetParameters()[0].ParameterType);
            Assert.IsNotNull(updateEdgeView);
            Assert.AreEqual(typeof(void), updateEdgeView.ReturnType);
            Assert.IsNotNull(runtimeTrailField);
            Assert.AreEqual(typeof(HashSet<string>), runtimeTrailField.FieldType);
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
