
using LWAssets;
using LWFramework.Core;

namespace LWCore
{
    /// <summary>
    /// Manager工具类
    /// </summary>
    public class ManagerUtility
    {
        /// <summary>
        /// 获取主管理类
        /// </summary>
        public static MainManager MainMgr
        {
            get
            {
                return MainManager.Instance;
            }
        }
        /// <summary>
        /// 获取资源管理类
        /// </summary>
        public static IAssetsManager AssetsMgr
        {
            get
            {
                return MainManager.Instance.GetManager<IAssetsManager>();
            }
        }
        /// <summary>
        /// 获取事件管理类
        /// </summary>
        public static IEventManager EventMgr
        {
            get
            {
                return MainManager.Instance.GetManager<IEventManager>();
            }
        }
        /// <summary>
        /// 获取UI管理类
        /// </summary>
        public static IUIManager UIMgr
        {
            get
            {
                return MainManager.Instance.GetManager<IUIManager>();
            }
        }
        /// <summary>
        /// 获取热更管理类
        /// </summary>
        public static IHotfixManager HotfixMgr
        {
            get
            {
                return MainManager.Instance.GetManager<IHotfixManager>();
            }
        }
    }
}

