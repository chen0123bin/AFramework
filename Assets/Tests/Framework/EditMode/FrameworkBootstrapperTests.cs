using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using LWFMS;
using LWAssets;
using LWCore;
using LWHotfix;
using LWUI;
using NUnit.Framework;
using UnityEngine;

namespace LWFramework.Tests.Framework.EditMode
{
    /// <summary>
    /// FrameworkBootstrapper 的核心注册与初始化行为测试。
    /// </summary>
    public sealed class FrameworkBootstrapperTests
    {
        private GameObject m_HostObject;

        /// <summary>
        /// 每条用例执行前清理主管理器状态，避免跨用例互相影响。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            ManagerUtility.MainMgr.ClearManager();
        }

        /// <summary>
        /// 每条用例执行后回收宿主对象并清理主管理器状态。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (m_HostObject != null)
            {
                UnityEngine.Object.DestroyImmediate(m_HostObject);
                m_HostObject = null;
            }

            ManagerUtility.MainMgr.ClearManager();
        }

        /// <summary>
        /// 验证默认核心引导仅注册核心模块，不注册 Audio 与 StepSystem。
        /// </summary>
        [Test]
        public void RegisterCoreManagers_OnlyCoreModulesRegistered()
        {
            FrameworkBootstrapperDependencies dependencies = new FrameworkBootstrapperDependencies();
            dependencies.CreateAssetsManager = () => new FakeAssetsManager();
            dependencies.CreateEventManager = () => new LWEventManager();
            dependencies.CreateUIManager = () => new FakeUIManager();
            dependencies.CreateHotfixManager = (mode) => new FakeHotfixManager(mode);
            dependencies.CreateFSMManager = () => new FSMManager();

            FrameworkBootstrapper bootstrapper = new FrameworkBootstrapper(dependencies);
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();

            bootstrapper.RegisterCoreManagers(settings);

            Assert.NotNull(ManagerUtility.AssetsMgr);
            Assert.NotNull(ManagerUtility.EventMgr);
            Assert.NotNull(ManagerUtility.UIMgr);
            Assert.NotNull(ManagerUtility.HotfixMgr);
            Assert.NotNull(ManagerUtility.FSMMgr);
            Assert.IsNull(ManagerUtility.AudioMgr);
            Assert.IsNull(ManagerUtility.StepMgr);
        }

        /// <summary>
        /// 验证核心初始化会触发 Assets 初始化并写入宿主 MonoBehaviour。
        /// </summary>
        [Test]
        public async System.Threading.Tasks.Task InitializeCoreAsync_InitializesAssetsAndAssignsHost()
        {
            FakeAssetsManager fakeAssetsManager = new FakeAssetsManager();
            FrameworkBootstrapperDependencies dependencies = new FrameworkBootstrapperDependencies();
            dependencies.CreateAssetsManager = () => fakeAssetsManager;
            dependencies.CreateEventManager = () => new LWEventManager();
            dependencies.CreateUIManager = () => new FakeUIManager();
            dependencies.CreateHotfixManager = (mode) => new FakeHotfixManager(mode);
            dependencies.CreateFSMManager = () => new FSMManager();

            FrameworkBootstrapper bootstrapper = new FrameworkBootstrapper(dependencies);
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();
            m_HostObject = new GameObject("FrameworkBootstrapperTestsHost");
            TestBootstrapHost host = m_HostObject.AddComponent<TestBootstrapHost>();

            await bootstrapper.InitializeCoreAsync(host, settings);

            Assert.AreEqual(1, fakeAssetsManager.InitializeAsyncCallCount);
            Assert.IsTrue(fakeAssetsManager.IsInitialized);
            Assert.AreSame(host, ReadMainManagerHost());
        }

        /// <summary>
        /// 验证默认依赖在 ByHyBridCLR 模式下会兼容回退并成功注册热更管理器。
        /// </summary>
        [Test]
        public void RegisterCoreManagers_ByHybridClrMode_UsesCompatibleFallback()
        {
            FrameworkBootstrapper bootstrapper = new FrameworkBootstrapper();
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();
            settings.EnableAssets = false;
            settings.EnableEvent = false;
            settings.EnableUI = false;
            settings.EnableFSM = false;
            settings.EnableHotfix = true;
            settings.HotfixMode = HotfixCodeRunMode.ByHyBridCLR;

            Assert.DoesNotThrow(() => bootstrapper.RegisterCoreManagers(settings));
            Assert.NotNull(ManagerUtility.HotfixMgr);
            Assert.IsInstanceOf<HotFixRefManager>(ManagerUtility.HotfixMgr);
        }

        /// <summary>
        /// 验证同一引导器先注册再初始化时不会重复注册并且资源仅初始化一次。
        /// </summary>
        [Test]
        public async System.Threading.Tasks.Task RegisterThenInitialize_DoesNotReRegisterAndAssetsInitOnce()
        {
            int createAssetsManagerCallCount = 0;
            int createEventManagerCallCount = 0;
            int createUIManagerCallCount = 0;
            int createHotfixManagerCallCount = 0;
            int createFSMManagerCallCount = 0;

            FakeAssetsManager firstAssetsManager = new FakeAssetsManager();
            FakeAssetsManager secondAssetsManager = new FakeAssetsManager();
            FrameworkBootstrapperDependencies dependencies = new FrameworkBootstrapperDependencies();
            dependencies.CreateAssetsManager = () =>
            {
                createAssetsManagerCallCount++;
                if (createAssetsManagerCallCount == 1)
                {
                    return firstAssetsManager;
                }

                return secondAssetsManager;
            };
            dependencies.CreateEventManager = () =>
            {
                createEventManagerCallCount++;
                return new LWEventManager();
            };
            dependencies.CreateUIManager = () =>
            {
                createUIManagerCallCount++;
                return new FakeUIManager();
            };
            dependencies.CreateHotfixManager = (mode) =>
            {
                createHotfixManagerCallCount++;
                return new FakeHotfixManager(mode);
            };
            dependencies.CreateFSMManager = () =>
            {
                createFSMManagerCallCount++;
                return new FSMManager();
            };

            FrameworkBootstrapper bootstrapper = new FrameworkBootstrapper(dependencies);
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();
            m_HostObject = new GameObject("FrameworkBootstrapperTestsHost_Idempotent");
            TestBootstrapHost host = m_HostObject.AddComponent<TestBootstrapHost>();

            bootstrapper.RegisterCoreManagers(settings);
            IAssetsManager assetsManagerAfterRegister = ManagerUtility.AssetsMgr;
            IEventManager eventManagerAfterRegister = ManagerUtility.EventMgr;
            IUIManager uiManagerAfterRegister = ManagerUtility.UIMgr;
            IHotfixManager hotfixManagerAfterRegister = ManagerUtility.HotfixMgr;
            IFSMManager fsmManagerAfterRegister = ManagerUtility.FSMMgr;

            await bootstrapper.InitializeCoreAsync(host, settings);

            Assert.AreEqual(1, createAssetsManagerCallCount);
            Assert.AreEqual(1, createEventManagerCallCount);
            Assert.AreEqual(1, createUIManagerCallCount);
            Assert.AreEqual(1, createHotfixManagerCallCount);
            Assert.AreEqual(1, createFSMManagerCallCount);
            Assert.AreEqual(1, firstAssetsManager.InitializeAsyncCallCount);
            Assert.AreEqual(0, secondAssetsManager.InitializeAsyncCallCount);
            Assert.AreSame(assetsManagerAfterRegister, ManagerUtility.AssetsMgr);
            Assert.AreSame(eventManagerAfterRegister, ManagerUtility.EventMgr);
            Assert.AreSame(uiManagerAfterRegister, ManagerUtility.UIMgr);
            Assert.AreSame(hotfixManagerAfterRegister, ManagerUtility.HotfixMgr);
            Assert.AreSame(fsmManagerAfterRegister, ManagerUtility.FSMMgr);
            Assert.AreSame(host, ReadMainManagerHost());
        }

        /// <summary>
        /// 通过反射读取 MainManager 的私有宿主字段，验证是否成功赋值。
        /// </summary>
        private static MonoBehaviour ReadMainManagerHost()
        {
            FieldInfo fieldInfo = typeof(MainManager).GetField("m_MonoBehaviour", BindingFlags.Instance | BindingFlags.NonPublic);
            return fieldInfo?.GetValue(ManagerUtility.MainMgr) as MonoBehaviour;
        }

        /// <summary>
        /// 用于测试的宿主脚本类型。
        /// </summary>
        private sealed class TestBootstrapHost : MonoBehaviour
        {
        }

        /// <summary>
        /// 测试专用的简化资产管理器，记录初始化调用次数。
        /// </summary>
        private sealed class FakeAssetsManager : IAssetsManager, IManager
        {
            public bool IsInitialized { get; private set; }

            public LWAssets.PlayMode CurrentPlayMode
            {
                get
                {
                    return LWAssets.PlayMode.EditorSimulate;
                }
            }

            public IAssetLoader Loader
            {
                get
                {
                    return null;
                }
            }

            public DownloadManager Downloader
            {
                get
                {
                    return null;
                }
            }

            public CacheManager Cache
            {
                get
                {
                    return null;
                }
            }

            public PreloadManager Preloader
            {
                get
                {
                    return null;
                }
            }

            public VersionManager Version
            {
                get
                {
                    return null;
                }
            }

            public int InitializeAsyncCallCount { get; private set; }

            /// <summary>
            /// 测试桩的同步初始化入口，无额外逻辑。
            /// </summary>
            public void Init()
            {
            }

            /// <summary>
            /// 测试桩每帧更新入口，无额外逻辑。
            /// </summary>
            public void Update()
            {
            }

            /// <summary>
            /// 记录资源系统初始化调用并标记为已初始化。
            /// </summary>
            public UniTask InitializeAsync(LWAssetsConfig config = null)
            {
                InitializeAsyncCallCount++;
                IsInitialized = true;
                return UniTask.CompletedTask;
            }

            /// <summary>
            /// 测试桩不执行 Shader 预热。
            /// </summary>
            public UniTask WarmupShadersAsync(CancellationToken token = default)
            {
                return UniTask.CompletedTask;
            }

            /// <summary>
            /// 销毁测试桩状态。
            /// </summary>
            public void Destroy()
            {
                IsInitialized = false;
            }

            /// <summary>
            /// 测试桩返回空清单。
            /// </summary>
            public UniTask<BundleManifest> LoadManifestAsync()
            {
                return UniTask.FromResult<BundleManifest>(null);
            }

            /// <summary>
            /// 测试桩不提供同步资源加载能力。
            /// </summary>
            public T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 测试桩不提供同步原始文件加载能力。
            /// </summary>
            public byte[] LoadRawFile(string assetPath)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 测试桩不提供同步文本原始文件加载能力。
            /// </summary>
            public string LoadRawFileText(string assetPath)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 测试桩不提供同步实例化能力。
            /// </summary>
            public GameObject Instantiate(string assetPath, Transform spawnPoint = null)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 测试桩不提供异步实例化能力。
            /// </summary>
            public UniTask<GameObject> InstantiateAsync(string assetPath, Transform spawnPoint = null)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 测试桩不提供异步资源加载能力。
            /// </summary>
            public UniTask<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 测试桩不提供异步原始文件加载能力。
            /// </summary>
            public UniTask<byte[]> LoadRawFileAsync(string assetPath, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 测试桩不提供异步文本原始文件加载能力。
            /// </summary>
            public UniTask<string> LoadRawFileTextAsync(string assetPath, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 测试桩不提供场景加载能力。
            /// </summary>
            public UniTask<SceneHandle> LoadSceneAsync(string scenePath, UnityEngine.SceneManagement.LoadSceneMode mode = UnityEngine.SceneManagement.LoadSceneMode.Single, bool activateOnLoad = true, IProgress<float> progress = null, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 测试桩不提供通过路径卸载场景能力。
            /// </summary>
            public UniTask UnloadSceneAsync(string scenePath, bool forceRelease = true, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 测试桩不提供通过句柄卸载场景能力。
            /// </summary>
            public UniTask UnloadSceneAsync(SceneHandle sceneHandle, bool forceRelease = true, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 测试桩不提供批量资源加载能力。
            /// </summary>
            public UniTask<T[]> LoadAssetsAsync<T>(string[] assetPaths, IProgress<float> progress = null, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 测试桩不提供资源释放能力。
            /// </summary>
            public void Release(UnityEngine.Object asset)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 测试桩不提供按路径释放资源能力。
            /// </summary>
            public void Release(string assetPath)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 测试桩不提供卸载未使用资源能力。
            /// </summary>
            public UniTask UnloadUnusedAssetsAsync()
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 测试桩不提供强制清理资源能力。
            /// </summary>
            public void ForceUnloadAll()
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 测试桩不提供下载大小统计能力。
            /// </summary>
            public UniTask<long> GetDownloadSizeAsync(string[] tags = null)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 测试桩不提供下载能力。
            /// </summary>
            public UniTask DownloadAsync(string[] tags = null, IProgress<DownloadProgress> progress = null, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 测试专用 UI 管理器，避免依赖场景节点。
        /// </summary>
        private sealed class FakeUIManager : BaseUIManager
        {
            private Canvas m_UICanvas;

            public override Canvas UICanvas
            {
                get
                {
                    return m_UICanvas;
                }
                set
                {
                    m_UICanvas = value;
                }
            }

            public override Camera UICamera
            {
                get
                {
                    return null;
                }
            }

            /// <summary>
            /// 打开泛型视图测试桩实现。
            /// </summary>
            public override T OpenView<T>(object data = null, bool isLastSibling = false, bool enterStack = false)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 打开字符串视图测试桩实现。
            /// </summary>
            public override BaseUIView OpenView(string viewType, object data = null, GameObject uiGameObject = null, bool isLastSibling = false, bool enterStack = false)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 异步打开视图测试桩实现。
            /// </summary>
            public override UniTask<T> OpenViewAsync<T>(object data = null, bool isLastSibling = false, bool enterStack = false)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 预加载默认 UI 测试桩实现。
            /// </summary>
            public override UniTask PreLoadDefaultUI()
            {
                return UniTask.CompletedTask;
            }

            /// <summary>
            /// 打开弹窗测试桩实现。
            /// </summary>
            public override void OpenDialog(string title, string content, Action<bool> resultCallback, bool isShowCancel = true, bool isShowClose = true, bool isLastSibling = true)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 异步打开弹窗测试桩实现。
            /// </summary>
            public override UniTask<bool> OpenDialogAsync(string title, string content, bool isShowCancel = true, bool isShowClose = true, bool isLastSibling = true)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 打开加载界面测试桩实现。
            /// </summary>
            public override void OpenLoadingBar(string tip = "当前正在加载...", bool isLastSibling = true)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 更新加载界面测试桩实现。
            /// </summary>
            public override void UpdateLoadingBar(float progress, string tip = null, bool isLastSibling = true)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// 关闭加载界面测试桩实现。
            /// </summary>
            public override void CloseLoadingBar()
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 测试专用热更管理器。
        /// </summary>
        private sealed class FakeHotfixManager : IHotfixManager, IManager
        {
            private readonly HotfixCodeRunMode m_Mode;

            /// <summary>
            /// 创建测试热更管理器并记录模式。
            /// </summary>
            public FakeHotfixManager(HotfixCodeRunMode mode)
            {
                m_Mode = mode;
            }

            public bool Loaded
            {
                get
                {
                    return m_Mode == HotfixCodeRunMode.ByCode || m_Mode == HotfixCodeRunMode.ByReflection;
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
            /// 测试桩脚本加载接口。
            /// </summary>
            public UniTask LoadScriptAsync(string hotfixDllName, string dir = "Hotfix/")
            {
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
    }
}
