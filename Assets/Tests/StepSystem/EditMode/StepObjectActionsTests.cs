using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
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
        private const string ResultFileEnv = "LW_STEP_OBJECT_ACTIONS_RESULT_FILE";
        private const string CaseNamesEnv = "LW_STEP_OBJECT_ACTIONS_CASES";
        private const string LogFileEnv = "LW_STEP_OBJECT_ACTIONS_LOG_FILE";

        private static bool? s_CanUseUnityObjects;
        private static readonly object s_BehaviorHarnessLock = new object();
        private static Dictionary<string, StepObjectActionsBatchmodeEntry.BehaviorCaseResult> s_BehaviorHarnessResults;

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
            DestroyIfExists("StepTarget_SetRotation");
            DestroyIfExists("StepTarget_SetScale");
            DestroyIfExists("StepParent_SetParent");
            DestroyIfExists("StepTarget_SetParent");
            DestroyIfExists("StepTarget_Destroy");
            DestroyIfExists("StepTarget_PlayParticleLoop");
            DestroyIfExists("StepPrefab_Instance");
        }

        /// <summary>
        /// Apply 应修改对象激活状态。
        /// </summary>
        [Test]
        public void StepSetActiveAction_Apply_ShouldChangeActiveState()
        {
            AssertBehaviorCasePasses(nameof(StepSetActiveAction_Apply_ShouldChangeActiveState));
        }

        /// <summary>
        /// Apply 应写入本地位置。
        /// </summary>
        [Test]
        public void StepSetPositionAction_Apply_ShouldWriteTransformPosition()
        {
            AssertBehaviorCasePasses(nameof(StepSetPositionAction_Apply_ShouldWriteTransformPosition));
        }

        /// <summary>
        /// Apply 应写入本地旋转。
        /// </summary>
        [Test]
        public void StepSetRotationAction_Apply_ShouldWriteTransformRotation()
        {
            AssertBehaviorCasePasses(nameof(StepSetRotationAction_Apply_ShouldWriteTransformRotation));
        }

        /// <summary>
        /// Apply 应写入本地缩放。
        /// </summary>
        [Test]
        public void StepSetScaleAction_Apply_ShouldWriteLocalScale()
        {
            AssertBehaviorCasePasses(nameof(StepSetScaleAction_Apply_ShouldWriteLocalScale));
        }

        /// <summary>
        /// Apply 应设置父节点。
        /// </summary>
        [Test]
        public void StepSetParentAction_Apply_ShouldSetParentTransform()
        {
            AssertBehaviorCasePasses(nameof(StepSetParentAction_Apply_ShouldSetParentTransform));
        }

        /// <summary>
        /// Apply 在编辑器中应立即销毁对象。
        /// </summary>
        [Test]
        public void StepDestroyTargetAction_Apply_ShouldDestroyTargetImmediatelyInEditor()
        {
            AssertBehaviorCasePasses(nameof(StepDestroyTargetAction_Apply_ShouldDestroyTargetImmediatelyInEditor));
        }

        /// <summary>
        /// 循环粒子在等待完成模式下应立即转为非阻塞完成。
        /// </summary>
        [Test]
        public void StepPlayParticleAction_Enter_WithLoopingParticlesAndWaitForFinish_ShouldFinishImmediately()
        {
            AssertBehaviorCasePasses(nameof(StepPlayParticleAction_Enter_WithLoopingParticlesAndWaitForFinish_ShouldFinishImmediately));
        }

        /// <summary>
        /// Apply 应实例化预制体。
        /// </summary>
        [Test]
        public void StepInstantiatePrefabAction_Apply_ShouldInstantiatePrefab()
        {
            AssertBehaviorCasePasses(nameof(StepInstantiatePrefabAction_Apply_ShouldInstantiatePrefab));
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
        /// 旋转对象动作应暴露完整的展示与参数元数据。
        /// </summary>
        [Test]
        public void StepRotateObjectAction_Metadata_ShouldMatchSpec()
        {
            StepActionInfoAttribute info = GetActionInfo(typeof(StepRotateObjectAction));
            StepParamAttribute targetParam = GetParamAttribute(typeof(StepRotateObjectAction), "target");
            StepParamAttribute xParam = GetParamAttribute(typeof(StepRotateObjectAction), "x");
            StepParamAttribute rotateTimeParam = GetParamAttribute(typeof(StepRotateObjectAction), "rotateTime");

            Assert.AreEqual("旋转对象", info.DisplayName);
            Assert.AreEqual("对象控制", info.Category);
            Assert.AreEqual("Rotate:{target}", info.SummaryTemplate);
            Assert.AreEqual("目标对象", targetParam.Label);
            Assert.AreEqual(0, targetParam.Order);
            Assert.AreEqual("目标X", xParam.Label);
            Assert.AreEqual(1, xParam.Order);
            Assert.AreEqual("旋转时长", rotateTimeParam.Label);
            Assert.AreEqual(5, rotateTimeParam.Order);
        }

        /// <summary>
        /// 缩放对象动作应暴露完整的展示与参数元数据。
        /// </summary>
        [Test]
        public void StepScaleObjectAction_Metadata_ShouldMatchSpec()
        {
            StepActionInfoAttribute info = GetActionInfo(typeof(StepScaleObjectAction));
            StepParamAttribute targetParam = GetParamAttribute(typeof(StepScaleObjectAction), "target");
            StepParamAttribute xParam = GetParamAttribute(typeof(StepScaleObjectAction), "x");
            StepParamAttribute scaleTimeParam = GetParamAttribute(typeof(StepScaleObjectAction), "scaleTime");

            Assert.AreEqual("缩放对象", info.DisplayName);
            Assert.AreEqual("对象控制", info.Category);
            Assert.AreEqual("Scale:{target}", info.SummaryTemplate);
            Assert.AreEqual("目标对象", targetParam.Label);
            Assert.AreEqual(0, targetParam.Order);
            Assert.AreEqual("缩放X", xParam.Label);
            Assert.AreEqual(1, xParam.Order);
            Assert.AreEqual("缩放时长", scaleTimeParam.Label);
            Assert.AreEqual(4, scaleTimeParam.Order);
        }

        /// <summary>
        /// 旧版动画动作应暴露完整的展示与参数元数据。
        /// </summary>
        [Test]
        public void StepLegacyAnimationAction_Metadata_ShouldMatchSpec()
        {
            StepActionInfoAttribute info = GetActionInfo(typeof(StepLegacyAnimationAction));
            StepParamAttribute targetParam = GetParamAttribute(typeof(StepLegacyAnimationAction), "target");
            StepParamAttribute stateParam = GetParamAttribute(typeof(StepLegacyAnimationAction), "state");
            StepParamAttribute reverseParam = GetParamAttribute(typeof(StepLegacyAnimationAction), "reverse");

            Assert.AreEqual("播放旧版动画", info.DisplayName);
            Assert.AreEqual("动画与特效", info.Category);
            Assert.AreEqual("Legacy:{state}", info.SummaryTemplate);
            Assert.AreEqual("目标对象", targetParam.Label);
            Assert.AreEqual(0, targetParam.Order);
            Assert.AreEqual("状态名", stateParam.Label);
            Assert.AreEqual(1, stateParam.Order);
            Assert.AreEqual("倒放", reverseParam.Label);
            Assert.AreEqual(2, reverseParam.Order);
        }

        /// <summary>
        /// Animator 动作参数应补齐展示标签与顺序。
        /// </summary>
        [Test]
        public void StepAnimationAction_ParamMetadata_ShouldMatchSpec()
        {
            StepParamAttribute targetParam = GetParamAttribute(typeof(StepAnimationAction), "target");
            StepParamAttribute stateParam = GetParamAttribute(typeof(StepAnimationAction), "state");
            StepParamAttribute reverseParam = GetParamAttribute(typeof(StepAnimationAction), "reverse");
            StepParamAttribute manualSpeedKeyParam = GetParamAttribute(typeof(StepAnimationAction), "manualSpeedKey");

            Assert.AreEqual("目标对象", targetParam.Label);
            Assert.AreEqual(0, targetParam.Order);
            Assert.AreEqual("状态名", stateParam.Label);
            Assert.AreEqual(1, stateParam.Order);
            Assert.AreEqual("倒放", reverseParam.Label);
            Assert.AreEqual(2, reverseParam.Order);
            Assert.AreEqual("速度上下文键", manualSpeedKeyParam.Label);
            Assert.AreEqual(3, manualSpeedKeyParam.Order);
        }

        /// <summary>
        /// 当前宿主无法直接访问 Unity 对象时，断言对应行为用例在 Unity batchmode 中执行通过。
        /// </summary>
        private static void AssertBehaviorCasePasses(string caseName)
        {
            StepObjectActionsBatchmodeEntry.BehaviorCaseResult result;
            if (CanUseUnityObjects())
            {
                result = StepObjectActionsBatchmodeEntry.ExecuteCase(caseName);
            }
            else
            {
                Dictionary<string, StepObjectActionsBatchmodeEntry.BehaviorCaseResult> results = EnsureBehaviorHarnessResults();
                if (!results.TryGetValue(caseName, out result))
                {
                    Assert.Fail("Unity harness 未返回用例结果: " + caseName);
                    return;
                }
            }

            Assert.IsTrue(result.IsPassed, result.Message);
        }

        /// <summary>
        /// 确保已获取 Unity batchmode 行为测试结果；首次调用时仅启动一次 Unity。
        /// </summary>
        private static Dictionary<string, StepObjectActionsBatchmodeEntry.BehaviorCaseResult> EnsureBehaviorHarnessResults()
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
        /// 启动一次 Unity batchmode，在真实 Editor 环境内执行对象行为断言。
        /// </summary>
        private static Dictionary<string, StepObjectActionsBatchmodeEntry.BehaviorCaseResult> RunBehaviorHarnessInUnity()
        {
            string projectRoot = FindProjectRoot();
            string unityExePath = ResolveUnityExePath(projectRoot);
            string harnessDirectory = Path.Combine(projectRoot, "Temp", "StepObjectActionsHarness");
            Directory.CreateDirectory(harnessDirectory);

            string resultFilePath = Path.Combine(harnessDirectory, "results.txt");
            string warmupLogFilePath = Path.Combine(harnessDirectory, "unity-warmup.log");
            string logFilePath = Path.Combine(harnessDirectory, "unity-batchmode.log");
            string[] caseNames = StepObjectActionsBatchmodeEntry.GetBehaviorCaseNames();

            int warmupExitCode = RunUnityBatchmodeProcess(
                unityExePath,
                projectRoot,
                null,
                warmupLogFilePath,
                null,
                null);
            if (warmupExitCode != 0)
            {
                return StepObjectActionsBatchmodeEntry.BuildFailureResults(
                    "Unity batchmode 预热失败，ExitCode=" + warmupExitCode + "。日志: " + warmupLogFilePath,
                    warmupLogFilePath,
                    caseNames);
            }

            Dictionary<string, string> environment = new Dictionary<string, string>
            {
                { ResultFileEnv, resultFilePath },
                { LogFileEnv, logFilePath },
                { CaseNamesEnv, string.Join(";", caseNames) }
            };
            int executeExitCode = RunUnityBatchmodeProcess(
                unityExePath,
                projectRoot,
                "LWFramework.Tests.StepSystem.EditMode.StepObjectActionsBatchmodeEntry.Run",
                logFilePath,
                environment,
                resultFilePath);
            if (executeExitCode != 0)
            {
                return StepObjectActionsBatchmodeEntry.BuildFailureResults(
                    "Unity batchmode 执行失败，ExitCode=" + executeExitCode + "。日志: " + logFilePath,
                    logFilePath,
                    caseNames);
            }

            if (!File.Exists(resultFilePath))
            {
                return StepObjectActionsBatchmodeEntry.BuildFailureResults(
                    "Unity harness 未写出结果文件: " + resultFilePath,
                    logFilePath,
                    caseNames);
            }

            return StepObjectActionsBatchmodeEntry.ReadResults(resultFilePath, logFilePath);
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
                    int hintStart = line.IndexOf("<HintPath>", StringComparison.Ordinal);
                    int unityEngineIndex = line.IndexOf("UnityEngine.dll", StringComparison.OrdinalIgnoreCase);
                    if (hintStart < 0 || unityEngineIndex < 0)
                    {
                        continue;
                    }

                    string hintPath = line.Substring(hintStart + "<HintPath>".Length);
                    int hintEnd = hintPath.IndexOf("</HintPath>", StringComparison.Ordinal);
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
    }
}
