namespace LWStep
{
    /// <summary>
    /// 步骤连线（有向边）
    /// </summary>
    public class StepEdge
    {
        public string FromId { get; private set; }
        public string ToId { get; private set; }
        public int Priority { get; private set; }
        public string Condition { get; private set; }
        public string Tag { get; private set; }

        /// <summary>
        /// 创建连线
        /// </summary>
        public StepEdge(string fromId, string toId, int priority, string condition, string tag)
        {
            FromId = fromId;
            ToId = toId;
            Priority = priority;
            Condition = condition;
            Tag = tag;
        }
    }
}

