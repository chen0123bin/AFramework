using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LWFMS
{
    public abstract class BaseFSMState
    {
        private bool m_IsInit = false;
        private Dictionary<string, object> m_Param = new Dictionary<string, object>();
        public bool IsInit
        {
            get => m_IsInit; set => m_IsInit = value;
        }
        public Dictionary<string, object> Param
        {
            get => m_Param; set => m_Param = value;
        }
        /// <summary>
        /// 所属状态机
        /// </summary>
        public FSMStateMachine StateMachine;

        /// <summary>
        /// 状态初始化
        /// </summary>
        public abstract void OnInit();
        /// <summary>
        /// 进入状态
        /// </summary>
        /// <param name="lastState">上一个离开的状态</param>
        public abstract void OnEnter(BaseFSMState lastState);
        /// <summary>
        /// 离开状态
        /// </summary>
        /// <param name="nextState">下一个进入的状态</param>
        public abstract void OnLeave(BaseFSMState nextState);
        /// <summary>
        /// 状态帧刷新
        /// </summary>
        public abstract void OnUpdate();
        /// <summary>
        /// 终止状态
        /// </summary>
        public abstract void OnTermination();
    }

}
