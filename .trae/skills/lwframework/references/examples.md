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
    /// 打开/关闭 View 并传递数据（同步）
    /// </summary>
    public void OpenAndCloseWithData<T>(object data) where T : BaseUIView
    {
        T view = ManagerUtility.UIMgr.OpenView<T>(data: data, isLastSibling: true, enterStack: true);
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

## UI 多条目/多元素：View + Item + GameObjectPool

```csharp
using LWUI;
using LWCore;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

/// <summary>
/// 视图：通过 OpenView(data) 接收列表，使用对象池批量生成条目
/// </summary>
[UIViewData("Assets/0Res/Prefabs/UI/StepView.prefab", (int)FindType.Name, "LWFramework/Canvas/Normal")]
public class StepView : BaseUIView
{
    internal static readonly string EVENT_CLOSE = "StepView_Close";

    [UIElement("PnlLeft/PnlStepList/Viewport/Content/PnlStepItem")]
    private Transform m_PnlStepItem;
    [UIElement("BtnPrev")]
    private Button m_BtnPrev;
    [UIElement("BtnNext")]
    private Button m_BtnNext;
    [UIElement("BtnBack")]
    private Button m_BtnBack;

    private GameObjectPool<PnlStepItem> m_PnlStepItemPool;

    /// <summary>
    /// 创建视图：绑定按钮事件并初始化对象池
    /// </summary>
    public override void CreateView(GameObject gameObject)
    {
        base.CreateView(gameObject);

        m_BtnPrev.onClick.AddListener(() => { });
        m_BtnNext.onClick.AddListener(() => { });
        m_BtnBack.onClick.AddListener(() => { ManagerUtility.EventMgr.DispatchEvent(EVENT_CLOSE); });

        m_PnlStepItemPool = new GameObjectPool<PnlStepItem>(poolMaxSize: 5, template: m_PnlStepItem.gameObject);
    }

    /// <summary>
    /// 打开视图：用 List<string> 作为数据源，批量借出条目并绑定回调
    /// </summary>
    public override void OpenView(object data = null)
    {
        base.OpenView(data);

        List<string> stepList = data as List<string>;
        if (stepList == null || stepList.Count == 0)
        {
            return;
        }

        for (int i = 0; i < stepList.Count; i++)
        {
            int index = i;
            PnlStepItem pnlStepItem = m_PnlStepItemPool.Spawn();
            pnlStepItem.StepIndex = (i + 1).ToString();
            pnlStepItem.StepTitle = stepList[i];
            pnlStepItem.OnClickStep = () => { LWDebug.Log($"点击了第{index + 1}个步骤"); };
        }
    }
}

/// <summary>
/// 条目：继承 BaseUIItem，暴露 Action 给 View 绑定点击
/// </summary>
public class PnlStepItem : BaseUIItem
{
    [UIElement("TxtStepIndex")]
    private Text m_TxtStepIndex;
    [UIElement("TxtStepTitle")]
    private Text m_TxtStepTitle;

    private Button m_BtnStep;

    public string StepIndex
    {
        get { return m_TxtStepIndex.text; }
        set { m_TxtStepIndex.text = value; }
    }

    public string StepTitle
    {
        get { return m_TxtStepTitle.text; }
        set { m_TxtStepTitle.text = value; }
    }

    public Action OnClickStep;

    /// <summary>
    /// 创建条目：绑定点击回调
    /// </summary>
    public override void Create(GameObject gameObject)
    {
        base.Create(gameObject);
        m_BtnStep = gameObject.GetComponent<Button>();
        m_BtnStep.onClick.AddListener(() => { OnClickStep?.Invoke(); });
    }

    /// <summary>
    /// 归还到池：按需做清理
    /// </summary>
    public override void OnUnSpawn()
    {
        base.OnUnSpawn();
    }

    /// <summary>
    /// 释放条目：按需做资源释放
    /// </summary>
    public override void OnRelease()
    {
        base.OnRelease();
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

## IAudioManager：播放 / 停止 / 暂停 / 全局音量

```csharp
using LWAudio;
using LWCore;
using UnityEngine;

public class AudioManagerExamples : MonoBehaviour
{
    [SerializeField] private AudioClip m_UIClickClip;
    [SerializeField] private AudioClip m_ExplosionClip;
    [SerializeField] private Transform m_Emitter;

    private AudioChannel m_UIClickChannel;

    /// <summary>
    /// 播放 2D 音效：返回的 AudioChannel 可用于后续控制
    /// </summary>
    public void PlayUIClick()
    {
        if (m_UIClickClip == null)
        {
            return;
        }

        m_UIClickChannel = ManagerUtility.AudioMgr.Play(m_UIClickClip, loop: false, fadeInSeconds: 0f, volume: -1f);
    }

    /// <summary>
    /// 播放 3D 音效：跟随挂点，并可按需覆写 3D 参数
    /// </summary>
    public void PlayExplosionOnEmitter()
    {
        if (m_ExplosionClip == null)
        {
            return;
        }

        Audio3DSettings audio3DSettings = Audio3DSettings.Default3D;
        audio3DSettings.MinDistance = 2f;
        audio3DSettings.MaxDistance = 30f;

        ManagerUtility.AudioMgr.Play(
            m_ExplosionClip,
            m_Emitter,
            loop: false,
            fadeInSeconds: 0.05f,
            volume: -1f,
            settings: audio3DSettings);
    }

    /// <summary>
    /// 设置全局音量：会作用于当前已激活的所有通道
    /// </summary>
    public void SetGlobalVolume(float volume01)
    {
        float volume = Mathf.Clamp01(volume01);
        ManagerUtility.AudioMgr.AudioVolume = volume;
    }

    /// <summary>
    /// 控制指定通道：暂停/恢复/停止（Stop 会触发淡出逻辑，取决于 Play 配置）
    /// </summary>
    public void ControlChannel()
    {
        if (m_UIClickChannel == null)
        {
            return;
        }

        ManagerUtility.AudioMgr.Pause(m_UIClickChannel);
        ManagerUtility.AudioMgr.Resume(m_UIClickChannel);
        ManagerUtility.AudioMgr.Stop(m_UIClickChannel);
        m_UIClickChannel = null;
    }

    /// <summary>
    /// 停止所有通道：适用于切场景/回主城/进入战斗等全局切换
    /// </summary>
    public void StopAll()
    {
        ManagerUtility.AudioMgr.StopAll();
    }
}
```

## GameObjectPool：创建 / 借出 / 归还 / 清理

```csharp
using LWCore;
using UnityEngine;

public sealed class BulletPoolItem : PoolGameObject
{
    /// <summary>
    /// 对象从池中借出时回调：做初始化/重置
    /// </summary>
    public override void OnSpawn()
    {
        if (m_Entity == null)
        {
            return;
        }

        m_Entity.transform.localPosition = Vector3.zero;
        m_Entity.transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// 对象归还进池时回调：做清理/取消绑定
    /// </summary>
    public override void OnUnSpawn()
    {
    }
}

public class ObjectPoolExamples : MonoBehaviour
{
    [SerializeField] private GameObject m_BulletTemplate;
    [SerializeField] private Transform m_PoolRoot;

    private GameObjectPool<BulletPoolItem> m_BulletPool;

    /// <summary>
    /// 初始化对象池：template 会被 SetActive(false) 作为克隆源
    /// </summary>
    public void Initialize()
    {
        if (m_BulletTemplate == null)
        {
            return;
        }

        m_BulletPool = new GameObjectPool<BulletPoolItem>(poolMaxSize: 64, template: m_BulletTemplate, parent: m_PoolRoot, ownsTemplate: false);
    }

    /// <summary>
    /// 借出对象：必要时会自动克隆 template
    /// </summary>
    public BulletPoolItem SpawnBullet()
    {
        if (m_BulletPool == null)
        {
            return null;
        }

        BulletPoolItem bullet = m_BulletPool.Spawn();
        return bullet;
    }

    /// <summary>
    /// 归还对象：超出容量会直接释放
    /// </summary>
    public void UnspawnBullet(BulletPoolItem bullet)
    {
        if (m_BulletPool == null)
        {
            return;
        }

        m_BulletPool.Unspawn(bullet);
    }

    /// <summary>
    /// 清空对象池：可选择是否释放“已借出”的对象
    /// </summary>
    public void ClearPool(bool releaseInUseObjects)
    {
        if (m_BulletPool == null)
        {
            return;
        }

        m_BulletPool.Clear(releaseInUseObjects);
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
