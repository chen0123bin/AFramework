using System;
using System.Collections.Generic;
using System.IO;
using LWStep;
using LWStep.Editor;
using NUnit.Framework;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// Step 示例 XML 导入导出回归测试。
    /// </summary>
    public sealed class StepExampleXmlTests
    {
        /// <summary>
        /// 示例模板目录应完整暴露 10 个示例 XML 路径。
        /// </summary>
        [Test]
        public void ExampleTemplateCatalog_ShouldExposeAllExamplePaths()
        {
            IReadOnlyList<string> examplePaths = StepExampleTemplateCatalog.ExamplePaths;

            Assert.IsNotNull(examplePaths);
            Assert.That(examplePaths.Count, Is.EqualTo(10));
            CollectionAssert.Contains(examplePaths, Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_BasicFlow.xml"));
            CollectionAssert.Contains(examplePaths, Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_Flow_GeneralPipeline.xml"));
        }

        /// <summary>
        /// 编辑器窗口应接入导入示例菜单与导入方法。
        /// </summary>
        [Test]
        public void StepEditorWindow_ShouldExposeExampleImportContract()
        {
            Assert.IsNotNull(typeof(StepEditorWindow).GetMethod("ImportExampleTemplate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));

            string source = ReadStepEditorWindowSource();
            StringAssert.Contains("导入示例", source);
            StringAssert.Contains("ToolbarMenu", source);
            StringAssert.Contains("ImportXmlText(xmlText, \"导入示例\")", source);
        }

        /// <summary>
        /// 10 个示例 XML 在导入、导出、运行时加载链路中都应保持可用。
        /// </summary>
        [TestCaseSource(nameof(GetExamplePaths))]
        public void ExampleXml_ShouldRoundTripAndLoadRuntimeGraph(string relativePath)
        {
            string projectRoot = FindProjectRoot();
            string fullPath = Path.Combine(projectRoot, relativePath);

            string xmlText = File.ReadAllText(fullPath);
            StepEditorGraphData editorData = StepXmlImporter.LoadFromText(xmlText);
            string exportedXml = StepXmlExporter.ExportToText(editorData);
            StepActionFactory actionFactory = new StepActionFactory();
            StepGraph graph = new StepXmlLoader().LoadFromText(exportedXml, Path.GetFileNameWithoutExtension(fullPath), actionFactory);

            Assert.IsNotNull(editorData, "编辑器导入结果不应为空: " + relativePath);
            Assert.IsNotNull(graph, "运行时加载结果不应为空: " + relativePath);
            Assert.IsNotNull(graph.Validate(), "图校验结果列表不应为空: " + relativePath);
            Assert.That(graph.Validate(), Is.Empty, "图校验应通过: " + relativePath);
        }

        /// <summary>
        /// 返回全部示例 XML 相对路径。
        /// </summary>
        private static IEnumerable<string> GetExamplePaths()
        {
            yield return Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_BasicFlow.xml");
            yield return Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_ConditionBranch.xml");
            yield return Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_ParallelActions.xml");
            yield return Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_ContextOps.xml");
            yield return Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_ObjectControl.xml");
            yield return Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_Actions_Context.xml");
            yield return Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_Actions_Object.xml");
            yield return Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_Actions_AudioFx.xml");
            yield return Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_Flow_TeachingDemo.xml");
            yield return Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_Flow_GeneralPipeline.xml");
        }

        /// <summary>
        /// 读取 StepEditorWindow 源码文本，用于轻量契约验证。
        /// </summary>
        private static string ReadStepEditorWindowSource()
        {
            string root = FindProjectRoot();
            string sourcePath = Path.Combine(root, "Assets", "LWFramework", "Editor", "StepSystem", "StepEditorWindow.cs");
            if (!File.Exists(sourcePath))
            {
                Assert.Fail("未找到 StepEditorWindow.cs，无法执行契约验证。");
            }

            return File.ReadAllText(sourcePath);
        }

        /// <summary>
        /// 从测试运行目录向上定位 Unity 项目根目录。
        /// </summary>
        private static string FindProjectRoot()
        {
            string[] startPaths =
            {
                TestContext.CurrentContext.TestDirectory,
                AppContext.BaseDirectory
            };

            for (int i = 0; i < startPaths.Length; i++)
            {
                string found = FindProjectRootFrom(startPaths[i]);
                if (!string.IsNullOrEmpty(found))
                {
                    return found;
                }
            }

            Assert.Fail("未找到 Unity 项目根目录。");
            return string.Empty;
        }

        /// <summary>
        /// 从指定路径向上查找同时包含 Assets 与 ProjectSettings 的目录。
        /// </summary>
        private static string FindProjectRootFrom(string startPath)
        {
            if (string.IsNullOrEmpty(startPath))
            {
                return null;
            }

            DirectoryInfo current = new DirectoryInfo(Path.GetFullPath(startPath));
            while (current != null)
            {
                bool hasAssets = Directory.Exists(Path.Combine(current.FullName, "Assets"));
                bool hasProjectSettings = Directory.Exists(Path.Combine(current.FullName, "ProjectSettings"));
                if (hasAssets && hasProjectSettings)
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            return null;
        }
    }
}
