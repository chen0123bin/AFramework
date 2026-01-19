using LWCore;
using LWUI;
using UnityEngine;
using UnityEngine.UI;

[UIViewData("Assets/0Res/Prefabs/UI/AssetsDemoView.prefab", (int)FindType.Name, "LWFramework/Canvas/Normal")]
public class AssetsDemoView : BaseUIView
{
    public const string EVENT_CLOSE = "CloseAssetsDemoView";
    public const string EVENT_UNLOAD_UNUSED = "AssetsDemoView.UnloadUnused";
    public const string EVENT_ASSETS_IS_INITIALIZED = "AssetsDemoView.AssetsIsInitialized";
    public const string EVENT_LOAD_SPRITE = "AssetsDemoView.LoadSprite";
    public const string EVENT_LOAD_PREFAB = "AssetsDemoView.LoadPrefab";
    public const string EVENT_LOAD_SCENE = "AssetsDemoView.LoadScene";
    public const string EVENT_RELEASE_ASSET = "AssetsDemoView.ReleaseAsset";
    public const string EVENT_UNLOAD_SCENE = "AssetsDemoView.UnloadScene";
    public const string EVENT_CHECK_VERSION = "AssetsDemoView.CheckVersion";

    [UIElement("PnlTop/BtnClose")]
    private Button m_BtnClose;

    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnUnloadUnused")]
    private Button m_BtnUnloadUnused;

    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnAssetsIsInitialized")]
    private Button m_BtnAssetsIsInitialized;

    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnLoadSprite")]
    private Button m_BtnLoadSprite;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnLoadPrefab")]
    private Button m_BtnLoadPrefab;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnLoadScene")]
    private Button m_BtnLoadScene;

    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnReleaseAsset")]
    private Button m_BtnReleaseAsset;

    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnUnloadScene")]
    private Button m_BtnUnloadScene;

    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnCheckVersion")]
    private Button m_BtnCheckVersion;
    [UIElement("ImgSprite")]
    private Image m_ImgSprite;

    public Sprite ImgSprite
    {
        set
        {
            m_ImgSprite.sprite = value;
            // 刷新图片显示
            m_ImgSprite.SetNativeSize();
        }

    }

    /// <summary>
    /// 创建并初始化资源演示界面：将按钮点击转为事件派发。
    /// </summary>
    /// <param name="gameObject">界面实例对象</param>
    public override void CreateView(GameObject gameObject)
    {
        base.CreateView(gameObject);

        m_BtnClose.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_CLOSE);
        });

        m_BtnUnloadUnused.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_UNLOAD_UNUSED);
        });
        m_BtnAssetsIsInitialized.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_ASSETS_IS_INITIALIZED);
        });

        m_BtnLoadSprite.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_LOAD_SPRITE);
        });
        m_BtnLoadPrefab.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_LOAD_PREFAB);
        });
        m_BtnLoadScene.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_LOAD_SCENE);
        });

        m_BtnReleaseAsset.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_RELEASE_ASSET);
        });
        m_BtnUnloadScene.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_UNLOAD_SCENE);
        });

        m_BtnCheckVersion.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_CHECK_VERSION);
        });
    }
}

