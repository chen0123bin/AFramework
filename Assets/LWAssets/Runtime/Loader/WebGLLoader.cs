using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace LWAssets
{
    /// <summary>
    /// WebGL加载器 - 针对WebGL平台的特殊处理
    /// </summary>
    public class WebGLLoader : AssetLoaderBase
    {
        private CacheManager _cacheManager;
        private DownloadManager _downloadManager;

        // WebGL缓存
        private readonly Dictionary<string, byte[]> _webglCache = new Dictionary<string, byte[]>();

        public WebGLLoader(LWAssetsConfig config, CacheManager cacheManager, DownloadManager downloadManager)
            : base(config)
        {
            _cacheManager = cacheManager;
            _downloadManager = downloadManager;
        }

        public override async UniTask InitializeAsync(BundleManifest manifest)
        {
            _manifest = manifest;
            await UniTask.CompletedTask;
            UnityEngine.Debug.Log("[LWAssets] WebGLLoader initialized");
        }

        #region 异步加载实现

        /// <summary>
        /// WebGL加载资源，并记录加载耗时/引用信息
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
            var assetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
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
            // 直接通过 manifest 获取 Bundle 信息
            var bundleInfo = _manifest.GetBundleByAsset(assetPath);
            if (bundleInfo == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Asset not found in manifest: {assetPath}");
                return null;
            }


            // WebGL从缓存或远程加载
            var cacheKey = bundleInfo.GetFileName();
            if (_webglCache.TryGetValue(cacheKey, out var cachedData))
            {
                return cachedData;
            }

            var url = _config.GetRemoteURL() + bundleInfo.GetFileName();
            using (var request = UnityWebRequest.Get(url))
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var data = request.downloadHandler.data;
                    _webglCache[cacheKey] = data;
                    return data;
                }
                else
                {
                    UnityEngine.Debug.LogError($"[LWAssets] Failed to download raw file: {url}, Error: {request.error}");
                    return null;
                }
            }
        }

        public override async UniTask<SceneHandle> LoadSceneAsync(string scenePath, LoadSceneMode mode,
            bool activateOnLoad, CancellationToken cancellationToken = default)
        {
            var sceneHandle = new SceneHandle(scenePath);
            var sw = Stopwatch.StartNew();
            try
            {
                // 直接通过 manifest 获取 Bundle 信息
                var bundleInfo = _manifest.GetBundleByAsset(scenePath);
                if (bundleInfo == null)
                {
                    sceneHandle.SetError(new System.IO.FileNotFoundException($"Scene not found: {scenePath}"));
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
                var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
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
                lock (_lockObj)
                {
                    _handleBaseCache[scenePath] = sceneHandle;
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
            var url = _config.GetRemoteURL() + bundleInfo.GetFileName();

            // WebGL使用UnityWebRequest加载Bundle
            using (var request = UnityWebRequestAssetBundle.GetAssetBundle(url, bundleInfo.CRC))
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    return DownloadHandlerAssetBundle.GetContent(request);
                }
                else
                {
                    UnityEngine.Debug.LogError($"[LWAssets] Failed to download bundle: {url}, Error: {request.error}");
                    return null;
                }
            }
        }

        #endregion

        public override void ForceReleaseAll()
        {
            base.ForceReleaseAll();
            _webglCache.Clear();
        }
    }
}
