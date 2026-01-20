using System.Collections.Generic;

namespace LWStep
{
    /// <summary>
    /// 上下文快照（用于回退恢复）
    /// </summary>
    public class StepContextSnapshot
    {
        private Dictionary<string, object> m_Data;

        /// <summary>
        /// 创建快照
        /// </summary>
        public StepContextSnapshot(Dictionary<string, object> data)
        {
            m_Data = new Dictionary<string, object>(data);
        }

        /// <summary>
        /// 恢复到上下文
        /// </summary>
        public void Restore(StepContext context)
        {
            context.Clear();
            foreach (KeyValuePair<string, object> kvp in m_Data)
            {
                context.SetValue(kvp.Key, kvp.Value);
            }
        }
    }
}

