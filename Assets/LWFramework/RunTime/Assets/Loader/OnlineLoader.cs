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
        private VersionManager m_VersionManager;

        public OnlineLoader(LWAssetsConfig config, CacheManager cacheManager,
            DownloadManager downloadManager, VersionManager versionManager)
            : base(config, cacheManager, downloadManager)
        {
            m_VersionManager = versionManager;
        }

        public override async UniTask InitializeAsync(BundleManifest manifest)
        {
            m_Manifest = manifest;
            await UniTask.CompletedTask;
            UnityEngine.Debug.Log("[LWAssets] OnlineLoader initialized");
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

            var sw = Stopwatch.StartNew();

            if (!File.Exists(filePath) || !m_CacheManager.ValidateBundle(bundleInfo))
            {
                await m_DownloadManager.DownloadAsync(new[] { bundleInfo }, null, cancellationToken);
                m_CacheManager.AddEntry(bundleInfo);
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
            if (!File.Exists(filePath) || !m_CacheManager.ValidateBundle(bundleInfo))
            {
                // 下载Bundle
                await DownloadBundleAsync(bundleInfo, cancellationToken);
                m_CacheManager.AddEntry(bundleInfo);
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
            var url = m_Config.GetRemoteURL() + bundleInfo.GetFileName();
            var savePath = Path.Combine(m_Config.GetPersistentDataPath(), bundleInfo.GetFileName());

            var task = new DownloadTask
            {
                Url = url,
                SavePath = savePath,
                ExpectedSize = bundleInfo.Size,
                ExpectedCRC = bundleInfo.CRC
            };

            await m_DownloadManager.DownloadAsync(new[] { bundleInfo }, null, cancellationToken);
        }

        protected override string GetBundlePath(BundleInfo bundleInfo)
        {
            // 优先从缓存目录加载
            var cachePath = Path.Combine(m_Config.GetPersistentDataPath(), bundleInfo.GetFileName());
            if (File.Exists(cachePath) && m_CacheManager.ValidateBundle(bundleInfo))
            {
                return cachePath;
            }

            // 其次从StreamingAssets加载
            var streamingPath = Path.Combine(m_Config.GetStreamingAssetsPath(), bundleInfo.GetFileName());
            if (File.Exists(streamingPath))
            {
                return streamingPath;
            }

            return cachePath; // 返回缓存路径，稍后下载
        }
    }
}
