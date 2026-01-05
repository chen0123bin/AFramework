
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
        //LWDebug.SetLogConfig(LWUtility.GlobalConfig.lwGuiLog, LWUtility.GlobalConfig.logLevel, LWUtility.GlobalConfig.writeLog);

        MainManager.Instance.Init();
        //添加各种管理器      

        MainManager.Instance.AddManager(typeof(IAssetsManager).ToString(), new LWAssetsManager());

        await ManagerUtility.AssetsMgr.InitializeAsync();
        MainManager.Instance.MonoBehaviour = this;


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
        MainManager.Instance.Update();
    }


    void OnDestroy()
    {
        // ManagerUtility.HotfixMgr.Destroy();
        MainManager.Instance.ClearManager();
        //SqliteHelp.Instance.Close();
    }



}
