---
name: lwframework
description: LWFramework 运行时核心接口速查与启动辅助。开发/生成代码优先通过 ManagerUtility.AssetsMgr/EventMgr/UIMgr/HotfixMgr 调用能力，并对照 Assets/LWFramework/RunTime/Core/InterfaceManager 下的 IAssetsManager/IEventManager/IUIManager/IHotfixManager/IManager 核对签名与生命周期，完成启动注册、初始化与调用排查。
---

# LWFramework 运行时接口速查（InterfaceManager）

## 目标

- 快速定位并读取 InterfaceManager 下的关键接口定义
- 在启动阶段避免 GetManager 返回 default 的常见坑
- 建立“接口 → 默认实现 → ManagerUtility.*Mgr 访问入口”的最短路径

## 使用约定（生成代码优先级）

- 业务代码优先使用 ManagerUtility.*Mgr 访问接口，不直接 new 管理器
- 启动代码只负责注册管理器与初始化依赖，业务模块不做注册
- 当你需要一个能力时：先找 ManagerUtility 对应 *Mgr，再回到接口定义确认方法签名

## 快速入口

- 接口目录：Assets/LWFramework/RunTime/Core/InterfaceManager/
- 访问入口：Assets/LWFramework/RunTime/Core/ManagerUtility.cs
- 管理器容器：Assets/LWFramework/RunTime/Core/MainManager.cs

## 快速开始（启动 + 典型调用）

```csharp
using Cysharp.Threading.Tasks;
using LWAssets;
using LWCore;
using LWHotfix;
using LWUI;
using UnityEngine;

public class LWFrameworkStartup : MonoBehaviour
{
    /// <summary>
    /// 启动示例：注册管理器并设置协程宿主
    /// </summary>
    private void Awake()
    {
        ManagerUtility.MainMgr.Init();
        ManagerUtility.MainMgr.MonoBehaviour = this;

        ManagerUtility.MainMgr.AddManager(typeof(IAssetsManager).ToString(), new LWAssetsManager());
        ManagerUtility.MainMgr.AddManager(typeof(IEventManager).ToString(), new LWEventManager());
        ManagerUtility.MainMgr.AddManager(typeof(IUIManager).ToString(), new UIManager());
        ManagerUtility.MainMgr.AddManager(typeof(IHotfixManager).ToString(), new HotFixRefManager());
    }

    /// <summary>
    /// 启动示例：初始化资源系统并启动流程
    /// </summary>
    private void Start()
    {
        StartAsync().Forget();
    }

    /// <summary>
    /// 启动异步逻辑：等待资源系统完成初始化
    /// </summary>
    private async UniTaskVoid StartAsync()
    {
        await ManagerUtility.AssetsMgr.InitializeAsync();
        ManagerUtility.MainMgr.StartProcedure();
    }
}
```

```csharp
using Cysharp.Threading.Tasks;
using LWCore;
using UnityEngine;

public class ExampleMgrUsage
{
    /// <summary>
    /// 资源系统示例：异步实例化预制体
    /// </summary>
    public async UniTask<GameObject> SpawnAsync(string assetPath)
    {
        GameObject gameObject = await ManagerUtility.AssetsMgr.InstantiateAsync(assetPath);
        return gameObject;
    }

    /// <summary>
    /// 事件系统示例：订阅与派发
    /// </summary>
    public void BindEvents()
    {
        ManagerUtility.EventMgr.AddListener<int>("CoinChanged", OnCoinChanged);
        ManagerUtility.EventMgr.DispatchEvent<int>("CoinChanged", 10);
    }

    /// <summary>
    /// 事件回调示例
    /// </summary>
    private void OnCoinChanged(int coin)
    {
    }
}
```

## 示例（按接口覆盖主要能力）

### IAssetsManager：初始化 / 预热 / 加载 / 实例化 / 释放

```csharp
using Cysharp.Threading.Tasks;
using LWAssets;
using LWCore;
using System.Threading;
using UnityEngine;

public class AssetsExamples
{
    /// <summary>
    /// 初始化资源系统，并预热 ShaderVariantCollection
    /// </summary>
    public async UniTask InitializeAndWarmupAsync(CancellationToken cancellationToken)
    {
        await ManagerUtility.AssetsMgr.InitializeAsync();
        await ManagerUtility.AssetsMgr.WarmupShadersAsync(cancellationToken);
    }

    /// <summary>
    /// 同步加载 Sprite 并设置到 Image 或其他组件
    /// </summary>
    public Sprite LoadSprite(string assetPath)
    {
        Sprite sprite = ManagerUtility.AssetsMgr.LoadAsset<Sprite>(assetPath);
        return sprite;
    }

    /// <summary>
    /// 异步加载 Texture2D（支持取消）
    /// </summary>
    public async UniTask<Texture2D> LoadTextureAsync(string assetPath, CancellationToken cancellationToken)
    {
        Texture2D texture = await ManagerUtility.AssetsMgr.LoadAssetAsync<Texture2D>(assetPath, cancellationToken);
        return texture;
    }

    /// <summary>
    /// 异步实例化预制体（支持取消）
    /// </summary>
    public async UniTask<GameObject> InstantiateAsync(string prefabPath, Transform parent, CancellationToken cancellationToken)
    {
        GameObject instance = await ManagerUtility.AssetsMgr.InstantiateAsync(prefabPath, parent);
        if (cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        return instance;
    }

    /// <summary>
    /// 释放资源：按对象/按路径
    /// </summary>
    public void ReleaseExample(UnityEngine.Object asset, string assetPath)
    {
        ManagerUtility.AssetsMgr.Release(asset);
        ManagerUtility.AssetsMgr.Release(assetPath);
    }

    /// <summary>
    /// 卸载未使用资源与强制卸载
    /// </summary>
    public async UniTask UnloadExampleAsync()
    {
        await ManagerUtility.AssetsMgr.UnloadUnusedAssetsAsync();
        ManagerUtility.AssetsMgr.ForceUnloadAll();
    }
}
```

### IAssetsManager：原始文件读取（字节/文本）

```csharp
using Cysharp.Threading.Tasks;
using LWCore;
using System.Text;
using System.Threading;

public class RawFileExamples
{
    /// <summary>
    /// 读取原始文本（例如 Json 配置、表格导出的文本等）
    /// </summary>
    public async UniTask<string> ReadTextAsync(string assetPath, CancellationToken cancellationToken)
    {
        string text = await ManagerUtility.AssetsMgr.LoadRawFileTextAsync(assetPath, cancellationToken);
        return text;
    }

    /// <summary>
    /// 读取原始字节并自行解析
    /// </summary>
    public async UniTask<string> ReadBytesAsUtf8Async(string assetPath, CancellationToken cancellationToken)
    {
        byte[] bytes = await ManagerUtility.AssetsMgr.LoadRawFileAsync(assetPath, cancellationToken);
        string text = bytes != null ? Encoding.UTF8.GetString(bytes) : null;
        return text;
    }
}
```

### IAssetsManager：加载场景（带进度）

```csharp
using Cysharp.Threading.Tasks;
using LWAssets;
using LWCore;
using System;
using System.Threading;
using UnityEngine.SceneManagement;

public class SceneExamples
{
    /// <summary>
    /// 带进度的场景加载：进度回调里可以驱动 UI
    /// </summary>
    public async UniTask<SceneHandle> LoadSceneWithProgressAsync(string scenePath, CancellationToken cancellationToken)
    {
        Progress<float> progress = new Progress<float>(OnSceneProgress);
        SceneHandle sceneHandle = await ManagerUtility.AssetsMgr.LoadSceneAsync(
            scenePath,
            LoadSceneMode.Single,
            true,
            progress,
            cancellationToken);

        return sceneHandle;
    }

    /// <summary>
    /// 场景加载进度回调
    /// </summary>
    private void OnSceneProgress(float p)
    {
        ManagerUtility.EventMgr.DispatchEvent<float>("SceneLoadingProgress", p);
    }
}
```

### IAssetsManager：批量加载（进度）

```csharp
using Cysharp.Threading.Tasks;
using LWCore;
using System;
using System.Threading;
using UnityEngine;

public class BatchLoadExamples
{
    /// <summary>
    /// 批量加载资源（例如图集/贴图数组），并获取整体进度
    /// </summary>
    public async UniTask<Sprite[]> LoadSpritesAsync(string[] spritePaths, CancellationToken cancellationToken)
    {
        Progress<float> progress = new Progress<float>(OnBatchProgress);
        Sprite[] sprites = await ManagerUtility.AssetsMgr.LoadAssetsAsync<Sprite>(spritePaths, progress, cancellationToken);
        return sprites;
    }

    /// <summary>
    /// 批量加载进度回调
    /// </summary>
    private void OnBatchProgress(float p)
    {
        ManagerUtility.EventMgr.DispatchEvent<float>("BatchLoadingProgress", p);
    }
}
```

### IAssetsManager：下载更新（大小预估 / 进度 / 取消）

```csharp
using Cysharp.Threading.Tasks;
using LWAssets;
using LWCore;
using System;
using System.Threading;

public class DownloadExamples
{
    /// <summary>
    /// 资源更新示例：先取大小，再下载（支持取消）
    /// </summary>
    public async UniTask DownloadIfNeededAsync(string[] tags, CancellationToken cancellationToken)
    {
        long bytes = await ManagerUtility.AssetsMgr.GetDownloadSizeAsync(tags);
        if (bytes <= 0)
        {
            return;
        }

        Progress<DownloadProgress> progress = new Progress<DownloadProgress>(OnDownloadProgress);
        await ManagerUtility.AssetsMgr.DownloadAsync(tags, progress, cancellationToken);
    }

    /// <summary>
    /// 下载进度回调
    /// </summary>
    private void OnDownloadProgress(DownloadProgress p)
    {
        ManagerUtility.EventMgr.DispatchEvent<float>("DownloadProgress", p.Progress);
    }
}
```

### IEventManager：多参数监听 / 移除 / 清理

```csharp
using LWCore;
using System;

public class EventExamples
{
    private const string EVENT_LOGIN = "Login";
    private const string EVENT_DAMAGE = "Damage";

    /// <summary>
    /// 注册事件监听（0/1/2 参数）
    /// </summary>
    public void Register()
    {
        ManagerUtility.EventMgr.AddListener(EVENT_LOGIN, OnLogin);
        ManagerUtility.EventMgr.AddListener<int>(EVENT_DAMAGE, OnDamage);
        ManagerUtility.EventMgr.AddListener<int, int>("HpChanged", OnHpChanged);
    }

    /// <summary>
    /// 派发事件（0/1/2 参数）
    /// </summary>
    public void Dispatch()
    {
        ManagerUtility.EventMgr.DispatchEvent(EVENT_LOGIN);
        ManagerUtility.EventMgr.DispatchEvent<int>(EVENT_DAMAGE, 10);
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
    }

    /// <summary>
    /// 单参事件回调
    /// </summary>
    private void OnDamage(int damage)
    {
    }

    /// <summary>
    /// 双参事件回调
    /// </summary>
    private void OnHpChanged(int oldHp, int newHp)
    {
    }
}
```

### IUIManager：打开/关闭/回退/预加载/风格

```csharp
using Cysharp.Threading.Tasks;
using LWCore;
using LWUI;

public class UIManagerExamples
{
    /// <summary>
    /// 切换 UI 风格（影响 UIViewDataAttribute 路径格式化）
    /// </summary>
    public void SetStyle(string style)
    {
        ManagerUtility.UIMgr.SetStyle(style);
    }

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

    /// <summary>
    /// 打开/关闭 View（同步）
    /// </summary>
    public void OpenAndClose<T>() where T : BaseUIView
    {
        T view = ManagerUtility.UIMgr.OpenView<T>(isLastSibling: true, enterStack: true);
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
}
```

### IHotfixManager：加载热更 / 类型与特性 / 实例化与调用

```csharp
using Cysharp.Threading.Tasks;
using LWCore;
using LWUI;
using System;
using System.Collections.Generic;

public class HotfixExamples
{
    /// <summary>
    /// 加载热更 DLL（热更入口文件名与目录按项目规范传入）
    /// </summary>
    public async UniTask LoadHotfixAsync(string hotfixDllName, string dir)
    {
        await ManagerUtility.HotfixMgr.LoadScriptAsync(hotfixDllName, dir);
    }

    /// <summary>
    /// 通过类型名获取 Type，并实例化对象
    /// </summary>
    public BaseUIView CreateViewInstance(string viewTypeName)
    {
        Type type = ManagerUtility.HotfixMgr.GetTypeByName(viewTypeName);
        BaseUIView view = ManagerUtility.HotfixMgr.Instantiate<BaseUIView>(type.FullName);
        return view;
    }

    /// <summary>
    /// 调用热更对象方法（type 为类型名，method 为方法名）
    /// </summary>
    public void InvokeMethod(string typeName, string methodName, object instance)
    {
        ManagerUtility.HotfixMgr.Invoke(typeName, methodName, instance);
    }

    /// <summary>
    /// 从热更域按特性取类型列表（示例：UIViewDataAttribute）
    /// </summary>
    public List<TypeAttr> GetUIViewTypes()
    {
        List<TypeAttr> list = ManagerUtility.HotfixMgr.GetAttrTypeDataList<UIViewDataAttribute>();
        return list;
    }

    /// <summary>
    /// 从类型名获取其特性实例（示例：UIViewDataAttribute）
    /// </summary>
    public UIViewDataAttribute GetUIViewData(string typeName)
    {
        UIViewDataAttribute attr = ManagerUtility.HotfixMgr.FindAttr<UIViewDataAttribute>(typeName);
        return attr;
    }
}
```

### IManager：自定义管理器与注册

```csharp
using LWCore;

public class CustomManager : IManager
{
    /// <summary>
    /// 自定义管理器初始化
    /// </summary>
    public void Init()
    {
    }

    /// <summary>
    /// 自定义管理器帧更新（MainManager.Update 会统一驱动）
    /// </summary>
    public void Update()
    {
    }
}

public class CustomManagerBootstrap
{
    /// <summary>
    /// 注册自定义管理器（仅启动/组装代码做注册）
    /// </summary>
    public void Register()
    {
        ManagerUtility.MainMgr.AddManager(typeof(CustomManager).ToString(), new CustomManager());
    }
}
```

## 核心结论（启动阶段必读）

- ManagerUtility.*Mgr 实际调用的是 MainManager.Instance.GetManager<T>()；如果你没有先把对应管理器 AddManager 进去，GetManager 会返回 default 并输出告警。
- MainManager 以 typeof(T).ToString() 作为 Key 保存管理器实例：添加与获取必须严格使用同一个泛型接口类型。
- IManager 只有 Init/Update；具体系统（资源/事件/UI/热更）的初始化能力全部体现在各自接口里。

## 接口清单与职责

### IManager

- 位置：Assets/LWFramework/RunTime/Core/InterfaceManager/IManager.cs
- 作用：统一的管理器生命周期
- 方法：Init()、Update()

### IAssetsManager（资源系统）

- 位置：Assets/LWFramework/RunTime/Core/InterfaceManager/IAssetsManager.cs
- 作用：抽象 LWAssets 的资源加载、下载、缓存、版本、场景管理
- 关键属性：IsInitialized、CurrentPlayMode、Loader、Downloader、Cache、Preloader、Version
- 初始化：InitializeAsync、WarmupShadersAsync、Destroy
- 清单：LoadManifestAsync
- 同步：LoadAsset、LoadRawFile、LoadRawFileText、Instantiate
- 异步：InstantiateAsync、LoadAssetAsync、LoadRawFileAsync、LoadRawFileTextAsync、LoadSceneAsync、LoadAssetsAsync
- 资源管理：Release(Object / path)、UnloadUnusedAssetsAsync、ForceUnloadAll
- 下载：GetDownloadSizeAsync、DownloadAsync

### IEventManager（事件系统）

- 位置：Assets/LWFramework/RunTime/Core/InterfaceManager/IEventManager.cs
- 作用：字符串事件名驱动的事件中心
- 监听：AddListener（0~4 参数）
- 移除：RemoveListener（0~4 参数）
- 派发：DispatchEvent（0~4 参数）
- 清理：Clear

### IUIManager（UI 系统）

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

### IHotfixManager（热更域/反射域）

- 位置：Assets/LWFramework/RunTime/Core/InterfaceManager/IHotfixManager.cs
- 作用：加载热更 DLL、反射获取类型/特性、实例化与方法调用
- 关键属性：Loaded
- 加载：LoadScriptAsync
- 类型：GetTypeByName
- 实例化：Instantiate<T>
- 调用：Invoke
- 生命周期：Destroy
- 特性系统：AddHotfixTypeAttr、GetAttrTypeDataList<T>、FindAttr<T>

## 默认实现与关联类（基于现有工程代码）

- IAssetsManager：LWAssetsManager（Assets/LWFramework/RunTime/Assets/Core/LWAssetsManager.cs）
- IEventManager：LWEventManager（Assets/LWFramework/RunTime/Core/LWEventManager.cs）
- IUIManager：UIManager（Assets/LWFramework/RunTime/UI/UIManager.cs）
- IHotfixManager：HotFixBaseManager（Assets/LWFramework/RunTime/HotFix/HotFixBaseManager.cs，抽象基类）

## 启动排查清单（最常见问题）

- 访问 ManagerUtility.AssetsMgr/EventMgr/UIMgr/HotfixMgr 之前，先确认已经对 MainManager 调用 AddManager 注册了对应接口类型的实例。
- 如果 GetManager<T>() 返回 default：检查 AddManager 的 Key 是否是 typeof(T).ToString()，以及 Add/ Get 使用的泛型类型是否一致。
- 如果 UI 打开/资源加载异常：优先确认 IAssetsManager.IsInitialized 是否为 true，以及资源系统 InitializeAsync 是否已完成。

## 生成代码时的落地规则

- 访问资源/事件/UI/热更：优先写成 ManagerUtility.AssetsMgr/EventMgr/UIMgr/HotfixMgr.xxx
- 只有启动/框架组装代码才出现 MainManager.AddManager；业务功能代码不注册管理器
- 当需要补齐能力：先回到 InterfaceManager 的接口定义核对签名，再调用 *Mgr
