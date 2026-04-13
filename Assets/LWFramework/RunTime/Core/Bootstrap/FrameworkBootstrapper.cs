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
    /// 框架核心引导器依赖工厂集合。
    /// </summary>
    public sealed class FrameworkBootstrapperDependencies
    {
        public Func<IAssetsManager> CreateAssetsManager = () => new LWAssetsManager();
        public Func<IEventManager> CreateEventManager = () => new LWEventManager();
        public Func<IUIManager> CreateUIManager = () => new UIManager();
        public Func<HotfixCodeRunMode, IHotfixManager> CreateHotfixManager = CreateDefaultHotfixManager;
        public Func<IFSMManager> CreateFSMManager = () => new FSMManager();

        /// <summary>
        /// 按热更模式创建默认热更管理器。
        /// </summary>
        private static IHotfixManager CreateDefaultHotfixManager(HotfixCodeRunMode mode)
        {
            switch (mode)
            {
                case HotfixCodeRunMode.ByCode:
                    return new HotFixCodeManager();
                case HotfixCodeRunMode.ByReflection:
                    return new HotFixRefManager();
                default:
                    throw new InvalidOperationException("默认核心引导仅支持 ByCode 与 ByReflection 模式。");
            }
        }
    }

    /// <summary>
    /// 负责核心模块注册与初始化的框架引导器。
    /// </summary>
    public sealed class FrameworkBootstrapper
    {
        private readonly FrameworkBootstrapperDependencies m_Dependencies;

        /// <summary>
        /// 创建框架核心引导器，支持注入替代依赖工厂。
        /// </summary>
        public FrameworkBootstrapper(FrameworkBootstrapperDependencies dependencies = null)
        {
            m_Dependencies = dependencies ?? new FrameworkBootstrapperDependencies();
        }

        /// <summary>
        /// 注册框架核心模块（Assets/Event/UI/Hotfix/FSM）。
        /// </summary>
        public void RegisterCoreManagers(FrameworkBootstrapSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            ManagerUtility.MainMgr.Init();

            if (settings.IsModuleEnabled(FrameworkModuleId.Assets))
            {
                RegisterManager<IAssetsManager>(m_Dependencies.CreateAssetsManager());
            }

            if (settings.IsModuleEnabled(FrameworkModuleId.Event))
            {
                RegisterManager<IEventManager>(m_Dependencies.CreateEventManager());
            }

            if (settings.IsModuleEnabled(FrameworkModuleId.UI))
            {
                RegisterManager<IUIManager>(m_Dependencies.CreateUIManager());
            }

            if (settings.IsModuleEnabled(FrameworkModuleId.Hotfix))
            {
                RegisterManager<IHotfixManager>(m_Dependencies.CreateHotfixManager(settings.HotfixMode));
            }

            if (settings.IsModuleEnabled(FrameworkModuleId.FSM))
            {
                RegisterManager<IFSMManager>(m_Dependencies.CreateFSMManager());
            }
        }

        /// <summary>
        /// 完成核心模块注册，并执行资源系统初始化与宿主绑定。
        /// </summary>
        public async UniTask InitializeCoreAsync(MonoBehaviour host, FrameworkBootstrapSettings settings)
        {
            RegisterCoreManagers(settings);

            IAssetsManager assetsManager = ManagerUtility.AssetsMgr;
            if (assetsManager != null && !assetsManager.IsInitialized)
            {
                await assetsManager.InitializeAsync();
            }

            ManagerUtility.MainMgr.MonoBehaviour = host;
        }

        /// <summary>
        /// 注册单个管理器并确保其实现 IManager。
        /// </summary>
        private static void RegisterManager<TService>(TService managerInstance) where TService : class
        {
            IManager manager = managerInstance as IManager;
            if (manager == null)
            {
                throw new InvalidOperationException(typeof(TService).Name + " 未实现 IManager，无法注册到 MainManager。");
            }

            ManagerUtility.MainMgr.AddManager(typeof(TService).ToString(), manager);
        }
    }
}
