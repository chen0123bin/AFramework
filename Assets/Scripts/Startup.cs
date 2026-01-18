
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LWAssets;
using LWAudio;
using LWCore;
using LWFMS;
using LWHotfix;
using LWUI;
using UnityEngine;
[DefaultExecutionOrder(1000)]
public class Startup : MonoBehaviour
{

    public string configUrl;
    public string procedureName = "StartProcedure";

    private LoadingBarView m_LoadingBarView;
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
        await ManagerUtility.AssetsMgr.InitializeAsync();
        ManagerUtility.MainMgr.MonoBehaviour = this;
        if (ManagerUtility.AssetsMgr.CurrentPlayMode == LWAssets.PlayMode.Online)
        {

            m_LoadingBarView = ManagerUtility.UIMgr.OpenView<LoadingBarView>();
            await DownloadAsync();
            await UniTask.Delay(500);

            ManagerUtility.UIMgr.CloseView<LoadingBarView>();
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
            m_LoadingBarView.Tip = "资管管理器未初始化!";
            return;
        }
        try
        {
            // 检查更新
            m_LoadingBarView.Tip = "检查更新...";
            var checkResult = await ManagerUtility.AssetsMgr.Version.CheckUpdateAsync();

            if (checkResult.Status == UpdateStatus.NoUpdate)
            {
                m_LoadingBarView.Tip = "没有可用更新。";
                return;
            }
            m_LoadingBarView.Tip = $"发现更新: {checkResult.RemoteVersion}, 大小: {FileUtility.FormatFileSize(checkResult.DownloadSize)}";
            // 开始下载
            var progress = new Progress<DownloadProgress>(p =>
            {
                m_LoadingBarView.Progress = p.Progress;
                m_LoadingBarView.Tip = $"下载中: {p.CompletedCount}/{p.TotalCount} - {FileUtility.FormatFileSize((long)p.Speed)}/s";
            });

            await ManagerUtility.AssetsMgr.DownloadAsync(null, progress);

            m_LoadingBarView.Progress = 1f;
            m_LoadingBarView.Tip = "下载完成!";
        }
        catch (Exception ex)
        {
            m_LoadingBarView.Tip = $"下载出错: {ex.Message}";
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
