using System.Collections.Generic;
using System.IO;

namespace LWStep.Editor
{
    /// <summary>
    /// Step 示例模板目录。
    /// </summary>
    public static class StepExampleTemplateCatalog
    {
        private static readonly IReadOnlyList<string> s_ExamplePaths = new List<string>
        {
            Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_BasicFlow.xml"),
            Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_ConditionBranch.xml"),
            Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_ParallelActions.xml"),
            Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_ContextOps.xml"),
            Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_ObjectControl.xml"),
            Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_Actions_Context.xml"),
            Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_Actions_Object.xml"),
            Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_Actions_AudioFx.xml"),
            Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_Flow_TeachingDemo.xml"),
            Path.Combine("Assets", "0Res", "RawFiles", "StepExamples", "StepExample_Flow_GeneralPipeline.xml")
        };

        /// <summary>
        /// 返回全部示例 XML 路径。
        /// </summary>
        public static IReadOnlyList<string> ExamplePaths
        {
            get { return s_ExamplePaths; }
        }

        /// <summary>
        /// 根据示例路径生成工具栏展示名。
        /// </summary>
        public static string GetDisplayName(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path) ?? string.Empty;
            if (fileName.StartsWith("StepExample_"))
            {
                return fileName.Substring("StepExample_".Length);
            }

            return fileName;
        }
    }
}
