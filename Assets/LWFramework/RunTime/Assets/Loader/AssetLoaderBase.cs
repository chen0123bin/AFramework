using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        protected LWAssetsConfig m_Config;
        protected BundleManifest m_Manifest;

        // Bundle缓存
        protected readonly Dictionary<string, BundleHandle> m_BundleHandleCache = new Dictionary<string, BundleHandle>();
        // 资源引用计数
        protected readonly Dictionary<string, HandleBase> m_HandleBaseCache = new Dictionary<string, HandleBase>();
        // 加载中的Bundle
        protected readonly Dictionary<string, UniTask<BundleHandle>> m_LoadingBundles = new Dictionary<string, UniTask<BundleHandle>>();

        protected readonly object m_LockObj = new object();

        public AssetLoaderBase(LWAssetsConfig config)
        {
            m_Config = config;
        }

        public abstract UniTask InitializeAsync(BundleManifest manifest);


        #region 同步加载 - 默认实现（阻塞等待异步）
        /// <summary>
        /// 同步实例化资源
        /// </summary>
        /// <returns></returns>
        public virtual GameObject Instantiate(string assetPath, Transform parent = null)
        {
            var asset = LoadAsset<GameObject>(assetPath);
            if (asset == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Failed to load asset: {assetPath}");
                return null;
            }

            var instance = UnityEngine.Object.Instantiate(asset, parent);
            if (instance == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Failed to instantiate asset: {assetPath}");
                return null;
            }

            var releaseComp = instance.AddComponent<AutoReleaseOnDestroy>();
            releaseComp.m_Path = assetPath;
            return instance;
        }
        /// <summary>
        /// 同步加载资源    
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public virtual T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            throw new NotSupportedException($"[LWAssets] {GetType().Name} 不支持同步加载资源，请使用 LoadAssetAsync: {assetPath}");
        }

        /// <summary>
        /// 同步加载原始文件    
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public virtual byte[] LoadRawFile(string assetPath)
        {
            throw new NotSupportedException($"[LWAssets] {GetType().Name} 不支持同步加载原始文件，请使用 LoadRawFileAsync: {assetPath}");
        }
        /// <summary>
        /// 同步加载原始文件文本    
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public virtual string LoadRawFileText(string assetPath)
        {
            throw new NotSupportedException($"[LWAssets] {GetType().Name} 不支持同步加载原始文件文本，请使用 LoadRawFileTextAsync: {assetPath}");
        }

        #endregion

        #region 异步加载

        /// <summary>
        /// 异步加载资源    
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        /// <summary>
        /// 通用：通过清单定位Bundle，再从Bundle异步加载资源
        /// </summary>
        public virtual async UniTask<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken = default)
            where T : UnityEngine.Object
        {
            var bundleInfo = m_Manifest.GetBundleByAsset(assetPath);
            if (bundleInfo == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Asset not found in manifest: {assetPath}");
                return null;
            }

            var sw = Stopwatch.StartNew();

            var bundleHandle = await LoadBundleAsync(bundleInfo.BundleName, cancellationToken);
            if (bundleHandle == null || !bundleHandle.IsValid)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Failed to load bundle: {bundleInfo.BundleName}");
                return null;
            }

            var assetName = Path.GetFileNameWithoutExtension(assetPath);
            var request = bundleHandle.Bundle.LoadAssetAsync<T>(assetName);
            await request;

            var asset = request.asset as T;
            sw.Stop();

            if (asset != null)
            {
                TrackAssetHandle(assetPath, asset, bundleInfo.BundleName, sw.Elapsed.TotalMilliseconds);
            }

            return asset;
        }

        /// <summary>
        /// 异步加载原始文件    
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public abstract UniTask<byte[]> LoadRawFileAsync(string assetPath, CancellationToken cancellationToken = default);
        /// <summary>
        /// 异步加载原始文件文本    
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public async UniTask<string> LoadRawFileTextAsync(string assetPath, CancellationToken cancellationToken = default)
        {
            var data = await LoadRawFileAsync(assetPath, cancellationToken);
            return data != null ? System.Text.Encoding.UTF8.GetString(data) : null;
        }

        /// <summary>
        /// 通用：通过清单定位场景Bundle，确保Bundle加载后再切场景
        /// </summary>
        public virtual async UniTask<SceneHandle> LoadSceneAsync(string scenePath, LoadSceneMode mode, bool activateOnLoad,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            var sceneHandle = new SceneHandle(scenePath);
            var sw = Stopwatch.StartNew();
            try
            {
                progress?.Report(0f);
                var bundleInfo = m_Manifest.GetBundleByAsset(scenePath);
                if (bundleInfo == null)
                {
                    sceneHandle.SetError(new FileNotFoundException($"Scene not found: {scenePath}"));
                    return sceneHandle;
                }

                var bundleHandle = await LoadBundleAsync(bundleInfo.BundleName, cancellationToken);
                if (bundleHandle == null || !bundleHandle.IsValid)
                {
                    sceneHandle.SetError(new Exception($"Failed to load scene bundle: {bundleInfo.BundleName}"));
                    return sceneHandle;
                }

                var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                var op = SceneManager.LoadSceneAsync(sceneName, mode);
                op.allowSceneActivation = activateOnLoad;

                while (!op.isDone)
                {
                    var normalizedProgress = op.progress < 0.9f ? Mathf.Clamp01(op.progress / 0.9f) : 1f;
                    sceneHandle.SetProgress(normalizedProgress);
                    progress?.Report(normalizedProgress);
                    if (op.progress >= 0.9f && !activateOnLoad)
                    {
                        break;
                    }
                    await UniTask.Yield(cancellationToken);
                }

                sw.Stop();

                sceneHandle.SetScene(SceneManager.GetSceneByName(sceneName), bundleInfo.BundleName, sw.Elapsed.TotalMilliseconds);
                sceneHandle.Retain();
                lock (m_LockObj)
                {
                    m_HandleBaseCache[scenePath] = sceneHandle;
                }
            }
            catch (Exception ex)
            {
                sceneHandle.SetError(ex);
            }

            return sceneHandle;
        }

        /// <summary>
        /// 异步实例化资源
        /// </summary>
        /// <returns></returns>
        public async UniTask<GameObject> InstantiateAsync(string assetPath, Transform parent = null, CancellationToken cancellationToken = default)
        {

            // 加载资源
            var asset = await LoadAssetAsync<GameObject>(assetPath, cancellationToken);
            if (asset == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Failed to load asset: {assetPath}");
                return null;
            }

            // 实例化资源
            var instance = UnityEngine.Object.Instantiate(asset, parent);
            if (instance == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Failed to instantiate asset: {assetPath}");
                return null;
            }
            // 挂上自动释放组件
            var releaseComp = instance.AddComponent<AutoReleaseOnDestroy>();
            releaseComp.m_Path = assetPath;
            return instance;
        }

        #endregion


        #region 资源管理

        /// <summary>
        /// 强制释放所有资源
        /// </summary>
        public virtual void ForceReleaseAll()
        {
            var toRemove = m_HandleBaseCache.Values.ToList();
            for (int i = 0; i < toRemove.Count; i++)
            {
                var handle = toRemove[i];
                ReleaseAsset(handle.Path, true);
            }
        }
        /// <summary>
        /// 强制释放指定资源
        /// </summary>
        /// <param name="assetPath"></param>
        public virtual void ForceReleaseAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return;

            ReleaseAsset(assetPath, true);
        }

        /// <summary>
        /// 释放指定资源
        /// </summary>
        /// <param name="asset"></param>
        public virtual void Release(UnityEngine.Object asset)
        {
            if (asset == null) return;

            var info = m_HandleBaseCache.FirstOrDefault(x => x.Value is AssetHandle ah && ah.AssetObject == asset);
            ReleaseAsset(info.Key);
        }
        /// <summary>
        /// 释放指定资源路径的资源
        /// </summary>
        /// <param name="assetPath"></param>
        public virtual void Release(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return;

            ReleaseAsset(assetPath);
        }

        /// <summary>
        /// 强制卸载指定Bundle
        /// </summary>
        public virtual void ForceUnloadBundle(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName)) return;
            if (m_BundleHandleCache.TryGetValue(bundleName, out var bundleHandle))
            {
                var toRemove = m_HandleBaseCache
                    .Where(kvp => kvp.Value != null && kvp.Value.BundleName == bundleName)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in toRemove)
                {
                    ReleaseAsset(key, true);
                }
                if (bundleHandle != null)
                {
                    ReleaseBundle(bundleHandle.BundleName, true);
                }
            }
        }
        /// <summary>
        /// 异步卸载未使用资源
        /// </summary>
        /// <returns></returns>
        public virtual async UniTask UnloadUnusedAssetsAsync()
        {
            lock (m_LockObj)
            {
                var toRemove = new List<HandleBase>();
                foreach (var kvp in m_HandleBaseCache)
                {
                    if (kvp.Value == null)
                    {
                        toRemove.Add(kvp.Value);
                        continue;
                    }

                    if (kvp.Value.IsDisposed || !kvp.Value.IsValid || kvp.Value.RefCount <= 0)
                    {
                        toRemove.Add(kvp.Value);
                    }
                }

                foreach (var handleBase in toRemove)
                {
                    m_HandleBaseCache.Remove(handleBase.Path);
                    handleBase.Dispose();
                }
            }

            lock (m_LockObj)
            {

                var toRemove = new List<BundleHandle>();
                foreach (var kvp in m_BundleHandleCache)
                {
                    if (kvp.Value == null)
                    {
                        toRemove.Add(kvp.Value);
                        continue;
                    }

                    if (kvp.Value.IsDisposed || !kvp.Value.IsValid || kvp.Value.RefCount <= 0)
                    {
                        toRemove.Add(kvp.Value);
                    }
                }

                foreach (var bundleHandle in toRemove)
                {
                    m_BundleHandleCache.Remove(bundleHandle.BundleName);
                    bundleHandle.Dispose();
                }
            }
            await Resources.UnloadUnusedAssets();
            GC.Collect();
        }


        public virtual void Dispose()
        {
            ForceReleaseAll();
            UnloadUnusedAssetsAsync().Forget();
        }

        #endregion

        #region Bundle加载

        /// <summary>
        /// 加载Bundle及其依赖
        /// </summary>
        protected async UniTask<BundleHandle> LoadBundleAsync(string bundleName, CancellationToken cancellationToken = default, bool isDepend = false)
        {
            UniTask<BundleHandle> taskToAwait = default;
            bool createdTask = false;

            lock (m_LockObj)
            {
                if (m_BundleHandleCache.TryGetValue(bundleName, out var cached))
                {
                    if (cached == null || cached.IsDisposed || !cached.IsValid)
                    {
                        m_BundleHandleCache.Remove(bundleName);
                    }
                    else
                    {
                        if (cached.IsDependLoad && !isDepend)
                        {
                            cached.IsDependLoad = false;
                            cached.Retain();
                        }

                        return cached;
                    }
                }

                if (!m_LoadingBundles.TryGetValue(bundleName, out taskToAwait))
                {
                    taskToAwait = LoadBundleInternalAsync(bundleName, CancellationToken.None, isDepend);
                    m_LoadingBundles[bundleName] = taskToAwait;
                    createdTask = true;
                }
            }

            try
            {
                return await taskToAwait.AttachExternalCancellation(cancellationToken);
            }
            finally
            {
                if (createdTask)
                {
                    lock (m_LockObj)
                    {
                        m_LoadingBundles.Remove(bundleName);
                    }
                }
            }
        }

        /// <summary>
        /// 内部Bundle加载实现
        /// </summary>
        protected virtual async UniTask<BundleHandle> LoadBundleInternalAsync(string bundleName,
            CancellationToken cancellationToken = default, bool isDepend = false)
        {
            var bundleInfo = m_Manifest.GetBundleInfo(bundleName);
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
                var depBundleHandle = await LoadBundleAsync(depName, cancellationToken, true);
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

            lock (m_LockObj)
            {
                m_BundleHandleCache[bundleName] = bundleHandle;
            }

            return bundleHandle;
        }

        /// <summary>
        /// 从源加载Bundle（子类实现具体逻辑）
        /// </summary>
        protected abstract UniTask<AssetBundle> LoadBundleFromSourceAsync(BundleInfo bundleInfo,
            CancellationToken cancellationToken = default);


        private void ReleaseAsset(string assetPath, bool force = false)
        {
            if (string.IsNullOrEmpty(assetPath)) return;

            if (m_HandleBaseCache.TryGetValue(assetPath, out var handleBase))
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
                // 递归释放依赖的资源
                if (handleBase.RefCount <= 0 || handleBase.IsDisposed || !handleBase.IsValid)
                {
                    //_handleBaseCache.Remove(assetPath);
                    ReleaseBundle(handleBase.BundleName);
                }


            }
        }



        private void ReleaseBundle(string bundleName, bool force = false)
        {
            if (string.IsNullOrEmpty(bundleName)) return;

            if (m_BundleHandleCache.TryGetValue(bundleName, out var bundleHandle) && bundleHandle != null)
            {

                if (force)
                {
                    while (bundleHandle.RefCount > 0)
                    {
                        bundleHandle.Release();
                    }
                }
                else
                {
                    bundleHandle.Release();
                }
                // 递归释放依赖的Bundle
                if (!bundleHandle.IsValid || bundleHandle.RefCount <= 0)
                {
                    foreach (var dep in bundleHandle.Dependencies)
                    {
                        ReleaseBundle(dep.BundleName, false);
                    }
                }
            }
        }

        /// <summary>
        /// 记录资源引用（带加载耗时）
        /// </summary>
        protected void TrackAssetHandle(string assetPath, UnityEngine.Object asset, string bundleName, double loadTimeMs)
        {
            if (asset == null) return;

            lock (m_LockObj)
            {
                if (m_HandleBaseCache.TryGetValue(assetPath, out var handle) && handle != null)
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
                    m_HandleBaseCache[assetPath] = handle;
                }
            }
        }

        /// <summary>
        ///     记录原始文件引用（带加载耗时）
        /// </summary>
        protected void TrackRawFileHandle(string assetPath, byte[] data, string bundleName, long fileSizeBytes, double loadTimeMs)
        {
            if (data == null) return;
            lock (m_LockObj)
            {
                if (m_HandleBaseCache.TryGetValue(assetPath, out var handle) && handle is RawFileHandle raw)
                {
                    raw.Retain();
                }
                else
                {
                    var rawHandle = new RawFileHandle(assetPath);
                    rawHandle.SetData(data, bundleName, fileSizeBytes, loadTimeMs);
                    rawHandle.Retain();
                    m_HandleBaseCache[assetPath] = rawHandle;
                }
            }
        }

        /// <summary>
        ///     从缓存中尝试获取原始文件数据
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected bool TryGetRawFileFromCache(string assetPath, out byte[] data)
        {
            data = null;
            if (string.IsNullOrEmpty(assetPath)) return false;
            if (m_HandleBaseCache.TryGetValue(assetPath, out var handle) && handle is RawFileHandle raw && raw.IsValid)
            {
                raw.Retain();
                data = raw.Data;
                return true;
            }
            return false;
        }


    }
}
#endregion



