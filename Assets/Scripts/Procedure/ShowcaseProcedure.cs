using System;
using System.Threading;
using UnityEngine;
using LWFMS;
using LWCore;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using LWUI;
using LWAudio;

[FSMTypeAttribute("Procedure", false)]
public class ShowcaseProcedure : BaseFSMState
{
    private const string TEST_SCENE_PATH = "Assets/0Res/Scenes2/Test2.unity";
    private const string CUBE_PREFAB_PATH = "Assets/0Res/Prefabs/Cube.prefab";
    private const string RAW_FILE_PATH = "Assets/0Res/RawFiles/333.txt";
    private const string EVENT_TEST = "TestEvent";

    private ShowcaseView m_FunctionShowcaseView;
    private bool m_IsBusy;
    private CancellationTokenSource m_CancellationTokenSource;


    private AudioClip m_BGMClip;
    private AudioChannel m_BGMChannel;
    public override void OnInit()
    {

    }

    public override void OnEnter(BaseFSMState lastState)
    {
        m_CancellationTokenSource?.Cancel();
        m_CancellationTokenSource?.Dispose();
        m_CancellationTokenSource = new CancellationTokenSource();

        m_FunctionShowcaseView = ManagerUtility.UIMgr.OpenView<ShowcaseView>(true, false);
        if (m_FunctionShowcaseView != null)
        {
            m_FunctionShowcaseView.ClearLog();
            m_FunctionShowcaseView.AppendLog("功能展示页已打开");
            m_FunctionShowcaseView.SetActionsInteractable(true);
        }

        ManagerUtility.EventMgr.AddListener(ShowcaseView.EVENT_CLOSE, OnCloseFunctionShowcaseView);
        ManagerUtility.EventMgr.AddListener(ShowcaseView.EVENT_LOAD_SCENE, OnViewRequestLoadScene);
        ManagerUtility.EventMgr.AddListener(ShowcaseView.EVENT_INSTANTIATE_CUBE, OnViewRequestInstantiateCube);
        ManagerUtility.EventMgr.AddListener(ShowcaseView.EVENT_LOAD_RAW_FILE, OnViewRequestLoadRawFile);
        ManagerUtility.EventMgr.AddListener(ShowcaseView.EVENT_DISPATCH_TEST_EVENT, OnViewRequestDispatchEvent);
        ManagerUtility.EventMgr.AddListener(ShowcaseView.EVENT_OPEN_LOADING, OnViewRequestOpenLoading);
        ManagerUtility.EventMgr.AddListener(ShowcaseView.EVENT_CLOSE_OTHER_VIEWS, OnViewRequestCloseOtherViews);
        ManagerUtility.EventMgr.AddListener(ShowcaseView.EVENT_BACK_VIEW, OnViewRequestBackView);
        // await ManagerUtility.MainMgr.LoadScene("Assets/0Res/Scenes2/Test2.unity");
        // ManagerUtility.EventMgr.AddListener<int>("TestEvent", OnTestEvent1);
        // ManagerUtility.EventMgr.AddListener<int>("TestEvent", OnTestEvent2);
        m_BGMClip = ManagerUtility.AssetsMgr.LoadAsset<AudioClip>("Assets/0Res/Audios/bgm.wav");
    }

    private void OnCloseFunctionShowcaseView()
    {
        AppendViewLog("关闭界面");
        if (ManagerUtility.UIMgr != null)
        {
            ManagerUtility.UIMgr.CloseView<ShowcaseView>();
        }
    }

    public override void OnLeave(BaseFSMState nextState)
    {
        m_CancellationTokenSource?.Cancel();
        m_CancellationTokenSource?.Dispose();
        m_CancellationTokenSource = null;

        ManagerUtility.EventMgr.RemoveListener(ShowcaseView.EVENT_CLOSE, OnCloseFunctionShowcaseView);
        ManagerUtility.EventMgr.RemoveListener(ShowcaseView.EVENT_LOAD_SCENE, OnViewRequestLoadScene);
        ManagerUtility.EventMgr.RemoveListener(ShowcaseView.EVENT_INSTANTIATE_CUBE, OnViewRequestInstantiateCube);
        ManagerUtility.EventMgr.RemoveListener(ShowcaseView.EVENT_LOAD_RAW_FILE, OnViewRequestLoadRawFile);
        ManagerUtility.EventMgr.RemoveListener(ShowcaseView.EVENT_DISPATCH_TEST_EVENT, OnViewRequestDispatchEvent);
        ManagerUtility.EventMgr.RemoveListener(ShowcaseView.EVENT_OPEN_LOADING, OnViewRequestOpenLoading);
        ManagerUtility.EventMgr.RemoveListener(ShowcaseView.EVENT_CLOSE_OTHER_VIEWS, OnViewRequestCloseOtherViews);
        ManagerUtility.EventMgr.RemoveListener(ShowcaseView.EVENT_BACK_VIEW, OnViewRequestBackView);

        m_FunctionShowcaseView = null;

        if (ManagerUtility.UIMgr != null)
        {
            ManagerUtility.UIMgr.CloseView<ShowcaseView>();
        }
        // ManagerUtility.EventMgr.RemoveListener<int>("TestEvent", OnTestEvent1);
        // ManagerUtility.EventMgr.RemoveListener<int>("TestEvent", OnTestEvent2);
    }

    public override void OnTermination()
    {

    }

    public override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (m_BGMChannel == null || !m_BGMChannel.IsPlay)
            {
                m_BGMChannel = ManagerUtility.AudioMgr.Play(m_BGMClip, false, 5, 1);
            }
            else
            {
                ManagerUtility.AudioMgr.Stop(m_BGMChannel);
            }
        }
    }

    private void SetBusy(bool isBusy)
    {
        m_IsBusy = isBusy;
        if (m_FunctionShowcaseView != null)
        {
            m_FunctionShowcaseView.SetActionsInteractable(!isBusy);
        }
    }

    private void AppendViewLog(string message)
    {
        if (m_FunctionShowcaseView != null)
        {
            m_FunctionShowcaseView.AppendLog(message);
        }
    }

    private void OnViewRequestLoadScene()
    {
        if (m_IsBusy)
        {
            AppendViewLog("操作进行中，请稍后...");
            return;
        }

        LoadSceneAsync().Forget();
    }

    private async UniTaskVoid LoadSceneAsync()
    {
        try
        {
            SetBusy(true);
            AppendViewLog("开始加载场景: " + TEST_SCENE_PATH);
            await ManagerUtility.MainMgr.LoadSceneWithUI(TEST_SCENE_PATH, LoadSceneMode.Additive);
            AppendViewLog("场景加载完成");
        }
        catch (Exception e)
        {
            AppendViewLog("场景加载失败: " + e.Message);
            Debug.LogException(e);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OnViewRequestInstantiateCube()
    {
        try
        {
            ManagerUtility.AssetsMgr.Instantiate(CUBE_PREFAB_PATH, null);
            AppendViewLog("已实例化: " + CUBE_PREFAB_PATH);
        }
        catch (Exception e)
        {
            AppendViewLog("实例化失败: " + e.Message);
            Debug.LogException(e);
        }
    }

    private void OnViewRequestLoadRawFile()
    {
        if (m_IsBusy)
        {
            AppendViewLog("操作进行中，请稍后...");
            return;
        }

        LoadRawFileAsync().Forget();
    }

    private async UniTaskVoid LoadRawFileAsync()
    {
        try
        {
            SetBusy(true);
            AppendViewLog("开始读取 RawFile: " + RAW_FILE_PATH);
            string text = await ManagerUtility.AssetsMgr.LoadRawFileTextAsync(RAW_FILE_PATH);
            string preview = text;
            if (!string.IsNullOrEmpty(text) && text.Length > 80)
            {
                preview = text.Substring(0, 80) + "...";
            }
            AppendViewLog("读取完成: " + preview);
        }
        catch (Exception e)
        {
            AppendViewLog("读取失败: " + e.Message);
            Debug.LogException(e);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OnViewRequestDispatchEvent()
    {
        try
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_TEST, 100);
            AppendViewLog("已派发事件: " + EVENT_TEST + " (100)");
        }
        catch (Exception e)
        {
            AppendViewLog("派发事件失败: " + e.Message);
            Debug.LogException(e);
        }
    }

    private void OnViewRequestOpenLoading()
    {
        if (m_IsBusy)
        {
            AppendViewLog("操作进行中，请稍后...");
            return;
        }

        OpenLoadingAsync().Forget();
    }

    private async UniTaskVoid OpenLoadingAsync()
    {
        try
        {
            SetBusy(true);

            ManagerUtility.UIMgr.OpenLoadingBar("示例 Loading...", true);
            ManagerUtility.UIMgr.UpdateLoadingBar(0f, "示例 Loading...", true);

            float duration = 1.0f;
            float elapsed = 0f;
            CancellationToken cancellationToken = m_CancellationTokenSource != null ? m_CancellationTokenSource.Token : CancellationToken.None;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                ManagerUtility.UIMgr.UpdateLoadingBar(progress, "示例 Loading...", true);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            AppendViewLog("LoadingView 演示完成");
        }
        catch (OperationCanceledException)
        {

        }
        catch (Exception e)
        {
            AppendViewLog("打开 LoadingView 失败: " + e.Message);
            Debug.LogException(e);
        }
        finally
        {
            ManagerUtility.UIMgr.CloseLoadingBar();
            SetBusy(false);
        }
    }

    private void OnViewRequestCloseOtherViews()
    {
        try
        {
            ManagerUtility.UIMgr.CloseOtherView<ShowcaseView>();
            AppendViewLog("已关闭其他界面");
        }
        catch (Exception e)
        {
            AppendViewLog("关闭其他界面失败: " + e.Message);
            Debug.LogException(e);
        }
    }

    private void OnViewRequestBackView()
    {
        try
        {
            BaseUIView uiViewBase = ManagerUtility.UIMgr.BackView(true);
            if (uiViewBase == null)
            {
                AppendViewLog("栈内没有可返回的界面");
            }
            else
            {
                AppendViewLog("已返回: " + uiViewBase.GetType().Name);
            }
        }
        catch (Exception e)
        {
            AppendViewLog("返回失败: " + e.Message);
            Debug.LogException(e);
        }
    }


}
