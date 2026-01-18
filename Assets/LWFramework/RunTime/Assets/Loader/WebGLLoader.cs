using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LWAssets
{
    /// <summary>
    /// WebGL加载器 - 针对WebGL平台的特殊处理
    /// </summary>
    public class WebGLLoader : AssetLoaderBase
    {
        // WebGL缓存
        private readonly Dictionary<string, byte[]> m_WebglCache = new Dictionary<string, byte[]>();

        public WebGLLoader(LWAssetsConfig config)
            : base(config)
        {
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
            if (TryGetRawFileFromCache(assetPath, out byte[] cached))
            {
                return cached;
            }

            // 直接通过 manifest 获取 Bundle 信息
            BundleInfo bundleInfo = m_Manifest.GetBundleByAsset(assetPath);
            if (bundleInfo == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Asset not found in manifest: {assetPath}");
                return null;
            }


            // WebGL从缓存或远程加载
            string cacheKey = bundleInfo.GetFileName();
            if (m_WebglCache.TryGetValue(cacheKey, out byte[] cachedData))
            {
                return cachedData;
            }

            Stopwatch sw = Stopwatch.StartNew();
            string url = m_Config.GetRemoteURL() + bundleInfo.GetFileName();
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    byte[] data = request.downloadHandler.data;
                    m_WebglCache[cacheKey] = data;
                    sw.Stop();
                    RetainRawFileReference(assetPath, data, bundleInfo.BundleName, bundleInfo.Size, sw.Elapsed.TotalMilliseconds);
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
            string url = m_Config.GetRemoteURL() + bundleInfo.GetFileName();

            // WebGL使用UnityWebRequest加载Bundle
            using (UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(url))
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
