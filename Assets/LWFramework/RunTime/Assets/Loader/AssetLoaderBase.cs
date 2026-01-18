using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            GameObject asset = LoadAsset<GameObject>(assetPath);
            if (asset == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Failed to load asset: {assetPath}");
                return null;
            }

            GameObject instance = UnityEngine.Object.Instantiate(asset, parent);
            if (instance == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Failed to instantiate asset: {assetPath}");
                return null;
            }

            AutoReleaseOnDestroy releaseComp = instance.AddComponent<AutoReleaseOnDestroy>();
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
        /// 通用：通过清单定位Bundle，再从Bundle异步加载资源
        /// </summary>
        public virtual async UniTask<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken = default)
            where T : UnityEngine.Object
        {
            BundleInfo bundleInfo = m_Manifest.GetBundleByAsset(assetPath);
            if (bundleInfo == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Asset not found in manifest: {assetPath}");
                return null;
            }

            Stopwatch sw = Stopwatch.StartNew();

            BundleHandle bundleHandle = await LoadBundleAsync(bundleInfo.BundleName, cancellationToken);
            if (bundleHandle == null || !bundleHandle.IsValid)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Failed to load bundle: {bundleInfo.BundleName}");
                return null;
            }

            string assetName = Path.GetFileNameWithoutExtension(assetPath);
            AssetBundleRequest request = bundleHandle.Bundle.LoadAssetAsync<T>(assetName);
            await request;

            T asset = request.asset as T;
            sw.Stop();

            if (asset != null)
            {
                RetainAssetReference(assetPath, asset, bundleInfo.BundleName, sw.Elapsed.TotalMilliseconds);
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
            byte[] data = await LoadRawFileAsync(assetPath, cancellationToken);
            return data != null ? System.Text.Encoding.UTF8.GetString(data) : null;
        }

        /// <summary>
        /// 通用：通过清单定位场景Bundle，确保Bundle加载后再切场景
        /// </summary>
        public virtual async UniTask<SceneHandle> LoadSceneAsync(string scenePath, LoadSceneMode mode, bool activateOnLoad,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            lock (m_LockObj)
            {
                if (m_HandleBaseCache.TryGetValue(scenePath, out HandleBase cached) && cached is SceneHandle cachedScene)
                {
                    if (!cachedScene.IsDisposed && cachedScene.IsValid)
                    {
                        cachedScene.Retain();
                        RetainBundleReferenceRecursive(cachedScene.BundleName);
                        return cachedScene;
                    }

                    m_HandleBaseCache.Remove(scenePath);
                }
            }

            SceneHandle sceneHandle = new SceneHandle(scenePath);
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                progress?.Report(0f);
                BundleInfo bundleInfo = m_Manifest.GetBundleByAsset(scenePath);
                if (bundleInfo == null)
                {
                    sceneHandle.SetError(new FileNotFoundException($"Scene not found: {scenePath}"));
                    return sceneHandle;
                }

                BundleHandle bundleHandle = await LoadBundleAsync(bundleInfo.BundleName, cancellationToken);
                if (bundleHandle == null || !bundleHandle.IsValid)
                {
                    sceneHandle.SetError(new Exception($"Failed to load scene bundle: {bundleInfo.BundleName}"));
                    return sceneHandle;
                }

                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, mode);
                op.allowSceneActivation = activateOnLoad;

                while (!op.isDone)
                {
                    float normalizedProgress = op.progress < 0.9f ? Mathf.Clamp01(op.progress / 0.9f) : 1f;
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

                RetainBundleReferenceRecursive(bundleInfo.BundleName);
            }
            catch (Exception ex)
            {
                sceneHandle.SetError(ex);
            }

            return sceneHandle;
        }

        /// <summary>
        /// 卸载场景，并按引用计数规则释放对应资源
        /// </summary>
        public virtual async UniTask UnloadSceneAsync(string scenePath, bool forceRelease = true, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(scenePath)) return;

            cancellationToken.ThrowIfCancellationRequested();

            SceneHandle sceneHandle = null;
            lock (m_LockObj)
            {
                if (m_HandleBaseCache.TryGetValue(scenePath, out HandleBase cached) && cached is SceneHandle cachedScene)
                {
                    sceneHandle = cachedScene;
                }
            }

            if (sceneHandle != null)
            {
                await sceneHandle.UnloadAsync();
            }
            else
            {
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                Scene scene = SceneManager.GetSceneByName(sceneName);
                if (scene.IsValid())
                {
                    AsyncOperation op = SceneManager.UnloadSceneAsync(scene);
                    if (op != null)
                    {
                        await op;
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            ReleaseAsset(scenePath, forceRelease);
        }

        /// <summary>
        /// 卸载场景句柄对应的场景，并按引用计数规则释放对应资源
        /// </summary>
        public virtual async UniTask UnloadSceneAsync(SceneHandle sceneHandle, bool forceRelease = true, CancellationToken cancellationToken = default)
        {
            if (sceneHandle == null) return;

            await UnloadSceneAsync(sceneHandle.Path, forceRelease, cancellationToken);
        }

        /// <summary>
        /// 异步实例化资源
        /// </summary>
        /// <returns></returns>
        public async UniTask<GameObject> InstantiateAsync(string assetPath, Transform parent = null, CancellationToken cancellationToken = default)
        {

            // 加载资源
            GameObject asset = await LoadAssetAsync<GameObject>(assetPath, cancellationToken);
            if (asset == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Failed to load asset: {assetPath}");
                return null;
            }
            // 实例化资源
            GameObject instance = UnityEngine.Object.Instantiate(asset, parent);
            // 挂上自动释放组件
            AutoReleaseOnDestroy releaseComp = instance.AddComponent<AutoReleaseOnDestroy>();
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
            List<string> assetPaths = new List<string>();
            lock (m_LockObj)
            {
                foreach (KeyValuePair<string, HandleBase> kvp in m_HandleBaseCache)
                {
                    if (kvp.Value == null)
                    {
                        continue;
                    }

                    assetPaths.Add(kvp.Key);
                }
            }

            for (int i = 0; i < assetPaths.Count; i++)
            {
                ReleaseAsset(assetPaths[i], true);
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

            string assetPath = null;
            lock (m_LockObj)
            {
                foreach (KeyValuePair<string, HandleBase> kvp in m_HandleBaseCache)
                {
                    if (kvp.Value is AssetHandle assetHandle && assetHandle.AssetObject == asset)
                    {
                        assetPath = kvp.Key;
                        break;
                    }
                }
            }

            ReleaseAsset(assetPath);
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

            List<string> assetPathsToRelease = new List<string>();
            lock (m_LockObj)
            {
                if (!m_BundleHandleCache.TryGetValue(bundleName, out BundleHandle bundleHandle))
                {
                    return;
                }

                foreach (KeyValuePair<string, HandleBase> kvp in m_HandleBaseCache)
                {
                    if (kvp.Value != null && kvp.Value.BundleName == bundleName)
                    {
                        assetPathsToRelease.Add(kvp.Key);
                    }
                }
            }

            for (int i = 0; i < assetPathsToRelease.Count; i++)
            {
                ReleaseAsset(assetPathsToRelease[i], true);
            }

            ReleaseBundleReferenceRecursive(bundleName, true);
        }
        /// <summary>
        /// 异步卸载未使用资源
        /// </summary>
        /// <returns></returns>
        public virtual async UniTask UnloadUnusedAssetsAsync()
        {
            List<string> handlePathsToRemove = new List<string>();
            List<string> bundleNamesToRemove = new List<string>();
            lock (m_LockObj)
            {
                foreach (KeyValuePair<string, HandleBase> kvp in m_HandleBaseCache)
                {
                    HandleBase handleBase = kvp.Value;
                    if (handleBase == null || handleBase.IsDisposed || !handleBase.IsValid || handleBase.RefCount <= 0)
                    {
                        handlePathsToRemove.Add(kvp.Key);
                    }
                }

                foreach (KeyValuePair<string, BundleHandle> kvp in m_BundleHandleCache)
                {
                    BundleHandle bundleHandle = kvp.Value;
                    if (bundleHandle == null || bundleHandle.IsDisposed || !bundleHandle.IsValid || bundleHandle.RefCount <= 0)
                    {
                        bundleNamesToRemove.Add(kvp.Key);
                    }
                }

                for (int i = 0; i < handlePathsToRemove.Count; i++)
                {
                    string assetPath = handlePathsToRemove[i];
                    if (m_HandleBaseCache.TryGetValue(assetPath, out HandleBase handleBase) && handleBase != null)
                    {
                        handleBase.Dispose();
                    }
                    m_HandleBaseCache.Remove(assetPath);
                }

                for (int i = 0; i < bundleNamesToRemove.Count; i++)
                {
                    string bundleName = bundleNamesToRemove[i];
                    if (m_BundleHandleCache.TryGetValue(bundleName, out BundleHandle bundleHandle) && bundleHandle != null)
                    {
                        bundleHandle.Dispose();
                    }
                    m_BundleHandleCache.Remove(bundleName);
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
                if (m_BundleHandleCache.TryGetValue(bundleName, out BundleHandle cached))
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
            BundleInfo bundleInfo = m_Manifest.GetBundleInfo(bundleName);
            if (bundleInfo == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Bundle not found: {bundleName}");
                return null;
            }

            Stopwatch sw = Stopwatch.StartNew();
            BundleHandle bundleHandle = new BundleHandle(bundleName);

            // 先加载依赖
            foreach (string depName in bundleInfo.Dependencies)
            {
                BundleHandle depBundleHandle = await LoadBundleAsync(depName, cancellationToken, true);
                bundleHandle.AddDependency(depBundleHandle);
            }
            // 加载Bundle
            AssetBundle bundle = await LoadBundleFromSourceAsync(bundleInfo, cancellationToken);
            sw.Stop();
            bundleHandle.IsDependLoad = isDepend;
            bundleHandle.SetBundle(bundle);
            bundleHandle.SetLoadInfo(bundleInfo.Size, sw.Elapsed.TotalMilliseconds);

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

        /// <summary>
        /// 释放指定资源路径的引用，并递归扣减其所属Bundle及依赖Bundle的引用计数
        /// </summary>
        private void ReleaseAsset(string assetPath, bool force = false)
        {
            if (string.IsNullOrEmpty(assetPath)) return;

            lock (m_LockObj)
            {
                if (!m_HandleBaseCache.TryGetValue(assetPath, out HandleBase handleBase) || handleBase == null)
                {
                    return;
                }

                string bundleName = handleBase.BundleName;
                if (force)
                {
                    while (handleBase.RefCount > 0)
                    {
                        handleBase.Release();
                        ReleaseBundleReferenceRecursive(bundleName, false);
                    }
                }
                else
                {
                    handleBase.Release();
                    ReleaseBundleReferenceRecursive(bundleName, false);
                }
            }
        }

        /// <summary>
        /// 递归扣减指定Bundle及其依赖Bundle的引用计数
        /// </summary>
        private void ReleaseBundleReferenceRecursive(string bundleName, bool force = false)
        {
            if (string.IsNullOrEmpty(bundleName)) return;

            lock (m_LockObj)
            {
                if (!m_BundleHandleCache.TryGetValue(bundleName, out BundleHandle bundleHandle) || bundleHandle == null)
                {
                    return;
                }

                if (force)
                {
                    while (bundleHandle.RefCount > 0)
                    {
                        int prevRefCount = bundleHandle.RefCount;
                        bundleHandle.Release();

                        if (prevRefCount > 0)
                        {
                            foreach (BundleHandle dep in bundleHandle.Dependencies)
                            {
                                if (dep != null)
                                {
                                    ReleaseBundleReferenceRecursive(dep.BundleName, false);
                                }
                            }
                        }
                    }
                }
                else
                {
                    int prevRefCount = bundleHandle.RefCount;
                    bundleHandle.Release();

                    if (prevRefCount > 0)
                    {
                        foreach (BundleHandle dep in bundleHandle.Dependencies)
                        {
                            if (dep != null)
                            {
                                ReleaseBundleReferenceRecursive(dep.BundleName, false);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 记录资源引用（带加载耗时）
        /// </summary>
        protected void RetainAssetReference(string assetPath, UnityEngine.Object asset, string bundleName, double loadTimeMs)
        {
            if (asset == null) return;

            lock (m_LockObj)
            {
                if (m_HandleBaseCache.TryGetValue(assetPath, out HandleBase handle) && handle != null)
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

                RetainBundleReferenceRecursive(bundleName);
            }
        }

        /// <summary>
        /// 递归增加指定Bundle及其依赖Bundle的引用计数
        /// </summary>
        protected void RetainBundleReferenceRecursive(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName)) return;

            lock (m_LockObj)
            {
                HashSet<string> visited = new HashSet<string>();
                RetainBundleReferenceRecursiveInternal(bundleName, visited);
            }
        }

        private void RetainBundleReferenceRecursiveInternal(string bundleName, HashSet<string> visited)
        {
            if (string.IsNullOrEmpty(bundleName)) return;
            if (!visited.Add(bundleName)) return;

            if (m_BundleHandleCache.TryGetValue(bundleName, out BundleHandle bundleHandle) && bundleHandle != null && !bundleHandle.IsDisposed && bundleHandle.IsValid)
            {
                bundleHandle.Retain();
                foreach (BundleHandle dep in bundleHandle.Dependencies)
                {
                    if (dep != null)
                    {
                        RetainBundleReferenceRecursiveInternal(dep.BundleName, visited);
                    }
                }
            }
        }

        /// <summary>
        ///     记录原始文件引用（带加载耗时）
        /// </summary>
        protected void RetainRawFileReference(string assetPath, byte[] data, string bundleName, long fileSizeBytes, double loadTimeMs)
        {
            if (data == null) return;
            lock (m_LockObj)
            {
                if (m_HandleBaseCache.TryGetValue(assetPath, out HandleBase handle) && handle is RawFileHandle raw)
                {
                    raw.Retain();
                }
                else
                {
                    RawFileHandle rawHandle = new RawFileHandle(assetPath);
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
            if (m_HandleBaseCache.TryGetValue(assetPath, out HandleBase handle) && handle is RawFileHandle raw && raw.IsValid)
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