using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
                if (m_HandleBaseCache.TryGetValue(assetPath, out HandleBase cached) && cached is AssetHandle assetHandle && assetHandle.IsValid)
                {
                    cached.Retain();
                    RetainBundleReference(cached.BundleName);
                    return assetHandle.AssetObject as T;
                }
            }

            BundleInfo bundleInfo = m_Manifest.GetBundleByAsset(assetPath);
            if (bundleInfo == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Asset not found in manifest: {assetPath}");
                return null;
            }

            Stopwatch sw = Stopwatch.StartNew();
            BundleHandle bundleHandle = LoadBundleSync(bundleInfo.BundleName);
            if (bundleHandle == null || !bundleHandle.IsValid)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Failed to load bundle: {bundleInfo.BundleName}");
                return null;
            }

            string assetName = Path.GetFileNameWithoutExtension(assetPath);
            T asset = bundleHandle.Bundle.LoadAsset<T>(assetName);
            sw.Stop();

            if (asset == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Asset not found in bundle: {assetPath}");
                return null;
            }

            RetainAssetReference(assetPath, asset, bundleInfo.BundleName, sw.Elapsed.TotalMilliseconds);
            return asset;
        }

        public override byte[] LoadRawFile(string assetPath)
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
            if (!File.Exists(filePath))
            {
                UnityEngine.Debug.LogError($"[LWAssets] Raw file not found: {filePath}");
                return null;
            }

            Stopwatch sw = Stopwatch.StartNew();
            byte[] data = File.ReadAllBytes(filePath);
            sw.Stop();
            RetainRawFileReference(assetPath, data, bundleInfo.BundleName, bundleInfo.Size, sw.Elapsed.TotalMilliseconds);
            return data;
        }

        public override string LoadRawFileText(string assetPath)
        {
            byte[] bytes = LoadRawFile(assetPath);
            return bytes != null ? System.Text.Encoding.UTF8.GetString(bytes) : null;
        }

        private BundleHandle LoadBundleSync(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName))
            {
                UnityEngine.Debug.LogError("[LWAssets] Bundle name is null or empty");
                return null;
            }

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
                        return cached;
                    }
                }
            }

            BundleInfo bundleInfo = m_Manifest.GetBundleInfo(bundleName);
            if (bundleInfo == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Bundle not found: {bundleName}");
                return null;
            }

            Stopwatch sw = Stopwatch.StartNew();
            BundleHandle bundleHandle = new BundleHandle(bundleName);

            foreach (string depName in bundleInfo.Dependencies)
            {
                BundleHandle depBundleHandle = LoadBundleSync(depName);
                bundleHandle.AddDependency(depBundleHandle);
            }

            string bundlePath = GetBundlePath(bundleInfo);
            if (!File.Exists(bundlePath))
            {
                UnityEngine.Debug.LogError($"[LWAssets] Bundle file not found: {bundlePath}. 同步加载不支持自动下载，请改用异步接口或提前预下载。");
                return null;
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
            sw.Stop();
            bundleHandle.SetBundle(bundle);
            bundleHandle.SetLoadInfo(bundleInfo.Size, sw.Elapsed.TotalMilliseconds);

            lock (m_LockObj)
            {
                m_BundleHandleCache[bundleName] = bundleHandle;
            }

            return bundleHandle;
        }

        #endregion

        #region 异步加载实现

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
            if (!File.Exists(filePath))
            {
                UnityEngine.Debug.LogError($"[LWAssets] Raw file not found: {filePath}");
                return null;
            }

            Stopwatch sw = Stopwatch.StartNew();
            byte[] data = await File.ReadAllBytesAsync(filePath, cancellationToken);
            sw.Stop();
            RetainRawFileReference(assetPath, data, bundleInfo.BundleName, bundleInfo.Size, sw.Elapsed.TotalMilliseconds);
            return data;
        }

        #endregion

        #region Bundle加载

        protected override async UniTask<AssetBundle> LoadBundleFromSourceAsync(BundleInfo bundleInfo,
            CancellationToken cancellationToken = default)
        {
            string filePath = GetBundlePath(bundleInfo);

            if (!File.Exists(filePath))
            {
                UnityEngine.Debug.LogError($"[LWAssets] Bundle file not found: {filePath}");
                return null;
            }

            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(filePath);
            await request;

            return request.assetBundle;
        }

        /// <summary>
        /// 获取Bundle文件路径
        /// </summary>
        protected virtual string GetBundlePath(BundleInfo bundleInfo)
        {
            // 优先从缓存目录加载
            string cachePath = Path.Combine(m_Config.GetPersistentDataPath(), bundleInfo.GetFileName());
            if (File.Exists(cachePath))
            {
                return cachePath;
            }

            // 其次从StreamingAssets加载
            string streamingPath = Path.Combine(m_Config.GetStreamingAssetsPath(), bundleInfo.GetFileName());
            return streamingPath;
        }


        #endregion
    }
}
