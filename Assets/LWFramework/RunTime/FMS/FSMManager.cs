using LWCore;
using System.Collections.Generic;
using UnityEngine;

namespace LWFMS
{
    /// <summary>
    /// 有限状态机管理者
    /// </summary>
    public sealed class FSMManager : IManager, IFSMManager
    {
        //private Dictionary<string, FSMStateMachine> _fsms = new Dictionary<string, FSMStateMachine>();
        private Dictionary<string, List<TypeAttr>> _fsmStateList = new Dictionary<string, List<TypeAttr>>();
        private List<FSMStateMachine> _fsms = new List<FSMStateMachine>();
        /// <summary>
        /// 注册状态机
        /// </summary>
        /// <param name="fsm">状态机</param>
        public void RegisterFSM(FSMStateMachine fsm)
        {
            if (!_fsms.Contains(fsm))
            {
                _fsms.Add(fsm);
            }
            else
            {
                LWDebug.Log("当前已经存在这个FMS " + fsm.Name);
            }
        }

        /// <summary>
        /// 移除已注册的状态机
        /// </summary>
        /// <param name="fsm">状态机</param>
        public void UnRegisterFSM(FSMStateMachine fsm)
        {
            if (_fsms.Contains(fsm))
            {
                fsm.TerminationFMS();
                _fsms.Remove(fsm);
            }
            else
            {
                LWDebug.Log("不存在这个FMS " + fsm.Name);
            }
        }

        /// <summary>
        /// 通过名称获取状态机
        /// </summary>
        /// <param name="name">状态机名称</param>
        /// <returns>状态机</returns>
        public FSMStateMachine GetFSMByName(string name)
        {
            return _fsms.Find((find) => find.Name == name);

        }
        /// <summary>
        /// 获取流程状态机
        /// </summary>
        /// <returns></returns>
        public FSMStateMachine GetFSMProcedure()
        {
            return GetFSMByName(nameof(FSMName.Procedure));
        }

        /// <summary>
        /// 是否存在指定的状态机
        /// </summary>
        /// <param name="name">状态机名称</param>
        /// <returns>是否存在</returns>
        public bool IsExistFSM(string name)
        {
            return _fsms.FindIndex((find) => find.Name == name) >= 0;
        }
        /// <summary>
        /// 通过名称去获取分类的ClassData
        /// </summary>
        /// <param name="fsmName"></param>
        /// <returns></returns>
        public List<TypeAttr> GetFsmClassDataByName(string fsmName)
        {
            return _fsmStateList[fsmName];
        }
        /// <summary>
        /// 初始化状态机
        /// </summary>
        public void InitFSMManager()
        {
            _fsmStateList.Clear();
            //找到所有的流程管理类
            List<TypeAttr> classDataList = ManagerUtility.HotfixMgr.GetAttrTypeDataList<FSMTypeAttribute>();
            for (int i = 0; i < classDataList.Count; i++)
            {
                string fsmName = (classDataList[i].attr as FSMTypeAttribute).FSMName;
                if (!_fsmStateList.ContainsKey(fsmName))
                {
                    _fsmStateList.Add(fsmName, new List<TypeAttr>());
                }
                _fsmStateList[fsmName].Add(classDataList[i]);
            }
        }

        public void Update()
        {
            for (int i = 0; i < _fsms.Count; i++)
            {
                _fsms[i].Update();
            }
            if (Input.GetKeyDown(KeyCode.P) && Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt))
            {
                Debug.Log(GetFSMProcedure().CurrentState);
            }
        }

        public void Init()
        {
        }
    }
}