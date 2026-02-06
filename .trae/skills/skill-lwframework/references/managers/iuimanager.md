# IUIManager UI 管理器

## 概述

IUIManager 是 LWFramework 的 UI 系统核心接口，提供 UIView 的打开、关闭、回退、预加载和风格切换等功能。

- **接口位置**: `Assets/LWFramework/RunTime/Core/InterfaceManager/IUIManager.cs`
- **默认实现**: `Assets/LWFramework/RunTime/UI/UIManager.cs`
- **访问入口**: `ManagerUtility.UIMgr`

---

## 核心属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `IUIUtility` | `IUIUtility` | UI 工具类 |
| `UICanvas` | `Transform` | UI 画布根节点 |
| `UICamera` | `Camera` | UI 相机 |

---

## API 参考

### 查询视图

```csharp
/// <summary>
/// 获取指定类型的视图实例
/// </summary>
T GetView<T>() where T : BaseUIView;

/// <summary>
/// 通过视图类型名获取视图实例
/// </summary>
BaseUIView GetView(string viewType);

/// <summary>
/// 获取所有已打开的视图
/// </summary>
List<BaseUIView> GetAllView();
```

### 打开视图

```csharp
/// <summary>
/// 同步打开指定类型的视图
/// </summary>
T OpenView<T>(object data = null, bool isLastSibling = false, bool enterStack = false) where T : BaseUIView;

/// <summary>
/// 通过视图类型名同步打开视图
/// </summary>
BaseUIView OpenView(string viewType, object data = null, GameObject uiGameObject = null, bool isLastSibling = false, bool enterStack = false);

/// <summary>
/// 异步打开指定类型的视图
/// </summary>
UniTask<T> OpenViewAsync<T>(object data = null, bool isLastSibling = false, bool enterStack = false) where T : BaseUIView;
```

### 关闭视图

```csharp
/// <summary>
/// 关闭指定类型的视图
/// </summary>
void CloseView<T>(bool enterStack = false) where T : BaseUIView;

/// <summary>
/// 关闭指定视图实例
/// </summary>
void CloseView(BaseUIView view, bool enterStack = false);

/// <summary>
/// 关闭除指定类型外的所有视图
/// </summary>
void CloseOtherView<T>() where T : BaseUIView;

/// <summary>
/// 关闭所有视图
/// </summary>
void CloseAllView();
```

### 回退视图

```csharp
/// <summary>
/// 回退到上一个视图（从栈中弹出）
/// </summary>
BaseUIView BackView(bool isLastSibling = false);

/// <summary>
/// 连续回退两次
/// </summary>
BaseUIView BackTwiceView(bool isLastSibling = false);

/// <summary>
/// 回退到最后一个视图
/// </summary>
BaseUIView BackUntilLastView(bool isLastSibling = false);
```

### 预加载

```csharp
/// <summary>
/// 异步预加载指定路径的视图
/// </summary>
UniTask PreloadViewAsync(string prefabPath);

/// <summary>
/// 异步预加载指定类型的视图
/// </summary>
UniTask PreloadViewAsync<T>() where T : BaseUIView;

/// <summary>
/// 预加载默认 UI
/// </summary>
void PreLoadDefaultUI();
```

### 清空视图

```csharp
/// <summary>
/// 清空除指定类型外的所有视图
/// </summary>
void ClearOtherView<T>() where T : BaseUIView;

/// <summary>
/// 清空指定类型的视图
/// </summary>
void ClearView<T>() where T : BaseUIView;

/// <summary>
/// 清空指定视图实例
/// </summary>
void ClearView(BaseUIView view);

/// <summary>
/// 清空所有视图
/// </summary>
void ClearAllView();
```

### 风格设置

```csharp
/// <summary>
/// 设置 UI 风格（影响 UIViewDataAttribute 路径格式化）
/// </summary>
void SetStyle(string style);

/// <summary>
/// 获取当前 UI 风格
/// </summary>
string GetStyle();
```

---

## 使用示例

### 打开和关闭视图

```csharp
using LWCore;
using LWUI;

public class UIManagerExamples
{
    /// <summary>
    /// 打开/关闭 View（同步）
    /// </summary>
    public void OpenAndClose<T>() where T : BaseUIView
    {
        T view = ManagerUtility.UIMgr.OpenView<T>(isLastSibling: true, enterStack: true);
        ManagerUtility.UIMgr.CloseView<T>(enterStack: true);
    }

    /// <summary>
    /// 打开/关闭 View 并传递数据（同步）
    /// </summary>
    public void OpenAndCloseWithData<T>(object data) where T : BaseUIView
    {
        T view = ManagerUtility.UIMgr.OpenView<T>(data: data, isLastSibling: true, enterStack: true);
        ManagerUtility.UIMgr.CloseView<T>(enterStack: true);
    }

    /// <summary>
    /// 异步打开 View（依赖资源异步实例化）
    /// </summary>
    public async UniTask<T> OpenAsync<T>() where T : BaseUIView
    {
        T view = await ManagerUtility.UIMgr.OpenViewAsync<T>(isLastSibling: true, enterStack: true);
        return view;
    }
}
```

### 视图回退

```csharp
/// <summary>
/// 回退：从栈中弹出并返回上一界面
/// </summary>
public BaseUIView Back()
{
    BaseUIView view = ManagerUtility.UIMgr.BackView(isLastSibling: true);
    return view;
}

/// <summary>
/// 关闭除指定 View 以外的所有界面
/// </summary>
public void CloseOther<T>() where T : BaseUIView
{
    ManagerUtility.UIMgr.CloseOtherView<T>();
}
```

### 预加载视图

```csharp
/// <summary>
/// 预加载指定路径 UI（后续 OpenView 更快）
/// </summary>
public async UniTask PreloadByPathAsync(string prefabPath)
{
    await ManagerUtility.UIMgr.PreloadViewAsync(prefabPath);
}

/// <summary>
/// 预加载指定 View 类型 UI
/// </summary>
public async UniTask PreloadByTypeAsync<T>() where T : BaseUIView
{
    await ManagerUtility.UIMgr.PreloadViewAsync<T>();
}
```

### 切换 UI 风格

```csharp
/// <summary>
/// 切换 UI 风格（影响 UIViewDataAttribute 路径格式化）
/// </summary>
public void SetStyle(string style)
{
    ManagerUtility.UIMgr.SetStyle(style);
}
```

### 在 Procedure 中管理视图

```csharp
using LWFMS;
using LWCore;
using UnityEngine;

[FSMTypeAttribute("Procedure", false)]
public class TestProcedure : BaseFSMState
{
    private FunctionShowcaseView m_View;

    public override void OnEnter(BaseFSMState lastState)
    {
        // 打开视图
        m_View = ManagerUtility.UIMgr.OpenView<FunctionShowcaseView>(true, false);
    }

    public override void OnLeave(BaseFSMState nextState)
    {
        // 关闭视图
        ManagerUtility.UIMgr.CloseView<FunctionShowcaseView>();
        m_View = null;
    }
}
```

---

## UIViewDataAttribute 说明

UIView 通过 `UIViewDataAttribute` 特性配置资源路径和层级：

```csharp
[UIViewData("Assets/0Res/Prefabs/UI/TestView.prefab", (int)FindType.Name, "LWFramework/Canvas/Normal")]
public class TestView : BaseUIView
{
    // 视图实现
}
```

### 参数说明

| 参数 | 说明 |
|------|------|
| `assetPath` | 预制体资源路径 |
| `findType` | 查找类型（Name/Tag/Layer等） |
| `parentPath` | 父节点路径 |

---

## UI 多条目/多元素：View + Item + GameObjectPool

适用于需要动态生成多个 UI 元素的场景（如列表、背包、任务列表等）。

### 实现模式

```csharp
using LWUI;
using LWCore;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

/// <summary>
/// 视图：通过 OpenView(data) 接收列表，使用对象池批量生成条目
/// </summary>
[UIViewData("Assets/0Res/Prefabs/UI/StepView.prefab", (int)FindType.Name, "LWFramework/Canvas/Normal")]
public class StepView : BaseUIView
{
    internal static readonly string EVENT_CLOSE = "StepView_Close";

    [UIElement("PnlLeft/PnlStepList/Viewport/Content/PnlStepItem")]
    private Transform m_PnlStepItem;
    [UIElement("BtnPrev")]
    private Button m_BtnPrev;
    [UIElement("BtnNext")]
    private Button m_BtnNext;
    [UIElement("BtnBack")]
    private Button m_BtnBack;

    private GameObjectPool<PnlStepItem> m_PnlStepItemPool;
    private List<PnlStepItem> m_ActiveItems = new List<PnlStepItem>();

    /// <summary>
    /// 创建视图：绑定按钮事件并初始化对象池
    /// </summary>
    public override void CreateView(GameObject gameObject)
    {
        base.CreateView(gameObject);

        m_BtnPrev.onClick.AddListener(() => { });
        m_BtnNext.onClick.AddListener(() => { });
        m_BtnBack.onClick.AddListener(() => { ManagerUtility.EventMgr.DispatchEvent(EVENT_CLOSE); });

        // 初始化对象池
        m_PnlStepItemPool = new GameObjectPool<PnlStepItem>(
            poolMaxSize: 5, 
            template: m_PnlStepItem.gameObject);
    }

    /// <summary>
    /// 打开视图：用 List<string> 作为数据源，批量借出条目并绑定回调
    /// </summary>
    public override void OpenView(object data = null)
    {
        base.OpenView(data);

        // 先归还之前的条目
        ClearItems();

        List<string> stepList = data as List<string>;
        if (stepList == null || stepList.Count == 0)
        {
            return;
        }

        for (int i = 0; i < stepList.Count; i++)
        {
            int index = i;
            PnlStepItem pnlStepItem = m_PnlStepItemPool.Spawn();
            pnlStepItem.StepIndex = (i + 1).ToString();
            pnlStepItem.StepTitle = stepList[i];
            pnlStepItem.OnClickStep = () => { Debug.Log($"点击了第{index + 1}个步骤"); };
            
            m_ActiveItems.Add(pnlStepItem);
        }
    }

    /// <summary>
    /// 关闭视图：归还所有条目
    /// </summary>
    public override void CloseView()
    {
        ClearItems();
        base.CloseView();
    }

    /// <summary>
    /// 归还所有激活的条目
    /// </summary>
    private void ClearItems()
    {
        foreach (var item in m_ActiveItems)
        {
            m_PnlStepItemPool.Unspawn(item);
        }
        m_ActiveItems.Clear();
    }
}

/// <summary>
/// 条目：继承 BaseUIItem，暴露 Action 给 View 绑定点击
/// </summary>
public class PnlStepItem : BaseUIItem
{
    [UIElement("TxtStepIndex")]
    private Text m_TxtStepIndex;
    [UIElement("TxtStepTitle")]
    private Text m_TxtStepTitle;

    private Button m_BtnStep;

    public string StepIndex
    {
        get { return m_TxtStepIndex.text; }
        set { m_TxtStepIndex.text = value; }
    }

    public string StepTitle
    {
        get { return m_TxtStepTitle.text; }
        set { m_TxtStepTitle.text = value; }
    }

    public Action OnClickStep;

    /// <summary>
    /// 创建条目：绑定点击回调
    /// </summary>
    public override void Create(GameObject gameObject)
    {
        base.Create(gameObject);
        m_BtnStep = gameObject.GetComponent<Button>();
        m_BtnStep.onClick.AddListener(() => { OnClickStep?.Invoke(); });
    }

    /// <summary>
    /// 归还到池：按需做清理
    /// </summary>
    public override void OnUnSpawn()
    {
        base.OnUnSpawn();
        OnClickStep = null;
    }

    /// <summary>
    /// 释放条目：按需做资源释放
    /// </summary>
    public override void OnRelease()
    {
        base.OnRelease();
    }
}
```

### 关键点说明

| 组件 | 职责 |
|------|------|
| **View (StepView)** | 管理条目生命周期，持有对象池，处理数据传递 |
| **Item (PnlStepItem)** | 单个条目的表现和交互，继承 BaseUIItem |
| **GameObjectPool** | 高效复用条目对象，避免频繁创建销毁 |

### 使用流程

1. **View 创建时**: 初始化对象池，传入条目模板
2. **OpenView 时**: 接收数据，从对象池 Spawn 条目，设置数据
3. **CloseView 时**: 归还所有条目到对象池
4. **Item 实现**: 继承 BaseUIItem，实现 Create/OnUnSpawn/OnRelease

---

## 最佳实践

1. **使用泛型方法**: 优先使用泛型方法 `OpenView<T>` 和 `CloseView<T>`，类型安全
2. **管理视图生命周期**: 在 Procedure 的 `OnEnter` 打开视图，在 `OnLeave` 关闭视图
3. **合理使用视图栈**: 需要回退功能的视图设置 `enterStack: true`
4. **预加载优化**: 对频繁打开的视图进行预加载，减少打开时的卡顿
5. **风格切换**: 使用 `SetStyle` 实现多套 UI 风格切换（如节日主题）
6. **列表优化**: 多条目场景使用 View + Item + GameObjectPool 模式，提升性能
