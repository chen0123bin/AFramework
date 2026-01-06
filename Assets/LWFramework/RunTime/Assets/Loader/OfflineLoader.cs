using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWAssets
{
    /// <summary>
    /// 离线加载器 - 从本地资源包加载
    /// </summary>
    public class OfflineLoader : AssetLoaderBase
    {
        protected CacheManager m_CacheManager;
        protected DownloadManager m_DownloadManager;

        public OfflineLoader(LWAssetsConfig config, CacheManager cacheManager, DownloadManager downloadManager)
            : base(config)
        {
            m_CacheManager = cacheManager;
            m_DownloadManager = downloadManager;
        }

        public override async UniTask InitializeAsync(BundleManifest manifest)
        {
            m_Manifest = manifest;
            await UniTask.CompletedTask;
            UnityEngine.Debug.Log("[LWAssets] OfflineLoader initialized");
        }

        #region 同步加载实现

        public override T LoadAsset<T>(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                UnityEngine.Debug.LogError("[LWAssets] Asset path is null or empty");
                return null;
            }

            lock (m_LockObj)
            {
                if (m_HandleBaseCache.TryGetValue(assetPath, out var cached) && cached is AssetHandle ah && ah.IsValid)
                {
                    cached.Retain();
                    return ah.AssetObject as T;
                }
            }

            var bundleInfo = m_Manifest.GetBundleByAsset(assetPath);
            if (bundleInfo == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Asset not found in manifest: {assetPath}");
                return null;
            }

            var sw = Stopwatch.StartNew();
            var bundleHandle = LoadBundleSync(bundleInfo.BundleName, false);
            if (bundleHandle == null || !bundleHandle.IsValid)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Failed to load bundle: {bundleInfo.BundleName}");
                return null;
            }

            var assetName = Path.GetFileNameWithoutExtension(assetPath);
            var asset = bundleHandle.Bundle.LoadAsset<T>(assetName);
            sw.Stop();

            if (asset == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Asset not found in bundle: {assetPath}");
                return null;
            }

            TrackAssetHandle(assetPath, asset, bundleInfo.BundleName, sw.Elapsed.TotalMilliseconds);
            return asset;
        }

        public override byte[] LoadRawFile(string assetPath)
        {
            if (TryGetRawFileFromCache(assetPath, out var cached))
            {
                return cached;
            }

            var bundleInfo = m_Manifest.GetBundleByAsset(assetPath);
            if (bundleInfo == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Raw file not found in manifest: {assetPath}");
                return null;
            }

            if (!bundleInfo.IsRawFile)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Asset is not a raw file: {assetPath}");
                return null;
            }

            var filePath = GetBundlePath(bundleInfo);
            if (!File.Exists(filePath))
            {
                UnityEngine.Debug.LogError($"[LWAssets] Raw file not found: {filePath}");
                return null;
            }

            var sw = Stopwatch.StartNew();
            var data = File.ReadAllBytes(filePath);
            sw.Stop();
            TrackRawFileHandle(assetPath, data, bundleInfo.BundleName, bundleInfo.Size, sw.Elapsed.TotalMilliseconds);
            return data;
        }

        public override string LoadRawFileText(string assetPath)
        {
            var bytes = LoadRawFile(assetPath);
            return bytes != null ? System.Text.Encoding.UTF8.GetString(bytes) : null;
        }

        private BundleHandle LoadBundleSync(string bundleName, bool isDepend)
        {
            if (string.IsNullOrEmpty(bundleName))
            {
                UnityEngine.Debug.LogError("[LWAssets] Bundle name is null or empty");
                return null;
            }

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
            }

            var bundleInfo = m_Manifest.GetBundleInfo(bundleName);
            if (bundleInfo == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Bundle not found: {bundleName}");
                return null;
            }

            var sw = Stopwatch.StartNew();
            var bundleHandle = new BundleHandle(bundleName);

            foreach (var depName in bundleInfo.Dependencies)
            {
                var depBundleHandle = LoadBundleSync(depName, true);
                bundleHandle.AddDependency(depBundleHandle);
                depBundleHandle?.Retain();
            }

            var bundlePath = GetBundlePath(bundleInfo);
            if (!File.Exists(bundlePath))
            {
                UnityEngine.Debug.LogError($"[LWAssets] Bundle file not found: {bundlePath}. 同步加载不支持自动下载，请改用异步接口或提前预下载。");
                return null;
            }

            var bundle = AssetBundle.LoadFromFile(bundlePath);
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

        #endregion

        #region 异步加载实现

        /// <summary>
        /// 从离线Bundle加载资源，并记录加载耗时/引用信息
        /// </summary>
        public override async UniTask<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken = default)
        {
            // 直接通过 manifest 获取 Bundle 信息
            var bundleInfo = m_Manifest.GetBundleByAsset(assetPath);
            if (bundleInfo == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Asset not found in manifest: {assetPath}");
                return null;
            }

            var sw = Stopwatch.StartNew();

            // 加载Bundle
            var bundleHandle = await LoadBundleAsync(bundleInfo.BundleName, cancellationToken);
            if (bundleHandle == null || !bundleHandle.IsValid)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Failed to load bundle: {bundleInfo.BundleName}");
                return null;
            }

            // 从Bundle加载资源
            var assetName = Path.GetFileNameWithoutExtension(assetPath);
            var request = bundleHandle.Bundle.LoadAssetAsync<T>(assetName);
            await request;

            var asset = request.asset as T;
            if (asset != null)
            {
                sw.Stop();
                TrackAssetHandle(assetPath, asset, bundleInfo.BundleName, sw.Elapsed.TotalMilliseconds);
            }
            else
            {
                sw.Stop();
            }

            return asset;
        }


        public override async UniTask<byte[]> LoadRawFileAsync(string assetPath, CancellationToken cancellationToken = default)
        {
            if (TryGetRawFileFromCache(assetPath, out var cached))
            {
                return cached;
            }

            var bundleInfo = m_Manifest.GetBundleByAsset(assetPath);
            if (bundleInfo == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Raw file not found in manifest: {assetPath}");
                return null;
            }

            if (!bundleInfo.IsRawFile)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Asset is not a raw file: {assetPath}");
                return null;
            }

            var filePath = GetBundlePath(bundleInfo);
            if (!File.Exists(filePath))
            {
                UnityEngine.Debug.LogError($"[LWAssets] Raw file not found: {filePath}");
                return null;
            }

            var sw = Stopwatch.StartNew();
            var data = await File.ReadAllBytesAsync(filePath, cancellationToken);
            sw.Stop();
            TrackRawFileHandle(assetPath, data, bundleInfo.BundleName, bundleInfo.Size, sw.Elapsed.TotalMilliseconds);
            return data;
        }

        public override async UniTask<SceneHandle> LoadSceneAsync(string scenePath, LoadSceneMode mode,
            bool activateOnLoad, CancellationToken cancellationToken = default)
        {
            var sceneHandle = new SceneHandle(scenePath);
            var sw = Stopwatch.StartNew();
            try
            {
                var bundleInfo = m_Manifest.GetBundleByAsset(scenePath);
                if (bundleInfo == null)
                {
                    sceneHandle.SetError(new FileNotFoundException($"Scene not found: {scenePath}"));
                    return sceneHandle;
                }

                // 加载场景Bundle
                var bundleHandle = await LoadBundleAsync(bundleInfo.BundleName, cancellationToken);
                if (bundleHandle == null || !bundleHandle.IsValid)
                {
                    sceneHandle.SetError(new Exception($"Failed to load scene bundle: {bundleInfo.BundleName}"));
                    return sceneHandle;
                }

                // 加载场景
                var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                var op = SceneManager.LoadSceneAsync(sceneName, mode);
                op.allowSceneActivation = activateOnLoad;

                while (!op.isDone)
                {
                    sceneHandle.SetProgress(op.progress);
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

        #endregion

        #region Bundle加载

        protected override async UniTask<AssetBundle> LoadBundleFromSourceAsync(BundleInfo bundleInfo,
            CancellationToken cancellationToken = default)
        {
            var filePath = GetBundlePath(bundleInfo);

            if (!File.Exists(filePath))
            {
                UnityEngine.Debug.LogError($"[LWAssets] Bundle file not found: {filePath}");
                return null;
            }

            var request = AssetBundle.LoadFromFileAsync(filePath);
            await request;

            return request.assetBundle;
        }

        /// <summary>
        /// 获取Bundle文件路径
        /// </summary>
        protected virtual string GetBundlePath(BundleInfo bundleInfo)
        {
            // 优先从缓存目录加载
            var cachePath = Path.Combine(m_Config.GetPersistentDataPath(), bundleInfo.GetFileName());
            if (File.Exists(cachePath))
            {
                return cachePath;
            }

            // 其次从StreamingAssets加载
            var streamingPath = Path.Combine(m_Config.GetStreamingAssetsPath(), bundleInfo.GetFileName());
            return streamingPath;
        }


        #endregion
    }
}
