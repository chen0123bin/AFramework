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
        private AssetBundle _bundle;
        private readonly List<BundleHandle> _dependencies = new List<BundleHandle>();
        internal bool UnloadAllLoadedObjectsOnDispose = false;
        public bool IsDependLoad = false;
        public AssetBundle Bundle => _bundle;
        public override bool IsValid => _bundle != null;
        public IReadOnlyList<BundleHandle> Dependencies => _dependencies;
        
        public BundleHandle(string bundleName) : base(bundleName)
        {
            BundleName = bundleName;
            _progress = 0f;
            _isDone = false;
        }
        
        internal void SetBundle(AssetBundle bundle)
        {
            _bundle = bundle;
            _progress = 1f;
            _isDone = true;
            InvokeComplete();
        }
        
        internal void AddDependency(BundleHandle dependency)
        {
            if (dependency != null && !_dependencies.Contains(dependency))
            {
                _dependencies.Add(dependency);              
            }
        }

        protected override void OnDispose()
        {
           
            _dependencies.Clear();

            if (_bundle != null)
            {
                _bundle.Unload(false);
                _bundle = null;
            }
        }
    }
}
