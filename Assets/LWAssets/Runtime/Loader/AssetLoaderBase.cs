using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        protected readonly Dictionary<string, BundleHandle> _bundleHandleCache = new Dictionary<string, BundleHandle>();
        // 资源引用计数
        protected readonly Dictionary<string, HandleBase> _handleBaseCache = new Dictionary<string, HandleBase>();
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

        // public virtual AssetHandle<T> LoadAssetWithHandle<T>(string assetPath) where T : UnityEngine.Object
        // {
        //     return LoadAssetWithHandleAsync<T>(assetPath).GetAwaiter().GetResult();
        // }

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
        protected async UniTask<BundleHandle> LoadBundleAsync(string bundleName, CancellationToken cancellationToken = default, bool isDepend = false)
        {
            // 检查缓存
            lock (_lockObj)
            {
                if (_bundleHandleCache.TryGetValue(bundleName, out var cached))
                {
                    if (cached == null || cached.IsDisposed || !cached.IsValid)
                    {
                        _bundleHandleCache.Remove(bundleName);
                    }
                    else
                    {                
                        //如果缓存的Bundle通过依赖加载进缓存，但是当前请求是不是加载，那么需要新增引用
                        if(cached.IsDependLoad && !isDepend)
                        {
                            cached.IsDependLoad = isDepend;
                            cached.Retain();
                        }       
                        return cached;
                    }
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
            var task = LoadBundleInternalAsync(bundleName, cancellationToken, isDepend);

            lock (_lockObj)
            {
                _loadingBundles[bundleName] = task;
            }

            try
            {
                var bundleHandle = await task;
                return bundleHandle;
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
            CancellationToken cancellationToken = default, bool isDepend = false)
        {
            var bundleInfo = _manifest.GetBundleInfo(bundleName);
            if (bundleInfo == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Bundle not found: {bundleName}");
                return null;
            }

            var sw = Stopwatch.StartNew();
            var bundleHandle = new BundleHandle(bundleName);

            // 先加载依赖
            foreach (var depName in bundleInfo.Dependencies)
            {
                var depBundleHandle = await LoadBundleAsync(depName,cancellationToken, true);
                bundleHandle.AddDependency(depBundleHandle);
                depBundleHandle.Retain();
            }
            // 加载Bundle
            var bundle = await LoadBundleFromSourceAsync(bundleInfo, cancellationToken);
            sw.Stop();
            bundleHandle.IsDependLoad = isDepend;
            bundleHandle.SetBundle(bundle);
            bundleHandle.SetLoadInfo(bundleInfo.Size, sw.Elapsed.TotalMilliseconds);
            if (!isDepend)
            {               
                bundleHandle.Retain(); 
            }

            lock (_lockObj)
            {
                _bundleHandleCache[bundleName] = bundleHandle;
            }

            return bundleHandle;
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
                var info = _handleBaseCache.FirstOrDefault(x => x.Value is AssetHandle ah && ah.AssetObject == asset);
                ReleaseAssetLocked(info.Key);
            }
        }

        public virtual void Release(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return;

            lock (_lockObj)
            {
                ReleaseAssetLocked(assetPath);
            }
        }
        /// <summary>
        /// 强制释放指定资源
        /// </summary>
        /// <param name="assetPath"></param>
        public virtual void ForceReleaseAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return;

            lock (_lockObj)
            {
                ReleaseAssetLocked(assetPath, true);
            }
        }

        /// <summary>
        /// 强制卸载指定Bundle（调试工具使用）
        /// </summary>
        public virtual void ForceUnloadBundle(string bundleName, bool unloadAllLoadedObjects = true)
        {
            if (string.IsNullOrEmpty(bundleName)) return;

            lock (_lockObj)
            {
                if (_bundleHandleCache.TryGetValue(bundleName, out var bundleHandle))
                {
                    var toRemove = _handleBaseCache
                        .Where(kvp => kvp.Value != null && kvp.Value.BundleName == bundleName)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in toRemove)
                    {
                        ReleaseAssetLocked(key, true);
                    }

                    if (bundleHandle != null)
                    {
                        ReleaseBundleLocked(bundleHandle.BundleName, true, unloadAllLoadedObjects);
                    }
                }
            }
        }
        public virtual async UniTask UnloadUnusedAssetsAsync()
        {
            lock (_lockObj)
            {
                var toRemove = new List<string>();

                foreach (var kvp in _bundleHandleCache)
                {
                    if (kvp.Value == null)
                    {
                        toRemove.Add(kvp.Key);
                        continue;
                    }

                    if (kvp.Value.IsDisposed || !kvp.Value.IsValid || kvp.Value.RefCount <= 0)
                    {
                        kvp.Value.Dispose();
                        toRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in toRemove)
                {
                    _bundleHandleCache.Remove(key);
                }
            }
    
            await UniTask.Yield();
        }

        public virtual void ForceUnloadAll()
        {
            var toRemove = _handleBaseCache.Values.ToList();
            for (int i = 0; i < toRemove.Count; i++)
            {
                var handle = toRemove[i];
                ReleaseAssetLocked(handle.Path,true);
            }
        }

        public virtual void Dispose()
        {
            ForceUnloadAll();
        }

        #endregion

        private void ReleaseAssetLocked(string assetPath, bool force = false)
        {
            if (string.IsNullOrEmpty(assetPath)) return;

            if (_handleBaseCache.TryGetValue(assetPath, out var handleBase) )
            {
               
                if (force)
                {
                    while (handleBase.RefCount > 0)
                    {
                        handleBase.Release();
                    }
                }
                else
                {
                    handleBase.Release();
                }
                
                if (handleBase.RefCount <= 0 || handleBase.IsDisposed || !handleBase.IsValid)
                {
                    _handleBaseCache.Remove(assetPath);
                    ReleaseBundleLocked(handleBase.BundleName);
                }

              
            }
        }

      

        private void ReleaseBundleLocked(string bundleName, bool force = false, bool unloadAllLoadedObjects = true)
        {
            if (string.IsNullOrEmpty(bundleName)) return;

            if (_bundleHandleCache.TryGetValue(bundleName, out var bundleHandle) && bundleHandle != null)
            {
                bundleHandle.UnloadAllLoadedObjectsOnDispose = unloadAllLoadedObjects;

                if (force)
                {
                    while (bundleHandle.RefCount > 0)
                    {
                        if (bundleHandle.RefCount == 1)
                        {
                            foreach (var dep in bundleHandle.Dependencies)
                            {
                                ReleaseBundleLocked(dep.BundleName, false, unloadAllLoadedObjects);
                            }
                        }
                        bundleHandle.Release();
                    }
                }
                else
                {
                    if (bundleHandle.RefCount == 1)
                    {
                        foreach (var dep in bundleHandle.Dependencies)
                        {
                            ReleaseBundleLocked(dep.BundleName, false, unloadAllLoadedObjects);
                        }
                    }
                    bundleHandle.Release();
                }
                if (bundleHandle.IsDisposed || !bundleHandle.IsValid || bundleHandle.RefCount <= 0)
                {
                    
                    _bundleHandleCache.Remove(bundleName);
                }
            }
        }

        /// <summary>
        /// 记录资源引用（带加载耗时）
        /// </summary>
        protected void TrackAssetHandle(string assetPath, UnityEngine.Object asset, string bundleName, double loadTimeMs)
        {
            if (asset == null) return;

            lock (_lockObj)
            {
                if (_handleBaseCache.TryGetValue(assetPath, out var handle) && handle != null)
                {
                    handle.Retain();
                }
                else
                {
                    handle = new AssetHandle(assetPath)
                    {
                        BundleName = bundleName,
                    };
                    ((AssetHandle)handle).SetAssetObject(asset, bundleName, loadTimeMs);
                    handle.Retain();
                    _handleBaseCache[assetPath] = handle;
                }
            }
        }

    }
}
