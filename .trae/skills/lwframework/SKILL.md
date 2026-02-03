---
name: lwframework
description: LWFramework 管理器用法速查（ManagerUtility.*Mgr）。适用于：资源加载/实例化/场景/下载更新（IAssetsManager）、事件监听与派发（IEventManager）、UI 打开关闭与预加载（IUIManager）、热更加载与反射调用（IHotfixManager）、Procedure/FSM 切换（IFSMManager）、音频播放控制（IAudioManager）、对象池（GameObjectPool）等。重点用法集中在 references/examples.md。
---

# LWFramework 管理器用法速查（ManagerUtility.*Mgr）

## 触发场景

- 询问 “IAssetsManager/IEventManager/IUIManager/IHotfixManager/IFSMManager/IAudioManager 怎么用”
- 询问 “怎么加载资源/实例化预制体/加载场景/批量加载/下载更新/释放资源”
- 询问 “怎么监听事件/移除事件/派发事件（0~多参）”
- 询问 “怎么打开/关闭/回退 UI，怎么预加载 UI，怎么切换 UI 风格”
- 询问 “怎么加载热更 DLL、按类型名取 Type、Instantiate/Invoke、按特性取类型”
- 询问 “Procedure 怎么切换状态，怎么获取流程状态机并 SwitchState”
- 询问 “怎么播放 2D/3D 音效、控制通道、设置全局音量”
- 询问 “GameObjectPool 怎么创建/借出/归还/清理”
- 排查 “ManagerUtility.*Mgr 为 null 或返回 default / 出现 GetManager<T>() 告警”

## 快速导航

- 常用调用示例（主入口）：[examples.md](references/examples.md)
- 接口清单与默认实现（核对签名/默认实现位置）：[interfaces.md](references/interfaces.md)
- 启动注册与初始化（框架组装，低频）：[startup.md](references/startup.md)
- View→事件→Procedure→View（UI 解耦流程）：[ui-view-procedure-event-flow.md](references/ui-view-procedure-event-flow.md)

## 用法索引（examples 内标题）

- IFSMManager：Procedure 流程状态机
- IAssetsManager：初始化/预热/加载/实例化/释放、原始文件读取、场景加载、批量加载、下载更新
- IEventManager：多参数监听/移除/派发/清理
- IUIManager：打开/关闭/回退/预加载/风格
- UI 多条目/多元素：View + Item + GameObjectPool
- IHotfixManager：加载热更/类型与特性/实例化与调用
- IAudioManager：播放/停止/暂停/全局音量
- GameObjectPool：创建/借出/归还/清理

## 使用约定（生成代码优先级）

- 业务代码优先使用 ManagerUtility.*Mgr 访问接口，不直接 new 管理器
- 启动代码只负责注册管理器与初始化依赖，业务模块不做注册
- 需要某个能力时：优先在 examples 里定位对应场景示例，再回到接口定义核对方法签名

## 工程入口

- 接口目录：Assets/LWFramework/RunTime/Core/InterfaceManager/
- 访问入口：Assets/LWFramework/RunTime/Core/ManagerUtility.cs
- 管理器容器：Assets/LWFramework/RunTime/Core/MainManager.cs
- 音频模块：Assets/LWFramework/RunTime/Audio/（AudioManager/AudioChannel/Audio3DSettings）
- 对象池模块：Assets/LWFramework/RunTime/Core/ObjectPool/（GameObjectPool/IPoolGameObject/PoolGameObject）

## 常见问题（取不到管理器/出现告警）

- ManagerUtility.*Mgr 实际调用的是 MainManager.Instance.GetManager<T>()；若未先将对应管理器 AddManager 注册，GetManager 会返回 default 并输出告警。
- MainManager 以 typeof(T).ToString() 作为 Key 保存管理器实例：添加与获取必须严格使用同一个泛型接口类型。
- IManager 只有 Init/Update；具体系统（资源/事件/UI/热更）的初始化能力全部体现在各自接口里。

## 排查清单（最常见问题）

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

- 访问资源/事件/UI/热更/FSM/音频：优先写成 ManagerUtility.*Mgr.xxx
- 只有启动/框架组装代码才出现 MainManager.AddManager；业务功能代码不注册管理器
- 当需要补齐能力：先回到 InterfaceManager 的接口定义核对签名，再调用 *Mgr
