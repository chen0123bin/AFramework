# 启动注册与初始化

## 最小启动骨架（注册管理器 + 设置协程宿主）

```csharp
using Cysharp.Threading.Tasks;
using LWAssets;
using LWCore;
using LWFMS;
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
        ManagerUtility.MainMgr.AddManager(typeof(IFSMManager).ToString(), new FSMManager());
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

## Procedure 启动规则

- MainManager.StartProcedure 会扫描带 FSMTypeAttribute 的状态类，并创建名为 nameof(FSMName.Procedure) 的状态机
- 启动第一状态有两种方式（二选一）
  - 将某个流程状态标记为 [FSMTypeAttribute("Procedure", true)]，由 StartFirst 启动
  - 启动前设置 MainManager.Instance.FirstFSMState（例如 typeof(StartProcedure) 或 HotfixMgr.GetTypeByName(procedureTypeName)）

## 常见启动坑

- GetManager<T>() 返回 default：通常是没 AddManager，或 Add/Get 的泛型类型不一致
- Key 不一致：MainManager 以 typeof(T).ToString() 作为 Key 保存管理器实例
- 资源系统未初始化：优先检查 IAssetsManager.IsInitialized，确保 InitializeAsync 已完成
- 未注册 IFSMManager：StartProcedure/ClearManager 内部会直接访问 IFSMManager，可能触发空引用
- 流程未指定 first：如果没有任何状态标记 isFirst=true 且未设置 FirstFSMState，StartFirst 可能触发空引用
