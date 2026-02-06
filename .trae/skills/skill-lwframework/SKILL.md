---
name: skill-lwframework
description: This skill should be used when the user asks about "IAssetsManager/IEventManager/IUIManager/IHotfixManager/IFSMManager/IAudioManager usage", "how to load assets/instantiate prefabs/load scenes", "how to listen/dispatch events", "how to open/close/preload UI", "how to load hotfix DLL", "how to switch Procedure states", "how to play 2D/3D audio", "GameObjectPool usage", or when encountering "ManagerUtility.*Mgr is null" issues.
---

# LWFramework 管理器用法速查

## 核心访问入口

所有管理器通过 `ManagerUtility.*Mgr` 访问：

| 管理器 | 访问入口 | 用途 |
|--------|----------|------|
| 资源系统 | `ManagerUtility.AssetsMgr` | 资源加载、实例化、场景、下载更新 |
| 事件系统 | `ManagerUtility.EventMgr` | 事件监听、派发、清理 |
| UI 系统 | `ManagerUtility.UIMgr` | UI 打开、关闭、回退、预加载 |
| 热更系统 | `ManagerUtility.HotfixMgr` | 热更 DLL 加载、反射调用 |
| 状态机 | `ManagerUtility.FSMMgr` | Procedure 流程状态机 |
| 音频系统 | `ManagerUtility.AudioMgr` | 2D/3D 音效播放、音量控制 |

---

## 快速开始

### 1. 资源加载

```csharp
// 同步加载
Sprite sprite = ManagerUtility.AssetsMgr.LoadAsset<Sprite>("Assets/UI/icon.png");

// 异步加载
Texture2D tex = await ManagerUtility.AssetsMgr.LoadAssetAsync<Texture2D>("Assets/Textures/bg.png", cancellationToken);

// 实例化预制体
GameObject go = await ManagerUtility.AssetsMgr.InstantiateAsync("Assets/Prefabs/Enemy.prefab", parent, cancellationToken);
```

### 2. 事件监听与派发

```csharp
// 注册监听
ManagerUtility.EventMgr.AddListener<int>("Damage", OnDamage);

// 派发事件
ManagerUtility.EventMgr.DispatchEvent<int>("Damage", 100);

// 移除监听
ManagerUtility.EventMgr.RemoveListener<int>("Damage", OnDamage);
```

### 3. UI 打开与关闭

```csharp
// 打开视图
var view = ManagerUtility.UIMgr.OpenView<MainView>(data: playerData, isLastSibling: true, enterStack: true);

// 关闭视图
ManagerUtility.UIMgr.CloseView<MainView>();

// 回退到上一视图
ManagerUtility.UIMgr.BackView();
```

### 4. Procedure 状态切换

```csharp
[FSMTypeAttribute("Procedure", false)]
public class MenuProcedure : BaseFSMState
{
    public override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 切换到游戏流程
            ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<GameProcedure>();
        }
    }
}
```

---

## 详细文档

### 管理器使用指南

每个管理器都有独立的详细文档，包含完整 API 说明和使用示例：

- **[IAssetsManager](references/managers/iassetsmanager.md)** - 资源加载、实例化、场景、批量加载、下载更新
- **[IEventManager](references/managers/ieventmanager.md)** - 事件监听、派发、清理（支持 0-4 参数）
- **[IUIManager](references/managers/iuimanager.md)** - UI 打开、关闭、回退、预加载、风格切换
- **[IHotfixManager](references/managers/ihotfixmanager.md)** - 热更 DLL 加载、反射获取类型、实例化、方法调用
- **[IFSMManager](references/managers/ifsmmanager.md)** - Procedure 流程状态机、状态切换
- **[IAudioManager](references/managers/iaudiomanager.md)** - 2D/3D 音效播放、通道控制、全局音量
- **[GameObjectPool](references/managers/gameobjectpool.md)** - 对象池创建、借出、归还、清理

### 其他参考文档

- **[启动注册与初始化](references/startup.md)** - 框架启动、管理器注册、初始化流程
- **[UI 解耦流程](references/ui-view-procedure-event-flow.md)** - View→事件→Procedure→View 推荐模式
- **[接口清单](references/interfaces.md)** - 完整接口定义与默认实现位置

---

## 工程结构

```
Assets/LWFramework/
├── RunTime/
│   ├── Core/
│   │   ├── InterfaceManager/     # 接口定义
│   │   │   ├── IAssetsManager.cs
│   │   │   ├── IEventManager.cs
│   │   │   ├── IUIManager.cs
│   │   │   ├── IHotfixManager.cs
│   │   │   ├── IFSMManager.cs
│   │   │   ├── IAudioManager.cs
│   │   │   └── IManager.cs
│   │   ├── ManagerUtility.cs     # 管理器访问入口
│   │   ├── MainManager.cs        # 管理器容器
│   │   └── ObjectPool/           # 对象池
│   │       ├── GameObjectPool.cs
│   │       ├── IPoolGameObject.cs
│   │       └── PoolGameObject.cs
│   ├── Assets/                   # 资源系统实现
│   ├── UI/                       # UI 系统实现
│   ├── HotFix/                   # 热更系统实现
│   ├── FMS/                      # 状态机实现
│   └── Audio/                    # 音频系统实现
│       ├── AudioManager.cs
│       ├── AudioChannel.cs
│       └── Audio3DSettings.cs
```

---

## 常见问题

### ManagerUtility.*Mgr 为 null

**原因**: 未注册对应管理器

**解决**: 在启动代码中注册

```csharp
ManagerUtility.MainMgr.AddManager(typeof(IAssetsManager).ToString(), new LWAssetsManager());
ManagerUtility.MainMgr.AddManager(typeof(IEventManager).ToString(), new LWEventManager());
ManagerUtility.MainMgr.AddManager(typeof(IUIManager).ToString(), new UIManager());
ManagerUtility.MainMgr.AddManager(typeof(IHotfixManager).ToString(), new HotFixRefManager());
ManagerUtility.MainMgr.AddManager(typeof(IFSMManager).ToString(), new FSMManager());
```

### 资源加载失败

**排查**: 
1. 检查 `IAssetsManager.IsInitialized` 是否为 true
2. 确认资源路径正确
3. 确认资源已打包或在 Editor 模式下

### UI 打开异常

**排查**:
1. 确认 UIView 有 `[UIViewData]` 特性
2. 确认预制体路径正确
3. 确认资源系统已初始化

### 事件监听不触发

**排查**:
1. 检查事件名是否一致（区分大小写）
2. 检查参数类型是否匹配
3. 确认在正确的生命周期注册（如 Procedure 的 OnEnter）

---

## 代码生成规则

生成代码时遵循以下优先级：

1. **优先使用 ManagerUtility.*Mgr**: 业务代码使用 `ManagerUtility.AssetsMgr/EventMgr/UIMgr` 等
2. **启动代码才注册**: 只有启动/框架组装代码出现 `MainManager.AddManager`
3. **先查文档再调用**: 需要某个能力时，先查看对应管理器的详细文档
4. **核对方法签名**: 调用前核对接口定义，确保参数正确

---

## 搜索关键词

定位问题时使用这些关键词搜索代码：

- `GetManager<` / `AddManager(` - 管理器注册与获取
- `ManagerUtility.AssetsMgr` / `ManagerUtility.EventMgr` / `ManagerUtility.UIMgr` - 管理器使用
- `StartProcedure()` / `SwitchState<` - 流程状态机
- `FSMTypeAttribute` - 状态标记
- `UIViewData` - UI 配置
- `GameObjectPool<` / `Spawn(` / `Unspawn(` - 对象池
