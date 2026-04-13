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
    /// 执行宿主启动链路：初始化核心、注册可选模块、更新资源并启动流程。
    /// </summary>
    async void Start()
    {
        LWDebug.Log("启动流程开始");
        Application.targetFrameRate = 60;
        DontDestroyOnLoad(gameObject);
        //LWUpdate.ManifestNameUtility = new DefaultManifestNameUtility();
        // await LWUtility.ReadServerConfigAsync(configUrl);
        //设置LWDebug数据
        LWDebug.SetLogConfig(true, 3, true);

        FrameworkBootstrapSettings settings = BuildBootstrapSettings();
        FrameworkBootstrapper bootstrapper = new FrameworkBootstrapper();
        await bootstrapper.InitializeCoreAsync(this, settings);
        StartupOptionalModules.RegisterOptionalManagers(settings);

        IAssetsManager assetsManager = ManagerUtility.AssetsMgr;
        if (assetsManager != null && assetsManager.CurrentPlayMode == LWAssets.PlayMode.Online)
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
    /// 逐帧驱动主管理器更新。
    /// </summary>
    void Update()
    {
        MainManager.Instance.Update();
    }

    /// <summary>
    /// 下载资源示例
    /// </summary>
    private async UniTask DownloadAsync()
    {
        if (!ManagerUtility.AssetsMgr.IsInitialized)
        {
            ManagerUtility.UIMgr.UpdateLoadingBar(0f, "资管管理器未初始化!", true);
            return;
        }

        try
        {
            // 检查更新
            ManagerUtility.UIMgr.UpdateLoadingBar(0f, "检查更新...", true);
            UpdateCheckResult checkResult = await ManagerUtility.AssetsMgr.Version.CheckUpdateAsync();

            if (checkResult.Status == UpdateStatus.NoUpdate)
            {
                ManagerUtility.UIMgr.UpdateLoadingBar(0f, "没有可用更新。", true);
                return;
            }

            ManagerUtility.UIMgr.UpdateLoadingBar(0f, $"发现更新: {checkResult.RemoteVersion}, 大小: {FileUtility.FormatFileSize(checkResult.DownloadSize)}", true);

            System.Progress<DownloadProgress> progress = new Progress<DownloadProgress>(downloadProgress =>
            {
                ManagerUtility.UIMgr.UpdateLoadingBar(downloadProgress.Progress, $"下载中: {downloadProgress.CompletedCount}/{downloadProgress.TotalCount} - {FileUtility.FormatFileSize((long)downloadProgress.Speed)}/s", true);
            });

            await ManagerUtility.AssetsMgr.DownloadAsync(null, progress);
            ManagerUtility.UIMgr.UpdateLoadingBar(1f, "下载完成!", true);
        }
        catch (Exception ex)
        {
            ManagerUtility.UIMgr.UpdateLoadingBar(0f, $"下载出错: {ex.Message}", true);
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// 构建框架启动配置并写入宿主可调参数。
    /// </summary>
    private FrameworkBootstrapSettings BuildBootstrapSettings()
    {
        FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();
        settings.ProcedureName = procedureName;
        settings.HotfixMode = hotfixCodeRunMode;
        settings.ReflectionHotfixAssemblyName = reflectionHotfixAssemblyName?.Trim() ?? string.Empty;
        settings.EnableAudio = enableAudio;
        settings.EnableStepSystem = enableStepSystem;
        return settings;
    }

    /// <summary>
    /// 触发启动脚本销毁收尾流程。
    /// </summary>
    void OnDestroy()
    {
        WaitDestroy();
    }

    /// <summary>
    /// 延迟一帧执行销毁清理，避免与当前帧逻辑竞争。
    /// </summary>
    async void WaitDestroy()
    {
        await UniTask.DelayFrame(1);
        // ManagerUtility.HotfixMgr.Destroy();
        //SqliteHelp.Instance.Close();
        MainManager.Instance.ClearManager();
        Debug.Log("Startup 已销毁");
    }
}
