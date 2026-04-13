using System;
using LWAudio;
using LWCore;
using LWStep;

/// <summary>
/// 宿主可选模块注册入口。
/// </summary>
public static class StartupOptionalModules
{
    /// <summary>
    /// 按启动配置注册宿主可选管理器（Audio、StepSystem）。
    /// </summary>
    public static void RegisterOptionalManagers(
        FrameworkBootstrapSettings settings,
        Func<IAudioManager> createAudioManager = null,
        Func<IStepManager> createStepManager = null)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (settings.EnableAudio)
        {
            Func<IAudioManager> audioFactory = createAudioManager ?? CreateDefaultAudioManager;
            RegisterOptionalManager<IAudioManager>(audioFactory());
        }

        if (settings.EnableStepSystem)
        {
            Func<IStepManager> stepFactory = createStepManager ?? CreateDefaultStepManager;
            RegisterOptionalManager<IStepManager>(stepFactory());
        }
    }

    /// <summary>
    /// 创建默认音频管理器实例。
    /// </summary>
    private static IAudioManager CreateDefaultAudioManager()
    {
        return new AudioManager();
    }

    /// <summary>
    /// 创建默认步骤管理器实例。
    /// </summary>
    private static IStepManager CreateDefaultStepManager()
    {
        return new StepManager();
    }

    /// <summary>
    /// 注册可选管理器并确保实例实现 IManager。
    /// </summary>
    private static void RegisterOptionalManager<TService>(TService managerInstance) where TService : class
    {
        IManager manager = managerInstance as IManager;
        if (manager == null)
        {
            throw new InvalidOperationException(typeof(TService).Name + " 未实现 IManager，无法注册到 MainManager。");
        }

        ManagerUtility.MainMgr.AddManager(typeof(TService).ToString(), manager);
    }
}
