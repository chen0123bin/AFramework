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
        public OnlineLoader(LWAssetsConfig config, CacheManager cacheManager,
            DownloadManager downloadManager)
            : base(config, cacheManager, downloadManager)
        {
        }

        public override async UniTask InitializeAsync(BundleManifest manifest)
        {
            m_Manifest = manifest;
            await UniTask.CompletedTask;
            UnityEngine.Debug.Log("[LWAssets] OnlineLoader initialized");
        }

        public override async UniTask<byte[]> LoadRawFileAsync(string assetPath, CancellationToken cancellationToken = default)
        {
            if (TryGetRawFileFromCache(assetPath, out byte[] cached))
            {
                return cached;
            }

            BundleInfo bundleInfo = m_Manifest.GetBundleByAsset(assetPath);
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

            string filePath = GetBundlePath(bundleInfo);

            Stopwatch sw = Stopwatch.StartNew();

            if (!File.Exists(filePath))
            {
                await m_DownloadManager.DownloadAsync(new[] { bundleInfo }, null, cancellationToken);
            }
            // 是否已缓存且有效
            if (!m_CacheManager.ValidateBundle(bundleInfo))
            {
                m_CacheManager.AddEntry(bundleInfo);
            }
            if (!File.Exists(filePath))
            {
                UnityEngine.Debug.LogError($"[LWAssets] Raw file not found: {filePath}");
                return null;
            }

            byte[] data = await File.ReadAllBytesAsync(filePath, cancellationToken);
            sw.Stop();
            RetainRawFileReference(assetPath, data, bundleInfo.BundleName, bundleInfo.Size, sw.Elapsed.TotalMilliseconds);
            return data;
        }

        protected override async UniTask<AssetBundle> LoadBundleFromSourceAsync(BundleInfo bundleInfo,
            CancellationToken cancellationToken = default)
        {
            string filePath = GetBundlePath(bundleInfo);

            // 检查是否需要下载
            if (!File.Exists(filePath))
            {
                // 下载Bundle
                await DownloadBundleAsync(bundleInfo, cancellationToken);

            }
            // 是否已缓存且有效
            if (!m_CacheManager.ValidateBundle(bundleInfo))
            {
                m_CacheManager.AddEntry(bundleInfo);
            }
            // 加载Bundle
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(filePath);
            await request;

            return request.assetBundle;
        }

        /// <summary>
        /// 下载Bundle
        /// </summary>
        private async UniTask DownloadBundleAsync(BundleInfo bundleInfo, CancellationToken cancellationToken = default)
        {
            await m_DownloadManager.DownloadAsync(new[] { bundleInfo }, null, cancellationToken);
        }

        protected override string GetBundlePath(BundleInfo bundleInfo)
        {
            // 优先从缓存目录加载
            string cachePath = Path.Combine(m_Config.GetPersistentDataPath(), bundleInfo.GetFileName());
            if (File.Exists(cachePath))
            {
                return cachePath;
            }

            // 其次从StreamingAssets加载
            string streamingPath = Path.Combine(m_Config.GetStreamingAssetsPath(), bundleInfo.GetFileName());
            if (File.Exists(streamingPath))
            {
                return streamingPath;
            }

            return cachePath; // 返回缓存路径，稍后下载
        }
    }
}
