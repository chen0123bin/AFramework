using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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
        /// 节点视图绑定计划应包含副标题、徽标、摘要与状态类映射。
        /// </summary>
        [Test]
        public void BuildBindingPlan_ShouldExposeSubtitleBadgesSummariesAndClasses()
        {
            StepNodePresentation presentation = new StepNodePresentation();
            presentation.Title = "node_bind";
            presentation.Subtitle = "副标题";
            presentation.Badges.Add("Start");
            presentation.Badges.Add("Serial");
            presentation.ActionSummaries.Add("Move:Cube");
            presentation.ActionSummaries.Add("Log:Ready");
            presentation.IsCompleted = true;
            presentation.IsInTrail = true;

            object bindingPlan = InvokeNonPublicStaticMethod(typeof(StepNodeView), "BuildBindingPlan", presentation);

            Assert.IsNotNull(bindingPlan);
            Assert.AreEqual("node_bind", GetMemberValue<string>(bindingPlan, "Title"));
            Assert.AreEqual("副标题", GetMemberValue<string>(bindingPlan, "Subtitle"));
            CollectionAssert.AreEqual(
                new[] { "Start", "Serial" },
                GetMemberValue<List<string>>(bindingPlan, "Badges"));
            CollectionAssert.AreEqual(
                new[] { "Move:Cube", "Log:Ready" },
                GetMemberValue<List<string>>(bindingPlan, "SummaryLines"));
            CollectionAssert.Contains(
                GetMemberValue<List<string>>(bindingPlan, "EnabledClasses"),
                "step-node-completed");
            CollectionAssert.Contains(
                GetMemberValue<List<string>>(bindingPlan, "EnabledClasses"),
                "step-node-trail");
        }

        /// <summary>
        /// BindPresentation 应通过绑定计划驱动节点 UI 渲染。
        /// </summary>
        [Test]
        public void BindPresentation_ShouldUseBindingPlanBuilder()
        {
            MethodInfo bindPresentation = GetNonPublicOrPublicInstanceMethod(typeof(StepNodeView), "BindPresentation");
            MethodInfo buildBindingPlan = GetNonPublicOrPublicStaticMethod(typeof(StepNodeView), "BuildBindingPlan");

            Assert.IsTrue(MethodCalls(bindPresentation, buildBindingPlan));
        }

        /// <summary>
        /// 运行时轨迹刷新应通过 UpdateAllNodeTitles 驱动节点重绑。
        /// </summary>
        [Test]
        public void SetRuntimeTrail_ShouldCallUpdateAllNodeTitles()
        {
            MethodInfo setRuntimeTrail = GetNonPublicOrPublicInstanceMethod(typeof(StepGraphView), "SetRuntimeTrail");
            MethodInfo updateAllNodeTitles = GetNonPublicOrPublicInstanceMethod(typeof(StepGraphView), "UpdateAllNodeTitles");

            Assert.IsTrue(MethodCalls(setRuntimeTrail, updateAllNodeTitles));
        }

        /// <summary>
        /// 节点标题刷新应同时调用展示构建器与节点绑定方法。
        /// </summary>
        [Test]
        public void UpdateAllNodeTitles_ShouldUsePresentationBuilderAndBindPresentation()
        {
            MethodInfo updateAllNodeTitles = GetNonPublicOrPublicInstanceMethod(typeof(StepGraphView), "UpdateAllNodeTitles");
            MethodInfo buildNodePresentation = GetNonPublicOrPublicStaticMethod(typeof(StepGraphPresentationBuilder), "BuildNodePresentation");
            MethodInfo bindPresentation = GetNonPublicOrPublicInstanceMethod(typeof(StepNodeView), "BindPresentation");

            Assert.IsTrue(MethodCalls(updateAllNodeTitles, buildNodePresentation));
            Assert.IsTrue(MethodCalls(updateAllNodeTitles, bindPresentation));
        }

        /// <summary>
        /// 连线配置应通过展示构建器生成标签并创建标签控件。
        /// </summary>
        [Test]
        public void ConfigureEdgeView_ShouldUseEdgePresentationBuilderAndCreateLabel()
        {
            MethodInfo configureEdgeView = GetNonPublicOrPublicInstanceMethod(typeof(StepGraphView), "ConfigureEdgeView");
            MethodInfo buildEdgePresentation = GetNonPublicOrPublicStaticMethod(typeof(StepGraphPresentationBuilder), "BuildEdgePresentation");
            MethodInfo getOrCreateEdgeLabel = GetNonPublicOrPublicInstanceMethod(typeof(StepGraphView), "GetOrCreateEdgeLabel");

            Assert.IsTrue(MethodCalls(configureEdgeView, buildEdgePresentation));
            Assert.IsTrue(MethodCalls(configureEdgeView, getOrCreateEdgeLabel));
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

        /// <summary>
        /// 通过反射调用非公开静态方法。
        /// </summary>
        private static object InvokeNonPublicStaticMethod(Type targetType, string methodName, params object[] parameters)
        {
            MethodInfo method = GetNonPublicOrPublicStaticMethod(targetType, methodName);
            return method.Invoke(null, parameters);
        }

        /// <summary>
        /// 读取对象字段或属性值。
        /// </summary>
        private static T GetMemberValue<T>(object target, string memberName)
        {
            Type targetType = target.GetType();
            PropertyInfo property = targetType.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                return (T)property.GetValue(target, null);
            }

            FieldInfo field = targetType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return (T)field.GetValue(target);
            }

            Assert.Fail("未找到成员: " + memberName);
            return default(T);
        }

        /// <summary>
        /// 获取公开或非公开的实例方法。
        /// </summary>
        private static MethodInfo GetNonPublicOrPublicInstanceMethod(Type targetType, string methodName)
        {
            MethodInfo method = targetType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "未找到实例方法: " + targetType.FullName + "." + methodName);
            return method;
        }

        /// <summary>
        /// 获取公开或非公开的静态方法。
        /// </summary>
        private static MethodInfo GetNonPublicOrPublicStaticMethod(Type targetType, string methodName)
        {
            MethodInfo method = targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "未找到静态方法: " + targetType.FullName + "." + methodName);
            return method;
        }

        /// <summary>
        /// 判断调用方法的 IL 中是否包含目标方法调用。
        /// </summary>
        private static bool MethodCalls(MethodInfo caller, MethodInfo callee)
        {
            Assert.IsNotNull(caller);
            Assert.IsNotNull(callee);

            MethodBody methodBody = caller.GetMethodBody();
            Assert.IsNotNull(methodBody, "方法缺少 IL 方法体: " + caller.Name);

            byte[] ilBytes = methodBody.GetILAsByteArray();
            Module module = caller.Module;
            int index = 0;
            while (index < ilBytes.Length)
            {
                OpCode opCode = ReadOpCode(ilBytes, ref index);
                if (opCode == OpCodes.Call || opCode == OpCodes.Callvirt)
                {
                    int metadataToken = ReadInt32(ilBytes, ref index);
                    MethodBase calledMethod = module.ResolveMethod(metadataToken);
                    MethodInfo calledMethodInfo = calledMethod as MethodInfo;
                    if (calledMethodInfo != null && AreSameMethod(calledMethodInfo, callee))
                    {
                        return true;
                    }
                    continue;
                }

                SkipOperand(ilBytes, ref index, opCode);
            }

            return false;
        }

        /// <summary>
        /// 判断两个方法是否指向同一声明。
        /// </summary>
        private static bool AreSameMethod(MethodInfo left, MethodInfo right)
        {
            return left.Module == right.Module && left.MetadataToken == right.MetadataToken;
        }

        /// <summary>
        /// 从 IL 字节流中读取操作码。
        /// </summary>
        private static OpCode ReadOpCode(byte[] ilBytes, ref int index)
        {
            byte code = ilBytes[index++];
            if (code != 0xFE)
            {
                return s_OneByteOpCodes[code];
            }

            byte secondCode = ilBytes[index++];
            return s_TwoByteOpCodes[secondCode];
        }

        /// <summary>
        /// 按操作数类型跳过当前 IL 指令的操作数部分。
        /// </summary>
        private static void SkipOperand(byte[] ilBytes, ref int index, OpCode opCode)
        {
            switch (opCode.OperandType)
            {
                case OperandType.InlineNone:
                    return;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    index += 1;
                    return;
                case OperandType.InlineVar:
                    index += 2;
                    return;
                case OperandType.InlineI:
                case OperandType.InlineBrTarget:
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.ShortInlineR:
                    index += 4;
                    return;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    index += 8;
                    return;
                case OperandType.InlineSwitch:
                    int caseCount = ReadInt32(ilBytes, ref index);
                    index += caseCount * 4;
                    return;
                default:
                    throw new NotSupportedException("未支持的 OperandType: " + opCode.OperandType);
            }
        }

        /// <summary>
        /// 从 IL 字节流中读取 Int32。
        /// </summary>
        private static int ReadInt32(byte[] ilBytes, ref int index)
        {
            int value = BitConverter.ToInt32(ilBytes, index);
            index += 4;
            return value;
        }

        private static readonly OpCode[] s_OneByteOpCodes = BuildOpCodeMap(false);
        private static readonly OpCode[] s_TwoByteOpCodes = BuildOpCodeMap(true);

        /// <summary>
        /// 构建单字节或双字节 IL 操作码映射表。
        /// </summary>
        private static OpCode[] BuildOpCodeMap(bool twoByte)
        {
            OpCode[] opCodes = new OpCode[256];
            FieldInfo[] fields = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType != typeof(OpCode))
                {
                    continue;
                }

                OpCode opCode = (OpCode)fields[i].GetValue(null);
                ushort value = unchecked((ushort)opCode.Value);
                if (twoByte)
                {
                    if ((value & 0xFF00) != 0xFE00)
                    {
                        continue;
                    }
                    opCodes[value & 0xFF] = opCode;
                    continue;
                }

                if ((value & 0xFF00) == 0xFE00)
                {
                    continue;
                }
                opCodes[value] = opCode;
            }

            return opCodes;
        }
    }
}
