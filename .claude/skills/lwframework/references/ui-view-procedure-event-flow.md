# View → 事件 → Procedure → View（推荐解耦流程）

目标：让 View 只负责 UI 展示与用户输入采集；所有业务逻辑都放在 Procedure（或其他业务层）中；业务层通过 View 的 public 方法回写 UI。

本模式适用于：

- UI 是 Prefab 驱动、生命周期由 UIManager 管理
- 需要让 View 尽量“薄”，避免直接调用 AssetsMgr/UIMgr/MainMgr 等逻辑
- 希望统一用 EventMgr 做模块间通信（降低 View 与业务层的直接耦合）

## 约定

- View 内部：UI 组件点击仅派发事件，不做业务
- Procedure 内部：AddListener 接收事件、执行业务、调用 View public 方法更新显示
- 事件名：用常量集中管理，避免硬编码字符串
- 解绑：Procedure 侧 RemoveListener 解除 EventMgr 监听

## 示例


### 1) View：只负责派发事件与展示

关键点：

- View 层推荐用 lambda 来生成 `UnityAction`
- 点击时只 `DispatchEvent`，不触碰业务系统（AssetsMgr/UIMgr/MainMgr 等）
- 提供 `AppendLog/SetActionsInteractable` 等 public 方法给 Procedure 调用

```csharp
using LWCore;
using LWUI;
using UnityEngine;
using UnityEngine.UI;

[UIViewData("Assets/0Res/Prefabs/UI/FunctionShowcaseView.prefab", (int)FindType.Name, "LWFramework/Canvas/Normal")]
public class FunctionShowcaseView : BaseUIView
{
    public const string EVENT_CLOSE = "CloseFunctionShowcaseView";
    public const string EVENT_LOAD_SCENE = "LoadSceneFunctionShowcaseView";
    public const string EVENT_AUTO_LOAD_CHANGED = "AutoLoadChangedFunctionShowcaseView";

    [UIElement("PnlTop/BtnClose")]
    private Button m_BtnClose;

    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnLoadScene")]
    private Button m_BtnLoadScene;

    [UIElement("PnlCard/TglAutoLoad")]
    private Toggle m_TglAutoLoad;

    /// <summary>
    /// 创建并初始化界面：注册 UI 交互并派发 EventMgr 事件。
    /// </summary>
    public override void CreateView(GameObject gameObject)
    {
        base.CreateView(gameObject);
        m_BtnClose.onClick.AddListener(() => 
        { 
            ManagerUtility.EventMgr.DispatchEvent(EVENT_CLOSE); 
        });
        m_BtnLoadScene.onClick.AddListener(() => 
        { 
            ManagerUtility.EventMgr.DispatchEvent(EVENT_LOAD_SCENE); 
        });
        m_TglAutoLoad.onValueChanged.AddListener((bool isOn) => 
        { 
            ManagerUtility.EventMgr.DispatchEvent<bool>(EVENT_AUTO_LOAD_CHANGED, isOn); 
        });
    }

    /// <summary>
    /// 清理界面：示例中不显式 RemoveListener，交由 View 销毁时释放。
    /// </summary>
    public override void ClearView()
    {   
        base.ClearView();
    }


    /// <summary>
    /// 追加日志：由 Procedure 调用更新显示。
    /// </summary>
    public void AppendLog(string message)
    {
    }

    /// <summary>
    /// 设置按钮交互：由 Procedure 在忙碌态切换。
    /// </summary>
    public void SetActionsInteractable(bool isInteractable)
    {
    }
}
```

### 2) Procedure：接收事件并执行业务，再回写 UI

关键点：

- `OnEnter`：打开 View、注册事件监听
- `OnLeave`：反注册事件监听、关闭 View
- 业务执行时，通过 `m_FunctionShowcaseView` 的 public 方法更新 UI

```csharp
using LWFMS;
using LWCore;
using UnityEngine;

[FSMTypeAttribute("Procedure", false)]
public class TestProcedure : BaseFSMState
{
    private FunctionShowcaseView m_FunctionShowcaseView;

    /// <summary>
    /// 进入流程：打开 View 并监听 View 派发的事件。
    /// </summary>
    public override void OnEnter(BaseFSMState lastState)
    {
        m_FunctionShowcaseView = ManagerUtility.UIMgr.OpenView<FunctionShowcaseView>(true, false);
        m_FunctionShowcaseView.AppendLog("功能展示页已打开");

        ManagerUtility.EventMgr.AddListener(FunctionShowcaseView.EVENT_CLOSE, OnCloseFunctionShowcaseView);
        ManagerUtility.EventMgr.AddListener(FunctionShowcaseView.EVENT_LOAD_SCENE, OnLoadSceneRequested);
        ManagerUtility.EventMgr.AddListener<bool>(FunctionShowcaseView.EVENT_AUTO_LOAD_CHANGED, OnAutoLoadChanged);
    }

    /// <summary>
    /// 离开流程：解除事件监听并关闭 View。
    /// </summary>
    public override void OnLeave(BaseFSMState nextState)
    {
        ManagerUtility.EventMgr.RemoveListener(FunctionShowcaseView.EVENT_CLOSE, OnCloseFunctionShowcaseView);
        ManagerUtility.EventMgr.RemoveListener(FunctionShowcaseView.EVENT_LOAD_SCENE, OnLoadSceneRequested);
        ManagerUtility.EventMgr.RemoveListener<bool>(FunctionShowcaseView.EVENT_AUTO_LOAD_CHANGED, OnAutoLoadChanged);

        ManagerUtility.UIMgr.CloseView<FunctionShowcaseView>();
        m_FunctionShowcaseView = null;
    }

    /// <summary>
    /// 响应关闭事件：关闭 View。
    /// </summary>
    private void OnCloseFunctionShowcaseView()
    {
        m_FunctionShowcaseView.AppendLog("关闭界面");
        ManagerUtility.UIMgr.CloseView<FunctionShowcaseView>();
    }

    /// <summary>
    /// 响应加载场景事件：执行业务并回写 UI。
    /// </summary>
    private void OnLoadSceneRequested()
    {
        m_FunctionShowcaseView.AppendLog("开始加载场景...");
        m_FunctionShowcaseView.SetActionsInteractable(false);

        // 在这里调用 MainMgr/AssetsMgr 做业务
        // 完成后再把 UI 恢复
        m_FunctionShowcaseView.SetActionsInteractable(true);
        m_FunctionShowcaseView.AppendLog("场景加载完成");
    }

    /// <summary>
    /// 响应 Toggle 变化：根据开关状态调整业务。
    /// </summary>
    private void OnAutoLoadChanged(bool isOn)
    {
        m_FunctionShowcaseView.AppendLog("Toggle 变化: " + isOn);

        // 在这里根据 isOn 调整业务逻辑（例如：是否自动加载、是否自动刷新等）
    }
}
```

## 注意事项

- 不要在 View 里直接调用业务管理器做复杂逻辑：只派发事件、只更新 UI
- View 如果每次都会销毁（Destroy），不移除监听通常不会造成泄漏，可以直接不移除
- Procedure 侧必须在 `OnLeave` 做 `RemoveListener`，避免流程切换后仍响应旧事件
