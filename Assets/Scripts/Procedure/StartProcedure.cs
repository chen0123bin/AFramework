using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LWFMS;
using LWCore;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

[FSMTypeAttribute("Procedure", false)]
public class StartProcedure : BaseFSMState
{
    public override void OnInit()
    {

    }

    public override void OnEnter(BaseFSMState lastState)
    {
        base.OnEnter(lastState);
    }
    public override void OnLeave(BaseFSMState nextState)
    {
        base.OnLeave(nextState);
    }

    public override void OnTermination()
    {

    }

    public override void OnUpdate()
    {
        if (IsEnter)
        {
            ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<LoginProcedure>();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ManagerUtility.AssetsMgr.LoadSceneAsync("Assets/0Res/Scenes/Test.unity", LoadSceneMode.Additive).Forget();
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            ManagerUtility.AssetsMgr.InstantiateAsync("Assets/0Res/Prefabs/Cube.prefab").Forget();
        }
    }


}
