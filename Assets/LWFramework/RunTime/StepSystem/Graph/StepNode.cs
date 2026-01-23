using System.Collections.Generic;

namespace LWStep
{
    public enum StepNodeMode
    {
        Serial = 0,
        Parallel = 1
    }

    /// <summary>
    /// 步骤节点
    /// </summary>
    public class StepNode
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public StepNodeMode Mode { get; private set; }

        private List<BaseStepAction> m_Actions;
        private int m_CurrentActionIndex;
        private bool m_IsEntered;
        private bool[] m_ActionExitStates;

        /// <summary>
        /// 创建节点
        /// </summary>
        public StepNode(string id, string name)
            : this(id, name, StepNodeMode.Serial)
        {
        }

        public StepNode(string id, string name, StepNodeMode mode)
        {
            Id = id;
            Name = name;
            Mode = mode;
            m_Actions = new List<BaseStepAction>();
            m_CurrentActionIndex = 0;
            m_IsEntered = false;
            m_ActionExitStates = null;
        }

        /// <summary>
        /// 添加动作
        /// </summary>
        public void AddAction(BaseStepAction action)
        {
            if (action == null)
            {
                return;
            }
            m_Actions.Add(action);
        }

        /// <summary>
        /// 进入节点（重置动作并进入首个动作）
        /// </summary>
        public void Enter(StepContext context)
        {
            BindContext(context);
            ResetActions();
            m_IsEntered = true;
            if (m_Actions.Count == 0)
            {
                return;
            }
            if (Mode == StepNodeMode.Parallel)
            {
                for (int i = 0; i < m_Actions.Count; i++)
                {
                    m_Actions[i].Enter();
                }
            }
            else
            {
                m_Actions[0].Enter();
            }
        }

        /// <summary>
        /// 更新节点（推进动作队列）
        /// </summary>
        public bool Update(out bool isActionChanged)
        {
            isActionChanged = false;
            if (!m_IsEntered)
            {
                return m_Actions.Count == 0;
            }
            if (m_CurrentActionIndex >= m_Actions.Count)
            {
                return true;
            }

            if (Mode == StepNodeMode.Parallel)
            {
                bool allFinished = true;
                for (int i = 0; i < m_Actions.Count; i++)
                {
                    BaseStepAction action = m_Actions[i];
                    if (!action.IsFinished)
                    {
                        action.Update();
                    }
                    if (action.IsFinished)
                    {
                        if (m_ActionExitStates != null && !m_ActionExitStates[i])
                        {
                            action.Exit();
                            m_ActionExitStates[i] = true;
                            isActionChanged = true;
                        }
                    }
                    else
                    {
                        allFinished = false;
                    }
                }
                if (allFinished)
                {
                    m_CurrentActionIndex = m_Actions.Count;
                }
                return allFinished;
            }
            else
            {
                BaseStepAction currentAction = m_Actions[m_CurrentActionIndex];
                currentAction.Update();
                if (currentAction.IsFinished)
                {
                    currentAction.Exit();
                    if (m_ActionExitStates != null && m_CurrentActionIndex < m_ActionExitStates.Length)
                    {
                        m_ActionExitStates[m_CurrentActionIndex] = true;
                    }
                    m_CurrentActionIndex += 1;
                    isActionChanged = true;
                    if (m_CurrentActionIndex < m_Actions.Count)
                    {
                        m_Actions[m_CurrentActionIndex].Enter();
                    }
                }
                return m_CurrentActionIndex >= m_Actions.Count;
            }
        }

        /// <summary>
        /// 快速应用节点（用于跳转补齐）
        /// </summary>
        public void Apply(StepContext context)
        {
            BindContext(context);
            for (int i = 0; i < m_Actions.Count; i++)
            {
                BaseStepAction action = m_Actions[i];
                action.Reset();
                action.Apply();
            }
            m_CurrentActionIndex = m_Actions.Count;
            m_IsEntered = true;
            if (m_ActionExitStates != null)
            {
                for (int i = 0; i < m_ActionExitStates.Length; i++)
                {
                    m_ActionExitStates[i] = true;
                }
            }
        }

        /// <summary>
        /// 快速应用剩余动作（用于前进/跳转前补齐）
        /// </summary>
        public void ApplyRemaining(StepContext context)
        {
            BindContext(context);
            if (Mode == StepNodeMode.Parallel)
            {
                for (int i = 0; i < m_Actions.Count; i++)
                {
                    BaseStepAction action = m_Actions[i];
                    if (action.IsFinished)
                    {
                        continue;
                    }
                    action.Reset();
                    action.Apply();
                    if (m_ActionExitStates != null)
                    {
                        m_ActionExitStates[i] = true;
                    }
                }
                m_CurrentActionIndex = m_Actions.Count;
            }
            else
            {
                for (int i = m_CurrentActionIndex; i < m_Actions.Count; i++)
                {
                    BaseStepAction action = m_Actions[i];
                    action.Reset();
                    action.Apply();
                }
                m_CurrentActionIndex = m_Actions.Count;
            }

        }

        /// <summary>
        /// 离开节点（安全退出当前动作）
        /// </summary>
        public void Leave()
        {
            if (Mode == StepNodeMode.Parallel)
            {
                for (int i = 0; i < m_Actions.Count; i++)
                {
                    if (m_ActionExitStates != null && m_ActionExitStates[i])
                    {
                        continue;
                    }
                    m_Actions[i].Exit();
                    if (m_ActionExitStates != null)
                    {
                        m_ActionExitStates[i] = true;
                    }
                }
            }
            else
            {
                if (m_CurrentActionIndex >= 0 && m_CurrentActionIndex < m_Actions.Count)
                {
                    m_Actions[m_CurrentActionIndex].Exit();
                    if (m_ActionExitStates != null && m_CurrentActionIndex < m_ActionExitStates.Length)
                    {
                        m_ActionExitStates[m_CurrentActionIndex] = true;
                    }
                }
            }
            m_IsEntered = false;
        }

        /// <summary>
        /// 获取当前动作名称
        /// </summary>
        public string GetCurrentActionName()
        {
            if (Mode == StepNodeMode.Parallel)
            {
                for (int i = 0; i < m_Actions.Count; i++)
                {
                    if (!m_Actions[i].IsFinished)
                    {
                        return m_Actions[i].GetType().Name;
                    }
                }
            }
            else
            {
                if (m_CurrentActionIndex >= 0 && m_CurrentActionIndex < m_Actions.Count)
                {
                    return m_Actions[m_CurrentActionIndex].GetType().Name;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取节点动作列表快照（用于基线捕获与恢复）
        /// </summary>
        public List<BaseStepAction> GetActionsSnapshot()
        {
            return new List<BaseStepAction>(m_Actions);
        }

        private void BindContext(StepContext context)
        {
            for (int i = 0; i < m_Actions.Count; i++)
            {
                m_Actions[i].SetContext(context);
            }
        }

        private void ResetActions()
        {
            for (int i = 0; i < m_Actions.Count; i++)
            {
                m_Actions[i].Reset();
            }
            m_CurrentActionIndex = 0;
            m_ActionExitStates = new bool[m_Actions.Count];
        }
    }
}
