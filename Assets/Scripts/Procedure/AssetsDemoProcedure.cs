using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LWAssets;
using LWCore;
using LWFMS;
using LWUI;
using UnityEngine;

[FSMTypeAttribute("Procedure", false)]
public class AssetsDemoProcedure : BaseFSMState
{
    private const string TEST_SPRITE_PATH = "Assets/0Res/Sprites/00008.png";
    private const string TEST_PREFAB_PATH = "Assets/0Res/Prefabs/Cube.prefab";
    private const string TEST_SCENE_PATH = "Assets/0Res/Scenes2/Test2.unity";

    private bool m_IsBusy;
    private CancellationTokenSource m_CancellationTokenSource;

    private Sprite m_LoadedSprite;
    private GameObject m_SpawnedPrefab;
    private SceneHandle m_SceneHandle;

    private AssetsDemoView m_AssetsDemoView;
    /// <summary>
    /// 初始化资源演示流程：重置内部状态。
    /// </summary>
    public override void OnInit()
    {
        m_IsBusy = false;
        m_CancellationTokenSource = null;
        m_LoadedSprite = null;
        m_SpawnedPrefab = null;
        m_SceneHandle = null;
    }

    /// <summary>
    /// 进入资源演示流程：注册界面事件并打开界面。
    /// </summary>
    /// <param name="lastState">上一个状态</param>
    public override void OnEnter(BaseFSMState lastState)
    {
        m_CancellationTokenSource?.Cancel();
        m_CancellationTokenSource?.Dispose();
        m_CancellationTokenSource = new CancellationTokenSource();

        ManagerUtility.EventMgr.AddListener(AssetsDemoView.EVENT_CLOSE, OnClose);
        ManagerUtility.EventMgr.AddListener(AssetsDemoView.EVENT_UNLOAD_UNUSED, OnUnloadUnused);
        ManagerUtility.EventMgr.AddListener(AssetsDemoView.EVENT_ASSETS_IS_INITIALIZED, OnAssetsIsInitialized);
        ManagerUtility.EventMgr.AddListener(AssetsDemoView.EVENT_LOAD_SPRITE, OnLoadSprite);
        ManagerUtility.EventMgr.AddListener(AssetsDemoView.EVENT_LOAD_PREFAB, OnLoadPrefab);
        ManagerUtility.EventMgr.AddListener(AssetsDemoView.EVENT_LOAD_SCENE, OnLoadScene);
        ManagerUtility.EventMgr.AddListener(AssetsDemoView.EVENT_UNLOAD_ASSET, OnUnloadAsset);
        ManagerUtility.EventMgr.AddListener(AssetsDemoView.EVENT_UNLOAD_SCENE, OnUnloadScene);
        ManagerUtility.EventMgr.AddListener(AssetsDemoView.EVENT_CHECK_VERSION, OnCheckVersion);

        m_AssetsDemoView = ManagerUtility.UIMgr.OpenView<AssetsDemoView>();
    }

    /// <summary>
    /// 离开资源演示流程：注销事件、关闭界面并清理演示资源。
    /// </summary>
    /// <param name="nextState">下一个状态</param>
    public override void OnLeave(BaseFSMState nextState)
    {
        m_CancellationTokenSource?.Cancel();
        m_CancellationTokenSource?.Dispose();
        m_CancellationTokenSource = null;

        ManagerUtility.EventMgr.RemoveListener(AssetsDemoView.EVENT_CLOSE, OnClose);
        ManagerUtility.EventMgr.RemoveListener(AssetsDemoView.EVENT_UNLOAD_UNUSED, OnUnloadUnused);
        ManagerUtility.EventMgr.RemoveListener(AssetsDemoView.EVENT_ASSETS_IS_INITIALIZED, OnAssetsIsInitialized);
        ManagerUtility.EventMgr.RemoveListener(AssetsDemoView.EVENT_LOAD_SPRITE, OnLoadSprite);
        ManagerUtility.EventMgr.RemoveListener(AssetsDemoView.EVENT_LOAD_PREFAB, OnLoadPrefab);
        ManagerUtility.EventMgr.RemoveListener(AssetsDemoView.EVENT_LOAD_SCENE, OnLoadScene);
        ManagerUtility.EventMgr.RemoveListener(AssetsDemoView.EVENT_UNLOAD_ASSET, OnUnloadAsset);
        ManagerUtility.EventMgr.RemoveListener(AssetsDemoView.EVENT_UNLOAD_SCENE, OnUnloadScene);
        ManagerUtility.EventMgr.RemoveListener(AssetsDemoView.EVENT_CHECK_VERSION, OnCheckVersion);

        ManagerUtility.UIMgr.CloseView<AssetsDemoView>();

        CleanupAsync().Forget();
    }

    /// <summary>
    /// 流程终止：确保取消任务并清理演示资源。
    /// </summary>
    public override void OnTermination()
    {
        m_CancellationTokenSource?.Cancel();
        m_CancellationTokenSource?.Dispose();
        m_CancellationTokenSource = null;

        CleanupAsync().Forget();
    }

    /// <summary>
    /// 流程更新：当前演示流程无需轮询逻辑。
    /// </summary>
    public override void OnUpdate()
    {

    }


    /// <summary>
    /// 关闭资源演示界面并返回菜单流程。
    /// </summary>
    private void OnClose()
    {
        ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<MenuProcedure>();
    }

    /// <summary>
    /// 初始化资源系统。
    /// </summary>
    private void OnUnloadUnused()
    {
        if (m_IsBusy)
        {
            LWDebug.LogWarning("资源操作进行中，请稍后再试");
            return;
        }
        ManagerUtility.AssetsMgr.UnloadUnusedAssetsAsync().Forget();
    }

    /// <summary>
    /// 查询并输出资源系统是否已初始化。
    /// </summary>
    private void OnAssetsIsInitialized()
    {
        bool isInitialized = ManagerUtility.AssetsMgr != null && ManagerUtility.AssetsMgr.IsInitialized;
        LWDebug.Log("AssetsMgr.IsInitialized = " + isInitialized);
    }

    /// <summary>
    /// 异步加载一个 Sprite 资源。
    /// </summary>
    private void OnLoadSprite()
    {
        if (m_IsBusy)
        {
            LWDebug.LogWarning("资源操作进行中，请稍后再试");
            return;
        }

        LoadSpriteAsync().Forget();
    }

    /// <summary>
    /// 异步实例化一个 Prefab。
    /// </summary>
    private void OnLoadPrefab()
    {
        if (m_IsBusy)
        {
            LWDebug.LogWarning("资源操作进行中，请稍后再试");
            return;
        }

        LoadPrefabAsync().Forget();
    }

    /// <summary>
    /// 异步加载一个场景（Additive）。
    /// </summary>
    private void OnLoadScene()
    {
        if (m_IsBusy)
        {
            LWDebug.LogWarning("资源操作进行中，请稍后再试");
            return;
        }

        LoadSceneAsync().Forget();
    }

    /// <summary>
    /// 释放已加载资源并回收实例。
    /// </summary>
    private void OnUnloadAsset()
    {
        try
        {
            if (m_SpawnedPrefab != null)
            {
                AutoReleaseOnDestroy autoReleaseOnDestroy = m_SpawnedPrefab.GetComponent<AutoReleaseOnDestroy>();
                bool hasAutoRelease = autoReleaseOnDestroy != null && !string.IsNullOrEmpty(autoReleaseOnDestroy.m_Path);
                GameObject.Destroy(m_SpawnedPrefab);
                m_SpawnedPrefab = null;

                if (!hasAutoRelease && ManagerUtility.AssetsMgr != null && ManagerUtility.AssetsMgr.IsInitialized)
                {
                    ManagerUtility.AssetsMgr.Release(TEST_PREFAB_PATH);
                }
            }

            if (m_LoadedSprite != null)
            {
                if (ManagerUtility.AssetsMgr != null && ManagerUtility.AssetsMgr.IsInitialized)
                {
                    ManagerUtility.AssetsMgr.Release(m_LoadedSprite);
                }
                m_LoadedSprite = null;
            }

            LWDebug.Log("已执行资源卸载/释放");
        }
        catch (Exception e)
        {
            LWDebug.LogError("卸载资源失败: " + e.Message);
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// 卸载已加载的场景。
    /// </summary>
    private void OnUnloadScene()
    {
        if (m_IsBusy)
        {
            LWDebug.LogWarning("资源操作进行中，请稍后再试");
            return;
        }

        UnloadSceneAsync().Forget();
    }

    /// <summary>
    /// 检查资源版本更新信息。
    /// </summary>
    private void OnCheckVersion()
    {
        if (m_IsBusy)
        {
            LWDebug.LogWarning("资源操作进行中，请稍后再试");
            return;
        }

        CheckVersionAsync().Forget();
    }

    /// <summary>
    /// 执行资源系统初始化（带异常保护与忙碌状态）。
    /// </summary>
    private async UniTaskVoid AssetsInitAsync()
    {
        if (ManagerUtility.AssetsMgr == null)
        {
            LWDebug.LogWarning("AssetsMgr 为空，无法初始化");
            return;
        }

        try
        {
            m_IsBusy = true;
            await ManagerUtility.AssetsMgr.InitializeAsync();
            LWDebug.Log("资源系统初始化完成");
        }
        catch (Exception e)
        {
            LWDebug.LogError("资源系统初始化失败: " + e.Message);
            Debug.LogException(e);
        }
        finally
        {
            m_IsBusy = false;
        }
    }

    /// <summary>
    /// 异步加载测试 Sprite。
    /// </summary>
    private async UniTaskVoid LoadSpriteAsync()
    {


        CancellationToken cancellationToken = m_CancellationTokenSource != null ? m_CancellationTokenSource.Token : CancellationToken.None;
        try
        {
            m_IsBusy = true;

            if (m_LoadedSprite != null)
            {
                ManagerUtility.AssetsMgr.Release(m_LoadedSprite);
                m_LoadedSprite = null;
            }

            Sprite sprite = await ManagerUtility.AssetsMgr.LoadAssetAsync<Sprite>(TEST_SPRITE_PATH, cancellationToken);
            m_LoadedSprite = sprite;
            m_AssetsDemoView.ImgSprite = sprite;
            LWDebug.Log(sprite != null ? ("Sprite 加载成功: " + TEST_SPRITE_PATH) : ("Sprite 加载失败: " + TEST_SPRITE_PATH));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            LWDebug.LogError("Sprite 加载失败: " + e.Message);
            Debug.LogException(e);
        }
        finally
        {
            m_IsBusy = false;
        }
    }

    /// <summary>
    /// 异步实例化测试 Prefab。
    /// </summary>
    private async UniTaskVoid LoadPrefabAsync()
    {

        try
        {
            m_IsBusy = true;
            if (m_SpawnedPrefab != null)
            {
                AutoReleaseOnDestroy autoReleaseOnDestroy = m_SpawnedPrefab.GetComponent<AutoReleaseOnDestroy>();
                bool hasAutoRelease = autoReleaseOnDestroy != null && !string.IsNullOrEmpty(autoReleaseOnDestroy.m_Path);
                GameObject.Destroy(m_SpawnedPrefab);
                m_SpawnedPrefab = null;

                if (!hasAutoRelease)
                {
                    ManagerUtility.AssetsMgr.Release(TEST_PREFAB_PATH);
                }
            }

            GameObject instance = await ManagerUtility.AssetsMgr.InstantiateAsync(TEST_PREFAB_PATH, null);
            m_SpawnedPrefab = instance;
            LWDebug.Log(instance != null ? ("Prefab 实例化成功: " + TEST_PREFAB_PATH) : ("Prefab 实例化失败: " + TEST_PREFAB_PATH));
        }
        catch (Exception e)
        {
            LWDebug.LogError("Prefab 实例化失败: " + e.Message);
            Debug.LogException(e);
        }
        finally
        {
            m_IsBusy = false;
        }
    }

    /// <summary>
    /// 异步加载测试场景（Additive）。
    /// </summary>
    private async UniTaskVoid LoadSceneAsync()
    {


        CancellationToken cancellationToken = m_CancellationTokenSource != null ? m_CancellationTokenSource.Token : CancellationToken.None;
        try
        {
            m_IsBusy = true;
            if (m_SceneHandle != null)
            {
                await m_SceneHandle.UnloadAsync();
                m_SceneHandle.Dispose();
                m_SceneHandle = null;
            }

            SceneHandle sceneHandle = await ManagerUtility.AssetsMgr.LoadSceneAsync(TEST_SCENE_PATH, UnityEngine.SceneManagement.LoadSceneMode.Additive, true, null, cancellationToken);
            m_SceneHandle = sceneHandle;

            if (sceneHandle != null && sceneHandle.IsValid)
            {
                LWDebug.Log("场景加载成功: " + sceneHandle.Scene.name);
            }
            else if (sceneHandle != null && sceneHandle.HasError)
            {
                LWDebug.LogWarning("场景加载失败: " + sceneHandle.Error);
            }
            else
            {
                LWDebug.LogWarning("场景加载失败: " + TEST_SCENE_PATH);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            LWDebug.LogError("场景加载失败: " + e.Message);
            Debug.LogException(e);
        }
        finally
        {
            m_IsBusy = false;
        }
    }

    /// <summary>
    /// 异步卸载已加载的测试场景。
    /// </summary>
    private async UniTaskVoid UnloadSceneAsync()
    {
        if (m_SceneHandle == null)
        {
            LWDebug.LogWarning("场景未加载，无法卸载");
            return;
        }

        try
        {
            m_IsBusy = true;
            await m_SceneHandle.UnloadAsync();
            m_SceneHandle.Dispose();
            m_SceneHandle = null;
            LWDebug.Log("场景卸载成功");
        }
        catch (Exception e)
        {
            LWDebug.LogError("场景卸载失败: " + e.Message);
            Debug.LogException(e);
        }
        finally
        {
            m_IsBusy = false;
        }
    }

    /// <summary>
    /// 异步检查更新并输出结果。
    /// </summary>
    private async UniTaskVoid CheckVersionAsync()
    {
        if (ManagerUtility.AssetsMgr == null || !ManagerUtility.AssetsMgr.IsInitialized)
        {
            LWDebug.LogWarning("AssetsMgr 未初始化，请先点击 AssetsInit");
            return;
        }

        try
        {
            m_IsBusy = true;
            UpdateCheckResult result = await ManagerUtility.AssetsMgr.Version.CheckUpdateAsync();
            LWDebug.Log("版本检查结果: " + result.Status + ", Local=" + result.LocalVersion + ", Remote=" + result.RemoteVersion + ", Size=" + result.DownloadSize + ", Count=" + result.DownloadCount + ", Error=" + result.Error);
        }
        catch (Exception e)
        {
            LWDebug.LogError("版本检查失败: " + e.Message);
            Debug.LogException(e);
        }
        finally
        {
            m_IsBusy = false;
        }
    }

    /// <summary>
    /// 清理演示过程中加载/实例化的资源与场景，避免跨流程残留。
    /// </summary>
    private async UniTaskVoid CleanupAsync()
    {
        try
        {
            m_IsBusy = true;

            if (m_SceneHandle != null)
            {
                try
                {
                    await m_SceneHandle.UnloadAsync();
                }
                catch (Exception e)
                {
                    LWDebug.LogWarning("清理场景失败: " + e.Message);
                }
                finally
                {
                    m_SceneHandle.Dispose();
                    m_SceneHandle = null;
                }
            }

            if (m_SpawnedPrefab != null)
            {
                AutoReleaseOnDestroy autoReleaseOnDestroy = m_SpawnedPrefab.GetComponent<AutoReleaseOnDestroy>();
                bool hasAutoRelease = autoReleaseOnDestroy != null && !string.IsNullOrEmpty(autoReleaseOnDestroy.m_Path);
                GameObject.Destroy(m_SpawnedPrefab);
                m_SpawnedPrefab = null;

                if (!hasAutoRelease && ManagerUtility.AssetsMgr != null && ManagerUtility.AssetsMgr.IsInitialized)
                {
                    ManagerUtility.AssetsMgr.Release(TEST_PREFAB_PATH);
                }
            }

            if (m_LoadedSprite != null)
            {
                if (ManagerUtility.AssetsMgr != null && ManagerUtility.AssetsMgr.IsInitialized)
                {
                    ManagerUtility.AssetsMgr.Release(m_LoadedSprite);
                }
                m_LoadedSprite = null;
            }
        }
        catch (Exception e)
        {
            LWDebug.LogError("清理资源失败: " + e.Message);
            Debug.LogException(e);
        }
        finally
        {
            m_IsBusy = false;
        }
    }


}
