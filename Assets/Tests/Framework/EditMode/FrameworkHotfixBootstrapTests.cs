using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using LWAudio;
using LWCore;
using LWStep;
using NUnit.Framework;
using UnityEngine;

namespace LWFramework.Tests.Framework.EditMode
{
    /// <summary>
    /// 热更预热与宿主可选模块注册行为测试。
    /// </summary>
    public sealed class FrameworkHotfixBootstrapTests
    {
        /// <summary>
        /// 每条用例执行前清理主管理器状态，避免跨用例互相影响。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            ManagerUtility.MainMgr.ClearManager();
        }

        /// <summary>
        /// 每条用例执行后清理主管理器状态。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            ManagerUtility.MainMgr.ClearManager();
        }

        /// <summary>
        /// 验证 ByCode 模式不会触发外部程序集加载。
        /// </summary>
        [Test]
        public async Task WarmupHotfixAsync_ByCode_ShouldSkipExternalAssemblyLoad()
        {
            FakeHotfixManager fakeHotfixManager = new FakeHotfixManager(false, true);
            FrameworkBootstrapperDependencies dependencies = new FrameworkBootstrapperDependencies();
            dependencies.CreateHotfixManager = (mode) => fakeHotfixManager;
            FrameworkBootstrapper bootstrapper = new FrameworkBootstrapper(dependencies);
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();
            settings.EnableAssets = false;
            settings.EnableEvent = false;
            settings.EnableUI = false;
            settings.EnableFSM = false;
            settings.EnableHotfix = true;
            settings.HotfixMode = HotfixCodeRunMode.ByCode;
            settings.ReflectionHotfixAssemblyName = "Game.Hotfix";

            bootstrapper.RegisterCoreManagers(settings);
            await bootstrapper.WarmupHotfixAsync(settings);

            Assert.AreEqual(0, fakeHotfixManager.LoadScriptAsyncCallCount);
        }

        /// <summary>
        /// 验证反射模式会使用固定 Hotfix 目录加载程序集。
        /// </summary>
        [Test]
        public async Task WarmupHotfixAsync_Reflection_ShouldUseFixedReflectionDirectory()
        {
            FakeHotfixManager fakeHotfixManager = new FakeHotfixManager(false, true);
            FrameworkBootstrapperDependencies dependencies = new FrameworkBootstrapperDependencies();
            dependencies.CreateHotfixManager = (mode) => fakeHotfixManager;
            FrameworkBootstrapper bootstrapper = new FrameworkBootstrapper(dependencies);
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();
            settings.EnableAssets = false;
            settings.EnableEvent = false;
            settings.EnableUI = false;
            settings.EnableFSM = false;
            settings.EnableHotfix = true;
            settings.HotfixMode = HotfixCodeRunMode.ByReflection;
            settings.ReflectionHotfixAssemblyName = "Game.Hotfix";

            bootstrapper.RegisterCoreManagers(settings);
            await bootstrapper.WarmupHotfixAsync(settings);

            Assert.AreEqual(1, fakeHotfixManager.LoadScriptAsyncCallCount);
            Assert.AreEqual("Game.Hotfix", fakeHotfixManager.LastLoadScriptDllName);
            Assert.AreEqual(FrameworkBootstrapSettings.DEFAULT_REFLECTION_HOTFIX_DIR, fakeHotfixManager.LastLoadScriptDirectory);
            Assert.IsTrue(fakeHotfixManager.Loaded);
        }

        /// <summary>
        /// 验证 HybridCLR 兼容回退路线也会走反射预热并成功加载。
        /// </summary>
        [Test]
        public async Task WarmupHotfixAsync_ByHybridClrFallback_ShouldWarmupAndLoad()
        {
            FakeHotfixManager fakeHotfixManager = new FakeHotfixManager(false, true);
            FrameworkBootstrapperDependencies dependencies = new FrameworkBootstrapperDependencies();
            dependencies.CreateHotfixManager = (mode) => fakeHotfixManager;
            FrameworkBootstrapper bootstrapper = new FrameworkBootstrapper(dependencies);
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();
            settings.EnableAssets = false;
            settings.EnableEvent = false;
            settings.EnableUI = false;
            settings.EnableFSM = false;
            settings.EnableHotfix = true;
            settings.HotfixMode = HotfixCodeRunMode.ByHyBridCLR;
            settings.ReflectionHotfixAssemblyName = "Game.Hotfix";

            bootstrapper.RegisterCoreManagers(settings);
            await bootstrapper.WarmupHotfixAsync(settings);

            Assert.AreEqual(1, fakeHotfixManager.LoadScriptAsyncCallCount);
            Assert.AreEqual("Game.Hotfix", fakeHotfixManager.LastLoadScriptDllName);
            Assert.AreEqual(FrameworkBootstrapSettings.DEFAULT_REFLECTION_HOTFIX_DIR, fakeHotfixManager.LastLoadScriptDirectory);
            Assert.IsTrue(fakeHotfixManager.Loaded);
        }

        /// <summary>
        /// 验证反射预热后若仍未加载成功会抛出异常，阻断后续流程。
        /// </summary>
        [Test]
        public void WarmupHotfixAsync_Reflection_WhenStillNotLoaded_ShouldThrow()
        {
            FakeHotfixManager fakeHotfixManager = new FakeHotfixManager(false, false);
            FrameworkBootstrapperDependencies dependencies = new FrameworkBootstrapperDependencies();
            dependencies.CreateHotfixManager = (mode) => fakeHotfixManager;
            FrameworkBootstrapper bootstrapper = new FrameworkBootstrapper(dependencies);
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();
            settings.EnableAssets = false;
            settings.EnableEvent = false;
            settings.EnableUI = false;
            settings.EnableFSM = false;
            settings.EnableHotfix = true;
            settings.HotfixMode = HotfixCodeRunMode.ByReflection;
            settings.ReflectionHotfixAssemblyName = "Game.Hotfix";

            bootstrapper.RegisterCoreManagers(settings);

            Assert.Throws<InvalidOperationException>(() =>
            {
                bootstrapper.WarmupHotfixAsync(settings).GetAwaiter().GetResult();
            });
            Assert.AreEqual(1, fakeHotfixManager.LoadScriptAsyncCallCount);
            Assert.IsFalse(fakeHotfixManager.Loaded);
        }

        /// <summary>
        /// 验证可选模块启用后会在核心模块已注册前提下注册 Audio 与 StepSystem，且核心模块不丢失。
        /// </summary>
        [Test]
        public void RegisterOptionalManagers_WhenEnabled_ShouldRegisterAudioAndStepSystem()
        {
            FakeHotfixManager fakeHotfixManager = new FakeHotfixManager(false, true);
            FrameworkBootstrapperDependencies dependencies = new FrameworkBootstrapperDependencies();
            dependencies.CreateHotfixManager = (mode) => fakeHotfixManager;
            FrameworkBootstrapper bootstrapper = new FrameworkBootstrapper(dependencies);
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();
            settings.EnableAssets = false;
            settings.EnableEvent = false;
            settings.EnableUI = false;
            settings.EnableHotfix = true;
            settings.EnableFSM = false;
            settings.EnableAudio = true;
            settings.EnableStepSystem = true;

            bootstrapper.RegisterCoreManagers(settings);
            IHotfixManager hotfixManagerBeforeOptional = ManagerUtility.HotfixMgr;

            InvokeRegisterOptionalManagers(
                settings,
                () => new FakeAudioManager(),
                () => new FakeStepManager());

            Assert.NotNull(ManagerUtility.AudioMgr);
            Assert.NotNull(StepManagerUtility.StepMgr);
            Assert.NotNull(ManagerUtility.HotfixMgr);
            Assert.AreSame(hotfixManagerBeforeOptional, ManagerUtility.HotfixMgr);
            Assert.IsNull(ManagerUtility.AssetsMgr);
            Assert.IsNull(ManagerUtility.EventMgr);
            Assert.IsNull(ManagerUtility.UIMgr);
            Assert.IsNull(ManagerUtility.FSMMgr);
        }

        /// <summary>
        /// 验证未先完成核心注册时调用可选模块注册会抛出异常。
        /// </summary>
        [Test]
        public void RegisterOptionalManagers_WithoutCoreRegistration_ShouldThrow()
        {
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();
            settings.EnableAudio = true;
            settings.EnableStepSystem = false;
            settings.EnableAssets = false;
            settings.EnableEvent = false;
            settings.EnableUI = false;
            settings.EnableHotfix = false;
            settings.EnableFSM = false;

            Assert.Throws<InvalidOperationException>(() =>
            {
                InvokeRegisterOptionalManagers(settings, () => new FakeAudioManager(), null);
            });
        }

        /// <summary>
        /// 通过反射调用宿主可选模块注册入口，避免测试程序集与宿主程序集的编译边界耦合。
        /// </summary>
        private static void InvokeRegisterOptionalManagers(
            FrameworkBootstrapSettings settings,
            Func<IAudioManager> createAudioManager,
            Func<IStepManager> createStepManager)
        {
            Type startupOptionalModulesType = Type.GetType("StartupOptionalModules, Assembly-CSharp");
            if (startupOptionalModulesType == null)
            {
                throw new InvalidOperationException("未找到 StartupOptionalModules 类型，无法执行可选模块注册测试。");
            }

            MethodInfo registerMethod = startupOptionalModulesType.GetMethod(
                "RegisterOptionalManagers",
                BindingFlags.Public | BindingFlags.Static);
            if (registerMethod == null)
            {
                throw new InvalidOperationException("未找到 StartupOptionalModules.RegisterOptionalManagers 方法。");
            }

            object[] registerParameters = new object[] { settings, createAudioManager, createStepManager };
            try
            {
                registerMethod.Invoke(null, registerParameters);
            }
            catch (TargetInvocationException exception)
            {
                if (exception.InnerException != null)
                {
                    throw exception.InnerException;
                }

                throw;
            }
        }

        /// <summary>
        /// 测试专用热更管理器，记录程序集加载参数。
        /// </summary>
        private sealed class FakeHotfixManager : IHotfixManager, IManager
        {
            private bool m_Loaded;
            private readonly bool m_IsLoadedAfterLoadScript;

            /// <summary>
            /// 创建可配置加载状态的热更管理器测试桩。
            /// </summary>
            public FakeHotfixManager(bool initialLoaded, bool isLoadedAfterLoadScript)
            {
                m_Loaded = initialLoaded;
                m_IsLoadedAfterLoadScript = isLoadedAfterLoadScript;
            }

            public bool Loaded
            {
                get
                {
                    return m_Loaded;
                }
            }

            public int LoadScriptAsyncCallCount { get; private set; }
            public string LastLoadScriptDllName { get; private set; }
            public string LastLoadScriptDirectory { get; private set; }

            /// <summary>
            /// 测试桩初始化入口，无额外逻辑。
            /// </summary>
            public void Init()
            {
            }

            /// <summary>
            /// 测试桩更新入口，无额外逻辑。
            /// </summary>
            public void Update()
            {
            }

            /// <summary>
            /// 记录热更程序集加载调用参数。
            /// </summary>
            public UniTask LoadScriptAsync(string hotfixDllName, string dir = "Hotfix/")
            {
                LoadScriptAsyncCallCount++;
                LastLoadScriptDllName = hotfixDllName;
                LastLoadScriptDirectory = dir;
                if (m_IsLoadedAfterLoadScript)
                {
                    m_Loaded = true;
                }

                return UniTask.CompletedTask;
            }

            /// <summary>
            /// 测试桩类型查询接口。
            /// </summary>
            public Type GetTypeByName(string typeName)
            {
                return null;
            }

            /// <summary>
            /// 测试桩实例化接口。
            /// </summary>
            public T Instantiate<T>(string typeName, object[] args = null)
            {
                return default;
            }

            /// <summary>
            /// 测试桩反射调用接口。
            /// </summary>
            public void Invoke(string type, string method, object instance, params object[] args)
            {
            }

            /// <summary>
            /// 测试桩销毁接口。
            /// </summary>
            public void Destroy()
            {
            }

            /// <summary>
            /// 测试桩热更类型登记接口。
            /// </summary>
            public void AddHotfixTypeAttr(List<Type> p_TypeArray)
            {
            }

            /// <summary>
            /// 测试桩按特性查询类型接口。
            /// </summary>
            public List<TypeAttr> GetAttrTypeDataList<T>()
            {
                return new List<TypeAttr>();
            }

            /// <summary>
            /// 测试桩按类型名称查询特性接口。
            /// </summary>
            public T FindAttr<T>(string typeName)
            {
                return default;
            }
        }

        /// <summary>
        /// 测试专用音频管理器。
        /// </summary>
        private sealed class FakeAudioManager : IAudioManager, IManager
        {
            public float AudioVolume
            {
                set
                {
                }
            }

            /// <summary>
            /// 测试桩初始化入口，无额外逻辑。
            /// </summary>
            public void Init()
            {
            }

            /// <summary>
            /// 测试桩更新入口，无额外逻辑。
            /// </summary>
            public void Update()
            {
            }

            /// <summary>
            /// 测试桩音频播放接口。
            /// </summary>
            public AudioChannel Play(AudioClip clip, bool loop = false, float fadeInSeconds = 0f, float volume = -1)
            {
                return null;
            }

            /// <summary>
            /// 测试桩音频播放接口。
            /// </summary>
            public AudioChannel Play(AudioClip clip, Transform emitter, bool loop = false, float fadeInSeconds = 0f, float volume = -1, Audio3DSettings? settings = null)
            {
                return null;
            }

            /// <summary>
            /// 测试桩音频播放接口。
            /// </summary>
            public AudioChannel Play(AudioClip clip, Vector3 point, bool loop = false, float fadeInSeconds = 0f, float volume = -1, Audio3DSettings? settings = null)
            {
                return null;
            }

            /// <summary>
            /// 测试桩停止播放接口。
            /// </summary>
            public void Stop(AudioChannel audioChannel)
            {
            }

            /// <summary>
            /// 测试桩立即停止播放接口。
            /// </summary>
            public void StopImmediate(AudioChannel audioChannel)
            {
            }

            /// <summary>
            /// 测试桩暂停播放接口。
            /// </summary>
            public void Pause(AudioChannel audioChannel)
            {
            }

            /// <summary>
            /// 测试桩恢复播放接口。
            /// </summary>
            public void Resume(AudioChannel audioChannel)
            {
            }

            /// <summary>
            /// 测试桩停止全部播放接口。
            /// </summary>
            public void StopAll()
            {
            }

            /// <summary>
            /// 测试桩暂停全部播放接口。
            /// </summary>
            public void PauseAll()
            {
            }

            /// <summary>
            /// 测试桩恢复全部播放接口。
            /// </summary>
            public void ResumeAll()
            {
            }
        }

        /// <summary>
        /// 测试专用步骤管理器。
        /// </summary>
        private sealed class FakeStepManager : IStepManager, IManager
        {
            public event Action<string> OnNodeEnter;
            public event Action<string> OnNodeLeave;
            public event Action<string> OnNodeChanged;
            public event Action<string> OnActionChanged;
            public event Action<string> OnJumpProgress;
            public event Action<string> OnJumpFailed;
            public event Action OnAllStepsCompleted;

            public bool IsRunning
            {
                get
                {
                    return false;
                }
            }

            public string CurrentNodeId
            {
                get
                {
                    return string.Empty;
                }
            }

            /// <summary>
            /// 测试桩初始化入口，无额外逻辑。
            /// </summary>
            public void Init()
            {
            }

            /// <summary>
            /// 测试桩更新入口，无额外逻辑。
            /// </summary>
            public void Update()
            {
            }

            /// <summary>
            /// 测试桩图加载接口。
            /// </summary>
            public void LoadGraph(string xmlAssetPath)
            {
            }

            /// <summary>
            /// 测试桩流程启动接口。
            /// </summary>
            public void Start(string graphName, string startNodeId = null)
            {
            }

            /// <summary>
            /// 测试桩流程停止接口。
            /// </summary>
            public void Stop()
            {
            }

            /// <summary>
            /// 测试桩流程重启接口。
            /// </summary>
            public void Restart()
            {
            }

            /// <summary>
            /// 测试桩上下文重置接口。
            /// </summary>
            public void ResetContext()
            {
            }

            /// <summary>
            /// 测试桩前进接口。
            /// </summary>
            public void Forward()
            {
            }

            /// <summary>
            /// 测试桩后退接口。
            /// </summary>
            public void Backward()
            {
            }

            /// <summary>
            /// 测试桩跳转接口。
            /// </summary>
            public void JumpTo(string targetNodeId)
            {
            }

            /// <summary>
            /// 测试桩获取全部节点接口。
            /// </summary>
            public List<StepNode> GetAllNodes(string graphName = null)
            {
                return new List<StepNode>();
            }

            /// <summary>
            /// 测试桩获取全部显示节点接口。
            /// </summary>
            public List<StepNode> GetAllDisplayNodes(string graphName = null)
            {
                return new List<StepNode>();
            }

            /// <summary>
            /// 测试桩节点状态查询接口。
            /// </summary>
            public StepNodeStatus GetNodeStatus(string nodeId)
            {
                return StepNodeStatus.Unfinished;
            }

            /// <summary>
            /// 测试桩可前进节点查询接口。
            /// </summary>
            public List<string> GetAvailableNextNodes()
            {
                return new List<string>();
            }

            /// <summary>
            /// 测试桩上下文查询接口。
            /// </summary>
            public StepContext GetStepContext()
            {
                return null;
            }

            /// <summary>
            /// 测试桩上下文序列化接口。
            /// </summary>
            public string GetContextToJson()
            {
                return string.Empty;
            }

            /// <summary>
            /// 测试桩运行时联调快照接口。
            /// </summary>
            public StepRuntimeDebugSnapshot GetRuntimeDebugSnapshot()
            {
                return new StepRuntimeDebugSnapshot(string.Empty, string.Empty, new List<string>(), new List<string>(), new Dictionary<string, string>());
            }

            /// <summary>
            /// 测试桩上下文反序列化接口。
            /// </summary>
            public void LoadContextFromJson(string json)
            {
            }
        }
    }
}
