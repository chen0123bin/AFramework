using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWAssets
{
    /// <summary>
    /// Bundle句柄
    /// </summary>
    public class BundleHandle : HandleBase
    {
        private AssetBundle m_Bundle;
        private readonly List<BundleHandle> m_Dependencies = new List<BundleHandle>();
        internal bool UnloadAllLoadedObjectsOnDispose { get; set; }
        public AssetBundle Bundle => m_Bundle;
        public override bool IsValid => m_Bundle != null;
        public IReadOnlyList<BundleHandle> Dependencies => m_Dependencies;

        public BundleHandle(string bundleName) : base(bundleName)
        {
            BundleName = bundleName;
            m_Progress = 0f;
            m_IsDone = false;
        }

        internal void SetBundle(AssetBundle bundle)
        {
            m_Bundle = bundle;
            m_Progress = 1f;
            m_IsDone = true;
            InvokeComplete();
        }

        internal void AddDependency(BundleHandle dependency)
        {
            if (dependency != null && !m_Dependencies.Contains(dependency))
            {
                m_Dependencies.Add(dependency);
            }
        }

        protected override void OnDispose()
        {

            m_Dependencies.Clear();

            if (m_Bundle != null)
            {
                m_Bundle.Unload(UnloadAllLoadedObjectsOnDispose);
                m_Bundle = null;
            }
        }
    }
}
