# LWAssets 资源管理系统

## 🌐 系统概述

LWAssets是一个功能完整、高效稳定的Unity资源管理系统，支持AssetBundle管理、资源加载、下载更新、缓存管理等核心功能。系统设计采用模块化架构，支持多种运行模式，可灵活应用于不同规模的Unity项目。

## ✨ 核心功能

- **多模式支持**：编辑器模拟、离线、在线、WebGL四种运行模式
- **异步优先**：基于UniTask实现高效异步资源加载
- **AssetBundle管理**：完整的Bundle构建、加载、依赖处理
- **智能缓存**：自动缓存管理和清理机制
- **断点续传**：支持资源包断点续传下载
- **内存优化**：资源自动释放和内存监控
- **批量加载**：支持资源批量加载和进度跟踪
- **编辑器工具**：丰富的开发辅助工具
- **热更新支持**：无缝集成热更新流程

## 🏗️ 架构设计

### 系统架构图

```
┌─────────────────────────────────────────────────────────┐
│                     LWAssetsManager                     │
└─────────────────┬─────────────────┬─────────────────────┘
                  │                 │
┌─────────────────▼─┐     ┌─────────▼─────────┐     ┌──────▼────────┐
│   IAssetLoader    │     │  DownloadManager  │     │ PreloadManager│
└─────────┬─────────┘     └───────────────────┘     └───────────────┘
          │
┌─────────▼──────────────────────────────────────────┐
│                   加载器实现                       │
│ ┌─────────────┐ ┌──────────┐ ┌──────────┐ ┌───────┐│
│ │EditorSimulate│ │ Offline  │ │  Online  │ │WebGL  ││
│ └─────────────┘ └──────────┘ └──────────┘ └───────┘│
└────────────────────────────────────────────────────┘
```

### 核心模块

| 模块 | 职责 | 位置 |
|------|------|------|
| **LWAssetsManager** | 资源管理系统入口 | `RunTime/Assets/Core/LWAssetsManager.cs` |
| **AssetLoader** | 资源加载器抽象层 | `RunTime/Assets/Loader/` |
| **CacheManager** | 缓存管理 | `RunTime/Assets/Cache/CacheManager.cs` |
| **VersionManager** | 版本管理 | `RunTime/Assets/Cache/VersionManager.cs` |
| **DownloadManager** | 下载管理 | `RunTime/Assets/Download/DownloadManager.cs` |
| **PreloadManager** | 预加载管理 | `RunTime/Assets/Preload/PreloadManager.cs` |
| **BundleManifest** | Bundle清单管理 | `RunTime/Assets/Bundle/BundleManifest.cs` |

## 🔑 访问方式

LWAssets 在框架启动链（Startup → Bootstrap → MainManager）中完成注册，由 `LWCore.ManagerUtility` 提供统一入口。`ManagerUtility.AssetsMgr` 会返回当前活跃的 `IAssetsManager` 实例，业务侧应优先通过它访问资源能力，而不是依赖 LWAssetsManager 提供的静态单例访问方式。这样可以确保在框架完整初始化前不会访问未注册的单例，访问方式与框架启动过程保持一致。

在框架默认接入逻辑中，LWAssets 会在 Core 层的启动流程中为 Assets 模块完成注册，热更、UI、FSM 以及其他模块可以通过 `ManagerUtility.AssetsMgr` 获取资源资源器并执行加载、下载、释放等操作。

## 🚀 快速开始

### 1. 安装配置

#### 创建配置文件

1. 在Unity编辑器中，右键菜单选择 `LWAssets/Config` 创建配置文件
2. 配置文件将自动生成在 `Assets/Resources/LWAssetsConfig.asset`
3. 根据项目需求调整配置参数

#### 依赖安装

项目依赖：
- [UniTask](https://github.com/Cysharp/UniTask) - 异步编程库
- LitJson - JSON解析（已内置）

### 2. 初始化系统

```csharp
using LWAssets;
using LWCore;
using UnityEngine;

public class GameStartup : MonoBehaviour
{
    private async void Start()
    {
        // 初始化资源系统（由 ManagerUtility.AssetsMgr 返回）
        await ManagerUtility.AssetsMgr.InitializeAsync();
        
        // 直接通过资源系统异步实例化预制体
        GameObject prefabInstance = await ManagerUtility.AssetsMgr.InstantiateAsync("path/to/prefab");
        
        // 初始化完成，进入游戏主逻辑
        EnterGame();
    }
    
    private void EnterGame()
    {
        // 游戏主逻辑
    }
}
```

### 3. 资源加载示例

#### 异步加载资源

```csharp
// 异步加载单个资源
var texture = await ManagerUtility.AssetsMgr.LoadAssetAsync<Texture2D>("textures/main_bg");

// 异步实例化预制体
var gameObject = await ManagerUtility.AssetsMgr.InstantiateAsync("prefabs/player");

// 异步加载场景
var sceneHandle = await ManagerUtility.AssetsMgr.LoadSceneAsync("scenes/main", LoadSceneMode.Additive);
```

#### 同步加载资源

```csharp
// 同步加载资源
var sprite = ManagerUtility.AssetsMgr.LoadAsset<Sprite>("sprites/icon");

// 同步实例化预制体
var enemy = ManagerUtility.AssetsMgr.Instantiate("prefabs/enemy");
```

#### 批量加载资源

```csharp
// 批量加载资源
string[] assetPaths = { "textures/ui1", "textures/ui2", "textures/ui3" };
var progress = new Progress<float>(p => Debug.Log($"加载进度: {p:P}"));
    var textures = await ManagerUtility.AssetsMgr.LoadAssetsAsync<Texture2D>(assetPaths, progress);
```

### 4. 资源更新示例

```csharp
// 检查资源更新
long downloadSize = await ManagerUtility.AssetsMgr.GetDownloadSizeAsync();
if (downloadSize > 0)
{
    Debug.Log($"需要下载: {FormatBytes(downloadSize)}");
    
    // 开始下载更新
    var progress = new Progress<DownloadProgress>(p => 
    {
        Debug.Log($"下载进度: {p.Progress:P}, 速度: {FormatBytes(p.Speed)}/s");
    });
    
    await ManagerUtility.AssetsMgr.DownloadAsync(progress: progress);
    Debug.Log("资源更新完成");
}

// 格式化字节大小
string FormatBytes(long bytes)
{
    if (bytes < 1024) return bytes + " B";
    if (bytes < 1024 * 1024) return (bytes / 1024f).ToString("F2") + " KB";
    return (bytes / (1024f * 1024f)).ToString("F2") + " MB";
}
```

## ⚙️ 配置说明

### 配置文件

系统配置文件 `LWAssetsConfig.asset` 包含以下主要配置项：

#### 基础设置
| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `PlayMode` | `PlayMode` | `EditorSimulate` | 运行模式 |
| `BuildOutputPath` | `string` | `AssetBundles` | 构建输出路径 |
| `RemoteURL` | `string` | `http://localhost:8080/` | 远程资源服务器URL |
| `ManifestFileName` | `string` | `manifest.json` | 清单文件名 |
| `VersionFileName` | `string` | `version.json` | 版本文件名 |

#### 下载设置
| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `MaxConcurrentDownloads` | `int` | `5` | 最大并发下载数 |
| `DownloadTimeout` | `int` | `30` | 下载超时时间(秒) |
| `MaxRetryCount` | `int` | `3` | 最大重试次数 |
| `RetryDelay` | `float` | `1f` | 重试延迟(秒) |
| `EnableBreakpointResume` | `bool` | `true` | 启用断点续传 |

#### 缓存设置
| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `MaxCacheSize` | `long` | `1GB` | 最大缓存大小 |
| `CacheExpirationDays` | `int` | `30` | 缓存过期天数 |
| `EnableAutoCleanup` | `bool` | `true` | 启用自动清理 |
| `CleanupThreshold` | `float` | `0.9f` | 缓存清理阈值(90%) |

#### 预加载设置
| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `EnablePreload` | `bool` | `true` | 启用预加载 |
| `MaxPreloadTasks` | `int` | `3` | 最大预加载任务数 |
| `MaxPreloadMemory` | `long` | `256MB` | 最大预加载内存 |

#### 内存设置
| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `MemoryWarningThreshold` | `long` | `512MB` | 内存警告阈值 |
| `MemoryCriticalThreshold` | `long` | `768MB` | 内存临界阈值 |
| `EnableAutoUnload` | `bool` | `true` | 启用自动卸载 |

#### 调试设置
| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `EnableDetailLog` | `bool` | `false` | 启用详细日志 |
| `EnableProfiler` | `bool` | `false` | 启用性能分析 |

## 🛠️ 编辑器工具

### 资源管理窗口

- **LWAssetsWindow**：资源构建和管理主窗口
- **AssetRuntimeMonitorWindow**：运行时资源监控
- **EventRuntimeMonitorWindow**：事件监控
- **AssetAnalyzer**：资源分析器
- **BundleViewer**：Bundle依赖查看器
- **DependencyViewer**：资源依赖关系查看

### 菜单入口

```
LWAssets/
├── Build AssetBundles     # 构建AssetBundle
├── Clear All Cache        # 清理所有缓存
├── Open Asset Window      # 打开资源管理窗口
└── Create Config          # 创建配置文件
```

## 📊 性能优化

### 资源加载优化

1. **异步加载优先**：尽量使用异步加载API，避免阻塞主线程
2. **合理使用缓存**：对于频繁使用的资源，考虑长期缓存
3. **批量加载**：将多个小资源合并为一个Bundle，减少加载次数
4. **资源预加载**：在合适时机预加载即将使用的资源
5. **依赖管理**：合理设置Bundle依赖，避免冗余加载

### 内存优化

1. **及时释放资源**：不再使用的资源及时调用Release释放
2. **定期清理**：定期调用UnloadUnusedAssetsAsync清理内存
3. **合理设置缓存大小**：根据项目需求调整最大缓存大小
4. **监控内存使用**：使用内置的内存监控功能
5. **避免内存泄漏**：注意资源引用关系，避免循环引用

## 📝 最佳实践

### 资源命名规范

```
# 推荐的资源路径格式
category/name

# 示例
textures/ui/main_bg
sprites/items/sword
prefabs/characters/player
scenes/levels/level1
rawfiles/config/game_config
```

### Bundle划分策略

1. **按功能模块划分**：将同一功能模块的资源放在一个Bundle
2. **按使用频率划分**：高频使用的资源单独打包
3. **按加载时机划分**：同时加载的资源放在同一Bundle
4. **按更新频率划分**：频繁更新的资源单独打包
5. **公共资源共享**：公共资源单独打包，供其他Bundle依赖

### 运行模式选择

| 模式 | 适用场景 | 优势 |
|------|----------|------|
| **EditorSimulate** | 开发阶段 | 快速迭代，无需构建Bundle |
| **Offline** | 单机游戏 | 无需网络，加载速度快 |
| **Online** | 网络游戏 | 支持资源热更新 |
| **WebGL** | WebGL平台 | 针对WebGL优化 |

## 🔧 常见问题

### Q: 如何查看资源加载日志？
A: 在配置文件中开启 `EnableDetailLog` 即可查看详细日志。

### Q: 资源加载失败怎么办？
A: 检查资源路径是否正确，Bundle是否构建成功，配置文件是否正确。

### Q: 如何处理资源依赖关系？
A: 系统会自动处理Bundle依赖，无需手动管理。

### Q: 如何清理缓存？
A: 可以通过菜单 `LWAssets/Clear All Cache` 清理，或调用 `CacheManager.ClearAll()` 方法。

### Q: 支持哪些平台？
A: 支持Windows、MacOS、Linux、Android、iOS、WebGL等主流Unity平台。

## 📄 API参考

框架通过 `LWCore.ManagerUtility.AssetsMgr` 暴露 `LWAssetsManager` 接口，业务代码无需直接引用 LWAssetsManager 提供的静态单例，可以在启动链完成后通过 ManagerUtility 获取通用的 `IAssetsManager` 实例进行调用。以下方法即对应 LWAssetsManager 提供的能力。

### LWAssetsManager

#### 初始化
```csharp
// 初始化资源系统
UniTask InitializeAsync(LWAssetsConfig config = null);

// 预热Shader
UniTask WarmupShadersAsync(CancellationToken token = default);
```

#### 资源加载
```csharp
// 异步加载资源
UniTask<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken = default);

// 同步加载资源
T LoadAsset<T>(string assetPath);

// 异步实例化预制体
UniTask<GameObject> InstantiateAsync(string assetPath, Transform spawnPoint = null);

// 同步实例化预制体
GameObject Instantiate(string assetPath, Transform spawnPoint = null);
```

#### 资源管理
```csharp
// 释放资源
void Release(UnityEngine.Object asset);
void Release(string assetPath);

// 卸载未使用资源
UniTask UnloadUnusedAssetsAsync();

// 强制卸载所有资源
void ForceUnloadAll();
```

#### 资源更新
```csharp
// 获取下载大小
UniTask<long> GetDownloadSizeAsync(string[] tags = null);

// 下载资源
UniTask DownloadAsync(string[] tags = null, IProgress<DownloadProgress> progress = null, CancellationToken cancellationToken = default);
```

## 🤝 贡献指南

欢迎提交Issue和Pull Request来帮助改进LWAssets系统！

## 📄 许可证

MIT License

## 📞 联系方式

如有问题或建议，欢迎通过以下方式联系：

- Email: your-email@example.com
- GitHub: https://github.com/your-repo/LWAssets

## 📋 更新日志

### v1.0.0
- ✅ 初始版本发布
- ✅ 支持四种运行模式
- ✅ 完整的AssetBundle管理
- ✅ 异步资源加载
- ✅ 资源下载更新
- ✅ 智能缓存管理
- ✅ 编辑器工具支持

---

**LWAssets资源管理系统** - 让Unity资源管理更简单、高效、可靠！ 🎮
