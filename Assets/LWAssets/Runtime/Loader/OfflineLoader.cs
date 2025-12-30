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
        protected CacheManager _cacheManager;
        protected DownloadManager _downloadManager;

        public OfflineLoader(LWAssetsConfig config, CacheManager cacheManager, DownloadManager downloadManager)
            : base(config)
        {
            _cacheManager = cacheManager;
            _downloadManager = downloadManager;
        }

        public override async UniTask InitializeAsync(BundleManifest manifest)
        {
            _manifest = manifest;
            await UniTask.CompletedTask;
            UnityEngine.Debug.Log("[LWAssets] OfflineLoader initialized");
        }

        #region 异步加载实现

        /// <summary>
        /// 从离线Bundle加载资源，并记录加载耗时/引用信息
        /// </summary>
        public override async UniTask<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken = default)
        {
            // 直接通过 manifest 获取 Bundle 信息
            var bundleInfo = _manifest.GetBundleByAsset(assetPath);
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
                TrackAsset(assetPath, asset, bundleInfo.BundleName, sw.Elapsed.TotalMilliseconds);
            }
            else
            {
                sw.Stop();
            }

            return asset;
        }

        // public override async UniTask<AssetHandle<T>> LoadAssetWithHandleAsync<T>(string assetPath,
        //     CancellationToken cancellationToken = default)
        // {
        //     var handle = new AssetHandle<T>(assetPath);

        //     try
        //     {
        //         var asset = await LoadAssetAsync<T>(assetPath, cancellationToken);
        //         if (asset != null)
        //         {
        //             handle.SetAsset(asset, bundleName: null, loadTimeMs: 0);
        //         }
        //         else
        //         {
        //             handle.SetError(new Exception($"Failed to load asset: {assetPath}"));
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         handle.SetError(ex);
        //     }

        //     return handle;
        // }

        public override async UniTask<byte[]> LoadRawFileAsync(string assetPath, CancellationToken cancellationToken = default)
        {
            var bundleInfo = _manifest.GetBundleByAsset(assetPath);
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

            return await File.ReadAllBytesAsync(filePath, cancellationToken);
        }

        public override async UniTask<SceneHandle> LoadSceneAsync(string scenePath, LoadSceneMode mode,
            bool activateOnLoad, CancellationToken cancellationToken = default)
        {
            var handle = new SceneHandle(scenePath);
            var sw = Stopwatch.StartNew();
            try
            {
                var bundleInfo = _manifest.GetBundleByAsset(scenePath);
                if (bundleInfo  == null)
                {
                    handle.SetError(new FileNotFoundException($"Scene not found: {scenePath}"));
                    return handle;
                }

                // 加载场景Bundle
                var bundleHandle = await LoadBundleAsync(bundleInfo.BundleName, cancellationToken);
                if (bundleHandle == null || !bundleHandle.IsValid)
                {
                    handle.SetError(new Exception($"Failed to load scene bundle: {bundleInfo.BundleName}"));
                    return handle;
                }

                // 加载场景
                var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                var op = SceneManager.LoadSceneAsync(sceneName, mode);
                op.allowSceneActivation = activateOnLoad;

                while (!op.isDone)
                {
                    handle.SetProgress(op.progress);

                    if (op.progress >= 0.9f && !activateOnLoad)
                    {
                        break;
                    }

                    await UniTask.Yield(cancellationToken);
                }
                sw.Stop();
                
                handle.SetScene(SceneManager.GetSceneByName(sceneName), bundleInfo.BundleName, sw.Elapsed.TotalMilliseconds);
                _handleBaseCache.Add(scenePath, handle);
            }
            catch (Exception ex)
            {
                handle.SetError(ex);
            }

            return handle;
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
            var cachePath = Path.Combine(_config.GetPersistentDataPath(), bundleInfo.GetFileName());
            if (File.Exists(cachePath))
            {
                return cachePath;
            }

            // 其次从StreamingAssets加载
            var streamingPath = Path.Combine(_config.GetStreamingAssetsPath(), bundleInfo.GetFileName());
            return streamingPath;
        }

        
        #endregion
    }
}
