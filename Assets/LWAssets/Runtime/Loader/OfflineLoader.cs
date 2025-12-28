using System;
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
            Debug.Log("[LWAssets] OfflineLoader initialized");
        }
        
        #region 异步加载实现
        
        public override async UniTask<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken = default)
        {
            var assetInfo = _manifest.GetAssetInfo(assetPath);
            if (assetInfo == null)
            {
                Debug.LogError($"[LWAssets] Asset not found in manifest: {assetPath}");
                return null;
            }
            
            // 加载Bundle
            var bundleHandle = await LoadBundleAsync(assetInfo.BundleName, cancellationToken);
            if (bundleHandle == null || !bundleHandle.IsValid)
            {
                Debug.LogError($"[LWAssets] Failed to load bundle: {assetInfo.BundleName}");
                return null;
            }
            
            // 从Bundle加载资源
            var assetName = Path.GetFileNameWithoutExtension(assetPath);
            var request = bundleHandle.Bundle.LoadAssetAsync<T>(assetName);
            await request;
            
            var asset = request.asset as T;
            if (asset != null)
            {
                TrackAsset(asset, assetInfo.BundleName);
            }
            
            return asset;
        }
        
        public override async UniTask<AssetHandle<T>> LoadAssetWithHandleAsync<T>(string assetPath, 
            CancellationToken cancellationToken = default)
        {
            var handle = new AssetHandle<T>(assetPath);
            
            try
            {
                var asset = await LoadAssetAsync<T>(assetPath, cancellationToken);
                if (asset != null)
                {
                    handle.SetAsset(asset);
                }
                else
                {
                    handle.SetError(new Exception($"Failed to load asset: {assetPath}"));
                }
            }
            catch (Exception ex)
            {
                handle.SetError(ex);
            }
            
            return handle;
        }
        
        public override async UniTask<byte[]> LoadRawFileAsync(string assetPath, CancellationToken cancellationToken = default)
        {
            var assetInfo = _manifest.GetAssetInfo(assetPath);
            if (assetInfo == null)
            {
                Debug.LogError($"[LWAssets] Raw file not found in manifest: {assetPath}");
                return null;
            }
            
            var bundleInfo = _manifest.GetBundleInfo(assetInfo.BundleName);
            if (bundleInfo == null)
            {
                return null;
            }
            
            var filePath = GetBundlePath(bundleInfo);
            if (!File.Exists(filePath))
            {
                Debug.LogError($"[LWAssets] Raw file not found: {filePath}");
                return null;
            }
            
            // 异步读取文件
            return await File.ReadAllBytesAsync(filePath, cancellationToken);
        }
        
        public override async UniTask<SceneHandle> LoadSceneAsync(string scenePath, LoadSceneMode mode, 
            bool activateOnLoad, CancellationToken cancellationToken = default)
        {
            var handle = new SceneHandle(scenePath);
            
            try
            {
                var assetInfo = _manifest.GetAssetInfo(scenePath);
                if (assetInfo == null)
                {
                    handle.SetError(new FileNotFoundException($"Scene not found: {scenePath}"));
                    return handle;
                }
                
                // 加载场景Bundle
                var bundleHandle = await LoadBundleAsync(assetInfo.BundleName, cancellationToken);
                if (bundleHandle == null || !bundleHandle.IsValid)
                {
                    handle.SetError(new Exception($"Failed to load scene bundle: {assetInfo.BundleName}"));
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
                
                handle.SetScene(SceneManager.GetSceneByName(sceneName));
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
                Debug.LogError($"[LWAssets] Bundle file not found: {filePath}");
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
