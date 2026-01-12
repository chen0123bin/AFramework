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
        private CacheManager m_CacheManager;
        private DownloadManager m_DownloadManager;

        // WebGL缓存
        private readonly Dictionary<string, byte[]> m_WebglCache = new Dictionary<string, byte[]>();

        public WebGLLoader(LWAssetsConfig config, CacheManager cacheManager, DownloadManager downloadManager)
            : base(config)
        {
            m_CacheManager = cacheManager;
            m_DownloadManager = downloadManager;
        }

        public override async UniTask InitializeAsync(BundleManifest manifest)
        {
            m_Manifest = manifest;
            await UniTask.CompletedTask;
            UnityEngine.Debug.Log("[LWAssets] WebGLLoader initialized");
        }

        #region 异步加载实现

        public override async UniTask<byte[]> LoadRawFileAsync(string assetPath, CancellationToken cancellationToken = default)
        {
            if (TryGetRawFileFromCache(assetPath, out var cached))
            {
                return cached;
            }

            // 直接通过 manifest 获取 Bundle 信息
            var bundleInfo = m_Manifest.GetBundleByAsset(assetPath);
            if (bundleInfo == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Asset not found in manifest: {assetPath}");
                return null;
            }


            // WebGL从缓存或远程加载
            var cacheKey = bundleInfo.GetFileName();
            if (m_WebglCache.TryGetValue(cacheKey, out var cachedData))
            {
                return cachedData;
            }

            var sw = Stopwatch.StartNew();
            var url = m_Config.GetRemoteURL() + bundleInfo.GetFileName();
            using (var request = UnityWebRequest.Get(url))
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var data = request.downloadHandler.data;
                    m_WebglCache[cacheKey] = data;
                    sw.Stop();
                    TrackRawFileHandle(assetPath, data, bundleInfo.BundleName, bundleInfo.Size, sw.Elapsed.TotalMilliseconds);
                    return data;
                }
                else
                {
                    sw.Stop();
                    UnityEngine.Debug.LogError($"[LWAssets] Failed to download raw file: {url}, Error: {request.error}");
                    return null;
                }
            }
        }



        #endregion

        #region Bundle加载

        protected override async UniTask<AssetBundle> LoadBundleFromSourceAsync(BundleInfo bundleInfo,
            CancellationToken cancellationToken = default)
        {
            var url = m_Config.GetRemoteURL() + bundleInfo.GetFileName();

            // WebGL使用UnityWebRequest加载Bundle
            using (var request = UnityWebRequestAssetBundle.GetAssetBundle(url))
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
            m_WebglCache.Clear();
        }
    }
}
