using System.Collections;
using System.Collections.Generic;
using LWCore;
using UnityEngine;
using UnityEngine.Serialization;

namespace LWAssets
{
    /// <summary>
    /// 自动在销毁时释放资源
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class AutoReleaseOnDestroy : MonoBehaviour
    {
        [HideInInspector]
        [FormerlySerializedAs("Path")] public string m_Path;
        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(m_Path))
            {
                ManagerUtility.AssetsMgr.Release(m_Path);
            }
        }
    }
}
