using System.Collections.Generic;

namespace LWStep
{
    /// <summary>
    /// 步骤节点
    /// </summary>
    public class StepNode
    {
        public string Id { get; private set; }
        public string Name { get; private set; }

        private List<BaseStepAction> m_Actions;
        private int m_CurrentActionIndex;
        private bool m_IsEntered;

        /// <summary>
        /// 创建节点
        /// </summary>
        public StepNode(string id, string name)
        {
            Id = id;
            Name = name;
            m_Actions = new List<BaseStepAction>();
            m_CurrentActionIndex = 0;
            m_IsEntered = false;
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
            if (m_Actions.Count > 0)
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

            BaseStepAction currentAction = m_Actions[m_CurrentActionIndex];
            currentAction.Update();
            if (currentAction.IsFinished)
            {
                currentAction.Exit();
                m_CurrentActionIndex += 1;
                isActionChanged = true;
                if (m_CurrentActionIndex < m_Actions.Count)
                {
                    m_Actions[m_CurrentActionIndex].Enter();
                }
            }
            return m_CurrentActionIndex >= m_Actions.Count;
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
        }

        /// <summary>
        /// 快速应用剩余动作（用于前进/跳转前补齐）
        /// </summary>
        public void ApplyRemaining(StepContext context)
        {
            BindContext(context);
            for (int i = m_CurrentActionIndex; i < m_Actions.Count; i++)
            {
                BaseStepAction action = m_Actions[i];
                action.Reset();
                action.Apply();
            }
            m_CurrentActionIndex = m_Actions.Count;
        }

        /// <summary>
        /// 离开节点（安全退出当前动作）
        /// </summary>
        public void Leave()
        {
            if (m_CurrentActionIndex >= 0 && m_CurrentActionIndex < m_Actions.Count)
            {
                m_Actions[m_CurrentActionIndex].Exit();
            }
            m_IsEntered = false;
        }

        /// <summary>
        /// 获取当前动作名称
        /// </summary>
        public string GetCurrentActionName()
        {
            if (m_CurrentActionIndex >= 0 && m_CurrentActionIndex < m_Actions.Count)
            {
                return m_Actions[m_CurrentActionIndex].GetType().Name;
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
        }
    }
}
