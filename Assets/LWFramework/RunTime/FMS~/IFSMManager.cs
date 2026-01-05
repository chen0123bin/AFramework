using System.Collections.Generic;
using LWCore;

namespace LWFMS
{
    public interface IFSMManager
    {
        /// <summary>
        /// 通过名称获取状态机
        /// </summary>
        /// <param name="name">状态机名称</param>
        /// <returns>状态机</returns>
        FSMStateMachine GetFSMByName(string name);
        /// <summary>
        /// 通过名称去获取分类的ClassData
        /// </summary>
        /// <param name="fsmName"></param>
        /// <returns></returns>
        List<TypeAttr> GetFsmClassDataByName(string fsmName);
        /// <summary>
        /// 获取流程状态机
        /// </summary>
        /// <returns></returns>
        FSMStateMachine GetFSMProcedure();

        /// <summary>
        /// 初始化状态机
        /// </summary>
        void InitFSMManager();
        /// <summary>
        /// 是否存在指定的状态机
        /// </summary>
        /// <param name="name">状态机名称</param>
        /// <returns>是否存在</returns>
        bool IsExistFSM(string name);
        /// <summary>
        /// 注册状态机
        /// </summary>
        /// <param name="fsm">状态机</param>
        void RegisterFSM(FSMStateMachine fsm);
        /// <summary>
        /// 移除已注册的状态机
        /// </summary>
        /// <param name="fsm">状态机</param>
        void UnRegisterFSM(FSMStateMachine fsm);
    }
}