using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LWStep.Editor
{
    /// <summary>
    /// StepObjectActionsTests 的 Unity batchmode 入口桥接器。
    /// </summary>
    public static class StepObjectActionsBatchmodeEntry
    {
        private const string ResultFileEnv = "LW_STEP_OBJECT_ACTIONS_RESULT_FILE";
        private const string CaseNamesEnv = "LW_STEP_OBJECT_ACTIONS_CASES";

        /// <summary>
        /// 供 Unity -executeMethod 调用的桥接入口。
        /// </summary>
        public static void Run()
        {
            string resultFilePath = Environment.GetEnvironmentVariable(ResultFileEnv);
            try
            {
                Debug.Log("StepObjectActionsBatchmodeEntry.Run invoked.");
                Dictionary<string, BehaviorCaseResult> results = ExecuteCases();
                WriteResults(resultFilePath, results);
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                WriteFallbackFailureResults(resultFilePath, ex);
                EditorApplication.Exit(0);
            }
        }

        /// <summary>
        /// 执行 3 条对象行为检查并收集每条结果。
        /// </summary>
        private static Dictionary<string, BehaviorCaseResult> ExecuteCases()
        {
            Dictionary<string, BehaviorCaseResult> results = new Dictionary<string, BehaviorCaseResult>();
            string[] caseNames = ParseCaseNames(Environment.GetEnvironmentVariable(CaseNamesEnv));
            for (int i = 0; i < caseNames.Length; i++)
            {
                string caseName = caseNames[i];
                results[caseName] = ExecuteCase(caseName);
            }

            return results;
        }

        /// <summary>
        /// 执行单条行为检查，并把异常转为失败结果。
        /// </summary>
        private static BehaviorCaseResult ExecuteCase(string caseName)
        {
            try
            {
                switch (caseName)
                {
                    case "StepSetActiveAction_Apply_ShouldChangeActiveState":
                        RunStepSetActiveActionCase();
                        break;
                    case "StepSetPositionAction_Apply_ShouldWriteTransformPosition":
                        RunStepSetPositionActionCase();
                        break;
                    case "StepDestroyTargetAction_Apply_ShouldDestroyTargetImmediatelyInEditor":
                        RunStepDestroyTargetActionCase();
                        break;
                    default:
                        return BehaviorCaseResult.Fail("未知的行为用例: " + caseName);
                }

                return BehaviorCaseResult.Pass();
            }
            catch (Exception ex)
            {
                return BehaviorCaseResult.Fail(ex.ToString());
            }
        }

        /// <summary>
        /// 执行设置激活状态行为断言。
        /// </summary>
        private static void RunStepSetActiveActionCase()
        {
            const string objectName = "StepTarget_SetActive";
            DestroyIfExists(objectName);
            GameObject target = new GameObject(objectName);
            try
            {
                StepSetActiveAction action = new StepSetActiveAction();
                action.SetParameters(new Dictionary<string, string>
                {
                    { "target", target.name },
                    { "active", "false" }
                });

                action.Apply();
                Assert.IsFalse(target.activeSelf);
            }
            finally
            {
                DestroyIfExists(objectName);
            }
        }

        /// <summary>
        /// 执行设置位置行为断言。
        /// </summary>
        private static void RunStepSetPositionActionCase()
        {
            const string objectName = "StepTarget_SetPosition";
            DestroyIfExists(objectName);
            GameObject target = new GameObject(objectName);
            try
            {
                StepSetPositionAction action = new StepSetPositionAction();
                action.SetParameters(new Dictionary<string, string>
                {
                    { "target", target.name },
                    { "x", "1" },
                    { "y", "2" },
                    { "z", "3" },
                    { "isLocal", "true" }
                });

                action.Apply();
                Assert.AreEqual(new Vector3(1f, 2f, 3f), target.transform.localPosition);
            }
            finally
            {
                DestroyIfExists(objectName);
            }
        }

        /// <summary>
        /// 执行销毁对象行为断言。
        /// </summary>
        private static void RunStepDestroyTargetActionCase()
        {
            const string objectName = "StepTarget_Destroy";
            DestroyIfExists(objectName);
            GameObject target = new GameObject(objectName);
            StepDestroyTargetAction action = new StepDestroyTargetAction();
            action.SetParameters(new Dictionary<string, string>
            {
                { "target", target.name }
            });

            action.Apply();
            Assert.IsNull(GameObject.Find(objectName));
        }

        /// <summary>
        /// 删除指定测试对象，避免跨用例污染。
        /// </summary>
        private static void DestroyIfExists(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            if (target == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(target);
        }

        /// <summary>
        /// 解析行为用例名称列表。
        /// </summary>
        private static string[] ParseCaseNames(string rawCaseNames)
        {
            if (string.IsNullOrEmpty(rawCaseNames))
            {
                return new[]
                {
                    "StepSetActiveAction_Apply_ShouldChangeActiveState",
                    "StepSetPositionAction_Apply_ShouldWriteTransformPosition",
                    "StepDestroyTargetAction_Apply_ShouldDestroyTargetImmediatelyInEditor"
                };
            }

            return rawCaseNames.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// 将行为结果写入本地文本文件，供 dotnet 宿主读取。
        /// </summary>
        private static void WriteResults(string resultFilePath, Dictionary<string, BehaviorCaseResult> results)
        {
            if (string.IsNullOrEmpty(resultFilePath))
            {
                throw new InvalidOperationException("结果文件路径环境变量缺失。");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(resultFilePath));
            StringBuilder builder = new StringBuilder();
            string[] caseNames = ParseCaseNames(Environment.GetEnvironmentVariable(CaseNamesEnv));
            for (int i = 0; i < caseNames.Length; i++)
            {
                string caseName = caseNames[i];
                BehaviorCaseResult result;
                if (!results.TryGetValue(caseName, out result))
                {
                    result = BehaviorCaseResult.Fail("未返回结果。");
                }

                string encodedMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(result.Message ?? string.Empty));
                builder.Append(caseName)
                    .Append('\t')
                    .Append(result.IsPassed ? "PASS" : "FAIL")
                    .Append('\t')
                    .Append(encodedMessage)
                    .AppendLine();
            }

            File.WriteAllText(resultFilePath, builder.ToString(), Encoding.UTF8);
            Debug.Log("StepObjectActionsBatchmodeEntry wrote results: " + resultFilePath);
        }

        /// <summary>
        /// 当桥接器本身失败时，兜底写出统一失败结果。
        /// </summary>
        private static void WriteFallbackFailureResults(string resultFilePath, Exception ex)
        {
            if (string.IsNullOrEmpty(resultFilePath))
            {
                return;
            }

            Dictionary<string, BehaviorCaseResult> results = new Dictionary<string, BehaviorCaseResult>();
            string[] caseNames = ParseCaseNames(Environment.GetEnvironmentVariable(CaseNamesEnv));
            for (int i = 0; i < caseNames.Length; i++)
            {
                results[caseNames[i]] = BehaviorCaseResult.Fail("Editor bridge failed: " + ex);
            }

            WriteResults(resultFilePath, results);
        }

        /// <summary>
        /// Unity harness 单条行为结果。
        /// </summary>
        private sealed class BehaviorCaseResult
        {
            public bool IsPassed { get; private set; }
            public string Message { get; private set; }

            /// <summary>
            /// 创建行为结果对象。
            /// </summary>
            public BehaviorCaseResult(bool isPassed, string message)
            {
                IsPassed = isPassed;
                Message = message ?? string.Empty;
            }

            /// <summary>
            /// 创建通过结果。
            /// </summary>
            public static BehaviorCaseResult Pass()
            {
                return new BehaviorCaseResult(true, string.Empty);
            }

            /// <summary>
            /// 创建失败结果。
            /// </summary>
            public static BehaviorCaseResult Fail(string message)
            {
                return new BehaviorCaseResult(false, message);
            }
        }
    }
}
