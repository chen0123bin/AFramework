using System;
using System.Diagnostics;
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
            UnityEngine.Debug.Log("[LWAssets] OnlineLoader initialized");
        }

        public override async UniTask<byte[]> LoadRawFileAsync(string assetPath, CancellationToken cancellationToken = default)
        {
            if (TryGetRawFileFromCache(assetPath, out var cached))
            {
                return cached;
            }

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

            var sw = Stopwatch.StartNew();

            if (!File.Exists(filePath) || !_cacheManager.ValidateBundle(bundleInfo))
            {
                await _downloadManager.DownloadAsync(new[] { bundleInfo }, null, cancellationToken);
                _cacheManager.AddEntry(bundleInfo);
            }

            if (!File.Exists(filePath))
            {
                UnityEngine.Debug.LogError($"[LWAssets] Raw file not found: {filePath}");
                return null;
            }

            var data = await File.ReadAllBytesAsync(filePath, cancellationToken);
            sw.Stop();
            TrackRawFileHandle(assetPath, data, bundleInfo.BundleName, bundleInfo.Size, sw.Elapsed.TotalMilliseconds);
            return data;
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
                _cacheManager.AddEntry(bundleInfo);
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
