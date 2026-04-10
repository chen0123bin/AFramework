using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LWFMS;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWCore
{
    /// <summary>
    /// 非热更环境主管理器
    /// </summary>
    public class MainManager : Singleton<MainManager>, IManager
    {
        /// <summary>
        /// 外部设置第一个启动的状态
        /// </summary>
        public Type FirstFSMState { set => m_FirstFSMState = value; }

        /// <summary>
        /// 设置一个默认的MonoBehaviour脚本
        /// </summary>
        public MonoBehaviour MonoBehaviour { set => m_MonoBehaviour = value; }

        private Type m_FirstFSMState;
        private MonoBehaviour m_MonoBehaviour;
        private Dictionary<string, IManager> m_ManagerDic;
        private List<IManager> m_ManagerList;

        /// <summary>
        /// 创建主管理器实例。
        /// </summary>
        public MainManager()
        {
        }

        /// <summary>
        /// 注册并初始化一个 Manager。
        /// </summary>
        /// <param name="type">Manager 对应的接口键。</param>
        /// <param name="manager">Manager 实例。</param>
        public void AddManager(string type, IManager manager)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentException("注册 Manager 时 type 不能为空", nameof(type));
            }

            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager), "注册 Manager 时实例不能为空");
            }

            if (m_ManagerDic.ContainsKey(type))
            {
                throw new InvalidOperationException("重复注册 Manager: " + type);
            }

            manager.Init();
            m_ManagerDic.Add(type, manager);
            m_ManagerList.Add(manager);
        }

        /// <summary>
        /// 清空所有已注册的 Manager。
        /// </summary>
        public void ClearManager()
        {
            if (m_ManagerDic == null || m_ManagerDic.Count <= 0)
            {
                return;
            }

            TerminateProcedureIfNeeded();
            m_ManagerDic.Clear();
            m_ManagerList.Clear();
            m_FirstFSMState = null;
            m_MonoBehaviour = null;
        }

        /// <summary>
        /// 尝试获取指定接口对应的 Manager。
        /// </summary>
        /// <typeparam name="T">Manager 接口类型。</typeparam>
        /// <param name="manager">返回的 Manager 实例。</param>
        /// <returns>是否获取成功。</returns>
        public bool TryGetManager<T>(out T manager) where T : class
        {
            manager = default;
            if (m_ManagerDic == null)
            {
                return false;
            }

            string typeStr = typeof(T).ToString();
            IManager managerInstance;
            if (!m_ManagerDic.TryGetValue(typeStr, out managerInstance))
            {
                return false;
            }

            manager = managerInstance as T;
            return manager != null;
        }

        /// <summary>
        /// 获取指定接口对应的 Manager。
        /// </summary>
        /// <typeparam name="T">Manager 接口类型。</typeparam>
        /// <returns>Manager 实例，找不到时返回 null。</returns>
        public T GetManager<T>() where T : class
        {
            T manager;
            if (TryGetManager<T>(out manager))
            {
                return manager;
            }

            Debug.LogWarning(typeof(T) + " 这个Manager 不存在，请先检查是否主动添加过Manager");
            return default;
        }

        /// <summary>
        /// 初始化主管理器内部容器。
        /// </summary>
        public void Init()
        {
            m_ManagerDic = new Dictionary<string, IManager>(StringComparer.Ordinal);
            m_ManagerList = new List<IManager>();
        }

        /// <summary>
        /// 逐帧驱动所有已注册的 Manager。
        /// </summary>
        public void Update()
        {
            if (m_ManagerList == null)
            {
                return;
            }

            for (int i = 0; i < m_ManagerList.Count; i++)
            {
                IManager manager = m_ManagerList[i];
                if (manager == null)
                {
                    continue;
                }

                try
                {
                    manager.Update();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        /// <summary>
        /// 启动流程管理
        /// </summary>
        public void StartProcedure()
        {
            IFSMManager fsmManager = GetManager<IFSMManager>();
            if (fsmManager == null)
            {
                LWDebug.LogError("未找到 IFSMManager，无法启动流程管理");
                return;
            }

            fsmManager.InitFSMManager();
            List<TypeAttr> procedureList = fsmManager.GetFsmClassDataByName(nameof(FSMName.Procedure));
            if (procedureList == null || procedureList.Count <= 0)
            {
                LWDebug.LogWarning("未找到第一个Procedure");
                return;
            }

            FSMStateMachine stateMachine = new FSMStateMachine(nameof(FSMName.Procedure), procedureList);
            fsmManager.RegisterFSM(stateMachine);

            FSMStateMachine procedureStateMachine = fsmManager.GetFSMProcedure();
            if (procedureStateMachine == null)
            {
                LWDebug.LogError("流程状态机创建失败，无法进入首个 Procedure");
                return;
            }

            if (m_FirstFSMState != null)
            {
                procedureStateMachine.SwitchState(m_FirstFSMState.Name);
            }
            else
            {
                procedureStateMachine.StartFirst();
            }
        }

        /// <summary>
        /// 启动Unity协程
        /// </summary>
        /// <param name="enumerator">协程枚举器。</param>
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

        /// <summary>
        /// 加载场景示例（带UI）
        /// </summary>
        /// <param name="scenePath">场景路径。</param>
        /// <param name="loadSceneMode">加载模式。</param>
        public async UniTask LoadSceneWithUI(string scenePath, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            Debug.Log("LoadScene2Async");
            ManagerUtility.UIMgr.OpenLoadingBar("场景加载中...", true);
            System.Progress<float> progress = new System.Progress<float>(p =>
            {
                ManagerUtility.UIMgr.UpdateLoadingBar(p, "场景加载中...", true);
            });

            try
            {
                await ManagerUtility.AssetsMgr.LoadSceneAsync(scenePath, loadSceneMode, true, progress);
            }
            finally
            {
                ManagerUtility.UIMgr.CloseLoadingBar();
            }
        }

        /// <summary>
        /// 退出游戏或停止编辑器播放。
        /// </summary>
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// 确保内部容器已经完成初始化。
        /// </summary>
        private void EnsureInitialized()
        {
            if (m_ManagerDic == null || m_ManagerList == null)
            {
                Init();
            }
        }

        /// <summary>
        /// 在清理前安全终止流程状态机。
        /// </summary>
        private void TerminateProcedureIfNeeded()
        {
            IFSMManager fsmManager;
            if (!TryGetManager<IFSMManager>(out fsmManager) || fsmManager == null)
            {
                return;
            }

            FSMStateMachine procedureStateMachine = fsmManager.GetFSMProcedure();
            if (procedureStateMachine != null)
            {
                procedureStateMachine.TerminationFMS();
            }
        }
    }

    /// <summary>
    /// 带特性的类型信息。
    /// </summary>
    public class TypeAttr
    {
        public Attribute attr;
        public Type type;
    }
}
