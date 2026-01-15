# 接口清单与默认实现

## IManager

- 位置：Assets/LWFramework/RunTime/Core/InterfaceManager/IManager.cs
- 作用：统一的管理器生命周期
- 方法：Init()、Update()

## IAssetsManager（资源系统）

- 位置：Assets/LWFramework/RunTime/Core/InterfaceManager/IAssetsManager.cs
- 作用：抽象 LWAssets 的资源加载、下载、缓存、版本、场景管理
- 关键属性：IsInitialized、CurrentPlayMode、Loader、Downloader、Cache、Preloader、Version
- 初始化：InitializeAsync、WarmupShadersAsync、Destroy
- 清单：LoadManifestAsync
- 同步：LoadAsset、LoadRawFile、LoadRawFileText、Instantiate
- 异步：InstantiateAsync、LoadAssetAsync、LoadRawFileAsync、LoadRawFileTextAsync、LoadSceneAsync、LoadAssetsAsync
- 资源管理：Release(Object / path)、UnloadUnusedAssetsAsync、ForceUnloadAll
- 下载：GetDownloadSizeAsync、DownloadAsync

## IEventManager（事件系统）

- 位置：Assets/LWFramework/RunTime/Core/InterfaceManager/IEventManager.cs
- 作用：字符串事件名驱动的事件中心
- 监听：AddListener（0~4 参数）
- 移除：RemoveListener（0~4 参数）
- 派发：DispatchEvent（0~4 参数）
- 清理：Clear

## IUIManager（UI 系统）

- 位置：Assets/LWFramework/RunTime/Core/InterfaceManager/IUIManager.cs
- 作用：UIView 管理、打开/关闭/回退、预加载、UI 风格切换
- 关键属性：IUIUtility、UICanvas、UICamera
- 查询：GetView<T>()、GetView(string)、GetAllView()
- 打开：OpenView<T>()、OpenView(string)、OpenViewAsync<T>()
- 回退：BackView、BackTwiceView、BackUntilLastView
- 预加载：PreloadViewAsync(string / T)、PreLoadDefaultUI
- 关闭：CloseOtherView、CloseView（多形态）、CloseAllView
- 清空：ClearOtherView、ClearView（多形态）、ClearAllView
- 风格：SetStyle、GetStyle

## IHotfixManager（热更域/反射域）

- 位置：Assets/LWFramework/RunTime/Core/InterfaceManager/IHotfixManager.cs
- 作用：加载热更 DLL、反射获取类型/特性、实例化与方法调用
- 关键属性：Loaded
- 加载：LoadScriptAsync
- 类型：GetTypeByName
- 实例化：Instantiate<T>
- 调用：Invoke
- 生命周期：Destroy
- 特性系统：AddHotfixTypeAttr、GetAttrTypeDataList<T>、FindAttr<T>

## IFSMManager（FSM/Procedure 流程状态机）

- 位置：Assets/LWFramework/RunTime/Core/InterfaceManager/IFSMManager.cs
- 作用：统一管理 FSMStateMachine 的注册、查询与流程状态机（Procedure）访问
- Procedure：MainManager.StartProcedure 会创建名为 nameof(FSMName.Procedure) 的 FSMStateMachine
- 状态类：继承 BaseFSMState，并通过 [FSMTypeAttribute("Procedure", isFirst)] 参与流程组装
- 关键方法：GetFSMByName、GetFSMProcedure、InitFSMManager、RegisterFSM、UnRegisterFSM、IsExistFSM、GetFsmClassDataByName

## 默认实现与关联类（基于现有工程代码）

- IAssetsManager：LWAssetsManager（Assets/LWFramework/RunTime/Assets/Core/LWAssetsManager.cs）
- IEventManager：LWEventManager（Assets/LWFramework/RunTime/Core/LWEventManager.cs）
- IUIManager：UIManager（Assets/LWFramework/RunTime/UI/UIManager.cs）
- IHotfixManager：HotFixBaseManager（Assets/LWFramework/RunTime/HotFix/HotFixBaseManager.cs，抽象基类）
- IFSMManager：FSMManager（Assets/LWFramework/RunTime/FMS/FSMManager.cs）
