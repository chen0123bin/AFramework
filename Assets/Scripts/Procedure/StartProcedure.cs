using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LWFMS;
using LWCore;
using Cysharp.Threading.Tasks;

[FSMTypeAttribute("Procedure", false)]
public class StartProcedure : BaseFSMState
{
    private bool m_IsEnter = false;
    public override void OnInit()
    {

    }

    public override void OnEnter(BaseFSMState lastState)
    {
        m_IsEnter = true;


    }
    public override void OnLeave(BaseFSMState nextState)
    {
        m_IsEnter = false;
    }

    public override void OnTermination()
    {

    }

    public override void OnUpdate()
    {
        if (m_IsEnter)
        {
            ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<LoginProcedure>();
        }
    }


}
