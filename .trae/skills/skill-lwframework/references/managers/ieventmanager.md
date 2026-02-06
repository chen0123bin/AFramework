# IEventManager 事件管理器

## 概述

IEventManager 是 LWFramework 的事件系统核心接口，提供基于字符串事件名的多播事件机制，支持 0-4 个参数的事件监听与派发。

- **接口位置**: `Assets/LWFramework/RunTime/Core/InterfaceManager/IEventManager.cs`
- **默认实现**: `Assets/LWFramework/RunTime/Core/LWEventManager.cs`
- **访问入口**: `ManagerUtility.EventMgr`

---

## 核心特性

- **字符串事件名**: 使用字符串标识事件，简单直观
- **多参数支持**: 支持 0-4 个参数的事件监听与派发
- **类型安全**: 泛型约束确保参数类型匹配
- **生命周期管理**: 支持手动移除监听和清空所有事件

---

## API 参考

### 添加监听

```csharp
/// <summary>
/// 添加无参事件监听
/// </summary>
void AddListener(string eventName, Action handler);

/// <summary>
/// 添加单参事件监听
/// </summary>
void AddListener<T>(string eventName, Action<T> handler);

/// <summary>
/// 添加双参事件监听
/// </summary>
void AddListener<T1, T2>(string eventName, Action<T1, T2> handler);

/// <summary>
/// 添加三参事件监听
/// </summary>
void AddListener<T1, T2, T3>(string eventName, Action<T1, T2, T3> handler);

/// <summary>
/// 添加四参事件监听
/// </summary>
void AddListener<T1, T2, T3, T4>(string eventName, Action<T1, T2, T3, T4> handler);
```

### 移除监听

```csharp
/// <summary>
/// 移除无参事件监听
/// </summary>
void RemoveListener(string eventName, Action handler);

/// <summary>
/// 移除单参事件监听
/// </summary>
void RemoveListener<T>(string eventName, Action<T> handler);

/// <summary>
/// 移除双参事件监听
/// </summary>
void RemoveListener<T1, T2>(string eventName, Action<T1, T2> handler);

/// <summary>
/// 移除三参事件监听
/// </summary>
void RemoveListener<T1, T2, T3>(string eventName, Action<T1, T2, T3> handler);

/// <summary>
/// 移除四参事件监听
/// </summary>
void RemoveListener<T1, T2, T3, T4>(string eventName, Action<T1, T2, T3, T4> handler);
```

### 派发事件

```csharp
/// <summary>
/// 派发无参事件
/// </summary>
void DispatchEvent(string eventName);

/// <summary>
/// 派发单参事件
/// </summary>
void DispatchEvent<T>(string eventName, T arg);

/// <summary>
/// 派发双参事件
/// </summary>
void DispatchEvent<T1, T2>(string eventName, T1 arg1, T2 arg2);

/// <summary>
/// 派发三参事件
/// </summary>
void DispatchEvent<T1, T2, T3>(string eventName, T1 arg1, T2 arg2, T3 arg3);

/// <summary>
/// 派发四参事件
/// </summary>
void DispatchEvent<T1, T2, T3, T4>(string eventName, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
```

### 清理

```csharp
/// <summary>
/// 清空所有事件监听
/// </summary>
void Clear();
```

---

## 使用示例

### 基础事件监听与派发

```csharp
using LWCore;

public class EventExamples
{
    private const string EVENT_LOGIN = "Login";
    private const string EVENT_DAMAGE = "Damage";

    /// <summary>
    /// 注册事件监听（0/1/2 参数）
    /// </summary>
    public void Register()
    {
        // 无参事件
        ManagerUtility.EventMgr.AddListener(EVENT_LOGIN, OnLogin);
        
        // 单参事件
        ManagerUtility.EventMgr.AddListener<int>(EVENT_DAMAGE, OnDamage);
        
        // 双参事件
        ManagerUtility.EventMgr.AddListener<int, int>("HpChanged", OnHpChanged);
    }

    /// <summary>
    /// 派发事件（0/1/2 参数）
    /// </summary>
    public void Dispatch()
    {
        // 派发无参事件
        ManagerUtility.EventMgr.DispatchEvent(EVENT_LOGIN);
        
        // 派发单参事件
        ManagerUtility.EventMgr.DispatchEvent<int>(EVENT_DAMAGE, 10);
        
        // 派发双参事件
        ManagerUtility.EventMgr.DispatchEvent<int, int>("HpChanged", 100, 90);
    }

    /// <summary>
    /// 移除监听与清空事件中心（注意生命周期，避免重复注册）
    /// </summary>
    public void UnregisterAndClear()
    {
        ManagerUtility.EventMgr.RemoveListener(EVENT_LOGIN, OnLogin);
        ManagerUtility.EventMgr.RemoveListener<int>(EVENT_DAMAGE, OnDamage);
        ManagerUtility.EventMgr.Clear();
    }

    /// <summary>
    /// 无参事件回调
    /// </summary>
    private void OnLogin()
    {
        Debug.Log("登录成功");
    }

    /// <summary>
    /// 单参事件回调
    /// </summary>
    private void OnDamage(int damage)
    {
        Debug.Log($"受到伤害: {damage}");
    }

    /// <summary>
    /// 双参事件回调
    /// </summary>
    private void OnHpChanged(int oldHp, int newHp)
    {
        Debug.Log($"血量变化: {oldHp} -> {newHp}");
    }
}
```

### 在 Procedure 中使用事件

```csharp
using LWFMS;
using LWCore;
using UnityEngine;

[FSMTypeAttribute("Procedure", false)]
public class TestProcedure : BaseFSMState
{
    public override void OnEnter(BaseFSMState lastState)
    {
        // 进入状态时注册事件监听
        ManagerUtility.EventMgr.AddListener<int>("TestEvent", OnTestEvent);
    }

    public override void OnLeave(BaseFSMState nextState)
    {
        // 离开状态时移除事件监听，避免内存泄漏
        ManagerUtility.EventMgr.RemoveListener<int>("TestEvent", OnTestEvent);
    }

    private void OnTestEvent(int value)
    {
        Debug.Log($"收到测试事件，值: {value}");
    }
}
```

### 在 View 中派发事件

```csharp
using LWCore;
using LWUI;
using UnityEngine;
using UnityEngine.UI;

[UIViewData("Assets/0Res/Prefabs/UI/TestView.prefab", (int)FindType.Name, "LWFramework/Canvas/Normal")]
public class TestView : BaseUIView
{
    public const string EVENT_BUTTON_CLICK = "TestView_ButtonClick";

    [UIElement("BtnTest")]
    private Button m_BtnTest;

    public override void CreateView(GameObject gameObject)
    {
        base.CreateView(gameObject);
        
        // 按钮点击时派发事件，不直接处理业务逻辑
        m_BtnTest.onClick.AddListener(() => 
        { 
            ManagerUtility.EventMgr.DispatchEvent(EVENT_BUTTON_CLICK); 
        });
    }
}
```

### 事件名常量管理（推荐）

```csharp
/// <summary>
/// 集中管理所有事件名称，避免硬编码字符串
/// </summary>
public static class EventNames
{
    // UI 事件
    public const string UI_OPEN_MAIN = "UI_OpenMain";
    public const string UI_CLOSE_MAIN = "UI_CloseMain";
    
    // 玩家事件
    public const string PLAYER_LEVEL_UP = "Player_LevelUp";
    public const string PLAYER_HP_CHANGED = "Player_HpChanged";
    public const string PLAYER_COINS_CHANGED = "Player_CoinsChanged";
    
    // 游戏事件
    public const string GAME_START = "Game_Start";
    public const string GAME_PAUSE = "Game_Pause";
    public const string GAME_RESUME = "Game_Resume";
    public const string GAME_OVER = "Game_Over";
    
    // 场景事件
    public const string SCENE_LOAD_START = "Scene_LoadStart";
    public const string SCENE_LOAD_PROGRESS = "Scene_LoadProgress";
    public const string SCENE_LOAD_COMPLETE = "Scene_LoadComplete";
}
```

---

## 最佳实践

1. **事件名管理**: 使用常量类集中管理事件名，避免硬编码字符串
2. **生命周期管理**: 在 `OnEnter` 注册监听，在 `OnLeave` 移除监听
3. **避免重复注册**: 注册前确保先移除，或使用标志位控制
4. **参数类型一致**: 监听和派发的参数类型必须严格匹配
5. **避免循环引用**: 事件回调中避免直接引用发送者，保持解耦
