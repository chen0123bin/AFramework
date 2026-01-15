# 常用调用示例

## IFSMManager：Procedure 流程状态机（示例参考 Assets/Scripts/Procedure）

流程状态通过 FSMTypeAttribute 归类到 "Procedure"，在状态内可通过 ManagerUtility.FSMMgr.GetFSMProcedure() 切换。

```csharp
using LWFMS;
using LWCore;
using UnityEngine;

[FSMTypeAttribute("Procedure", true)]
public class StartProcedure : BaseFSMState
{
    public override void OnInit()
    {
    }

    public override void OnEnter(BaseFSMState lastState)
    {
    }

    public override void OnLeave(BaseFSMState nextState)
    {
    }

    public override void OnTermination()
    {
    }

    public override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<TestProcedure>();
        }
    }
}
```

```csharp
using LWFMS;
using LWCore;
using UnityEngine;

[FSMTypeAttribute("Procedure", false)]
public class TestProcedure : BaseFSMState
{
    public override void OnInit()
    {
    }

    public override void OnEnter(BaseFSMState lastState)
    {
        ManagerUtility.EventMgr.AddListener<int>("TestEvent", OnTestEvent);
    }

    public override void OnLeave(BaseFSMState nextState)
    {
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
    }
}
```

## IAssetsManager：初始化 / 预热 / 加载 / 实例化 / 释放

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
    public void ReleaseExample(Object asset, string assetPath)
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

## IAssetsManager：原始文件读取（字节/文本）

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

## IAssetsManager：加载场景（带进度）

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

## IAssetsManager：批量加载（进度）

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

## IAssetsManager：下载更新（大小预估 / 进度 / 取消）

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

## IEventManager：多参数监听 / 移除 / 清理

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

## IUIManager：打开/关闭/回退/预加载/风格

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

## IHotfixManager：加载热更 / 类型与特性 / 实例化与调用

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

## IManager：自定义管理器与注册

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
