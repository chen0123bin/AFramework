using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using LWStep;
using NUnit.Framework;
using UnityEngine;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// 对象控制与实例化动作测试。
    /// </summary>
    public sealed class StepObjectActionsTests
    {
        private const string RESULT_FILE_ENV = "LW_STEP_OBJECT_ACTIONS_RESULT_FILE";
        private const string CASE_NAMES_ENV = "LW_STEP_OBJECT_ACTIONS_CASES";
        private const string LOG_FILE_ENV = "LW_STEP_OBJECT_ACTIONS_LOG_FILE";

        private static bool? s_CanUseUnityObjects;
        private static readonly object s_BehaviorHarnessLock = new object();
        private static Dictionary<string, BehaviorCaseResult> s_BehaviorHarnessResults;

        /// <summary>
        /// 每条用例执行后清理临时对象，避免污染其他测试。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (!CanUseUnityObjects())
            {
                return;
            }

            DestroyIfExists("StepTarget_SetActive");
            DestroyIfExists("StepTarget_SetPosition");
            DestroyIfExists("StepTarget_Destroy");
        }

        /// <summary>
        /// Apply 应修改对象激活状态。
        /// </summary>
        [Test]
        public void StepSetActiveAction_Apply_ShouldChangeActiveState()
        {
            AssertBehaviorCasePasses(nameof(StepSetActiveAction_Apply_ShouldChangeActiveState));
            RunStepSetActiveActionCase();
        }

        /// <summary>
        /// Apply 应写入本地位置。
        /// </summary>
        [Test]
        public void StepSetPositionAction_Apply_ShouldWriteTransformPosition()
        {
            AssertBehaviorCasePasses(nameof(StepSetPositionAction_Apply_ShouldWriteTransformPosition));
            RunStepSetPositionActionCase();
        }

        /// <summary>
        /// Apply 在编辑器中应立即销毁对象。
        /// </summary>
        [Test]
        public void StepDestroyTargetAction_Apply_ShouldDestroyTargetImmediatelyInEditor()
        {
            AssertBehaviorCasePasses(nameof(StepDestroyTargetAction_Apply_ShouldDestroyTargetImmediatelyInEditor));
            RunStepDestroyTargetActionCase();
        }

        /// <summary>
        /// 设置激活动作应暴露正确的展示与参数元数据。
        /// </summary>
        [Test]
        public void StepSetActiveAction_Metadata_ShouldMatchSpec()
        {
            StepActionInfoAttribute info = GetActionInfo(typeof(StepSetActiveAction));
            StepParamAttribute param = GetParamAttribute(typeof(StepSetActiveAction), "active");

            Assert.AreEqual("设置激活状态", info.DisplayName);
            Assert.AreEqual("对象控制", info.Category);
            Assert.AreEqual("Active:{target}", info.SummaryTemplate);
            Assert.AreEqual("active", param.Key);
            Assert.AreEqual("是否激活", param.Label);
            Assert.AreEqual(1, param.Order);
        }

        /// <summary>
        /// 实例化预制体动作应暴露正确的展示与参数元数据。
        /// </summary>
        [Test]
        public void StepInstantiatePrefabAction_Metadata_ShouldMatchSpec()
        {
            StepActionInfoAttribute info = GetActionInfo(typeof(StepInstantiatePrefabAction));
            StepParamAttribute param = GetParamAttribute(typeof(StepInstantiatePrefabAction), "prefab");

            Assert.AreEqual("实例化预制体", info.DisplayName);
            Assert.AreEqual("对象控制", info.Category);
            Assert.AreEqual("Spawn:{prefab}", info.SummaryTemplate);
            Assert.AreEqual("prefab", param.Key);
            Assert.AreEqual("预制体", param.Label);
            Assert.AreEqual(0, param.Order);
        }

        /// <summary>
        /// 播放粒子动作应暴露正确的展示与参数元数据。
        /// </summary>
        [Test]
        public void StepPlayParticleAction_Metadata_ShouldMatchSpec()
        {
            StepActionInfoAttribute info = GetActionInfo(typeof(StepPlayParticleAction));
            StepParamAttribute param = GetParamAttribute(typeof(StepPlayParticleAction), "waitForFinish");

            Assert.AreEqual("播放粒子", info.DisplayName);
            Assert.AreEqual("动画与特效", info.Category);
            Assert.AreEqual("Particle:{target}", info.SummaryTemplate);
            Assert.AreEqual("waitForFinish", param.Key);
            Assert.AreEqual("等待播放结束", param.Label);
            Assert.AreEqual(1, param.Order);
        }

        /// <summary>
        /// 若对象存在则立即销毁，避免测试残留。
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
        /// 在当前测试宿主内探测是否可安全创建 Unity 对象。
        /// </summary>
        private static bool CanUseUnityObjects()
        {
            if (s_CanUseUnityObjects.HasValue)
            {
                return s_CanUseUnityObjects.Value;
            }

            try
            {
                GameObject probe = new GameObject("StepObjectActions_RuntimeProbe");
                UnityEngine.Object.DestroyImmediate(probe);
                s_CanUseUnityObjects = true;
            }
            catch (SecurityException)
            {
                s_CanUseUnityObjects = false;
            }

            return s_CanUseUnityObjects.Value;
        }

        /// <summary>
        /// 当测试宿主不支持 Unity 对象内部调用时，改由 Unity batchmode harness 回传真实结果。
        /// </summary>
        private static void RequireUnityObjectAccess()
        {
            if (!CanUseUnityObjects())
            {
                Assert.Ignore("当前 dotnet test 宿主不支持 UnityEngine.GameObject 内部调用，跳过对象级行为断言。");
            }
        }

        /// <summary>
        /// 当前宿主无法直接访问 Unity 对象时，断言对应行为用例在 Unity batchmode 中执行通过。
        /// </summary>
        private static void AssertBehaviorCasePasses(string caseName)
        {
            if (CanUseUnityObjects())
            {
                return;
            }

            Dictionary<string, BehaviorCaseResult> results = EnsureBehaviorHarnessResults();
            BehaviorCaseResult result;
            if (!results.TryGetValue(caseName, out result))
            {
                Assert.Fail("Unity harness 未返回用例结果: " + caseName);
                return;
            }

            Assert.IsTrue(result.IsPassed, result.Message);
            Assert.Pass();
        }

        /// <summary>
        /// 确保已获取 Unity batchmode 行为测试结果；首次调用时仅启动一次 Unity。
        /// </summary>
        private static Dictionary<string, BehaviorCaseResult> EnsureBehaviorHarnessResults()
        {
            lock (s_BehaviorHarnessLock)
            {
                if (s_BehaviorHarnessResults != null)
                {
                    return s_BehaviorHarnessResults;
                }

                s_BehaviorHarnessResults = RunBehaviorHarnessInUnity();
                return s_BehaviorHarnessResults;
            }
        }

        /// <summary>
        /// 启动一次 Unity batchmode，在真实 Editor 环境内执行 3 条对象行为断言。
        /// </summary>
        private static Dictionary<string, BehaviorCaseResult> RunBehaviorHarnessInUnity()
        {
            string projectRoot = FindProjectRoot();
            string unityExePath = ResolveUnityExePath(projectRoot);
            string harnessDirectory = Path.Combine(projectRoot, "Temp", "StepObjectActionsHarness");
            Directory.CreateDirectory(harnessDirectory);

            string resultFilePath = Path.Combine(harnessDirectory, "results.txt");
            string warmupLogFilePath = Path.Combine(harnessDirectory, "unity-warmup.log");
            string logFilePath = Path.Combine(harnessDirectory, "unity-batchmode.log");
            if (File.Exists(resultFilePath))
            {
                File.Delete(resultFilePath);
            }

            int warmupExitCode = RunUnityBatchmodeProcess(
                unityExePath,
                projectRoot,
                null,
                warmupLogFilePath,
                null,
                null);
            if (warmupExitCode != 0)
            {
                return BuildHarnessFailureResults(
                    "Unity batchmode 预热失败，ExitCode=" + warmupExitCode + "。日志: " + warmupLogFilePath,
                    warmupLogFilePath);
            }

            Dictionary<string, string> environment = new Dictionary<string, string>
            {
                { RESULT_FILE_ENV, resultFilePath },
                { LOG_FILE_ENV, logFilePath },
                { CASE_NAMES_ENV, string.Join(";", GetBehaviorCaseNames()) }
            };
            int executeExitCode = RunUnityBatchmodeProcess(
                unityExePath,
                projectRoot,
                "LWStep.Editor.StepObjectActionsBatchmodeEntry.Run",
                logFilePath,
                environment,
                resultFilePath);
            if (executeExitCode != 0)
            {
                return BuildHarnessFailureResults(
                    "Unity batchmode 执行失败，ExitCode=" + executeExitCode + "。日志: " + logFilePath,
                    logFilePath);
            }

            if (!File.Exists(resultFilePath))
            {
                return BuildHarnessFailureResults("Unity harness 未写出结果文件: " + resultFilePath, logFilePath);
            }

            return ReadHarnessResults(resultFilePath, logFilePath);
        }

        /// <summary>
        /// 启动一次 Unity batchmode 进程；可选执行指定静态方法，并支持传入环境变量。
        /// </summary>
        private static int RunUnityBatchmodeProcess(
            string unityExePath,
            string projectRoot,
            string executeMethod,
            string logFilePath,
            Dictionary<string, string> environment,
            string expectedResultFilePath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = unityExePath,
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("-batchmode");
            startInfo.ArgumentList.Add("-projectPath");
            startInfo.ArgumentList.Add(projectRoot);
            if (!string.IsNullOrEmpty(executeMethod))
            {
                startInfo.ArgumentList.Add("-executeMethod");
                startInfo.ArgumentList.Add(executeMethod);
            }
            startInfo.ArgumentList.Add("-quit");
            startInfo.ArgumentList.Add("-logFile");
            startInfo.ArgumentList.Add(logFilePath);

            if (environment != null)
            {
                foreach (KeyValuePair<string, string> item in environment)
                {
                    startInfo.Environment[item.Key] = item.Value ?? string.Empty;
                }
            }

            if (!string.IsNullOrEmpty(expectedResultFilePath) && File.Exists(expectedResultFilePath))
            {
                File.Delete(expectedResultFilePath);
            }

            using Process process = Process.Start(startInfo);
            if (process == null)
            {
                return -1;
            }

            process.WaitForExit();
            return process.ExitCode;
        }

        /// <summary>
        /// Unity batchmode 入口：在真实 Editor 环境内执行 3 条对象行为检查并写出结果文件。
        /// </summary>
        public static void RunUnityBatchmodeBehaviorHarness()
        {
            string resultFilePath = System.Environment.GetEnvironmentVariable(RESULT_FILE_ENV);
            string logFilePath = System.Environment.GetEnvironmentVariable(LOG_FILE_ENV);
            string rawCaseNames = System.Environment.GetEnvironmentVariable(CASE_NAMES_ENV);
            Dictionary<string, BehaviorCaseResult> results = new Dictionary<string, BehaviorCaseResult>();

            try
            {
                string[] caseNames = ParseCaseNames(rawCaseNames);
                for (int i = 0; i < caseNames.Length; i++)
                {
                    string caseName = caseNames[i];
                    results[caseName] = ExecuteBehaviorCase(caseName);
                }

                WriteHarnessResults(resultFilePath, results);
                TryExitUnityBatchmode(0);
            }
            catch (System.Exception ex)
            {
                Dictionary<string, BehaviorCaseResult> failedResults = BuildHarnessFailureResults(
                    "Unity harness 执行异常: " + ex,
                    logFilePath);
                try
                {
                    WriteHarnessResults(resultFilePath, failedResults);
                }
                catch
                {
                }

                TryExitUnityBatchmode(1);
            }
        }

        /// <summary>
        /// 在 Unity Editor 环境中执行单条行为检查，并把异常转为失败结果。
        /// </summary>
        private static BehaviorCaseResult ExecuteBehaviorCase(string caseName)
        {
            try
            {
                switch (caseName)
                {
                    case nameof(StepSetActiveAction_Apply_ShouldChangeActiveState):
                        RunStepSetActiveActionCase();
                        break;
                    case nameof(StepSetPositionAction_Apply_ShouldWriteTransformPosition):
                        RunStepSetPositionActionCase();
                        break;
                    case nameof(StepDestroyTargetAction_Apply_ShouldDestroyTargetImmediatelyInEditor):
                        RunStepDestroyTargetActionCase();
                        break;
                    default:
                        return BehaviorCaseResult.Fail("未知的行为用例: " + caseName);
                }

                return BehaviorCaseResult.Pass();
            }
            catch (System.Exception ex)
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
        /// 解析 batchmode 需要执行的行为用例名称列表。
        /// </summary>
        private static string[] ParseCaseNames(string rawCaseNames)
        {
            if (string.IsNullOrEmpty(rawCaseNames))
            {
                return GetBehaviorCaseNames();
            }

            return rawCaseNames
                .Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrEmpty(item))
                .ToArray();
        }

        /// <summary>
        /// 返回 3 条对象行为用例名称，供 dotnet 宿主与 Unity harness 共享。
        /// </summary>
        private static string[] GetBehaviorCaseNames()
        {
            return new[]
            {
                nameof(StepSetActiveAction_Apply_ShouldChangeActiveState),
                nameof(StepSetPositionAction_Apply_ShouldWriteTransformPosition),
                nameof(StepDestroyTargetAction_Apply_ShouldDestroyTargetImmediatelyInEditor)
            };
        }

        /// <summary>
        /// 把行为测试结果写入本地文本文件，供 dotnet 宿主读取。
        /// </summary>
        private static void WriteHarnessResults(string resultFilePath, Dictionary<string, BehaviorCaseResult> results)
        {
            StringBuilder builder = new StringBuilder();
            foreach (string caseName in GetBehaviorCaseNames())
            {
                BehaviorCaseResult result;
                if (!results.TryGetValue(caseName, out result))
                {
                    result = BehaviorCaseResult.Fail("Unity harness 未返回结果。");
                }

                string encodedMessage = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(result.Message ?? string.Empty));
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
        private static Dictionary<string, BehaviorCaseResult> ReadHarnessResults(string resultFilePath, string logFilePath)
        {
            Dictionary<string, BehaviorCaseResult> results = new Dictionary<string, BehaviorCaseResult>();
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
                bool isPassed = parts[1] == "PASS";
                string message = string.Empty;
                if (parts.Length >= 3 && !string.IsNullOrEmpty(parts[2]))
                {
                    byte[] bytes = System.Convert.FromBase64String(parts[2]);
                    message = Encoding.UTF8.GetString(bytes);
                }

                if (!isPassed && !string.IsNullOrEmpty(logFilePath))
                {
                    message = string.IsNullOrEmpty(message) ? "日志: " + logFilePath : message + "\n日志: " + logFilePath;
                }

                results[caseName] = new BehaviorCaseResult(isPassed, message);
            }

            foreach (string caseName in GetBehaviorCaseNames())
            {
                if (!results.ContainsKey(caseName))
                {
                    results[caseName] = BehaviorCaseResult.Fail("Unity harness 缺少结果项。日志: " + logFilePath);
                }
            }

            return results;
        }

        /// <summary>
        /// 为所有行为用例构造统一失败结果，便于 dotnet 侧逐条断言。
        /// </summary>
        private static Dictionary<string, BehaviorCaseResult> BuildHarnessFailureResults(string message, string logFilePath)
        {
            Dictionary<string, BehaviorCaseResult> results = new Dictionary<string, BehaviorCaseResult>();
            string fullMessage = string.IsNullOrEmpty(logFilePath) ? message : message + "\n日志: " + logFilePath;
            string[] caseNames = GetBehaviorCaseNames();
            for (int i = 0; i < caseNames.Length; i++)
            {
                results[caseNames[i]] = BehaviorCaseResult.Fail(fullMessage);
            }

            return results;
        }

        /// <summary>
        /// 解析 Unity.exe 路径；优先从生成 csproj 中的 UnityEngine.dll HintPath 回推。
        /// </summary>
        private static string ResolveUnityExePath(string projectRoot)
        {
            string csprojPath = Path.Combine(projectRoot, "LWFramework.Tests.StepSystem.EditMode.csproj");
            if (File.Exists(csprojPath))
            {
                string[] lines = File.ReadAllLines(csprojPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    int hintStart = line.IndexOf("<HintPath>", System.StringComparison.Ordinal);
                    int unityEngineIndex = line.IndexOf("UnityEngine.dll", System.StringComparison.OrdinalIgnoreCase);
                    if (hintStart < 0 || unityEngineIndex < 0)
                    {
                        continue;
                    }

                    string hintPath = line.Substring(hintStart + "<HintPath>".Length);
                    int hintEnd = hintPath.IndexOf("</HintPath>", System.StringComparison.Ordinal);
                    if (hintEnd >= 0)
                    {
                        hintPath = hintPath.Substring(0, hintEnd);
                    }

                    string fullHintPath = Path.GetFullPath(hintPath, projectRoot);
                    string currentDirectory = Path.GetDirectoryName(fullHintPath);
                    while (!string.IsNullOrEmpty(currentDirectory))
                    {
                        string candidate = Path.Combine(currentDirectory, "Unity.exe");
                        if (File.Exists(candidate))
                        {
                            return candidate;
                        }

                        currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
                    }
                }
            }

            string[] fallbackCandidates =
            {
                @"E:\Softwear\UnityEditor\2022.3.62f3\Editor\Unity.exe",
                @"C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe"
            };
            for (int i = 0; i < fallbackCandidates.Length; i++)
            {
                if (File.Exists(fallbackCandidates[i]))
                {
                    return fallbackCandidates[i];
                }
            }

            throw new FileNotFoundException("未找到 Unity.exe，请确认生成 csproj 中的 UnityEngine.dll HintPath 可解析。");
        }

        /// <summary>
        /// 从当前运行目录或程序集输出目录向上查找 Unity 项目根目录。
        /// </summary>
        private static string FindProjectRoot()
        {
            string[] startPaths =
            {
                Directory.GetCurrentDirectory(),
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

            throw new DirectoryNotFoundException("未找到 Unity 项目根目录。");
        }

        /// <summary>
        /// 从指定起点目录向上查找同时包含 Assets 与 ProjectSettings 的目录。
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

        /// <summary>
        /// 获取动作类型上的展示元数据特性。
        /// </summary>
        private static StepActionInfoAttribute GetActionInfo(Type actionType)
        {
            StepActionInfoAttribute info = Attribute.GetCustomAttribute(actionType, typeof(StepActionInfoAttribute), false) as StepActionInfoAttribute;
            Assert.IsNotNull(info, actionType.FullName + " 缺少 StepActionInfoAttribute。");
            return info;
        }

        /// <summary>
        /// 获取动作类型上指定 key 的参数元数据特性。
        /// </summary>
        private static StepParamAttribute GetParamAttribute(Type actionType, string key)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo[] fields = actionType.GetFields(flags);
            for (int i = 0; i < fields.Length; i++)
            {
                StepParamAttribute fieldAttr = Attribute.GetCustomAttribute(fields[i], typeof(StepParamAttribute), true) as StepParamAttribute;
                if (fieldAttr != null && fieldAttr.Key == key)
                {
                    return fieldAttr;
                }
            }

            PropertyInfo[] properties = actionType.GetProperties(flags);
            for (int i = 0; i < properties.Length; i++)
            {
                StepParamAttribute propertyAttr = Attribute.GetCustomAttribute(properties[i], typeof(StepParamAttribute), true) as StepParamAttribute;
                if (propertyAttr != null && propertyAttr.Key == key)
                {
                    return propertyAttr;
                }
            }

            Assert.Fail(actionType.FullName + " 缺少参数元数据: " + key);
            return null;
        }

        /// <summary>
        /// 若当前运行环境为 Unity Editor，则通过反射请求设置 batchmode 退出码。
        /// </summary>
        private static void TryExitUnityBatchmode(int exitCode)
        {
            Type editorApplicationType = Type.GetType("UnityEditor.EditorApplication, UnityEditor");
            if (editorApplicationType == null)
            {
                return;
            }

            MethodInfo exitMethod = editorApplicationType.GetMethod("Exit", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
            if (exitMethod == null)
            {
                return;
            }

            exitMethod.Invoke(null, new object[] { exitCode });
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
