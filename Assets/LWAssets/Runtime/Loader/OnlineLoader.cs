using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LWAssets
{
    /// <summary>
    /// 联机加载器 - 支持从服务器下载资源
    /// </summary>
    public class OnlineLoader : OfflineLoader
    {
        private VersionManager _versionManager;
        
        public OnlineLoader(LWAssetsConfig config, CacheManager cacheManager, 
            DownloadManager downloadManager, VersionManager versionManager) 
            : base(config, cacheManager, downloadManager)
        {
            _versionManager = versionManager;
        }
        
        public override async UniTask InitializeAsync(BundleManifest manifest)
        {
            _manifest = manifest;
            await UniTask.CompletedTask;
            Debug.Log("[LWAssets] OnlineLoader initialized");
        }
        
        protected override async UniTask<AssetBundle> LoadBundleFromSourceAsync(BundleInfo bundleInfo, 
            CancellationToken cancellationToken = default)
        {
            var filePath = GetBundlePath(bundleInfo);
            
            // 检查是否需要下载
            if (!File.Exists(filePath) || !_cacheManager.ValidateBundle(bundleInfo))
            {
                // 下载Bundle
                await DownloadBundleAsync(bundleInfo, cancellationToken);
            }
            
            // 加载Bundle
            var request = AssetBundle.LoadFromFileAsync(filePath);
            await request;
            
            return request.assetBundle;
        }
        
        /// <summary>
        /// 下载Bundle
        /// </summary>
        private async UniTask DownloadBundleAsync(BundleInfo bundleInfo, CancellationToken cancellationToken = default)
        {
            var url = _config.GetRemoteURL() + bundleInfo.GetFileName();
            var savePath = Path.Combine(_config.GetPersistentDataPath(), bundleInfo.GetFileName());
            
            var task = new DownloadTask
            {
                Url = url,
                SavePath = savePath,
                ExpectedSize = bundleInfo.Size,
                ExpectedCRC = bundleInfo.CRC
            };
            
            await _downloadManager.DownloadAsync(new[] { bundleInfo }, null, cancellationToken);
        }
        
        protected override string GetBundlePath(BundleInfo bundleInfo)
        {
            // 优先从缓存目录加载
            var cachePath = Path.Combine(_config.GetPersistentDataPath(), bundleInfo.GetFileName());
            if (File.Exists(cachePath) && _cacheManager.ValidateBundle(bundleInfo))
            {
                return cachePath;
            }
            
            // 其次从StreamingAssets加载
            var streamingPath = Path.Combine(_config.GetStreamingAssetsPath(), bundleInfo.GetFileName());
            if (File.Exists(streamingPath))
            {
                return streamingPath;
            }
            
            return cachePath; // 返回缓存路径，稍后下载
        }
    }
}
