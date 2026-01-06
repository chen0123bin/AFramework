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
    public class AutoReleaseOnDestroy : MonoBehaviour
    {
        [HideInInspector, FormerlySerializedAs("Path")]
        public string m_Path;
        void Start()
        {
            LWDebug.Log("Start");
        }
        private void OnDestroy()
        {
            LWDebug.Log("OnDestroy");
            if (!string.IsNullOrEmpty(m_Path) && ManagerUtility.AssetsMgr != null)
            {
                ManagerUtility.AssetsMgr.Release(m_Path);
            }
        }
    }
}
