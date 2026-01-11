
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LWAssets;
using LWCore;
using LWHotfix;
using LWUI;
using UnityEngine;
[DefaultExecutionOrder(1000)]
public class Startup : MonoBehaviour
{

    public string configUrl;
    public string procedureName = "StartProcedure";

    private LoadingView m_LoadingView;
    async void Start()
    {
        LWDebug.Log("Start");
        Application.targetFrameRate = 60;
        DontDestroyOnLoad(gameObject);
        //LWUpdate.ManifestNameUtility = new DefaultManifestNameUtility();
        // await LWUtility.ReadServerConfigAsync(configUrl);
        //设置LWDebug数据
        LWDebug.SetLogConfig(true, 3, true);

        MainManager.Instance.Init();
        //添加各种管理器      

        MainManager.Instance.AddManager(typeof(IAssetsManager).ToString(), new LWAssetsManager());
        MainManager.Instance.AddManager(typeof(IEventManager).ToString(), new LWEventManager());
        MainManager.Instance.AddManager(typeof(IUIManager).ToString(), new UIManager());
        MainManager.Instance.AddManager(typeof(IHotfixManager).ToString(), new HotFixCodeManager());

        await ManagerUtility.AssetsMgr.InitializeAsync();
        MainManager.Instance.MonoBehaviour = this;
        if (ManagerUtility.AssetsMgr.CurrentPlayMode == LWAssets.PlayMode.Online)
        {

            m_LoadingView = ManagerUtility.UIMgr.OpenView<LoadingView>();
            await DownloadAsync();
            await UniTask.Delay(1500);
            ManagerUtility.UIMgr.CloseView<LoadingView>();
        }

        ManagerUtility.EventMgr.AddListener<int>("TestEvent", OnTestEvent1);
        ManagerUtility.EventMgr.AddListener<int>("TestEvent", OnTestEvent2);

    }

    private void OnTestEvent2(int obj)
    {
        LoadScene2Async().Forget();
        LWDebug.Log($"OnTestEvent2 {obj}");
    }

    private void OnTestEvent1(int obj)
    {
        LWDebug.Log($"OnTestEvent1 {obj}");
        ManagerUtility.UIMgr.OpenView<TestView>();
    }


    /// <summary>
    /// 默认资源更新完成
    /// </summary>
    /// <param name="obj"></param>
    private async void OnUpdateCallback(bool obj)
    {

    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            ManagerUtility.AssetsMgr.InstantiateAsync("Assets/0Res/Prefabs/Cube.prefab", null);

        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            //GameObject go = ManagerUtility.AssetsMgr.LoadAsset<GameObject>("Assets/0Res/Prefabs/Cube.prefab");
            ManagerUtility.AssetsMgr.Instantiate("Assets/0Res/Prefabs/Cube.prefab", null);

        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            ManagerUtility.EventMgr.DispatchEvent("TestEvent", 100);
        }

        MainManager.Instance.Update();
    }

    /// <summary>
    /// 下载资源示例
    /// </summary>
    private async UniTask DownloadAsync()
    {
        if (!ManagerUtility.AssetsMgr.IsInitialized)
        {
            m_LoadingView.Tip = "Please initialize first!";
            return;
        }

        try
        {
            // 检查更新
            m_LoadingView.Tip = "Checking for updates...";
            var checkResult = await ManagerUtility.AssetsMgr.Version.CheckUpdateAsync();

            if (checkResult.Status == UpdateStatus.NoUpdate)
            {
                m_LoadingView.Tip = "No updates available.";
                return;
            }

            m_LoadingView.Tip = $"Update available: {checkResult.RemoteVersion}, Size: {FileUtility.FormatFileSize(checkResult.DownloadSize)}";

            // 开始下载
            var progress = new Progress<DownloadProgress>(p =>
            {
                m_LoadingView.Progress = p.Progress;
                m_LoadingView.Tip = $"Downloading: {p.CompletedCount}/{p.TotalCount} - {FileUtility.FormatFileSize((long)p.Speed)}/s";
            });

            await ManagerUtility.AssetsMgr.DownloadAsync(null, progress);

            m_LoadingView.Progress = 1f;
            m_LoadingView.Tip = "Download completed!";
        }
        catch (Exception ex)
        {
            m_LoadingView.Tip = $"Download failed: {ex.Message}";
            Debug.LogException(ex);
        }
    }
    /// <summary>
    /// 加载场景示例
    /// </summary>
    private async UniTaskVoid LoadScene2Async()
    {

        Debug.Log("LoadScene2Async");
        var handle = await ManagerUtility.AssetsMgr.LoadSceneAsync(
               "Assets/0Res/Scenes2/Test2.unity",
               UnityEngine.SceneManagement.LoadSceneMode.Additive,
               true);
        if (handle.IsValid)
        {
            LWDebug.Log($"Scene loaded: {handle.Scene.name}");
        }
        else if (handle.HasError)
        {
            LWDebug.Log($"Scene load error: {handle.Error.Message}");
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
