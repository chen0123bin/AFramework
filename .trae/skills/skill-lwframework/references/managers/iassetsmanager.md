# IAssetsManager 资源管理器

## 概述

IAssetsManager 是 LWFramework 的资源系统核心接口，提供资源加载、实例化、场景管理、批量加载和下载更新等功能。

- **接口位置**: `Assets/LWFramework/RunTime/Core/InterfaceManager/IAssetsManager.cs`
- **默认实现**: `Assets/LWFramework/RunTime/Assets/Core/LWAssetsManager.cs`
- **访问入口**: `ManagerUtility.AssetsMgr`

---

## 核心属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `IsInitialized` | `bool` | 资源系统是否已完成初始化 |
| `CurrentPlayMode` | `Enum` | 当前运行模式（Editor/Package等） |
| `Loader` | `IAssetLoader` | 资源加载器 |
| `Downloader` | `IDownloader` | 资源下载器 |
| `Cache` | `IAssetCache` | 资源缓存 |
| `Preloader` | `IPreloader` | 资源预加载器 |
| `Version` | `IVersion` | 版本管理 |

---

## API 参考

### 初始化与销毁

```csharp
/// <summary>
/// 异步初始化资源系统
/// </summary>
UniTask InitializeAsync();

/// <summary>
/// 异步预热 ShaderVariantCollection
/// </summary>
UniTask WarmupShadersAsync(CancellationToken cancellationToken);

/// <summary>
/// 销毁资源系统
/// </summary>
void Destroy();
```

### 同步加载

```csharp
/// <summary>
/// 同步加载指定类型资源
/// </summary>
T LoadAsset<T>(string assetPath) where T : Object;

/// <summary>
/// 同步实例化预制体
/// </summary>
GameObject Instantiate(string prefabPath, Transform parent = null);

/// <summary>
/// 同步加载原始文件（字节）
/// </summary>
byte[] LoadRawFile(string assetPath);

/// <summary>
/// 同步加载原始文件（文本）
/// </summary>
string LoadRawFileText(string assetPath);
```

### 异步加载

```csharp
/// <summary>
/// 异步加载指定类型资源
/// </summary>
UniTask<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken) where T : Object;

/// <summary>
/// 异步实例化预制体
/// </summary>
UniTask<GameObject> InstantiateAsync(string prefabPath, Transform parent, CancellationToken cancellationToken);

/// <summary>
/// 异步加载原始文件（字节）
/// </summary>
UniTask<byte[]> LoadRawFileAsync(string assetPath, CancellationToken cancellationToken);

/// <summary>
/// 异步加载原始文件（文本）
/// </summary>
UniTask<string> LoadRawFileTextAsync(string assetPath, CancellationToken cancellationToken);

/// <summary>
/// 异步加载场景
/// </summary>
UniTask<SceneHandle> LoadSceneAsync(string scenePath, LoadSceneMode loadSceneMode, bool activateOnLoad, IProgress<float> progress, CancellationToken cancellationToken);

/// <summary>
/// 批量异步加载资源
/// </summary>
UniTask<T[]> LoadAssetsAsync<T>(string[] assetPaths, IProgress<float> progress, CancellationToken cancellationToken) where T : Object;
```

### 资源释放

```csharp
/// <summary>
/// 按对象引用释放资源
/// </summary>
void Release(Object asset);

/// <summary>
/// 按路径释放资源
/// </summary>
void Release(string assetPath);

/// <summary>
/// 异步卸载未使用资源
/// </summary>
UniTask UnloadUnusedAssetsAsync();

/// <summary>
/// 强制卸载所有资源
/// </summary>
void ForceUnloadAll();
```

### 下载更新

```csharp
/// <summary>
/// 获取指定标签资源的下载大小
/// </summary>
UniTask<long> GetDownloadSizeAsync(string[] tags);

/// <summary>
/// 异步下载指定标签资源
/// </summary>
UniTask DownloadAsync(string[] tags, IProgress<DownloadProgress> progress, CancellationToken cancellationToken);
```

---

## 使用示例

### 初始化与预热

```csharp
using Cysharp.Threading.Tasks;
using LWAssets;
using LWCore;
using System.Threading;

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
}
```

### 同步加载资源

```csharp
/// <summary>
/// 同步加载 Sprite 并设置到 Image 或其他组件
/// </summary>
public Sprite LoadSprite(string assetPath)
{
    Sprite sprite = ManagerUtility.AssetsMgr.LoadAsset<Sprite>(assetPath);
    return sprite;
}

/// <summary>
/// 同步加载 GameObject 预制体
/// </summary>
public GameObject LoadPrefab(string prefabPath)
{
    GameObject prefab = ManagerUtility.AssetsMgr.LoadAsset<GameObject>(prefabPath);
    return prefab;
}
```

### 异步加载资源

```csharp
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
```

### 读取原始文件

```csharp
using System.Text;

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

### 场景加载（带进度）

```csharp
using UnityEngine.SceneManagement;
using System;

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

### 批量加载（带进度）

```csharp
public class BatchLoadExamples
{
