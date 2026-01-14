using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LWFMS;
using LWCore;
using Cysharp.Threading.Tasks;

[FSMTypeAttribute("Procedure", false)]
public class StartProcedure : BaseFSMState
{
    public override void OnInit()
    {

    }

    public override void OnEnter(BaseFSMState lastState)
    {
        ManagerUtility.EventMgr.AddListener<int>("TestEvent", OnTestEvent1);
        ManagerUtility.EventMgr.AddListener<int>("TestEvent", OnTestEvent2);
        ManagerUtility.UIMgr.OpenView<TestView>();

    }
    public override void OnLeave(BaseFSMState nextState)
    {
        ManagerUtility.EventMgr.RemoveListener<int>("TestEvent", OnTestEvent1);
        ManagerUtility.EventMgr.RemoveListener<int>("TestEvent", OnTestEvent2);
    }

    public override void OnTermination()
    {

    }

    public override void OnUpdate()
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

    }
    private void OnTestEvent2(int obj)
    {
        LoadScene2Async().Forget();
        LWDebug.Log($"OnTestEvent2 {obj}");
    }

    private async void OnTestEvent1(int obj)
    {
        LWDebug.Log($"OnTestEvent1 {obj}");
        string text = await ManagerUtility.AssetsMgr.LoadRawFileTextAsync("Assets/0Res/RawFiles/333.txt");
        LWDebug.Log("RAWFile" + text);
    }

    /// <summary>
    /// 加载场景示例
    /// </summary>
    private async UniTaskVoid LoadScene2Async()
    {
        Debug.Log("LoadScene2Async");
        await ManagerUtility.MainMgr.LoadScene("Assets/0Res/Scenes2/Test2.unity");
    }

}
