using System;
using Cysharp.Threading.Tasks;
using LWAssets;
using LWAudio;
using LWCore;
using LWFMS;
using LWHotfix;
using LWStep;
using LWUI;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class Startup : MonoBehaviour
{

    public string configUrl;
    public string procedureName = "StartProcedure";

    async void Start()
    {
        LWDebug.Log("Start");
        Application.targetFrameRate = 60;
        DontDestroyOnLoad(gameObject);
        //LWUpdate.ManifestNameUtility = new DefaultManifestNameUtility();
        // await LWUtility.ReadServerConfigAsync(configUrl);
        //设置LWDebug数据
        LWDebug.SetLogConfig(true, 3, true);

        ManagerUtility.MainMgr.Init();
        //添加各种管理器      
        ManagerUtility.MainMgr.AddManager(typeof(IAssetsManager).ToString(), new LWAssetsManager());
        ManagerUtility.MainMgr.AddManager(typeof(IEventManager).ToString(), new LWEventManager());
        ManagerUtility.MainMgr.AddManager(typeof(IUIManager).ToString(), new UIManager());
        ManagerUtility.MainMgr.AddManager(typeof(IHotfixManager).ToString(), new HotFixCodeManager());
        ManagerUtility.MainMgr.AddManager(typeof(IFSMManager).ToString(), new FSMManager());
        ManagerUtility.MainMgr.AddManager(typeof(IAudioManager).ToString(), new AudioManager());
        ManagerUtility.MainMgr.AddManager(typeof(IStepManager).ToString(), new StepManager());
        await ManagerUtility.AssetsMgr.InitializeAsync();
        ManagerUtility.MainMgr.MonoBehaviour = this;

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

        //设置第一个启动的流程
        MainManager.Instance.FirstFSMState = ManagerUtility.HotfixMgr.GetTypeByName(procedureName);
        MainManager.Instance.StartProcedure();

    }

    // Update is called once per frame
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
            var checkResult = await ManagerUtility.AssetsMgr.Version.CheckUpdateAsync();

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

    void OnDestroy()
    {
        WaitDestroy();
    }

    async void WaitDestroy()
    {
        await UniTask.DelayFrame(1);
        // ManagerUtility.HotfixMgr.Destroy();
        //SqliteHelp.Instance.Close();
        MainManager.Instance.ClearManager();
        Debug.Log("Startup OnDestroy");
    }


}
