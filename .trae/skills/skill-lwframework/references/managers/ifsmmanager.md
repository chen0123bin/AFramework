# IFSMManager 状态机管理器

## 概述

IFSMManager 是 LWFramework 的有限状态机（FSM）系统核心接口，提供 FSMStateMachine 的注册、查询和流程状态机（Procedure）访问等功能。

- **接口位置**: `Assets/LWFramework/RunTime/Core/InterfaceManager/IFSMManager.cs`
- **默认实现**: `Assets/LWFramework/RunTime/FMS/FSMManager.cs`
- **访问入口**: `ManagerUtility.FSMMgr`

---

## 核心概念

### Procedure（流程状态机）

- 由 `MainManager.StartProcedure()` 创建
- 状态机名称为 `nameof(FSMName.Procedure)`
- 状态类继承 `BaseFSMState`
- 通过 `[FSMTypeAttribute("Procedure", isFirst)]` 特性参与流程组装

### FSMTypeAttribute

```csharp
[FSMTypeAttribute("Procedure", true)]   // isFirst=true 表示这是第一个状态
public class StartProcedure : BaseFSMState
{
    // 状态实现
}
```

---

## API 参考

### 获取状态机

```csharp
/// <summary>
/// 通过名称获取 FSMStateMachine
/// </summary>
FSMStateMachine GetFSMByName(string fsmName);

/// <summary>
/// 获取流程状态机（Procedure）
/// </summary>
FSMStateMachine GetFSMProcedure();
```

### 注册与注销

```csharp
/// <summary>
/// 初始化 FSM 管理器
/// </summary>
void InitFSMManager();

/// <summary>
/// 注册状态机
/// </summary>
void RegisterFSM(FSMStateMachine fsm);

/// <summary>
/// 注销状态机
/// </summary>
void UnRegisterFSM(FSMStateMachine fsm);

/// <summary>
/// 检查状态机是否存在
/// </summary>
bool IsExistFSM(string fsmName);
```

### 获取状态类数据

```csharp
/// <summary>
/// 通过状态机名称获取状态类数据
/// </summary>
FsmClassData GetFsmClassDataByName(string fsmName);
```

---

## 使用示例

### 创建 Procedure 状态

```csharp
using LWFMS;
using LWCore;
using UnityEngine;

/// <summary>
/// 启动流程状态（第一个状态）
/// </summary>
[FSMTypeAttribute("Procedure", true)]
public class StartProcedure : BaseFSMState
{
    public override void OnInit()
    {
        // 状态初始化
    }

    public override void OnEnter(BaseFSMState lastState)
    {
        // 进入状态
        Debug.Log("进入启动流程");
    }

    public override void OnLeave(BaseFSMState nextState)
    {
        // 离开状态
    }

    public override void OnTermination()
    {
        // 状态终止
    }

    public override void OnUpdate()
    {
        // 每帧更新
        if (Input.GetKeyDown(KeyCode.C))
        {
            // 切换到测试流程
            ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<TestProcedure>();
        }
    }
}
```

### 普通 Procedure 状态

```csharp
using LWFMS;
using LWCore;
using UnityEngine;

/// <summary>
/// 测试流程状态
/// </summary>
[FSMTypeAttribute("Procedure", false)]
public class TestProcedure : BaseFSMState
{
    public override void OnInit()
    {
    }

    public override void OnEnter(BaseFSMState lastState)
    {
        // 进入状态时注册事件监听
        ManagerUtility.EventMgr.AddListener<int>("TestEvent", OnTestEvent);
    }

    public override void OnLeave(BaseFSMState nextState)
    {
        // 离开状态时移除事件监听
        ManagerUtility.EventMgr.RemoveListener<int>("TestEvent", OnTestEvent);
    }

    public override void OnTermination()
    {
    }

    public override void OnUpdate()
    {
    }

    private void OnTestEvent(int obj)
    {
        Debug.Log($"收到测试事件: {obj}");
    }
}
```

### 切换状态

```csharp
/// <summary>
/// 切换到指定状态
/// </summary>
public void SwitchToState<T>() where T : BaseFSMState
{
    ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<T>();
}

/// <summary>
/// 通过类型切换状态
/// </summary>
public void SwitchToState(Type stateType)
{
    ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState(stateType);
}
```

### 启动 Procedure

```csharp
using LWCore;
using UnityEngine;

public class GameStartup : MonoBehaviour
{
    private void Awake()
    {
        // 注册 FSM 管理器
        ManagerUtility.MainMgr.AddManager(typeof(IFSMManager).ToString(), new FSMManager());
    }

    private void Start()
    {
        // 启动流程（会自动创建 Procedure 状态机）
        ManagerUtility.MainMgr.StartProcedure();
    }
}
```

### 指定第一个状态

```csharp
using LWCore;
using UnityEngine;

public class GameStartup : MonoBehaviour
{
    private void Start()
    {
        // 方式1：设置 FirstFSMState（优先级高于 isFirst 标记）
        ManagerUtility.MainMgr.FirstFSMState = typeof(StartProcedure);
        
        // 方式2：从热更域获取类型
        // ManagerUtility.MainMgr.FirstFSMState = ManagerUtility.HotfixMgr.GetTypeByName("StartProcedure");
        
        // 启动流程
        ManagerUtility.MainMgr.StartProcedure();
    }
}
```

---

## BaseFSMState 生命周期

| 方法 | 调用时机 | 说明 |
|------|----------|------|
| `OnInit()` | 状态机初始化时 | 只调用一次，用于初始化状态 |
| `OnEnter()` | 进入状态时 | 每次进入都会调用 |
| `OnUpdate()` | 每帧更新 | 处于当前状态时持续调用 |
| `OnLeave()` | 离开状态时 | 切换到其他状态前调用 |
| `OnTermination()` | 状态机销毁时 | 用于清理资源 |

---

## 最佳实践

1. **事件生命周期**: 在 `OnEnter` 注册事件监听，在 `OnLeave` 移除
2. **状态切换**: 使用 `SwitchState<T>()` 进行状态切换
3. **资源管理**: 在 `OnLeave` 或 `OnTermination` 中释放资源
4. **避免重复注册**: 确保事件监听和视图打开在正确的生命周期方法中
5. **热更支持**: 可以从热更域获取状态类型，支持热更流程
