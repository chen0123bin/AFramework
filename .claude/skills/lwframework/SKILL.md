---
name: lwframework
description: LWFramework 运行时核心接口速查与启动辅助。适用于：LWFramework 启动注册/初始化、ManagerUtility.*Mgr 返回 default 或告警排查、Procedure 流程状态机（IFSMManager）启动与切换、查询 IAssetsManager/IAudioManager /IEventManager/IUIManager/IHotfixManager/IFSMManager/IManager 的职责与调用方式。业务代码优先通过 ManagerUtility.AssetsMgr/EventMgr/UIMgr/HotfixMgr/FSMMgr/AudioMgr 调用能力，并对照 Assets/LWFramework/RunTime/Core/InterfaceManager 下接口核对签名与生命周期。
---

# LWFramework 运行时接口速查（InterfaceManager）

## 使用约定（生成代码优先级）

- 业务代码优先使用 ManagerUtility.*Mgr 访问接口，不直接 new 管理器
- 启动代码只负责注册管理器与初始化依赖，业务模块不做注册
- 当你需要一个能力时：先找 ManagerUtility 对应 *Mgr，再回到接口定义确认方法签名

## 快速入口

- 接口目录：Assets/LWFramework/RunTime/Core/InterfaceManager/
- 访问入口：Assets/LWFramework/RunTime/Core/ManagerUtility.cs
- 管理器容器：Assets/LWFramework/RunTime/Core/MainManager.cs
- 音频模块：Assets/LWFramework/RunTime/Audio/（AudioManager/AudioChannel/Audio3DSettings）
- 对象池模块：Assets/LWFramework/RunTime/Core/ObjectPool/（GameObjectPool/IPoolGameObject/PoolGameObject）

## 快速导航（按场景）

- 启动注册与初始化：references/startup.md
- 常用调用示例（资源/事件/UI/音频/对象池/热更/FSM/自定义管理器）：references/examples.md
- 接口清单与默认实现：references/interfaces.md
- View→事件→Procedure→View（UI 解耦推荐流程）：references/ui-view-procedure-event-flow.md

## 核心结论（启动阶段必读）

- ManagerUtility.*Mgr 实际调用的是 MainManager.Instance.GetManager<T>()；如果你没有先把对应管理器 AddManager 进去，GetManager 会返回 default 并输出告警。
- MainManager 以 typeof(T).ToString() 作为 Key 保存管理器实例：添加与获取必须严格使用同一个泛型接口类型。
- IManager 只有 Init/Update；具体系统（资源/事件/UI/热更）的初始化能力全部体现在各自接口里。

## 启动排查清单（最常见问题）

- 访问 ManagerUtility.AssetsMgr/EventMgr/UIMgr/HotfixMgr 之前，先确认已经对 MainManager 调用 AddManager 注册了对应接口类型的实例。
- 访问 ManagerUtility.FSMMgr 或调用 StartProcedure/ClearManager 之前，先确认已注册 IFSMManager。
- 如果 GetManager<T>() 返回 default：检查 AddManager 的 Key 是否是 typeof(T).ToString()，以及 Add/ Get 使用的泛型类型是否一致。
- 如果 UI 打开/资源加载异常：优先确认 IAssetsManager.IsInitialized 是否为 true，以及资源系统 InitializeAsync 是否已完成。

## 搜索关键词（定位问题更快）

- GetManager<
- AddManager(
- typeof(IAssetsManager).ToString()
- ManagerUtility.AssetsMgr
- ManagerUtility.EventMgr
- ManagerUtility.UIMgr
- ManagerUtility.HotfixMgr
- ManagerUtility.FSMMgr
- ManagerUtility.AudioMgr
- StartProcedure()
- FSMTypeAttribute
- SwitchState(
- IAudioManager
- AudioManager
- AudioChannel
- Audio3DSettings
- Audio3DSettings.Default3D
- GameObjectPool<
- Spawn(
- Unspawn(
- IPoolGameObject
- IsInPool(

## 生成代码时的落地规则

- 访问资源/事件/UI/热更：优先写成 ManagerUtility.AssetsMgr/EventMgr/UIMgr/HotfixMgr.xxx
- 只有启动/框架组装代码才出现 MainManager.AddManager；业务功能代码不注册管理器
- 当需要补齐能力：先回到 InterfaceManager 的接口定义核对签名，再调用 *Mgr
