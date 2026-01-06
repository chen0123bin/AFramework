using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LWCore
{
    /// <summary>
    /// 非热更环境主管理器
    /// </summary>
    public class MainManager : Singleton<MainManager>, IManager
    {  /// <summary>
       /// 外部设置第一个启动的状态
       /// </summary>
        public Type FirstFSMState { set => m_FirstFSMState = value; }
        /// <summary>
        /// 设置一个默认的MonoBehaviour脚本
        /// </summary>
        public MonoBehaviour MonoBehaviour { set => m_MonoBehaviour = value; }


        private Type m_FirstFSMState;
        private MonoBehaviour m_MonoBehaviour;




        //热更DLL中所有的type
        private List<Type> m_TypeHotfixList;
        //管理热更中的所有的Type
        private Dictionary<string, List<TypeAttr>> m_AttrTypeListDic;
        private Dictionary<string, IManager> m_ManagerDic;
        private List<IManager> m_ManagerList;

        public MainManager()
        {


        }

        public void AddManager(string type, IManager t)
        {
            t.Init();
            m_ManagerDic.Add(type, t);
            m_ManagerList.Add(t);
        }

        public void ClearManager()
        {
            //GetManager<IFSMManager>().GetFSMProcedure().TerminationFMS();
            if (m_ManagerDic != null && m_ManagerDic.Count > 0)
            {
                m_ManagerDic.Clear();
                m_ManagerList.Clear();
            }
        }

        public T GetManager<T>()
        {

            IManager manager = default;
            string typeStr = typeof(T).ToString();

            if (m_ManagerDic != null && m_ManagerDic.TryGetValue(typeStr, out manager))
            {
                return (T)manager;
            }
            else
            {
                Debug.LogWarning(typeStr + " 这个Manager 不存在，请先检查是否主动添加过Manager");
                return default;
            }
        }
        public void Init()
        {
            m_ManagerDic = new Dictionary<string, IManager>();
            m_ManagerList = new List<IManager>();
        }

        public void Update()
        {
            for (int i = 0; m_ManagerList != null && i < m_ManagerList.Count; i++)
            {
                m_ManagerList[i].Update();
            }

            //TimeHelp.realDeltaTime = Time.realtimeSinceStartup - TimeHelp.realtimeSinceLast;
            //TimeHelp.realtimeSinceLast = Time.realtimeSinceStartup;
        }


        /// <summary>
        /// 启动流程管理
        /// </summary>
        // public void StartProcedure()
        // {
        //     GetManager<IFSMManager>().InitFSMManager();
        //     //找到所有的流程管理类
        //     List<TypeAttr> procedureList = GetManager<IFSMManager>().GetFsmClassDataByName(nameof(FSMName.Procedure));
        //     if (procedureList.Count > 0)
        //     {
        //         //创建一个流程管理状态机       
        //         FSMStateMachine stateMachine = new FSMStateMachine(nameof(FSMName.Procedure), procedureList);
        //         GetManager<IFSMManager>().RegisterFSM(stateMachine);
        //         if (m_FirstFSMState != null)
        //         {
        //             GetManager<IFSMManager>().GetFSMProcedure().SwitchState(m_FirstFSMState.Name);
        //         }
        //         else
        //         {
        //             GetManager<IFSMManager>().GetFSMProcedure().StartFirst();
        //         }

        //     }
        //     else
        //     {
        //         LWDebug.LogWarning("未找到第一个Procedure");
        //     }

        // }

        /// <summary>
        /// 启动Unity协程
        /// </summary>
        /// <param name="enumerator"></param>
        public void StartCoroutine(IEnumerator enumerator)
        {
            if (m_MonoBehaviour != null)
            {
                m_MonoBehaviour.StartCoroutine(enumerator);
            }
            else
            {
                LWDebug.LogError($"{m_MonoBehaviour}为空，需要设置默认值，一般使用Startup脚本！！！");
            }

        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    public class TypeAttr
    {
        public Attribute attr;
        public Type type;
    }
}
