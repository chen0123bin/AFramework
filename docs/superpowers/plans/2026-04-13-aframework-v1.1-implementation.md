# AFramework v1.1 Reuse Stabilization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在不大规模迁移目录的前提下，完成 `AFramework v1.1` 的保守模块化收口，让核心能力、宿主项目、业务插件的边界清晰可测，并让 `Hotfix` 与 `StepSystem` 的接入规则真正落地。

**Architecture:** 本计划把实现拆成 5 个顺序任务。前两项先建立可测试的启动配置与核心引导器，第三项把默认 `ByCode` / 可选 `Reflection` 的热更规则接到真正的启动链路里，第四项把 `StepSystem` 从 `LWCore` 的直接访问入口中抽离成业务插件访问方式，第五项统一修正文档和接入说明。这样每个任务完成后，工程都保持“可编译、可测、可提交”。

**Tech Stack:** Unity 2022.3.62f3、C#、UniTask、现有 `MainManager + ManagerUtility` 架构、Unity EditMode Tests、Markdown 文档

---

## 文件结构与职责

### 新增文件

- `Assets/LWFramework/RunTime/Core/Bootstrap/FrameworkModuleId.cs`
  - 定义 `v1.1` 的模块标识枚举，统一表达核心模块与可选模块。
- `Assets/LWFramework/RunTime/Core/Bootstrap/FrameworkBootstrapSettings.cs`
  - 定义默认启动配置、固定 `Reflection` 热更目录、模块启停开关。
- `Assets/LWFramework/RunTime/Core/Bootstrap/FrameworkBootstrapper.cs`
  - 提供核心模块注册、核心初始化、热更预热与首个流程类型解析。
- `Assets/Scripts/StartupOptionalModules.cs`
  - 宿主项目层的可选模块注册器，负责 `Audio` 与 `StepSystem` 的显式接入。
- `Assets/LWFramework/RunTime/StepSystem/StepManagerUtility.cs`
  - 提供 `StepSystem` 插件层自己的 Manager 访问入口，替代 `LWCore.ManagerUtility.StepMgr`。
- `Assets/Tests/Framework/EditMode/FrameworkBootstrapSettingsTests.cs`
  - 保护默认启动配置与模块边界。
- `Assets/Tests/Framework/EditMode/FrameworkBootstrapperTests.cs`
  - 保护核心模块注册与核心初始化逻辑。
- `Assets/Tests/Framework/EditMode/FrameworkHotfixBootstrapTests.cs`
  - 保护默认 `ByCode` / 可选 `Reflection` 热更预热行为，以及固定目录约束。
- `Assets/Tests/StepSystem/EditMode/StepManagerUtilityTests.cs`
  - 保护 `StepSystem` 插件访问入口在“未注册 / 已注册”两种场景下的行为。
- `docs/v1.1/框架接入与模块边界.md`
  - 提供 v1.1 接入说明，明确核心层、宿主项目层、业务插件层与热更规则。

### 修改文件

- `Assets/Scripts/Startup.cs`
  - 从“直接写死注册所有模块”改为“构造启动配置 + 调用核心引导器 + 注册宿主可选模块”。
- `Assets/LWFramework/RunTime/Core/ManagerUtility.cs`
  - 删除 `StepMgr` 直接访问入口，避免 `LWCore` 反向依赖 `StepSystem`。
- `Assets/LWFramework/RunTime/Core/InterfaceManager/IHotfixManager.cs`
  - 修正文档与注释口径，使默认 `ByCode`、可选 `Reflection` 的规则与实现一致。
- `Assets/Scripts/Procedure/StepProcedure.cs`
  - 改用 `StepManagerUtility` 访问步骤管理器。
- `Assets/LWFramework/Editor/StepSystem/StepEditorWindow.cs`
  - 改用 `StepManagerUtility` 访问步骤管理器。
- `Assets/LWFramework/RunTime/Assets/README.md`
  - 移除过时的 `LWAssetsManager.Instance` 文档写法，改成当前接入方式。

### 只读参考文件

- `Assets/LWFramework/RunTime/Core/MainManager.cs`
- `Assets/LWFramework/RunTime/HotFix/HotFixCodeManager.cs`
- `Assets/LWFramework/RunTime/HotFix/HotFixRefManager.cs`
- `Assets/LWFramework/RunTime/Core/InterfaceManager/IStepManager.cs`
- `Assets/Tests/Framework/EditMode/HotfixReflectionTests.cs`
- `Assets/Tests/Framework/EditMode/UIRuntimeTests.cs`
- `docs/superpowers/specs/2026-04-13-aframework-v1.1-design.md`

---

### Task 1: 建立启动配置模型

**Files:**
- Create: `Assets/LWFramework/RunTime/Core/Bootstrap/FrameworkModuleId.cs`
- Create: `Assets/LWFramework/RunTime/Core/Bootstrap/FrameworkBootstrapSettings.cs`
- Test: `Assets/Tests/Framework/EditMode/FrameworkBootstrapSettingsTests.cs`

- [ ] **Step 1: 先写失败的配置测试**

```csharp
using LWCore;
using NUnit.Framework;

namespace LWFramework.Tests.Framework.EditMode
{
    /// <summary>
    /// FrameworkBootstrapSettings 默认值测试。
    /// </summary>
    [TestFixture]
    public class FrameworkBootstrapSettingsTests
    {
        /// <summary>
        /// 验证默认配置只启用核心模块，不启用 Audio 和 StepSystem。
        /// </summary>
        [Test]
        public void CreateDefault_ShouldEnableOnlyCoreModules()
        {
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();

            Assert.IsTrue(settings.IsModuleEnabled(FrameworkModuleId.Assets));
            Assert.IsTrue(settings.IsModuleEnabled(FrameworkModuleId.Event));
            Assert.IsTrue(settings.IsModuleEnabled(FrameworkModuleId.UI));
            Assert.IsTrue(settings.IsModuleEnabled(FrameworkModuleId.Hotfix));
            Assert.IsTrue(settings.IsModuleEnabled(FrameworkModuleId.FSM));
            Assert.IsFalse(settings.IsModuleEnabled(FrameworkModuleId.Audio));
            Assert.IsFalse(settings.IsModuleEnabled(FrameworkModuleId.StepSystem));
        }

        /// <summary>
        /// 验证默认热更路线、默认流程名与固定 Reflection 目录常量。
        /// </summary>
        [Test]
        public void CreateDefault_ShouldUseByCodeAndFixedReflectionDirectory()
        {
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();

            Assert.AreEqual(HotfixCodeRunMode.ByCode, settings.HotfixMode);
            Assert.AreEqual("StartProcedure", settings.ProcedureName);
            Assert.AreEqual(string.Empty, settings.ReflectionHotfixAssemblyName);
            Assert.AreEqual("Hotfix/", FrameworkBootstrapSettings.DEFAULT_REFLECTION_HOTFIX_DIR);
        }
    }
}
```

- [ ] **Step 2: 运行测试，确认当前缺少配置模型而失败**

Run:

```powershell
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter LWFramework.Tests.Framework.EditMode.FrameworkBootstrapSettingsTests -testResults Logs/FrameworkBootstrapSettingsTests.xml -quit
```

Expected:
- 编译失败或测试失败，提示 `FrameworkBootstrapSettings` 或 `FrameworkModuleId` 不存在。

- [ ] **Step 3: 写最小实现，让配置模型具备稳定默认值**

`Assets/LWFramework/RunTime/Core/Bootstrap/FrameworkModuleId.cs`

```csharp
namespace LWCore
{
    /// <summary>
    /// v1.1 框架模块标识。
    /// </summary>
    public enum FrameworkModuleId
    {
        Assets = 0,
        Event = 1,
        UI = 2,
        Hotfix = 3,
        FSM = 4,
        Audio = 5,
        StepSystem = 6,
    }
}
```

`Assets/LWFramework/RunTime/Core/Bootstrap/FrameworkBootstrapSettings.cs`

```csharp
using System;

namespace LWCore
{
    /// <summary>
    /// v1.1 框架启动配置。
    /// </summary>
    [Serializable]
    public sealed class FrameworkBootstrapSettings
    {
        public const string DEFAULT_PROCEDURE_NAME = "StartProcedure";
        public const string DEFAULT_REFLECTION_HOTFIX_DIR = "Hotfix/";

        public string ProcedureName = DEFAULT_PROCEDURE_NAME;
        public HotfixCodeRunMode HotfixMode = HotfixCodeRunMode.ByCode;
        public string ReflectionHotfixAssemblyName = string.Empty;

        public bool EnableAssets = true;
        public bool EnableEvent = true;
        public bool EnableUI = true;
        public bool EnableHotfix = true;
        public bool EnableFSM = true;
        public bool EnableAudio = false;
        public bool EnableStepSystem = false;

        /// <summary>
        /// 创建默认启动配置。
        /// </summary>
        /// <returns>默认配置。</returns>
        public static FrameworkBootstrapSettings CreateDefault()
        {
            return new FrameworkBootstrapSettings();
        }

        /// <summary>
        /// 判断指定模块是否启用。
        /// </summary>
        /// <param name="moduleId">模块标识。</param>
        /// <returns>是否启用。</returns>
        public bool IsModuleEnabled(FrameworkModuleId moduleId)
        {
            switch (moduleId)
            {
                case FrameworkModuleId.Assets:
                    return EnableAssets;
                case FrameworkModuleId.Event:
                    return EnableEvent;
                case FrameworkModuleId.UI:
                    return EnableUI;
                case FrameworkModuleId.Hotfix:
                    return EnableHotfix;
                case FrameworkModuleId.FSM:
                    return EnableFSM;
                case FrameworkModuleId.Audio:
                    return EnableAudio;
                case FrameworkModuleId.StepSystem:
                    return EnableStepSystem;
                default:
                    return false;
            }
        }
    }
}
```

- [ ] **Step 4: 再跑一遍默认值测试，确认通过**

Run:

```powershell
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter LWFramework.Tests.Framework.EditMode.FrameworkBootstrapSettingsTests -testResults Logs/FrameworkBootstrapSettingsTests.xml -quit
```

Expected:
- `FrameworkBootstrapSettingsTests` 全部 PASS。

- [ ] **Step 5: 提交配置模型**

```bash
git add Assets/LWFramework/RunTime/Core/Bootstrap/FrameworkModuleId.cs Assets/LWFramework/RunTime/Core/Bootstrap/FrameworkBootstrapSettings.cs Assets/Tests/Framework/EditMode/FrameworkBootstrapSettingsTests.cs
git commit -m "feat: 增加框架启动配置模型"
```

---

### Task 2: 增加核心引导器并保护默认核心注册

**Files:**
- Create: `Assets/LWFramework/RunTime/Core/Bootstrap/FrameworkBootstrapper.cs`
- Test: `Assets/Tests/Framework/EditMode/FrameworkBootstrapperTests.cs`

- [ ] **Step 1: 先写失败的核心引导测试**

```csharp
using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using LWFMS;
using LWAssets;
using LWCore;
using LWUI;
using LWHotfix;
using NUnit.Framework;
using UnityEngine;

namespace LWFramework.Tests.Framework.EditMode
{
    /// <summary>
    /// FrameworkBootstrapper 核心引导测试。
    /// </summary>
    [TestFixture]
    public class FrameworkBootstrapperTests
    {
        private MainManager m_MainManager;

        [SetUp]
        public void SetUp()
        {
            ResetMainManagerSingleton();
            m_MainManager = MainManager.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            if (m_MainManager != null)
            {
                m_MainManager.ClearManager();
            }

            ResetMainManagerSingleton();
            m_MainManager = null;
        }

        /// <summary>
        /// 验证默认核心引导只注册核心模块，不注册 Audio 与 StepSystem。
        /// </summary>
        [Test]
        public void RegisterCoreManagers_DefaultSettings_ShouldRegisterOnlyCoreManagers()
        {
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();
            FrameworkBootstrapperDependencies dependencies = new FrameworkBootstrapperDependencies
            {
                CreateAssetsManager = () => new FakeAssetsManager(),
                CreateEventManager = () => new FakeEventManager(),
                CreateUIManager = () => new FakeUIManager(),
                CreateHotfixManager = _ => new FakeHotfixManager(),
                CreateFSMManager = () => new FakeFSMManager(),
            };

            FrameworkBootstrapper bootstrapper = new FrameworkBootstrapper(dependencies);
            bootstrapper.RegisterCoreManagers(settings);

            Assert.IsTrue(m_MainManager.TryGetManager<IAssetsManager>(out _));
            Assert.IsTrue(m_MainManager.TryGetManager<IEventManager>(out _));
            Assert.IsTrue(m_MainManager.TryGetManager<IUIManager>(out _));
            Assert.IsTrue(m_MainManager.TryGetManager<IHotfixManager>(out _));
            Assert.IsTrue(m_MainManager.TryGetManager<IFSMManager>(out _));
            Assert.IsFalse(m_MainManager.TryGetManager<IAudioManager>(out _));
            Assert.IsFalse(m_MainManager.TryGetManager<IStepManager>(out _));
        }

        /// <summary>
        /// 验证核心初始化会初始化 Assets 并设置 MonoBehaviour 宿主。
        /// </summary>
        [Test]
        public async UniTask InitializeCoreAsync_ShouldInitializeAssetsAndSetHost()
        {
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();
            FakeAssetsManager fakeAssetsManager = new FakeAssetsManager();
            FrameworkBootstrapperDependencies dependencies = new FrameworkBootstrapperDependencies
            {
                CreateAssetsManager = () => fakeAssetsManager,
                CreateEventManager = () => new FakeEventManager(),
                CreateUIManager = () => new FakeUIManager(),
                CreateHotfixManager = _ => new FakeHotfixManager(),
                CreateFSMManager = () => new FakeFSMManager(),
            };

            GameObject hostObject = new GameObject("FrameworkBootstrapperTests_Host");
            TestBootstrapHost host = hostObject.AddComponent<TestBootstrapHost>();

            try
            {
                FrameworkBootstrapper bootstrapper = new FrameworkBootstrapper(dependencies);
                await bootstrapper.InitializeCoreAsync(host, settings);
                Assert.AreEqual(1, fakeAssetsManager.InitializeCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hostObject);
            }
        }

        private static void ResetMainManagerSingleton()
        {
            Type singletonType = typeof(Singleton<MainManager>);
            FieldInfo instanceField = singletonType.GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            instanceField.SetValue(null, null);
        }

        private sealed class TestBootstrapHost : MonoBehaviour
        {
        }

        private sealed class FakeAssetsManager : IAssetsManager, IManager
        {
            public int InitializeCount { get; private set; }
            public bool IsInitialized { get; private set; }
            public PlayMode CurrentPlayMode => PlayMode.EditorSimulate;
            public IAssetLoader Loader => null;
            public DownloadManager Downloader => null;
            public CacheManager Cache => null;
            public PreloadManager Preloader => null;
            public VersionManager Version => null;
            public void Init() { }
            public void Update() { }
            public UniTask InitializeAsync(LWAssetsConfig config = null) { InitializeCount++; IsInitialized = true; return UniTask.CompletedTask; }
            public UniTask WarmupShadersAsync(System.Threading.CancellationToken token = default) { return UniTask.CompletedTask; }
            public UniTask<BundleManifest> LoadManifestAsync() { return UniTask.FromResult<BundleManifest>(null); }
            public T LoadAsset<T>(string assetPath) where T : UnityEngine.Object { return null; }
            public byte[] LoadRawFile(string assetPath) { return null; }
            public string LoadRawFileText(string assetPath) { return null; }
            public GameObject Instantiate(string assetPath, Transform spawnPoint = null) { return null; }
            public UniTask<GameObject> InstantiateAsync(string assetPath, Transform spawnPoint = null) { return UniTask.FromResult<GameObject>(null); }
            public UniTask<T> LoadAssetAsync<T>(string assetPath, System.Threading.CancellationToken cancellationToken = default) where T : UnityEngine.Object { return UniTask.FromResult<T>(null); }
            public UniTask<byte[]> LoadRawFileAsync(string assetPath, System.Threading.CancellationToken cancellationToken = default) { return UniTask.FromResult<byte[]>(null); }
            public UniTask<string> LoadRawFileTextAsync(string assetPath, System.Threading.CancellationToken cancellationToken = default) { return UniTask.FromResult<string>(null); }
            public UniTask<SceneHandle> LoadSceneAsync(string scenePath, UnityEngine.SceneManagement.LoadSceneMode mode = UnityEngine.SceneManagement.LoadSceneMode.Single, bool activateOnLoad = true, IProgress<float> progress = null, System.Threading.CancellationToken cancellationToken = default) { return UniTask.FromResult<SceneHandle>(null); }
            public UniTask UnloadSceneAsync(string scenePath, bool forceRelease = true, System.Threading.CancellationToken cancellationToken = default) { return UniTask.CompletedTask; }
            public UniTask UnloadSceneAsync(SceneHandle sceneHandle, bool forceRelease = true, System.Threading.CancellationToken cancellationToken = default) { return UniTask.CompletedTask; }
            public UniTask<T[]> LoadAssetsAsync<T>(string[] assetPaths, IProgress<float> progress = null, System.Threading.CancellationToken cancellationToken = default) where T : UnityEngine.Object { return UniTask.FromResult(new T[0]); }
            public void Release(UnityEngine.Object asset) { }
            public void Release(string assetPath) { }
            public UniTask UnloadUnusedAssetsAsync() { return UniTask.CompletedTask; }
            public void ForceUnloadAll() { }
            public UniTask<long> GetDownloadSizeAsync(string[] tags = null) { return UniTask.FromResult(0L); }
            public UniTask DownloadAsync(string[] tags = null, IProgress<DownloadProgress> progress = null, System.Threading.CancellationToken cancellationToken = default) { return UniTask.CompletedTask; }
            public void Destroy() { IsInitialized = false; }
        }

        private sealed class FakeEventManager : IEventManager, IManager
        {
            public void Init() { }
            public void Update() { }
            public void AddListener(string eventName, Action callback) { }
            public void AddListener<T>(string eventName, Action<T> callback) { }
            public void AddListener<T1, T2>(string eventName, Action<T1, T2> callback) { }
            public void AddListener<T1, T2, T3>(string eventName, Action<T1, T2, T3> callback) { }
            public void AddListener<T1, T2, T3, T4>(string eventName, Action<T1, T2, T3, T4> callback) { }
            public void RemoveListener(string eventName, Action callback) { }
            public void RemoveListener<T>(string eventName, Action<T> callback) { }
            public void RemoveListener<T1, T2>(string eventName, Action<T1, T2> callback) { }
            public void RemoveListener<T1, T2, T3>(string eventName, Action<T1, T2, T3> callback) { }
            public void RemoveListener<T1, T2, T3, T4>(string eventName, Action<T1, T2, T3, T4> callback) { }
            public void DispatchEvent(string eventName) { }
            public void DispatchEvent<T>(string eventName, T info) { }
            public void DispatchEvent<T1, T2>(string eventName, T1 info1, T2 info2) { }
            public void DispatchEvent<T1, T2, T3>(string eventName, T1 info1, T2 info2, T3 info3) { }
            public void DispatchEvent<T1, T2, T3, T4>(string eventName, T1 info1, T2 info2, T3 info3, T4 info4) { }
            public void Clear() { }
            public void ClearDispatchCounts() { }
            public void GetRuntimeInfos(System.Collections.Generic.List<LWEventManager.EventRuntimeInfo> results) { results.Clear(); }
        }

        private sealed class FakeUIManager : IUIManager, IManager
        {
            public UIUtility UIUtility => null;
            public Canvas UICanvas { get; set; }
            public Camera UICamera => null;
            public void Init() { }
            public void Update() { }
            public T OpenView<T>(object data = null, bool isLastSibling = false, bool enterStack = false) where T : BaseUIView { return null; }
            public BaseUIView OpenView(string viewType, object data = null, GameObject uiGameObject = null, bool isLastSibling = false, bool enterStack = false) { return null; }
            public UniTask<T> OpenViewAsync<T>(object data = null, bool isLastSibling = false, bool enterStack = false) where T : BaseUIView { return UniTask.FromResult<T>(null); }
            public void OpenDialog(string title, string content, Action<bool> ResultCallback, bool isShowCancel = true, bool isShowClose = true, bool isLastSibling = true) { }
            public UniTask<bool> OpenDialogAsync(string title, string content, bool isShowCancel = true, bool isShowClose = true, bool isLastSibling = true) { return UniTask.FromResult(false); }
            public void OpenLoadingBar(string tip = "当前正在加载...", bool isLastSibling = true) { }
            public void UpdateLoadingBar(float progress, string tip = null, bool isLastSibling = true) { }
            public void CloseLoadingBar() { }
            public BaseUIView BackView(bool isLastSibling = true) { return null; }
            public BaseUIView BackTwiceView(bool isLastSibling = true) { return null; }
            public BaseUIView BackUntilLastView(bool isLastSibling = true) { return null; }
            public T GetView<T>() where T : BaseUIView { return null; }
            public BaseUIView GetView(string viewType = null) { return null; }
            public BaseUIView[] GetAllView() { return Array.Empty<BaseUIView>(); }
            public void CloseView<T>(bool enterStack = false) { }
            public void CloseView(string viewName, bool enterStack = false) { }
            public void CloseView(BaseUIView view, bool enterStack = false) { }
            public void CloseOtherView<T>() { }
            public void CloseOtherView(string viewName) { }
            public void CloseOtherView(string[] viewNameArray) { }
            public void CloseAllView() { }
            public void ClearView(string viewName) { }
            public void ClearView<T>() { }
            public void ClearOtherView(string[] viewNameArray) { }
            public void ClearOtherView<T>() { }
            public void ClearAllView() { }
            public UniTask PreloadViewAsync(string loadPath) { return UniTask.CompletedTask; }
            public UniTask PreloadViewAsync<T>() where T : BaseUIView { return UniTask.CompletedTask; }
            public UniTask PreLoadDefaultUI() { return UniTask.CompletedTask; }
            public void SetStyle(string styleName) { }
            public string GetStyle() { return "Default"; }
        }

        private sealed class FakeHotfixManager : IHotfixManager, IManager
        {
            public bool Loaded => true;
            public void Init() { }
            public void Update() { }
            public UniTask LoadScriptAsync(string hotfixDllName, string dir = "Hotfix/") { return UniTask.CompletedTask; }
            public Type GetTypeByName(string typeName) { return null; }
            public T Instantiate<T>(string typeName, object[] args = null) { return default; }
            public void Invoke(string type, string method, object instance, params object[] args) { }
            public void Destroy() { }
            public void AddHotfixTypeAttr(System.Collections.Generic.List<Type> p_TypeArray) { }
            public System.Collections.Generic.List<TypeAttr> GetAttrTypeDataList<T>() { return new System.Collections.Generic.List<TypeAttr>(); }
            public T FindAttr<T>(string typeName) { return default; }
        }

        private sealed class FakeFSMManager : IFSMManager, IManager
        {
            public void Init() { }
            public void Update() { }
            public void RegisterFSM(FSMStateMachine fsm) { }
            public void UnRegisterFSM(FSMStateMachine fsm) { }
            public FSMStateMachine GetFSMByName(string name) { return null; }
            public FSMStateMachine GetFSMProcedure() { return null; }
            public bool IsExistFSM(string name) { return false; }
            public System.Collections.Generic.List<TypeAttr> GetFsmClassDataByName(string fsmName) { return new System.Collections.Generic.List<TypeAttr>(); }
            public void InitFSMManager() { }
        }
    }
}
```

- [ ] **Step 2: 运行测试，确认当前缺少核心引导器而失败**

Run:

```powershell
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter LWFramework.Tests.Framework.EditMode.FrameworkBootstrapperTests -testResults Logs/FrameworkBootstrapperTests.xml -quit
```

Expected:
- 编译失败或测试失败，提示 `FrameworkBootstrapper`、`FrameworkBootstrapperDependencies` 不存在。

- [ ] **Step 3: 实现核心引导器与依赖注入测试缝**

`Assets/LWFramework/RunTime/Core/Bootstrap/FrameworkBootstrapper.cs`

```csharp
using System;
using Cysharp.Threading.Tasks;
using LWFMS;
using LWAssets;
using LWHotfix;
using LWUI;
using UnityEngine;

namespace LWCore
{
    /// <summary>
    /// 核心引导依赖集合，用于生产环境默认实例化，也用于测试注入替身。
    /// </summary>
    public sealed class FrameworkBootstrapperDependencies
    {
        public Func<IAssetsManager> CreateAssetsManager = () => new LWAssetsManager();
        public Func<IEventManager> CreateEventManager = () => new LWEventManager();
        public Func<IUIManager> CreateUIManager = () => new UIManager();
        public Func<HotfixCodeRunMode, IHotfixManager> CreateHotfixManager = mode =>
        {
            switch (mode)
            {
                case HotfixCodeRunMode.ByReflection:
                    return new HotFixRefManager();
                case HotfixCodeRunMode.ByCode:
                default:
                    return new HotFixCodeManager();
            }
        };
        public Func<IFSMManager> CreateFSMManager = () => new FSMManager();
    }

    /// <summary>
    /// v1.1 核心引导器。
    /// </summary>
    public sealed class FrameworkBootstrapper
    {
        private readonly FrameworkBootstrapperDependencies m_Dependencies;

        /// <summary>
        /// 创建核心引导器。
        /// </summary>
        /// <param name="dependencies">可选依赖工厂。</param>
        public FrameworkBootstrapper(FrameworkBootstrapperDependencies dependencies = null)
        {
            m_Dependencies = dependencies ?? new FrameworkBootstrapperDependencies();
        }

        /// <summary>
        /// 注册默认核心模块。
        /// </summary>
        /// <param name="settings">启动配置。</param>
        public void RegisterCoreManagers(FrameworkBootstrapSettings settings)
        {
            ManagerUtility.MainMgr.Init();

            if (settings.IsModuleEnabled(FrameworkModuleId.Assets))
            {
                AddManager(typeof(IAssetsManager).ToString(), m_Dependencies.CreateAssetsManager());
            }

            if (settings.IsModuleEnabled(FrameworkModuleId.Event))
            {
                AddManager(typeof(IEventManager).ToString(), m_Dependencies.CreateEventManager());
            }

            if (settings.IsModuleEnabled(FrameworkModuleId.UI))
            {
                AddManager(typeof(IUIManager).ToString(), m_Dependencies.CreateUIManager());
            }

            if (settings.IsModuleEnabled(FrameworkModuleId.Hotfix))
            {
                AddManager(typeof(IHotfixManager).ToString(), m_Dependencies.CreateHotfixManager(settings.HotfixMode));
            }

            if (settings.IsModuleEnabled(FrameworkModuleId.FSM))
            {
                AddManager(typeof(IFSMManager).ToString(), m_Dependencies.CreateFSMManager());
            }
        }

        /// <summary>
        /// 初始化核心模块并记录宿主 MonoBehaviour。
        /// </summary>
        /// <param name="host">宿主组件。</param>
        /// <param name="settings">启动配置。</param>
        public async UniTask InitializeCoreAsync(MonoBehaviour host, FrameworkBootstrapSettings settings)
        {
            RegisterCoreManagers(settings);

            if (ManagerUtility.AssetsMgr != null && !ManagerUtility.AssetsMgr.IsInitialized)
            {
                await ManagerUtility.AssetsMgr.InitializeAsync();
            }

            ManagerUtility.MainMgr.MonoBehaviour = host;
        }

        private static void AddManager<TService>(string key, TService manager) where TService : class
        {
            IManager runtimeManager = manager as IManager;
            if (runtimeManager == null)
            {
                throw new InvalidOperationException("注册的服务没有实现 IManager: " + typeof(TService));
            }

            ManagerUtility.MainMgr.AddManager(key, runtimeManager);
        }
    }
}
```

- [ ] **Step 4: 再跑核心引导测试，确认通过**

Run:

```powershell
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter LWFramework.Tests.Framework.EditMode.FrameworkBootstrapperTests -testResults Logs/FrameworkBootstrapperTests.xml -quit
```

Expected:
- `FrameworkBootstrapperTests` 全部 PASS。

- [ ] **Step 5: 提交核心引导器**

```bash
git add Assets/LWFramework/RunTime/Core/Bootstrap/FrameworkBootstrapper.cs Assets/Tests/Framework/EditMode/FrameworkBootstrapperTests.cs
git commit -m "feat: 增加框架核心引导器"
```

---

### Task 3: 接通 Startup 与 Hotfix 规则

**Files:**
- Create: `Assets/Scripts/StartupOptionalModules.cs`
- Create: `Assets/Tests/Framework/EditMode/FrameworkHotfixBootstrapTests.cs`
- Modify: `Assets/Scripts/Startup.cs`
- Modify: `Assets/LWFramework/RunTime/Core/Bootstrap/FrameworkBootstrapper.cs`
- Modify: `Assets/LWFramework/RunTime/Core/InterfaceManager/IHotfixManager.cs`

- [ ] **Step 1: 先写失败的热更引导与可选模块测试**

```csharp
using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using LWFMS;
using LWAssets;
using LWAudio;
using LWCore;
using LWHotfix;
using LWStep;
using LWUI;
using NUnit.Framework;
using UnityEngine;

namespace LWFramework.Tests.Framework.EditMode
{
    /// <summary>
    /// 启动链中的热更与可选模块规则测试。
    /// </summary>
    [TestFixture]
    public class FrameworkHotfixBootstrapTests
    {
        private MainManager m_MainManager;

        [SetUp]
        public void SetUp()
        {
            ResetMainManagerSingleton();
            m_MainManager = MainManager.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            if (m_MainManager != null)
            {
                m_MainManager.ClearManager();
            }

            ResetMainManagerSingleton();
            m_MainManager = null;
        }

        /// <summary>
        /// 默认 ByCode 路线不应主动加载外部 DLL。
        /// </summary>
        [Test]
        public async UniTask WarmupHotfixAsync_ByCode_ShouldSkipExternalAssemblyLoad()
        {
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();
            FakeHotfixManager fakeHotfixManager = new FakeHotfixManager();
            FrameworkBootstrapper bootstrapper = CreateBootstrapper(fakeHotfixManager);

            bootstrapper.RegisterCoreManagers(settings);
            await bootstrapper.WarmupHotfixAsync(settings);

            Assert.AreEqual(0, fakeHotfixManager.LoadScriptCallCount);
        }

        /// <summary>
        /// Reflection 路线应通过固定目录加载配置的 DLL 名称。
        /// </summary>
        [Test]
        public async UniTask WarmupHotfixAsync_Reflection_ShouldUseFixedReflectionDirectory()
        {
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();
            settings.HotfixMode = HotfixCodeRunMode.ByReflection;
            settings.ReflectionHotfixAssemblyName = "Game.Hotfix";

            FakeHotfixManager fakeHotfixManager = new FakeHotfixManager();
            FrameworkBootstrapper bootstrapper = CreateBootstrapper(fakeHotfixManager);

            bootstrapper.RegisterCoreManagers(settings);
            await bootstrapper.WarmupHotfixAsync(settings);

            Assert.AreEqual(1, fakeHotfixManager.LoadScriptCallCount);
            Assert.AreEqual("Game.Hotfix", fakeHotfixManager.LastAssemblyName);
            Assert.AreEqual(FrameworkBootstrapSettings.DEFAULT_REFLECTION_HOTFIX_DIR, fakeHotfixManager.LastDirectory);
        }

        /// <summary>
        /// 宿主项目显式开启 Audio 与 StepSystem 时，应只注册这两个可选模块。
        /// </summary>
        [Test]
        public void RegisterOptionalManagers_WhenEnabled_ShouldRegisterAudioAndStepSystem()
        {
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();
            settings.EnableAudio = true;
            settings.EnableStepSystem = true;

            m_MainManager.Init();
            StartupOptionalModules.RegisterOptionalManagers(
                settings,
                () => new FakeAudioManager(),
                () => new FakeStepManager());

            Assert.IsTrue(m_MainManager.TryGetManager<IAudioManager>(out _));
            Assert.IsTrue(m_MainManager.TryGetManager<IStepManager>(out _));
        }

        private FrameworkBootstrapper CreateBootstrapper(FakeHotfixManager hotfixManager)
        {
            FrameworkBootstrapperDependencies dependencies = new FrameworkBootstrapperDependencies
            {
                CreateAssetsManager = () => new FakeAssetsManager(),
                CreateEventManager = () => new FakeEventManager(),
                CreateUIManager = () => new FakeUIManager(),
                CreateHotfixManager = _ => hotfixManager,
                CreateFSMManager = () => new FakeFSMManager(),
            };

            return new FrameworkBootstrapper(dependencies);
        }

        private static void ResetMainManagerSingleton()
        {
            Type singletonType = typeof(Singleton<MainManager>);
            FieldInfo instanceField = singletonType.GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            instanceField.SetValue(null, null);
        }

        private sealed class FakeAssetsManager : IAssetsManager, IManager
        {
            public bool IsInitialized => true;
            public PlayMode CurrentPlayMode => PlayMode.EditorSimulate;
            public IAssetLoader Loader => null;
            public DownloadManager Downloader => null;
            public CacheManager Cache => null;
            public PreloadManager Preloader => null;
            public VersionManager Version => null;
            public void Init() { }
            public void Update() { }
            public UniTask InitializeAsync(LWAssetsConfig config = null) { return UniTask.CompletedTask; }
            public UniTask WarmupShadersAsync(System.Threading.CancellationToken token = default) { return UniTask.CompletedTask; }
            public UniTask<BundleManifest> LoadManifestAsync() { return UniTask.FromResult<BundleManifest>(null); }
            public T LoadAsset<T>(string assetPath) where T : UnityEngine.Object { return null; }
            public byte[] LoadRawFile(string assetPath) { return null; }
            public string LoadRawFileText(string assetPath) { return null; }
            public GameObject Instantiate(string assetPath, Transform spawnPoint = null) { return null; }
            public UniTask<GameObject> InstantiateAsync(string assetPath, Transform spawnPoint = null) { return UniTask.FromResult<GameObject>(null); }
            public UniTask<T> LoadAssetAsync<T>(string assetPath, System.Threading.CancellationToken cancellationToken = default) where T : UnityEngine.Object { return UniTask.FromResult<T>(null); }
            public UniTask<byte[]> LoadRawFileAsync(string assetPath, System.Threading.CancellationToken cancellationToken = default) { return UniTask.FromResult<byte[]>(null); }
            public UniTask<string> LoadRawFileTextAsync(string assetPath, System.Threading.CancellationToken cancellationToken = default) { return UniTask.FromResult<string>(null); }
            public UniTask<SceneHandle> LoadSceneAsync(string scenePath, UnityEngine.SceneManagement.LoadSceneMode mode = UnityEngine.SceneManagement.LoadSceneMode.Single, bool activateOnLoad = true, IProgress<float> progress = null, System.Threading.CancellationToken cancellationToken = default) { return UniTask.FromResult<SceneHandle>(null); }
            public UniTask UnloadSceneAsync(string scenePath, bool forceRelease = true, System.Threading.CancellationToken cancellationToken = default) { return UniTask.CompletedTask; }
            public UniTask UnloadSceneAsync(SceneHandle sceneHandle, bool forceRelease = true, System.Threading.CancellationToken cancellationToken = default) { return UniTask.CompletedTask; }
            public UniTask<T[]> LoadAssetsAsync<T>(string[] assetPaths, IProgress<float> progress = null, System.Threading.CancellationToken cancellationToken = default) where T : UnityEngine.Object { return UniTask.FromResult(new T[0]); }
            public void Release(UnityEngine.Object asset) { }
            public void Release(string assetPath) { }
            public UniTask UnloadUnusedAssetsAsync() { return UniTask.CompletedTask; }
            public void ForceUnloadAll() { }
            public UniTask<long> GetDownloadSizeAsync(string[] tags = null) { return UniTask.FromResult(0L); }
            public UniTask DownloadAsync(string[] tags = null, IProgress<DownloadProgress> progress = null, System.Threading.CancellationToken cancellationToken = default) { return UniTask.CompletedTask; }
            public void Destroy() { }
        }

        private sealed class FakeEventManager : IEventManager, IManager
        {
            public void Init() { }
            public void Update() { }
            public void AddListener(string eventName, Action callback) { }
            public void AddListener<T>(string eventName, Action<T> callback) { }
            public void AddListener<T1, T2>(string eventName, Action<T1, T2> callback) { }
            public void AddListener<T1, T2, T3>(string eventName, Action<T1, T2, T3> callback) { }
            public void AddListener<T1, T2, T3, T4>(string eventName, Action<T1, T2, T3, T4> callback) { }
            public void RemoveListener(string eventName, Action callback) { }
            public void RemoveListener<T>(string eventName, Action<T> callback) { }
            public void RemoveListener<T1, T2>(string eventName, Action<T1, T2> callback) { }
            public void RemoveListener<T1, T2, T3>(string eventName, Action<T1, T2, T3> callback) { }
            public void RemoveListener<T1, T2, T3, T4>(string eventName, Action<T1, T2, T3, T4> callback) { }
            public void DispatchEvent(string eventName) { }
            public void DispatchEvent<T>(string eventName, T info) { }
            public void DispatchEvent<T1, T2>(string eventName, T1 info1, T2 info2) { }
            public void DispatchEvent<T1, T2, T3>(string eventName, T1 info1, T2 info2, T3 info3) { }
            public void DispatchEvent<T1, T2, T3, T4>(string eventName, T1 info1, T2 info2, T3 info3, T4 info4) { }
            public void Clear() { }
            public void ClearDispatchCounts() { }
            public void GetRuntimeInfos(System.Collections.Generic.List<LWEventManager.EventRuntimeInfo> results) { results.Clear(); }
        }

        private sealed class FakeUIManager : IUIManager, IManager
        {
            public UIUtility UIUtility => null;
            public Canvas UICanvas { get; set; }
            public Camera UICamera => null;
            public void Init() { }
            public void Update() { }
            public T OpenView<T>(object data = null, bool isLastSibling = false, bool enterStack = false) where T : BaseUIView { return null; }
            public BaseUIView OpenView(string viewType, object data = null, GameObject uiGameObject = null, bool isLastSibling = false, bool enterStack = false) { return null; }
            public UniTask<T> OpenViewAsync<T>(object data = null, bool isLastSibling = false, bool enterStack = false) where T : BaseUIView { return UniTask.FromResult<T>(null); }
            public void OpenDialog(string title, string content, Action<bool> ResultCallback, bool isShowCancel = true, bool isShowClose = true, bool isLastSibling = true) { }
            public UniTask<bool> OpenDialogAsync(string title, string content, bool isShowCancel = true, bool isShowClose = true, bool isLastSibling = true) { return UniTask.FromResult(false); }
            public void OpenLoadingBar(string tip = "当前正在加载...", bool isLastSibling = true) { }
            public void UpdateLoadingBar(float progress, string tip = null, bool isLastSibling = true) { }
            public void CloseLoadingBar() { }
            public BaseUIView BackView(bool isLastSibling = true) { return null; }
            public BaseUIView BackTwiceView(bool isLastSibling = true) { return null; }
            public BaseUIView BackUntilLastView(bool isLastSibling = true) { return null; }
            public T GetView<T>() where T : BaseUIView { return null; }
            public BaseUIView GetView(string viewType = null) { return null; }
            public BaseUIView[] GetAllView() { return Array.Empty<BaseUIView>(); }
            public void CloseView<T>(bool enterStack = false) { }
            public void CloseView(string viewName, bool enterStack = false) { }
            public void CloseView(BaseUIView view, bool enterStack = false) { }
            public void CloseOtherView<T>() { }
            public void CloseOtherView(string viewName) { }
            public void CloseOtherView(string[] viewNameArray) { }
            public void CloseAllView() { }
            public void ClearView(string viewName) { }
            public void ClearView<T>() { }
            public void ClearOtherView(string[] viewNameArray) { }
            public void ClearOtherView<T>() { }
            public void ClearAllView() { }
            public UniTask PreloadViewAsync(string loadPath) { return UniTask.CompletedTask; }
            public UniTask PreloadViewAsync<T>() where T : BaseUIView { return UniTask.CompletedTask; }
            public UniTask PreLoadDefaultUI() { return UniTask.CompletedTask; }
            public void SetStyle(string styleName) { }
            public string GetStyle() { return "Default"; }
        }

        private sealed class FakeHotfixManager : IHotfixManager, IManager
        {
            public bool Loaded => true;
            public int LoadScriptCallCount { get; private set; }
            public string LastAssemblyName { get; private set; }
            public string LastDirectory { get; private set; }
            public void Init() { }
            public void Update() { }
            public UniTask LoadScriptAsync(string hotfixDllName, string dir = "Hotfix/") { LoadScriptCallCount++; LastAssemblyName = hotfixDllName; LastDirectory = dir; return UniTask.CompletedTask; }
            public Type GetTypeByName(string typeName) { return null; }
            public T Instantiate<T>(string typeName, object[] args = null) { return default; }
            public void Invoke(string type, string method, object instance, params object[] args) { }
            public void Destroy() { }
            public void AddHotfixTypeAttr(System.Collections.Generic.List<Type> p_TypeArray) { }
            public System.Collections.Generic.List<TypeAttr> GetAttrTypeDataList<T>() { return new System.Collections.Generic.List<TypeAttr>(); }
            public T FindAttr<T>(string typeName) { return default; }
        }

        private sealed class FakeFSMManager : IFSMManager, IManager
        {
            public void Init() { }
            public void Update() { }
            public void RegisterFSM(FSMStateMachine fsm) { }
            public void UnRegisterFSM(FSMStateMachine fsm) { }
            public FSMStateMachine GetFSMByName(string name) { return null; }
            public FSMStateMachine GetFSMProcedure() { return null; }
            public bool IsExistFSM(string name) { return false; }
            public System.Collections.Generic.List<TypeAttr> GetFsmClassDataByName(string fsmName) { return new System.Collections.Generic.List<TypeAttr>(); }
            public void InitFSMManager() { }
        }

        private sealed class FakeAudioManager : IAudioManager, IManager
        {
            public float AudioVolume { set { } }
            public void Init() { }
            public void Update() { }
            public AudioChannel Play(AudioClip clip, bool loop = false, float fadeInSeconds = 0f, float volume = -1) { return null; }
            public AudioChannel Play(AudioClip clip, Transform emitter, bool loop = false, float fadeInSeconds = 0f, float volume = -1, Audio3DSettings? settings = null) { return null; }
            public AudioChannel Play(AudioClip clip, Vector3 point, bool loop = false, float fadeInSeconds = 0f, float volume = -1, Audio3DSettings? settings = null) { return null; }
            public void StopImmediate(AudioChannel audioChannel) { }
            public void Stop(AudioChannel audioChannel) { }
            public void StopAll() { }
            public void Pause(AudioChannel audioChannel) { }
            public void PauseAll() { }
            public void Resume(AudioChannel audioChannel) { }
            public void ResumeAll() { }
        }

        private sealed class FakeStepManager : IStepManager, IManager
        {
            public event Action<string> OnNodeEnter;
            public event Action<string> OnNodeLeave;
            public event Action<string> OnNodeChanged;
            public event Action<string> OnActionChanged;
            public event Action<string> OnJumpProgress;
            public event Action<string> OnJumpFailed;
            public event Action OnAllStepsCompleted;
            public bool IsRunning => false;
            public string CurrentNodeId => string.Empty;
            public void Init() { }
            public void Update() { }
            public void LoadGraph(string xmlAssetPath) { }
            public void Start(string graphName, string startNodeId = null) { }
            public void Stop() { }
            public void Restart() { }
            public void ResetContext() { }
            public void Forward() { }
            public void Backward() { }
            public void JumpTo(string targetNodeId) { }
            public System.Collections.Generic.List<StepNode> GetAllNodes(string graphName = null) { return new System.Collections.Generic.List<StepNode>(); }
            public System.Collections.Generic.List<StepNode> GetAllDisplayNodes(string graphName = null) { return new System.Collections.Generic.List<StepNode>(); }
            public StepNodeStatus GetNodeStatus(string nodeId) { return StepNodeStatus.Unfinished; }
            public System.Collections.Generic.List<string> GetAvailableNextNodes() { return new System.Collections.Generic.List<string>(); }
            public StepContext GetStepContext() { return null; }
            public string GetContextToJson() { return string.Empty; }
            public void LoadContextFromJson(string json) { }
            public StepExecutionReport GetExecutionReport() { return new StepExecutionReport(); }
            public string GetExecutionReportJson() { return string.Empty; }
            public void ClearExecutionReport() { }
        }
    }
}
```

- [ ] **Step 2: 运行测试，确认当前热更引导和宿主模块注册能力还不存在**

Run:

```powershell
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter LWFramework.Tests.Framework.EditMode.FrameworkHotfixBootstrapTests -testResults Logs/FrameworkHotfixBootstrapTests.xml -quit
```

Expected:
- 编译失败或测试失败，提示 `WarmupHotfixAsync`、`StartupOptionalModules` 等不存在。

- [ ] **Step 3: 实现 Startup 热更预热、固定 Reflection 目录与宿主可选模块注册**

`Assets/LWFramework/RunTime/Core/Bootstrap/FrameworkBootstrapper.cs` 增加以下方法：

```csharp
/// <summary>
/// 按配置预热热更管理器。
/// </summary>
/// <param name="settings">启动配置。</param>
public async UniTask WarmupHotfixAsync(FrameworkBootstrapSettings settings)
{
    if (!settings.IsModuleEnabled(FrameworkModuleId.Hotfix))
    {
        return;
    }

    if (settings.HotfixMode != HotfixCodeRunMode.ByReflection)
    {
        return;
    }

    if (string.IsNullOrWhiteSpace(settings.ReflectionHotfixAssemblyName))
    {
        throw new InvalidOperationException("Reflection 模式必须配置 ReflectionHotfixAssemblyName。");
    }

    await ManagerUtility.HotfixMgr.LoadScriptAsync(
        settings.ReflectionHotfixAssemblyName,
        FrameworkBootstrapSettings.DEFAULT_REFLECTION_HOTFIX_DIR);
}

/// <summary>
/// 解析首个流程状态类型。
/// </summary>
/// <param name="settings">启动配置。</param>
/// <returns>流程状态类型。</returns>
public Type ResolveFirstProcedureType(FrameworkBootstrapSettings settings)
{
    if (ManagerUtility.HotfixMgr == null || string.IsNullOrEmpty(settings.ProcedureName))
    {
        return null;
    }

    return ManagerUtility.HotfixMgr.GetTypeByName(settings.ProcedureName);
}
```

`Assets/Scripts/StartupOptionalModules.cs`

```csharp
using System;
using LWAudio;
using LWCore;
using LWStep;

/// <summary>
/// 宿主项目的可选模块注册器。
/// </summary>
public static class StartupOptionalModules
{
    /// <summary>
    /// 按启动配置注册宿主项目需要的可选模块。
    /// </summary>
    /// <param name="settings">启动配置。</param>
    /// <param name="createAudioManager">可选音频管理器工厂。</param>
    /// <param name="createStepManager">可选步骤管理器工厂。</param>
    public static void RegisterOptionalManagers(
        FrameworkBootstrapSettings settings,
        Func<IAudioManager> createAudioManager = null,
        Func<IStepManager> createStepManager = null)
    {
        if (settings.EnableAudio)
        {
            IAudioManager audioManager = createAudioManager != null ? createAudioManager() : new AudioManager();
            RegisterManager(typeof(IAudioManager).ToString(), audioManager);
        }

        if (settings.EnableStepSystem)
        {
            IStepManager stepManager = createStepManager != null ? createStepManager() : new StepManager();
            RegisterManager(typeof(IStepManager).ToString(), stepManager);
        }
    }

    /// <summary>
    /// 注册一个实现了 IManager 的可选模块实例。
    /// </summary>
    /// <typeparam name="TService">服务类型。</typeparam>
    /// <param name="typeKey">注册键。</param>
    /// <param name="manager">模块实例。</param>
    private static void RegisterManager<TService>(string typeKey, TService manager) where TService : class
    {
        IManager runtimeManager = manager as IManager;
        if (runtimeManager == null)
        {
            throw new InvalidOperationException("可选模块未实现 IManager: " + typeof(TService));
        }

        ManagerUtility.MainMgr.AddManager(typeKey, runtimeManager);
    }
}
```

`Assets/Scripts/Startup.cs` 将 `Start`、字段和辅助方法修改为：

```csharp
using System;
using Cysharp.Threading.Tasks;
using LWAssets;
using LWCore;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class Startup : MonoBehaviour
{
    public string configUrl;
    public string procedureName = "StartProcedure";
    public HotfixCodeRunMode hotfixCodeRunMode = HotfixCodeRunMode.ByCode;
    public string reflectionHotfixAssemblyName = string.Empty;
    public bool enableAudio = false;
    public bool enableStepSystem = false;

    /// <summary>
    /// 启动框架入口并完成基础管理器注册。
    /// </summary>
    async void Start()
    {
        LWDebug.Log("Start");
        Application.targetFrameRate = 60;
        DontDestroyOnLoad(gameObject);
        LWDebug.SetLogConfig(true, 3, true);

        FrameworkBootstrapSettings settings = BuildBootstrapSettings();
        FrameworkBootstrapper bootstrapper = new FrameworkBootstrapper();

        await bootstrapper.InitializeCoreAsync(this, settings);
        StartupOptionalModules.RegisterOptionalManagers(settings);

        if (ManagerUtility.AssetsMgr.CurrentPlayMode == LWAssets.PlayMode.Online)
        {
            ManagerUtility.UIMgr.OpenLoadingBar("检查更新...", true);
            try
            {
                await DownloadAsync();
                await UniTask.Delay(500);
            }
            finally
            {
                ManagerUtility.UIMgr.CloseLoadingBar();
            }
        }

        await bootstrapper.WarmupHotfixAsync(settings);
        MainManager.Instance.FirstFSMState = bootstrapper.ResolveFirstProcedureType(settings);
        MainManager.Instance.StartProcedure();
    }

    /// <summary>
    /// 根据当前 Inspector 配置构建启动配置。
    /// </summary>
    /// <returns>启动配置。</returns>
    private FrameworkBootstrapSettings BuildBootstrapSettings()
    {
        FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();
        settings.ProcedureName = procedureName;
        settings.HotfixMode = hotfixCodeRunMode;
        settings.ReflectionHotfixAssemblyName = reflectionHotfixAssemblyName != null ? reflectionHotfixAssemblyName.Trim() : string.Empty;
        settings.EnableAudio = enableAudio;
        settings.EnableStepSystem = enableStepSystem;
        return settings;
    }
}
```

`Assets/LWFramework/RunTime/Core/InterfaceManager/IHotfixManager.cs` 中将枚举注释收口到以下口径：

```csharp
/// <summary>
/// Reflection 路线。
/// v1.1 中作为可选动态装载路线，仅在宿主项目显式启用时使用。
/// 运行时通过 Assets 资源系统，从固定 Hotfix 目录加载程序集。
/// </summary>
ByReflection = 1,

/// <summary>
/// ByCode 路线。
/// v1.1 中作为默认稳定路线，所有项目都可以直接使用。
/// 它代表热更能力存在，但并不要求项目必须加载外部 DLL。
/// </summary>
ByCode = 2,
```

- [ ] **Step 4: 再跑热更引导测试，确认默认 / 可选行为都受保护**

Run:

```powershell
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter LWFramework.Tests.Framework.EditMode.FrameworkHotfixBootstrapTests -testResults Logs/FrameworkHotfixBootstrapTests.xml -quit
```

Expected:
- `FrameworkHotfixBootstrapTests` 全部 PASS。

- [ ] **Step 5: 提交 Startup 与 Hotfix 规则收口**

```bash
git add Assets/Scripts/Startup.cs Assets/Scripts/StartupOptionalModules.cs Assets/LWFramework/RunTime/Core/Bootstrap/FrameworkBootstrapper.cs Assets/LWFramework/RunTime/Core/InterfaceManager/IHotfixManager.cs Assets/Tests/Framework/EditMode/FrameworkHotfixBootstrapTests.cs
git commit -m "feat: 收口启动链与热更默认规则"
```

---

### Task 4: 将 StepSystem 调整为业务插件访问方式

**Files:**
- Create: `Assets/LWFramework/RunTime/StepSystem/StepManagerUtility.cs`
- Create: `Assets/Tests/StepSystem/EditMode/StepManagerUtilityTests.cs`
- Modify: `Assets/LWFramework/RunTime/Core/ManagerUtility.cs`
- Modify: `Assets/Scripts/Procedure/StepProcedure.cs`
- Modify: `Assets/LWFramework/Editor/StepSystem/StepEditorWindow.cs`

- [ ] **Step 1: 先写失败的 Step 插件访问测试**

```csharp
using System;
using System.Reflection;
using LWCore;
using LWStep;
using NUnit.Framework;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// StepManagerUtility 插件访问测试。
    /// </summary>
    [TestFixture]
    public class StepManagerUtilityTests
    {
        private MainManager m_MainManager;

        [SetUp]
        public void SetUp()
        {
            ResetMainManagerSingleton();
            m_MainManager = MainManager.Instance;
            m_MainManager.Init();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_MainManager != null)
            {
                m_MainManager.ClearManager();
            }

            ResetMainManagerSingleton();
            m_MainManager = null;
        }

        /// <summary>
        /// 未注册 StepManager 时，插件访问入口应返回空。
        /// </summary>
        [Test]
        public void TryGetStepMgr_WithoutRegistration_ShouldReturnFalse()
        {
            bool hasStepManager = StepManagerUtility.TryGetStepMgr(out IStepManager stepManager);

            Assert.IsFalse(hasStepManager);
            Assert.IsNull(stepManager);
            Assert.IsNull(StepManagerUtility.StepMgr);
        }

        /// <summary>
        /// 已注册 StepManager 时，插件访问入口应返回同一实例。
        /// </summary>
        [Test]
        public void TryGetStepMgr_WithRegistration_ShouldReturnRegisteredInstance()
        {
            TestStepManager stepManager = new TestStepManager();
            m_MainManager.AddManager(typeof(IStepManager).ToString(), stepManager);

            bool hasStepManager = StepManagerUtility.TryGetStepMgr(out IStepManager resolvedManager);

            Assert.IsTrue(hasStepManager);
            Assert.AreSame(stepManager, resolvedManager);
            Assert.AreSame(stepManager, StepManagerUtility.StepMgr);
        }

        private static void ResetMainManagerSingleton()
        {
            Type singletonType = typeof(Singleton<MainManager>);
            FieldInfo instanceField = singletonType.GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            instanceField.SetValue(null, null);
        }

        private sealed class TestStepManager : IStepManager, IManager
        {
            public event Action<string> OnNodeEnter;
            public event Action<string> OnNodeLeave;
            public event Action<string> OnNodeChanged;
            public event Action<string> OnActionChanged;
            public event Action<string> OnJumpProgress;
            public event Action<string> OnJumpFailed;
            public event Action OnAllStepsCompleted;
            public bool IsRunning => false;
            public string CurrentNodeId => string.Empty;
            public void Init() { }
            public void Update() { }
            public void LoadGraph(string xmlAssetPath) { }
            public void Start(string graphName, string startNodeId = null) { }
            public void Stop() { }
            public void Restart() { }
            public void ResetContext() { }
            public void Forward() { }
            public void Backward() { }
            public void JumpTo(string targetNodeId) { }
            public System.Collections.Generic.List<StepNode> GetAllNodes(string graphName = null) { return new System.Collections.Generic.List<StepNode>(); }
            public System.Collections.Generic.List<StepNode> GetAllDisplayNodes(string graphName = null) { return new System.Collections.Generic.List<StepNode>(); }
            public StepNodeStatus GetNodeStatus(string nodeId) { return StepNodeStatus.Unfinished; }
            public System.Collections.Generic.List<string> GetAvailableNextNodes() { return new System.Collections.Generic.List<string>(); }
            public StepContext GetStepContext() { return null; }
            public string GetContextToJson() { return string.Empty; }
            public void LoadContextFromJson(string json) { }
            public StepExecutionReport GetExecutionReport() { return new StepExecutionReport(); }
            public string GetExecutionReportJson() { return string.Empty; }
            public void ClearExecutionReport() { }
        }
    }
}
```

- [ ] **Step 2: 运行测试，确认当前还没有插件访问入口**

Run:

```powershell
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter LWFramework.Tests.StepSystem.EditMode.StepManagerUtilityTests -testResults Logs/StepManagerUtilityTests.xml -quit
```

Expected:
- 编译失败或测试失败，提示 `StepManagerUtility` 不存在。

- [ ] **Step 3: 新增 Step 插件访问入口，并移除 LWCore 里的直接 Step 依赖**

`Assets/LWFramework/RunTime/StepSystem/StepManagerUtility.cs`

```csharp
using LWCore;

namespace LWStep
{
    /// <summary>
    /// StepSystem 插件层的 Manager 访问入口。
    /// </summary>
    public static class StepManagerUtility
    {
        /// <summary>
        /// 获取当前步骤管理器。
        /// </summary>
        public static IStepManager StepMgr
        {
            get
            {
                return MainManager.Instance.GetManager<IStepManager>();
            }
        }

        /// <summary>
        /// 尝试获取当前步骤管理器。
        /// </summary>
        /// <param name="stepManager">返回的步骤管理器。</param>
        /// <returns>是否获取成功。</returns>
        public static bool TryGetStepMgr(out IStepManager stepManager)
        {
            return MainManager.Instance.TryGetManager<IStepManager>(out stepManager);
        }
    }
}
```

`Assets/LWFramework/RunTime/Core/ManagerUtility.cs` 删除 `StepMgr` 属性后，文件尾部保持为：

```csharp
        /// <summary>
        /// 获取音频管理类
        /// </summary>
        public static IAudioManager AudioMgr
        {
            get
            {
                return MainManager.Instance.GetManager<IAudioManager>();
            }
        }
    }
}
```

对下列文件执行明确替换：

```text
Assets/Scripts/Procedure/StepProcedure.cs
- 将所有 `ManagerUtility.StepMgr` 替换为 `StepManagerUtility.StepMgr`

Assets/LWFramework/Editor/StepSystem/StepEditorWindow.cs
- 将所有 `ManagerUtility.StepMgr` 替换为 `StepManagerUtility.StepMgr`
- 将 `ManagerUtility.StepMgr != null` 这类判定替换为 `StepManagerUtility.TryGetStepMgr(out IStepManager stepManager)` 后再使用 `stepManager`
```

替换后至少要出现以下代码片段：

`Assets/Scripts/Procedure/StepProcedure.cs`

```csharp
using LWStep;

public override void OnEnter(BaseFSMState lastState)
{
    base.OnEnter(lastState);
    StepManagerUtility.StepMgr.OnAllStepsCompleted += OnAllStepsCompleted;
    StepManagerUtility.StepMgr.OnNodeEnter += OnNodeEnter;
    StepManagerUtility.StepMgr.OnNodeLeave += OnNodeLeave;
    StepManagerUtility.StepMgr.OnNodeChanged += OnNodeChanged;
    StepManagerUtility.StepMgr.OnActionChanged += OnActionChanged;
    StepManagerUtility.StepMgr.OnJumpProgress += OnJumpProgress;
    StepManagerUtility.StepMgr.OnJumpFailed += OnJumpFailed;
}
```

`Assets/LWFramework/Editor/StepSystem/StepEditorWindow.cs`

```csharp
using LWStep;

private void UpdateContextPanel()
{
    if (m_ContextText == null || !m_AutoUpdateContext)
    {
        return;
    }

    if (!EditorApplication.isPlaying)
    {
        m_ContextText.value = "仅在运行时显示 StepContext1";
        UpdateReportPanel("仅在运行时显示 ExecutionReport");
        return;
    }

    if (!StepManagerUtility.TryGetStepMgr(out IStepManager stepManager) || stepManager == null)
    {
        m_ContextText.value = "StepManager 未初始化";
        UpdateReportPanel("StepManager 未初始化");
        return;
    }

    if (!stepManager.IsRunning)
    {
        m_ContextText.value = "StepManager 未运行";
        UpdateReportPanel(stepManager.GetExecutionReportJson());
        return;
    }

    string contextJson = stepManager.GetContextToJson();
    m_ContextText.value = string.IsNullOrEmpty(contextJson) ? "StepContext 为空" : contextJson;
    UpdateReportPanel(stepManager.GetExecutionReportJson());
}
```

- [ ] **Step 4: 再跑 Step 插件边界测试，确认通过**

Run:

```powershell
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter LWFramework.Tests.StepSystem.EditMode.StepManagerUtilityTests -testResults Logs/StepManagerUtilityTests.xml -quit
```

Expected:
- `StepManagerUtilityTests` 全部 PASS。

- [ ] **Step 5: 提交 StepSystem 插件边界调整**

```bash
git add Assets/LWFramework/RunTime/StepSystem/StepManagerUtility.cs Assets/LWFramework/RunTime/Core/ManagerUtility.cs Assets/Scripts/Procedure/StepProcedure.cs Assets/LWFramework/Editor/StepSystem/StepEditorWindow.cs Assets/Tests/StepSystem/EditMode/StepManagerUtilityTests.cs
git commit -m "refactor: 将 StepSystem 调整为业务插件访问方式"
```

---

### Task 5: 补接入文档并修正文档口径

**Files:**
- Create: `docs/v1.1/框架接入与模块边界.md`
- Modify: `Assets/LWFramework/RunTime/Assets/README.md`

- [ ] **Step 1: 先写文档目标文件，明确 v1.1 的接入边界**

`docs/v1.1/框架接入与模块边界.md`

```markdown
# AFramework v1.1 框架接入与模块边界

## 1. 核心层

- `Core`
- `Event`
- `FSM/Procedure`
- `Assets`
- `UI`
- `Hotfix`

说明：
- 以上模块构成 v1.1 默认内置核心能力。
- 默认热更路线为 `ByCode`。
- 如宿主项目选择 `Reflection`，则通过 `Assets` 从固定 `Hotfix/` 目录加载 DLL。

## 2. 宿主项目层

宿主项目负责：
- `Startup/Bootstrap`
- 业务 `Procedure`
- 业务 `UI/View`
- 场景、Prefab、配置表与项目资源
- 是否启用 `Audio`
- 是否启用 `StepSystem`

## 3. 业务插件层

`StepSystem` 属于业务插件层：
- 不进入默认核心启动链
- 由宿主项目显式启用
- 可以依赖 `Core / Event / Assets / UI`
- 核心层不能反向依赖 `StepSystem`

## 4. 最小接入步骤

1. 引入框架源码或 Git/UPM 引用
2. 在宿主项目中准备 `Startup`
3. 配置首个 `Procedure`
4. 默认使用 `ByCode`
5. 如果启用 `Reflection`，填写程序集名称并将 DLL 放入固定 `Hotfix/` 目录
6. 需要时再开启 `Audio` 和 `StepSystem`
```

- [ ] **Step 2: 修正资源系统 README 中过时的单例示例**

将 `Assets/LWFramework/RunTime/Assets/README.md` 中所有 `LWAssetsManager.Instance` 改为当前接入方式，并把示例收口到以下写法：

```csharp
using LWCore;
using UnityEngine;

public class GameStartup : MonoBehaviour
{
    private async void Start()
    {
        await ManagerUtility.AssetsMgr.InitializeAsync();

        GameObject prefab = await ManagerUtility.AssetsMgr.LoadAssetAsync<GameObject>("Assets/0Res/Prefabs/Demo.prefab");
        Instantiate(prefab);
    }
}
```

同时在 README 的初始化部分新增一段说明：

```markdown
> 说明：
> `LWAssets` 在当前工程中通过框架启动链完成注册。
> 业务侧应优先通过 `ManagerUtility.AssetsMgr` 访问资源系统，而不是假设存在全局单例 `LWAssetsManager.Instance`。
```

- [ ] **Step 3: 运行文档一致性检查与一轮关键回归测试**

Run:

```powershell
rg "LWAssetsManager\\.Instance" Assets/LWFramework/RunTime/Assets/README.md
```

Expected:
- 无输出。

Run:

```powershell
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/EditModeTests.v1.1.xml -quit
```

Expected:
- 新增的 `FrameworkBootstrapSettingsTests`、`FrameworkBootstrapperTests`、`FrameworkHotfixBootstrapTests`、`StepManagerUtilityTests` 全部 PASS。
- 现有 `HotfixReflectionTests`、`UIRuntimeTests`、`MainManagerRuntimeDiagnosticsTests`、`StepSystemExecutionReportTests` 保持 PASS。

- [ ] **Step 4: 快速自查文档与实现口径是否一致**

检查点：

```text
1. 文档中是否明确写出默认 Hotfix 路线是 ByCode。
2. 文档中是否明确写出 Reflection 通过固定 Hotfix/ 目录加载 DLL。
3. 文档中是否明确写出 StepSystem 是业务插件，而不是默认核心。
4. README 是否已经去掉 LWAssetsManager.Instance 的旧写法。
```

Expected:
- 四项都满足后再提交。

- [ ] **Step 5: 提交文档与 README 修订**

```bash
git add docs/v1.1/框架接入与模块边界.md Assets/LWFramework/RunTime/Assets/README.md
git commit -m "docs: 补充 v1.1 接入说明并修正文档口径"
```

---

## 自检

### 1. 规格覆盖

- `3.1 保守模块化收口`：Task 1、Task 2、Task 5
- `3.2 核心能力边界确定`：Task 1、Task 2、Task 5
- `3.3 宿主项目职责收口`：Task 3、Task 5
- `3.4 启动链路与模块启停规范`：Task 2、Task 3
- `3.5 Hotfix 路线收敛`：Task 3、Task 5
- `3.6 StepSystem 插件化`：Task 4、Task 5
- `3.7 跨项目接入与 UPM-ready 约束`：Task 5

结论：规格条目全部有对应任务，没有遗漏到“后续再说”的内容。

### 2. 占位符扫描

- 已检查计划正文，没有 `TODO`、`TBD`、`后续实现`、`类似处理` 这类占位语句。
- 每个代码步骤都给出了明确文件路径、代码片段和运行命令。

### 3. 类型与命名一致性

- 启动配置统一使用 `FrameworkBootstrapSettings`
- 核心引导统一使用 `FrameworkBootstrapper`
- 宿主可选模块注册统一使用 `StartupOptionalModules`
- Step 插件访问统一使用 `StepManagerUtility`
- 固定 Reflection 目录统一使用 `FrameworkBootstrapSettings.DEFAULT_REFLECTION_HOTFIX_DIR`

结论：计划内命名一致，没有前后漂移。
