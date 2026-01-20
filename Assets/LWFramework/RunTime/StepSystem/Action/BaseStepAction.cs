using System.Collections.Generic;

namespace LWStep
{
    public interface IStepBaselineStateAction
    {
        void CaptureBaselineState();
        void RestoreBaselineState();
    }

    /// <summary>
    /// 步骤动作基类（可继承扩展）
    /// </summary>
    public abstract class BaseStepAction
    {
        private bool m_IsFinished;
        private bool m_HasEntered;
        private bool m_HasExited;
        private StepContext m_Context;
        private Dictionary<string, string> m_Parameters;

        /// <summary>
        /// 动作是否完成
        /// </summary>
        public bool IsFinished
        {
            get { return m_IsFinished; }
        }

        /// <summary>
        /// 设置上下文
        /// </summary>
        public void SetContext(StepContext context)
        {
            m_Context = context;
        }

        /// <summary>
        /// 获取上下文
        /// </summary>
        protected StepContext GetContext()
        {
            return m_Context;
        }

        /// <summary>
        /// 设置动作参数
        /// </summary>
        public void SetParameters(Dictionary<string, string> parameters)
        {
            m_Parameters = parameters;
        }

        /// <summary>
        /// 获取动作参数
        /// </summary>
        protected Dictionary<string, string> GetParameters()
        {
            return m_Parameters;
        }

        /// <summary>
        /// 重置动作状态
        /// </summary>
        public void Reset()
        {
            m_IsFinished = false;
            m_HasEntered = false;
            m_HasExited = false;
        }

        /// <summary>
        /// 进入动作
        /// </summary>
        public void Enter()
        {
            if (m_HasEntered)
            {
                return;
            }
            m_HasEntered = true;
            OnEnter();
        }

        /// <summary>
        /// 更新动作
        /// </summary>
        public void Update()
        {
            if (m_IsFinished)
            {
                return;
            }
            OnUpdate();
        }

        /// <summary>
        /// 退出动作
        /// </summary>
        public void Exit()
        {
            if (m_HasExited)
            {
                return;
            }
            m_HasExited = true;
            OnExit();
        }

        /// <summary>
        /// 快速应用动作（用于跳转补齐）
        /// </summary>
        public void Apply()
        {
            if (!m_HasEntered)
            {
                Enter();
            }
            OnApply();
            if (!m_IsFinished)
            {
                Finish();
            }
            Exit();
        }

        /// <summary>
        /// 标记动作完成
        /// </summary>
        protected void Finish()
        {
            m_IsFinished = true;
        }

        /// <summary>
        /// 动作进入时调用
        /// </summary>
        protected abstract void OnEnter();

        /// <summary>
        /// 动作更新时调用
        /// </summary>
        protected abstract void OnUpdate();

        /// <summary>
        /// 动作退出时调用
        /// </summary>
        protected abstract void OnExit();

        /// <summary>
        /// 动作快速应用时调用
        /// </summary>
        protected abstract void OnApply();
    }
}
