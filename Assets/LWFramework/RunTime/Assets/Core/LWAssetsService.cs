using System;

namespace LWAssets
{
    /// <summary>
    /// LWAssets 默认服务入口（用于在项目中提供一个全局 IAssetsManager 实例）
    /// </summary>
    public static class LWAssetsService
    {
        private static readonly object m_LockObj = new object();
        private static IAssetsManager m_Instance;

        /// <summary>
        /// 获取全局 IAssetsManager 实例（默认实现为 LWAssets）
        /// </summary>
        public static IAssetsManager Assets
        {
            get
            {
                if (m_Instance != null)
                    return m_Instance;

                lock (m_LockObj)
                {
                    if (m_Instance == null)
                        m_Instance = new LWAssetsManager();
                }

                return m_Instance;
            }
        }

        /// <summary>
        /// 设置全局 IAssetsManager 实例（用于注入自定义实现或测试替身）
        /// </summary>
        public static void Set(IAssetsManager assetsManager)
        {
            if (assetsManager == null)
                throw new ArgumentNullException(nameof(assetsManager));

            lock (m_LockObj)
            {
                m_Instance = assetsManager;
            }
        }
    }
}
