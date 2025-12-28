using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace LWAssets
{
    /// <summary>
    /// Bundle句柄
    /// </summary>
    public class BundleHandle : IDisposable
    {
        private readonly string _bundleName;
        private AssetBundle _bundle;
        private int _referenceCount;
        private bool _isDisposed;
        private readonly List<BundleHandle> _dependencies = new List<BundleHandle>();
        
        public string BundleName => _bundleName;
        public AssetBundle Bundle => _bundle;
        public int ReferenceCount => _referenceCount;
        public bool IsDisposed => _isDisposed;
        public bool IsValid => _bundle != null;
        public IReadOnlyList<BundleHandle> Dependencies => _dependencies;
        
        public BundleHandle(string bundleName)
        {
            _bundleName = bundleName;
            _referenceCount = 0;
        }
        
        internal void SetBundle(AssetBundle bundle)
        {
            _bundle = bundle;
        }
        
        internal void AddDependency(BundleHandle dependency)
        {
            if (dependency != null && !_dependencies.Contains(dependency))
            {
                _dependencies.Add(dependency);
                dependency.Retain();
            }
        }
        
        public void Retain()
        {
            if (_isDisposed)
            {
                Debug.LogWarning($"[LWAssets] Cannot retain disposed bundle: {_bundleName}");
                return;
            }
            _referenceCount++;
        }
        
        public void Release()
        {
            if (_isDisposed) return;
            
            _referenceCount--;
            if (_referenceCount <= 0)
            {
                Dispose();
            }
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            
            _isDisposed = true;
            
            // 释放依赖
            foreach (var dep in _dependencies)
            {
                dep.Release();
            }
            _dependencies.Clear();
            
            // 卸载Bundle
            if (_bundle != null)
            {
                _bundle.Unload(true);
                _bundle = null;
            }
        }
    }
}
