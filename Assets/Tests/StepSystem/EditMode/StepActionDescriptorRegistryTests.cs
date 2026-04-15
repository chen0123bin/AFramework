using System.Collections.Generic;
using LWStep;
using LWStep.Editor;
using LWStep.Editor.Metadata;
using NUnit.Framework;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// StepActionDescriptorRegistry 元数据构建测试。
    /// </summary>
    public sealed class StepActionDescriptorRegistryTests
    {
        /// <summary>
        /// 已标注动作应暴露展示元数据。
        /// </summary>
        [Test]
        public void GetDescriptor_ForAnnotatedAction_ShouldExposeDisplayMetadata()
        {
            StepActionDescriptor descriptor = StepActionDescriptorRegistry.GetDescriptor(typeof(StepLogAction));

            Assert.IsNotNull(descriptor);
            Assert.AreEqual("输出日志", descriptor.DisplayName);
            Assert.AreEqual("调试", descriptor.Category);
            Assert.AreEqual("Log:{message}", descriptor.SummaryTemplate);
            Assert.IsNotNull(descriptor.Parameters);
            Assert.GreaterOrEqual(descriptor.Parameters.Count, 1);
            Assert.AreEqual("message", descriptor.Parameters[0].Key);
        }

        /// <summary>
        /// 摘要模板应使用目标参数进行占位替换。
        /// </summary>
        [Test]
        public void BuildSummary_ForMoveAction_ShouldUseTargetParameter()
        {
            List<StepEditorParameterData> parameters = new List<StepEditorParameterData>
            {
                CreateParameter("target", "Cube"),
                CreateParameter("moveTime", "0.5")
            };

            string summary = StepActionDescriptorRegistry.BuildSummary(typeof(StepMoveObjectAction).FullName, parameters);

            Assert.AreEqual("Move:Cube", summary);
        }

        /// <summary>
        /// 外部修改返回的描述对象不应污染全局缓存。
        /// </summary>
        [Test]
        public void GetDescriptor_WhenMutatedExternally_ShouldNotPolluteCachedDescriptor()
        {
            StepActionDescriptor firstDescriptor = StepActionDescriptorRegistry.GetDescriptor(typeof(StepLogAction));
            Assert.IsNotNull(firstDescriptor);

            firstDescriptor.DisplayName = "被污染名称";
            firstDescriptor.Parameters[0].Key = "tampered";
            firstDescriptor.Keywords.Add("tampered");

            StepActionDescriptor secondDescriptor = StepActionDescriptorRegistry.GetDescriptor(typeof(StepLogAction));
            Assert.IsNotNull(secondDescriptor);
            Assert.AreEqual("输出日志", secondDescriptor.DisplayName);
            Assert.AreEqual("message", secondDescriptor.Parameters[0].Key);
            Assert.IsFalse(secondDescriptor.Keywords.Contains("tampered"));
        }

        /// <summary>
        /// 创建测试用参数数据。
        /// </summary>
        private static StepEditorParameterData CreateParameter(string key, string value)
        {
            StepEditorParameterData parameter = new StepEditorParameterData();
            parameter.Key = key;
            parameter.Value = value;
            return parameter;
        }
    }
}
