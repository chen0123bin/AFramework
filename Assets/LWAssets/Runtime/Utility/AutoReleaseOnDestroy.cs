using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LWAssets
{
    /// <summary>
    /// 自动在销毁时释放资源
    /// </summary>
    public class AutoReleaseOnDestroy : MonoBehaviour
    {
        [HideInInspector]
        public string Path;
        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(Path))
            {
                LWAssets.Release(Path);
            }
        }
    }
}
