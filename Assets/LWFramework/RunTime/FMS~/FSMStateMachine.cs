using System;
using System.Collections.Generic;
using LWCore;
namespace LWFMS
{
    public class FSMStateMachine
    {
        public string Name { get; set; }
        private Dictionary<string, BaseFSMState> _stateDic = new Dictionary<string, BaseFSMState>();
        private BaseFSMState _currState;
        private BaseFSMState _firstState;
        public FSMStateMachine(string fmsName)
        {
            _stateDic = new Dictionary<string, BaseFSMState>();
            Name = fmsName;
        }
        public FSMStateMachine(string fmsName, List<TypeAttr> classDataList)
        {
            _stateDic = new Dictionary<string, BaseFSMState>();
            Name = fmsName;
            //根据Type 实例化状态
            for (int i = 0; i < classDataList.Count; i++)
            {
                //获取与当前名称一致的stateBase
                // FSMTypeAttribute attr = (FSMTypeAttribute)classDataList[i].attr;
                // if (attr.FSMName == fmsName)
                {
                    // BaseFSMState stateBase = ManagerUtility.HotfixMgr.Instantiate<BaseFSMState>(classDataList[i].type.ToString());//Activator.CreateInstance(classDataList[i].type) as BaseFSMState;
                    // stateBase.StateMachine = this;

                    // if (((FSMTypeAttribute)classDataList[i].attr).isFirst)
                    // {
                    //     _firstState = stateBase;

                    // }
                    //_stateDic.Add(classDataList[i].type.Name, stateBase);
                }
            }
        }
        public FSMStateMachine(string fmsName, List<BaseFSMState> list)
        {
            _stateDic = new Dictionary<string, BaseFSMState>();
            Name = fmsName;
            //根据Type 实例化状态
            for (int i = 0; i < list.Count; i++)
            {
                //获取与当前名称一致的stateBase
                _stateDic.Add(list[i].ToString(), list[i]);
            }
        }
        /// <summary>
        /// 帧更新
        /// </summary>
        public void Update()
        {
            if (_currState != null)
            {
                _currState.OnUpdate();
            }
        }
        /// <summary>
        /// 当前状态
        /// </summary>
        public BaseFSMState CurrentState
        {
            get
            {
                return _currState;
            }
        }
        /// <summary>
        /// IL模式下禁止不能使用切换状态 因为typeof 获取到的会是适配器
        /// </summary>
        /// <typeparam name="T">状态类型</typeparam>
        public void SwitchState<T>()
        {
            Type type = typeof(T);
            SwitchState(typeof(T).Name);
        }
        /// <summary>
        /// 切换状态
        /// </summary>
        /// <param name="type">状态类型</param>
        public void SwitchState(string typeName)
        {
            if (_stateDic.ContainsKey(typeName))
            {
                if (_currState == _stateDic[typeName])
                {
                    return;
                }

                BaseFSMState lastState = _currState;
                BaseFSMState nextState = _stateDic[typeName];
                if (lastState != null)
                {
                    lastState.OnLeave(nextState);
                }
                if (!nextState.IsInit)
                {
                    nextState.OnInit();
                    nextState.IsInit = true;
                }

                nextState.OnEnter(lastState);
                _currState = nextState;

            }
            else
            {

            }
        }
        /// <summary>
        /// 获取状态
        /// </summary>
        /// <param name="type">状态类型</param>
        /// <returns>状态实例</returns>
        public BaseFSMState GetState(string typeName)
        {
            if (_stateDic.ContainsKey(typeName))
            {
                return _stateDic[typeName];
            }
            else
            {
                LWDebug.LogError("当前状态不存在：：：" + typeName);
                return null;
            }
        }
        /// <summary>
        /// IL模式下禁止不能使用 获取状态
        /// </summary>
        /// <typeparam name="T">状态类型</typeparam>
        /// <returns>状态实例</returns>
        public T GetState<T>() where T : BaseFSMState
        {
            if (_stateDic.ContainsKey(typeof(T).Name))
            {
                return _stateDic[typeof(T).Name] as T;
            }
            else
            {
                LWDebug.LogError("当前状态不存在：：：" + typeof(T).Name);
                return default(T);
            }
        }
        /// <summary>
        /// 是否存在状态
        /// </summary>
        /// <param name="type">状态类型</param>
        /// <returns>是否存在</returns>
        public bool IsExistState(string typeName)
        {
            return _stateDic.ContainsKey(typeName);
        }
        //清空所有的状态
        public void ClearFMS()
        {

            _stateDic.Clear();

        }
        public void TerminationFMS()
        {
            foreach (var state in _stateDic)
            {
                state.Value.OnTermination();
            }


        }
        /// <summary>
        /// 启动默认状态
        /// </summary>
        public void StartFirst()
        {
            _currState = _firstState;
            _currState.OnEnter(null);
        }
    }
}


