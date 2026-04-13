using LWCore;

namespace LWStep
{
    /// <summary>
    /// StepSystem 插件访问入口。
    /// </summary>
    public static class StepManagerUtility
    {
        /// <summary>
        /// 获取步骤管理器实例。
        /// </summary>
        public static IStepManager StepMgr
        {
            get
            {
                return MainManager.Instance.GetManager<IStepManager>();
            }
        }

        /// <summary>
        /// 尝试获取步骤管理器实例。
        /// </summary>
        /// <param name="stepManager">输出的步骤管理器实例。</param>
        /// <returns>是否获取成功。</returns>
        public static bool TryGetStepMgr(out IStepManager stepManager)
        {
            return MainManager.Instance.TryGetManager<IStepManager>(out stepManager);
        }
    }
}
