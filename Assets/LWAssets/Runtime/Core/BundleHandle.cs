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
        private readonly string _bundleName;
        private AssetBundle _bundle;
        private readonly List<BundleHandle> _dependencies = new List<BundleHandle>();

        private bool _isDone;
        private float _progress;

        private bool _unloadAllLoadedObjectsOnDispose = true;
        
        public string BundleName => _bundleName;
        public AssetBundle Bundle => _bundle;
        public override bool IsValid => _bundle != null;
        public override bool IsDone => _isDone;
        public override float Progress => _progress;
        public IReadOnlyList<BundleHandle> Dependencies => _dependencies;
        
        public BundleHandle(string bundleName) : base(bundleName)
        {
            _bundleName = bundleName;
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
                dependency.Retain();
            }
        }

        /// <summary>
        /// 卸载Bundle并释放依赖（unloadAllLoadedObjects 对应 AssetBundle.Unload 参数）
        /// </summary>
        public void Dispose(bool unloadAllLoadedObjects)
        {
            _unloadAllLoadedObjectsOnDispose = unloadAllLoadedObjects;
            Dispose();
        }

        protected override void OnDispose()
        {
            foreach (var dep in _dependencies)
            {
                dep.Release();
            }
            _dependencies.Clear();

            if (_bundle != null)
            {
                _bundle.Unload(_unloadAllLoadedObjectsOnDispose);
                _bundle = null;
            }
        }
    }
}
