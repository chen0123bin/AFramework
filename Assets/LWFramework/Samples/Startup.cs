
using System;
using LWAssets;
using LWCore;
using UnityEngine;
[DefaultExecutionOrder(1000)]
public class Startup : MonoBehaviour
{

    public string configUrl;
    public string procedureName = "StartProcedure";
    async void Start()
    {

        Application.targetFrameRate = 60;
        DontDestroyOnLoad(gameObject);
        //LWUpdate.ManifestNameUtility = new DefaultManifestNameUtility();
        // await LWUtility.ReadServerConfigAsync(configUrl);
        //设置LWDebug数据
        LWDebug.SetLogConfig(true, 3, true);

        MainManager.Instance.Init();
        //添加各种管理器      

        MainManager.Instance.AddManager(typeof(IAssetsManager).ToString(), new LWAssetsManager());
        MainManager.Instance.AddManager(typeof(IEventManager).ToString(), new LWEventManager());

        await ManagerUtility.AssetsMgr.InitializeAsync();
        MainManager.Instance.MonoBehaviour = this;

        ManagerUtility.EventMgr.AddListener<int>("TestEvent", OnTestEvent1);
        ManagerUtility.EventMgr.AddListener<int>("TestEvent", OnTestEvent2);
    }

    private void OnTestEvent2(int obj)
    {
        LWDebug.Log($"OnTestEvent2 {obj}");
    }

    private void OnTestEvent1(int obj)
    {
        LWDebug.Log($"OnTestEvent1 {obj}");
    }


    /// <summary>
    /// 默认资源更新完成
    /// </summary>
    /// <param name="obj"></param>
    private async void OnUpdateCallback(bool obj)
    {

    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            ManagerUtility.AssetsMgr.InstantiateAsync("Assets/0Res/Prefabs/Cube.prefab", null);

        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            //GameObject go = ManagerUtility.AssetsMgr.LoadAsset<GameObject>("Assets/0Res/Prefabs/Cube.prefab");
            ManagerUtility.AssetsMgr.Instantiate("Assets/0Res/Prefabs/Cube.prefab", null);

        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            ManagerUtility.EventMgr.DispatchEvent("TestEvent", 100);
        }
        if (Input.GetKeyDown(KeyCode.Print))
        {
            //生成截图
            ScreenCapture.CaptureScreenshot("Assets/0Res/Screenshots/Test.png");
        }
        MainManager.Instance.Update();
    }


    void OnDestroy()
    {
        // ManagerUtility.HotfixMgr.Destroy();
        MainManager.Instance.ClearManager();
        //SqliteHelp.Instance.Close();
    }



}
