using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LWStep;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// StepObjectActionsTests 的 Unity batchmode 入口桥接器。
    /// </summary>
    public static class StepObjectActionsBatchmodeEntry
    {
        private const string ResultFileEnv = "LW_STEP_OBJECT_ACTIONS_RESULT_FILE";
        private const string CaseNamesEnv = "LW_STEP_OBJECT_ACTIONS_CASES";
        private const string GeneratedPrefabFolderPath = "Assets/Tests/StepSystem/EditMode/__GeneratedStepObjectActions";
        private const float VectorTolerance = 0.001f;

        /// <summary>
        /// 供 Unity -executeMethod 调用的桥接入口。
        /// </summary>
        public static void Run()
        {
            string resultFilePath = Environment.GetEnvironmentVariable(ResultFileEnv);
            try
            {
                string[] caseNames = ParseCaseNames(Environment.GetEnvironmentVariable(CaseNamesEnv));
                Dictionary<string, BehaviorCaseResult> results = ExecuteCases(caseNames);
                WriteResults(resultFilePath, results, caseNames);
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Dictionary<string, BehaviorCaseResult> failedResults = BuildFailureResults("Editor bridge failed: " + ex, null, null);
                try
                {
                    WriteResults(resultFilePath, failedResults, null);
                }
                catch
                {
                }

                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// 返回所有对象行为用例名称，供测试宿主与 batchmode 桥共享。
        /// </summary>
        public static string[] GetBehaviorCaseNames()
        {
            return new[]
            {
                "StepSetActiveAction_Apply_ShouldChangeActiveState",
                "StepSetPositionAction_Apply_ShouldWriteTransformPosition",
                "StepSetRotationAction_Apply_ShouldWriteTransformRotation",
                "StepSetScaleAction_Apply_ShouldWriteLocalScale",
                "StepSetParentAction_Apply_ShouldSetParentTransform",
                "StepDestroyTargetAction_Apply_ShouldDestroyTargetImmediatelyInEditor",
                "StepPlayParticleAction_Enter_WithLoopingParticlesAndWaitForFinish_ShouldFinishImmediately",
                "StepInstantiatePrefabAction_Apply_ShouldInstantiatePrefab"
            };
        }

        /// <summary>
        /// 执行指定行为用例列表并汇总结果。
        /// </summary>
        public static Dictionary<string, BehaviorCaseResult> ExecuteCases(IEnumerable<string> caseNames)
        {
            Dictionary<string, BehaviorCaseResult> results = new Dictionary<string, BehaviorCaseResult>(StringComparer.Ordinal);
            string[] resolvedCaseNames = caseNames != null ? new List<string>(caseNames).ToArray() : GetBehaviorCaseNames();
            for (int i = 0; i < resolvedCaseNames.Length; i++)
            {
                string caseName = resolvedCaseNames[i];
                results[caseName] = ExecuteCase(caseName);
            }

            return results;
        }

        /// <summary>
        /// 执行单条行为用例，并把异常转换为失败结果。
        /// </summary>
        public static BehaviorCaseResult ExecuteCase(string caseName)
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
                    case "StepSetRotationAction_Apply_ShouldWriteTransformRotation":
                        RunStepSetRotationActionCase();
                        break;
                    case "StepSetScaleAction_Apply_ShouldWriteLocalScale":
                        RunStepSetScaleActionCase();
                        break;
                    case "StepSetParentAction_Apply_ShouldSetParentTransform":
                        RunStepSetParentActionCase();
                        break;
                    case "StepDestroyTargetAction_Apply_ShouldDestroyTargetImmediatelyInEditor":
                        RunStepDestroyTargetActionCase();
                        break;
                    case "StepPlayParticleAction_Enter_WithLoopingParticlesAndWaitForFinish_ShouldFinishImmediately":
                        RunStepPlayParticleLoopCase();
                        break;
                    case "StepInstantiatePrefabAction_Apply_ShouldInstantiatePrefab":
                        RunStepInstantiatePrefabCase();
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
        /// 将行为结果写入本地文本文件，供 dotnet 宿主读取。
        /// </summary>
        public static void WriteResults(string resultFilePath, IDictionary<string, BehaviorCaseResult> results, IEnumerable<string> caseNames)
        {
            if (string.IsNullOrEmpty(resultFilePath))
            {
                throw new InvalidOperationException("结果文件路径环境变量缺失。");
            }

            string[] resolvedCaseNames = caseNames != null ? new List<string>(caseNames).ToArray() : GetBehaviorCaseNames();
            Directory.CreateDirectory(Path.GetDirectoryName(resultFilePath));

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < resolvedCaseNames.Length; i++)
            {
                string caseName = resolvedCaseNames[i];
                BehaviorCaseResult result;
                if (results == null || !results.TryGetValue(caseName, out result) || result == null)
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
        }

        /// <summary>
        /// 读取 Unity harness 输出的结果文件。
        /// </summary>
        public static Dictionary<string, BehaviorCaseResult> ReadResults(string resultFilePath, string logFilePath)
        {
            Dictionary<string, BehaviorCaseResult> results = new Dictionary<string, BehaviorCaseResult>(StringComparer.Ordinal);
            string[] caseNames = GetBehaviorCaseNames();
            string[] lines = File.ReadAllLines(resultFilePath, Encoding.UTF8);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] parts = line.Split(new[] { '\t' }, 3);
                if (parts.Length < 2)
                {
                    continue;
                }

                string caseName = parts[0];
                bool isPassed = string.Equals(parts[1], "PASS", StringComparison.Ordinal);
                string message = string.Empty;
                if (parts.Length >= 3 && !string.IsNullOrEmpty(parts[2]))
                {
                    byte[] bytes = Convert.FromBase64String(parts[2]);
                    message = Encoding.UTF8.GetString(bytes);
                }

                if (!isPassed && !string.IsNullOrEmpty(logFilePath))
                {
                    message = string.IsNullOrEmpty(message) ? "日志: " + logFilePath : message + "\n日志: " + logFilePath;
                }

                results[caseName] = new BehaviorCaseResult(isPassed, message);
            }

            for (int i = 0; i < caseNames.Length; i++)
            {
                string caseName = caseNames[i];
                if (!results.ContainsKey(caseName))
                {
                    results[caseName] = BehaviorCaseResult.Fail("Unity harness 缺少结果项。日志: " + logFilePath);
                }
            }

            return results;
        }

        /// <summary>
        /// 为所有行为用例构造统一失败结果。
        /// </summary>
        public static Dictionary<string, BehaviorCaseResult> BuildFailureResults(string message, string logFilePath, IEnumerable<string> caseNames)
        {
            Dictionary<string, BehaviorCaseResult> results = new Dictionary<string, BehaviorCaseResult>(StringComparer.Ordinal);
            string[] resolvedCaseNames = caseNames != null ? new List<string>(caseNames).ToArray() : GetBehaviorCaseNames();
            string fullMessage = string.IsNullOrEmpty(logFilePath) ? message : message + "\n日志: " + logFilePath;
            for (int i = 0; i < resolvedCaseNames.Length; i++)
            {
                results[resolvedCaseNames[i]] = BehaviorCaseResult.Fail(fullMessage);
            }

            return results;
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
                AssertVector3Approximately(target.transform.localPosition, new Vector3(1f, 2f, 3f));
            }
            finally
            {
                DestroyIfExists(objectName);
            }
        }

        /// <summary>
        /// 执行设置旋转行为断言。
        /// </summary>
        private static void RunStepSetRotationActionCase()
        {
            const string objectName = "StepTarget_SetRotation";
            DestroyIfExists(objectName);
            GameObject target = new GameObject(objectName);
            try
            {
                StepSetRotationAction action = new StepSetRotationAction();
                action.SetParameters(new Dictionary<string, string>
                {
                    { "target", target.name },
                    { "x", "10" },
                    { "y", "20" },
                    { "z", "30" },
                    { "isLocal", "true" }
                });

                action.Apply();
                AssertVector3Approximately(target.transform.localEulerAngles, new Vector3(10f, 20f, 30f));
            }
            finally
            {
                DestroyIfExists(objectName);
            }
        }

        /// <summary>
        /// 执行设置缩放行为断言。
        /// </summary>
        private static void RunStepSetScaleActionCase()
        {
            const string objectName = "StepTarget_SetScale";
            DestroyIfExists(objectName);
            GameObject target = new GameObject(objectName);
            try
            {
                StepSetScaleAction action = new StepSetScaleAction();
                action.SetParameters(new Dictionary<string, string>
                {
                    { "target", target.name },
                    { "x", "2" },
                    { "y", "3" },
                    { "z", "4" }
                });

                action.Apply();
                AssertVector3Approximately(target.transform.localScale, new Vector3(2f, 3f, 4f));
            }
            finally
            {
                DestroyIfExists(objectName);
            }
        }

        /// <summary>
        /// 执行设置父节点行为断言。
        /// </summary>
        private static void RunStepSetParentActionCase()
        {
            const string parentName = "StepParent_SetParent";
            const string childName = "StepTarget_SetParent";
            DestroyIfExists(childName);
            DestroyIfExists(parentName);
            GameObject parent = new GameObject(parentName);
            GameObject child = new GameObject(childName);
            try
            {
                StepSetParentAction action = new StepSetParentAction();
                action.SetParameters(new Dictionary<string, string>
                {
                    { "target", child.name },
                    { "parent", parent.name },
                    { "worldPositionStays", "false" }
                });

                action.Apply();
                Assert.AreSame(parent.transform, child.transform.parent);
            }
            finally
            {
                DestroyIfExists(childName);
                DestroyIfExists(parentName);
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
        /// 执行循环粒子等待行为断言。
        /// </summary>
        private static void RunStepPlayParticleLoopCase()
        {
            const string objectName = "StepTarget_PlayParticleLoop";
            DestroyIfExists(objectName);
            GameObject target = new GameObject(objectName);
            try
            {
                ParticleSystem particleSystem = target.AddComponent<ParticleSystem>();
                ParticleSystem.MainModule main = particleSystem.main;
                main.loop = true;

                StepPlayParticleAction action = new StepPlayParticleAction();
                action.SetParameters(new Dictionary<string, string>
                {
                    { "target", target.name },
                    { "waitForFinish", "true" },
                    { "restart", "true" }
                });

                action.Enter();
                Assert.IsTrue(action.IsFinished);
            }
            finally
            {
                DestroyIfExists(objectName);
            }
        }

        /// <summary>
        /// 执行实例化预制体行为断言。
        /// </summary>
        private static void RunStepInstantiatePrefabCase()
        {
            const string sourceName = "StepPrefab_Source";
            const string instanceName = "StepPrefab_Instance";
            DestroyIfExists(sourceName);
            DestroyIfExists(instanceName);

            EnsureGeneratedPrefabFolder();
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath(GeneratedPrefabFolderPath + "/StepObjectActionsPrefab.prefab");
            GameObject source = new GameObject(sourceName);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(source, prefabPath);
            }
            finally
            {
                DestroyIfExists(sourceName);
            }

            try
            {
                StepInstantiatePrefabAction action = new StepInstantiatePrefabAction();
                action.SetParameters(new Dictionary<string, string>
                {
                    { "prefab", prefabPath },
                    { "instanceName", instanceName }
                });

                action.Apply();
                GameObject instance = GameObject.Find(instanceName);
                Assert.IsNotNull(instance);
            }
            finally
            {
                DestroyIfExists(instanceName);
                AssetDatabase.DeleteAsset(prefabPath);
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// 确保临时预制体目录存在。
        /// </summary>
        private static void EnsureGeneratedPrefabFolder()
        {
            if (AssetDatabase.IsValidFolder(GeneratedPrefabFolderPath))
            {
                return;
            }

            string parentFolder = "Assets/Tests/StepSystem/EditMode";
            const string folderName = "__GeneratedStepObjectActions";
            if (!AssetDatabase.IsValidFolder(parentFolder))
            {
                throw new DirectoryNotFoundException("缺少测试目录: " + parentFolder);
            }

            AssetDatabase.CreateFolder(parentFolder, folderName);
            AssetDatabase.Refresh();
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
                return GetBehaviorCaseNames();
            }

            string[] segments = rawCaseNames.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i] = segments[i].Trim();
            }

            return segments;
        }

        /// <summary>
        /// 断言两个 Vector3 在允许误差内近似相等。
        /// </summary>
        private static void AssertVector3Approximately(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(VectorTolerance));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(VectorTolerance));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(VectorTolerance));
        }

        /// <summary>
        /// Unity harness 单条行为结果。
        /// </summary>
        public sealed class BehaviorCaseResult
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
