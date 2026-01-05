
using LWAssets;

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


    }
}

