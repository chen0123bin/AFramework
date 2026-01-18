using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using LWCore;
using LWFMS;
using LWUI;
using UnityEngine;

[FSMTypeAttribute("Procedure", false)]
public class UIDemoProcedure : BaseFSMState
{
    private CancellationTokenSource m_CancellationTokenSource;

    public override void OnInit()
    {
    }

    /// <summary>
    /// 进入 UI 演示流程：打开界面并注册事件监听。
    /// </summary>
    /// <param name="lastState">上一个状态</param>
    public override void OnEnter(BaseFSMState lastState)
    {
        m_CancellationTokenSource?.Cancel();
        m_CancellationTokenSource?.Dispose();
        m_CancellationTokenSource = new CancellationTokenSource();

        ManagerUtility.EventMgr.AddListener(UIDemoView.EVENT_CLOSE, OnClose);
        ManagerUtility.EventMgr.AddListener(UIDemoView.EVENT_OPEN_LOGIN_VIEW, OnOpenLoginView);
        ManagerUtility.EventMgr.AddListener(UIDemoView.EVENT_OPEN_LOADING_VIEW, OnOpenLoadingView);
        ManagerUtility.EventMgr.AddListener(UIDemoView.EVENT_OPEN_FUNCTION_SHOWCASE_VIEW, OnOpenFunctionShowcaseView);
        ManagerUtility.EventMgr.AddListener(UIDemoView.EVENT_CLOSE_TOP_VIEW, OnCloseTopView);
        ManagerUtility.EventMgr.AddListener(UIDemoView.EVENT_CLOSE_ALL_VIEWS, OnCloseAllViews);
        ManagerUtility.EventMgr.AddListener(UIDemoView.EVENT_BACK_VIEW, OnBackView);
        ManagerUtility.EventMgr.AddListener(UIDemoView.EVENT_OPEN_DIALOG, OnOpenDialog);

        ManagerUtility.UIMgr.OpenView<UIDemoView>();
    }

    /// <summary>
    /// 离开 UI 演示流程：关闭界面并移除事件监听。
    /// </summary>
    /// <param name="nextState">下一个状态</param>
    public override void OnLeave(BaseFSMState nextState)
    {
        m_CancellationTokenSource?.Cancel();
        m_CancellationTokenSource?.Dispose();
        m_CancellationTokenSource = null;

        ManagerUtility.EventMgr.RemoveListener(UIDemoView.EVENT_CLOSE, OnClose);
        ManagerUtility.EventMgr.RemoveListener(UIDemoView.EVENT_OPEN_LOGIN_VIEW, OnOpenLoginView);
        ManagerUtility.EventMgr.RemoveListener(UIDemoView.EVENT_OPEN_LOADING_VIEW, OnOpenLoadingView);
        ManagerUtility.EventMgr.RemoveListener(UIDemoView.EVENT_OPEN_FUNCTION_SHOWCASE_VIEW, OnOpenFunctionShowcaseView);
        ManagerUtility.EventMgr.RemoveListener(UIDemoView.EVENT_CLOSE_TOP_VIEW, OnCloseTopView);
        ManagerUtility.EventMgr.RemoveListener(UIDemoView.EVENT_CLOSE_ALL_VIEWS, OnCloseAllViews);
        ManagerUtility.EventMgr.RemoveListener(UIDemoView.EVENT_BACK_VIEW, OnBackView);
        ManagerUtility.EventMgr.RemoveListener(UIDemoView.EVENT_OPEN_DIALOG, OnOpenDialog);

        ManagerUtility.UIMgr.CloseView<UIDemoView>();
    }

    public override void OnTermination()
    {
    }

    public override void OnUpdate()
    {
    }

    /// <summary>
    /// 关闭 UI 演示界面并返回菜单流程。
    /// </summary>
    private void OnClose()
    {
        ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<MenuProcedure>();
    }

    /// <summary>
    /// 进入登录流程（由 LoginProcedure 负责打开 LoginView 并监听提交事件）。
    /// </summary>
    private void OnOpenLoginView()
    {
        ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<LoginProcedure>();
    }

    /// <summary>
    /// 打开并演示 LoadingBarView 的简单进度变化。
    /// </summary>
    private void OnOpenLoadingView()
    {
        OpenLoadingAsync().Forget();
    }

    /// <summary>
    /// 进入功能展示流程。
    /// </summary>
    private void OnOpenFunctionShowcaseView()
    {
        ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<ShowcaseProcedure>();
    }

    /// <summary>
    /// 关闭当前“栈顶”界面（若存在）。
    /// </summary>
    private void OnCloseTopView()
    {
        ManagerUtility.UIMgr.BackView(true);
    }

    /// <summary>
    /// 关闭除 UIDemoView 以外的所有界面。
    /// </summary>
    private void OnCloseAllViews()
    {
        ManagerUtility.UIMgr.CloseOtherView<UIDemoView>();
    }

    /// <summary>
    /// 返回上一个入栈界面。
    /// </summary>
    private void OnBackView()
    {
        ManagerUtility.UIMgr.BackView(true);
    }

    /// <summary>
    /// 弹窗演示入口（当前工程未提供内置 Dialog View，暂用日志提示）。
    /// </summary>
    private async void OnOpenDialog()
    {
        bool isConfirm = await ManagerUtility.UIMgr.OpenDialogAsync("确认操作", "是否继续执行？", true, true, true);
        if (isConfirm)
        {
            LWDebug.Log("用户点击了确认按钮");
        }
        else
        {
            LWDebug.Log("用户点击了取消按钮");
        }
    }

    /// <summary>
    /// 简易 Loading 演示：打开 LoadingBarView 并在 1 秒内推进到 100%。
    /// </summary>
    private async UniTaskVoid OpenLoadingAsync()
    {
        CancellationToken cancellationToken = m_CancellationTokenSource != null ? m_CancellationTokenSource.Token : CancellationToken.None;
        try
        {
            ManagerUtility.UIMgr.OpenLoadingBar("当前正在加载...", true);
            ManagerUtility.UIMgr.UpdateLoadingBar(0f, "当前正在加载...", true);

            float duration = 1.0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                ManagerUtility.UIMgr.UpdateLoadingBar(progress, "当前正在加载...", true);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            LWDebug.LogError("打开 LoadingBarView 失败: " + e.Message);
            Debug.LogException(e);
        }
        finally
        {
            ManagerUtility.UIMgr.CloseLoadingBar();
        }
    }
}
