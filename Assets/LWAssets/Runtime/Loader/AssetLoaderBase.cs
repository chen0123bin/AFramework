using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWAssets
{
    /// <summary>
    /// 加载器基类
    /// </summary>
    public abstract class AssetLoaderBase : IAssetLoader
    {
        protected LWAssetsConfig _config;
        protected BundleManifest _manifest;
        
        // Bundle缓存
        protected readonly Dictionary<string, BundleHandle> _bundleCache = new Dictionary<string, BundleHandle>();
        // 资源引用计数
        protected readonly Dictionary<UnityEngine.Object, string> _assetRefMap = new Dictionary<UnityEngine.Object, string>();
        // 加载中的Bundle
        protected readonly Dictionary<string, UniTask<BundleHandle>> _loadingBundles = new Dictionary<string, UniTask<BundleHandle>>();
        
        protected readonly object _lockObj = new object();
        
        public AssetLoaderBase(LWAssetsConfig config)
        {
            _config = config;
        }
        
        public abstract UniTask InitializeAsync(BundleManifest manifest);
        
        #region 同步加载 - 默认实现（阻塞等待异步）
        
        public virtual T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            // 注意：同步加载在某些平台可能导致问题
            return LoadAssetAsync<T>(assetPath).GetAwaiter().GetResult();
        }
        
        public virtual AssetHandle<T> LoadAssetWithHandle<T>(string assetPath) where T : UnityEngine.Object
        {
            return LoadAssetWithHandleAsync<T>(assetPath).GetAwaiter().GetResult();
        }
        
        public virtual byte[] LoadRawFile(string assetPath)
        {
            return LoadRawFileAsync(assetPath).GetAwaiter().GetResult();
        }
        
        public virtual string LoadRawFileText(string assetPath)
        {
            return LoadRawFileTextAsync(assetPath).GetAwaiter().GetResult();
        }
        
        #endregion
        
        #region 异步加载
        
        public abstract UniTask<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken = default) 
            where T : UnityEngine.Object;
        
        public abstract UniTask<AssetHandle<T>> LoadAssetWithHandleAsync<T>(string assetPath, 
            CancellationToken cancellationToken = default) where T : UnityEngine.Object;
        
        public abstract UniTask<byte[]> LoadRawFileAsync(string assetPath, CancellationToken cancellationToken = default);
        
        public async UniTask<string> LoadRawFileTextAsync(string assetPath, CancellationToken cancellationToken = default)
        {
            var data = await LoadRawFileAsync(assetPath, cancellationToken);
            return data != null ? System.Text.Encoding.UTF8.GetString(data) : null;
        }
        
        public abstract UniTask<SceneHandle> LoadSceneAsync(string scenePath, LoadSceneMode mode, bool activateOnLoad,
            CancellationToken cancellationToken = default);
        
        #endregion
        
        #region Bundle加载
        
        /// <summary>
        /// 加载Bundle及其依赖
        /// </summary>
        protected async UniTask<BundleHandle> LoadBundleAsync(string bundleName, CancellationToken cancellationToken = default)
        {
            // 检查缓存
            lock (_lockObj)
            {
                if (_bundleCache.TryGetValue(bundleName, out var cached))
                {
                    cached.Retain();
                    return cached;
                }
            }
            
            // 检查是否正在加载
            UniTask<BundleHandle> loadingTask = default;
            bool isLoading = false;
            
            lock (_lockObj)
            {
                isLoading = _loadingBundles.TryGetValue(bundleName, out loadingTask);
            }
            
            if (isLoading)
            {
                return await loadingTask;
            }
            
            // 创建加载任务
            var task = LoadBundleInternalAsync(bundleName, cancellationToken);
            
            lock (_lockObj)
            {
                _loadingBundles[bundleName] = task;
            }
            
            try
            {
                var handle = await task;
                return handle;
            }
            finally
            {
                lock (_lockObj)
                {
                    _loadingBundles.Remove(bundleName);
                }
            }
        }
        
        /// <summary>
        /// 内部Bundle加载实现
        /// </summary>
        protected virtual async UniTask<BundleHandle> LoadBundleInternalAsync(string bundleName, 
            CancellationToken cancellationToken = default)
        {
            var bundleInfo = _manifest.GetBundleInfo(bundleName);
            if (bundleInfo == null)
            {
                Debug.LogError($"[LWAssets] Bundle not found: {bundleName}");
                return null;
            }
            
            var handle = new BundleHandle(bundleName);
            
            // 先加载依赖
            foreach (var depName in bundleInfo.Dependencies)
            {
                var depHandle = await LoadBundleAsync(depName, cancellationToken);
                handle.AddDependency(depHandle);
            }
            
            // 加载Bundle
            var bundle = await LoadBundleFromSourceAsync(bundleInfo, cancellationToken);
            handle.SetBundle(bundle);
            handle.Retain();
            
            lock (_lockObj)
            {
                _bundleCache[bundleName] = handle;
            }
            
            return handle;
        }
        
        /// <summary>
        /// 从源加载Bundle（子类实现具体逻辑）
        /// </summary>
        protected abstract UniTask<AssetBundle> LoadBundleFromSourceAsync(BundleInfo bundleInfo, 
            CancellationToken cancellationToken = default);
        
        #endregion
        
        #region 资源管理
        
        public virtual void Release(UnityEngine.Object asset)
        {
            if (asset == null) return;
            
            lock (_lockObj)
            {
                if (_assetRefMap.TryGetValue(asset, out var bundleName))
                {
                    _assetRefMap.Remove(asset);
                    
                    if (_bundleCache.TryGetValue(bundleName, out var handle))
                    {
                        handle.Release();
                    }
                }
            }
        }
        
        public virtual async UniTask UnloadUnusedAssetsAsync()
        {
            lock (_lockObj)
            {
                var toRemove = new List<string>();
                
                foreach (var kvp in _bundleCache)
                {
                    if (kvp.Value.ReferenceCount <= 0)
                    {
                        kvp.Value.Dispose();
                        toRemove.Add(kvp.Key);
                    }
                }
                
                foreach (var key in toRemove)
                {
                    _bundleCache.Remove(key);
                }
            }
            
            await UniTask.Yield();
        }
        
        public virtual void ForceUnloadAll()
        {
            lock (_lockObj)
            {
                foreach (var handle in _bundleCache.Values)
                {
                    handle.Dispose();
                }
                _bundleCache.Clear();
                _assetRefMap.Clear();
            }
        }
        
        public virtual void Dispose()
        {
            ForceUnloadAll();
        }
        
        #endregion
        
        #region 工具方法
        
        /// <summary>
        /// 记录资源引用
        /// </summary>
        protected void TrackAsset(UnityEngine.Object asset, string bundleName)
        {
            if (asset == null) return;
            
            lock (_lockObj)
            {
                _assetRefMap[asset] = bundleName;
            }
        }
        
        #endregion
    }
}
