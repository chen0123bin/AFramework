using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LWAssets
{
    /// <summary>
    /// 版本信息
    /// </summary>
    [Serializable]
    public class VersionInfo
    {
        public string Version;
        public string ManifestHash;
        public long ManifestSize;
        public string BuildTime;
        public string MinAppVersion;
        public bool ForceUpdate;
    }
    
    /// <summary>
    /// 版本管理器
    /// </summary>
    public class VersionManager
    {
        private readonly LWAssetsConfig m_Config;
        private readonly CacheManager m_CacheManager;
        
        private VersionInfo m_LocalVersion;
        private VersionInfo m_RemoteVersion;
        private BundleManifest m_LocalManifest;
        private BundleManifest m_RemoteManifest;
        
        public VersionInfo LocalVersion => m_LocalVersion;
        public VersionInfo RemoteVersion => m_RemoteVersion;
        public bool HasNewVersion => m_RemoteVersion != null && m_LocalVersion?.Version != m_RemoteVersion.Version;
        
        public VersionManager(LWAssetsConfig config, CacheManager cacheManager)
        {
            m_Config = config;
            m_CacheManager = cacheManager;
        }
        
        #region 初始化
        
        /// <summary>
        /// 初始化版本信息
        /// </summary>
        public async UniTask InitializeAsync()
        {
            // 加载本地版本信息
            m_LocalVersion = await LoadLocalVersionAsync();
            
            // 如果是在线模式，检查远程版本
            if (m_Config.PlayMode == PlayMode.Online)
            {
                try
                {
                    m_RemoteVersion = await LoadRemoteVersionAsync();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LWAssets] Failed to load remote version: {ex.Message}");
                }
            }
            
            Debug.Log($"[LWAssets] Version initialized - Local: {m_LocalVersion?.Version}, Remote: {m_RemoteVersion?.Version}");
        }
        
        /// <summary>
        /// 加载本地版本信息
        /// </summary>
        private async UniTask<VersionInfo> LoadLocalVersionAsync()
        {
            // 优先从缓存目录加载
            var cachePath = Path.Combine(m_Config.GetPersistentDataPath(), m_Config.VersionFileName);
            if (File.Exists(cachePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(cachePath);
                    return JsonUtility.FromJson<VersionInfo>(json);
                }
                catch { }
            }
            
            // 其次从StreamingAssets加载
            var streamingPath = Path.Combine(m_Config.GetStreamingAssetsPath(), m_Config.VersionFileName);
            
            #if UNITY_ANDROID && !UNITY_EDITOR
            // Android需要使用UnityWebRequest
            using (var request = UnityWebRequest.Get(streamingPath))
            {
                await request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    return JsonUtility.FromJson<VersionInfo>(request.downloadHandler.text);
                }
            }
            #else
            if (File.Exists(streamingPath))
            {
                var json = await File.ReadAllTextAsync(streamingPath);
                return JsonUtility.FromJson<VersionInfo>(json);
            }
            #endif
            
            return null;
        }
        
        /// <summary>
        /// 加载远程版本信息
        /// </summary>
        private async UniTask<VersionInfo> LoadRemoteVersionAsync()
        {
            var url = m_Config.GetRemoteURL() + m_Config.VersionFileName;
            
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = 10;
                await request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    return JsonUtility.FromJson<VersionInfo>(request.downloadHandler.text);
                }
                else
                {
                    throw new Exception($"Failed to load remote version: {request.error}");
                }
            }
        }
        
        #endregion
        
        #region 清单管理
        
        /// <summary>
        /// 加载清单文件
        /// </summary>
        public async UniTask<BundleManifest> LoadManifestAsync()
        {
            // 检查是否需要更新清单
            if (m_Config.PlayMode == PlayMode.Online && HasNewVersion)
            {
                try
                {
                    var manifest = await LoadRemoteManifestAsync();
                    if (manifest != null)
                    {
                        // 保存到本地
                        await SaveManifestAsync(manifest);
                        await SaveVersionAsync(m_RemoteVersion);
                        m_LocalManifest = manifest;
                        m_LocalVersion = m_RemoteVersion;
                        return manifest;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LWAssets] Failed to load remote manifest: {ex.Message}");
                }
            }
            
            // 加载本地清单
            m_LocalManifest = await LoadLocalManifestAsync();
            return m_LocalManifest;
        }
        
        /// <summary>
        /// 加载本地清单
        /// </summary>
        private async UniTask<BundleManifest> LoadLocalManifestAsync()
        {
            // 优先从缓存目录加载
            var cachePath = Path.Combine(m_Config.GetPersistentDataPath(), m_Config.ManifestFileName);
            if (File.Exists(cachePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(cachePath);
                    return BundleManifest.FromJson(json);
                }
                catch { }
            }
            
            // 其次从StreamingAssets加载
            var streamingPath = Path.Combine(m_Config.GetStreamingAssetsPath(), m_Config.ManifestFileName);
            
            #if UNITY_ANDROID && !UNITY_EDITOR
            using (var request = UnityWebRequest.Get(streamingPath))
            {
                await request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    return BundleManifest.FromJson(request.downloadHandler.text);
                }
            }
            #else
            if (File.Exists(streamingPath))
            {
                var json = await File.ReadAllTextAsync(streamingPath);
                return BundleManifest.FromJson(json);
            }
            #endif
            
            return new BundleManifest();
        }
        
        /// <summary>
        /// 加载远程清单
        /// </summary>
        private async UniTask<BundleManifest> LoadRemoteManifestAsync()
        {
            var url = m_Config.GetRemoteURL() + m_Config.ManifestFileName;
            
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = 30;
                await request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    m_RemoteManifest = BundleManifest.FromJson(request.downloadHandler.text);
                    return m_RemoteManifest;
                }
                else
                {
                    throw new Exception($"Failed to load remote manifest: {request.error}");
                }
            }
        }
        
        /// <summary>
        /// 保存清单到本地
        /// </summary>
        private async UniTask SaveManifestAsync(BundleManifest manifest)
        {
            var path = Path.Combine(m_Config.GetPersistentDataPath(), m_Config.ManifestFileName);
            var directory = Path.GetDirectoryName(path);
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var json = manifest.ToJson();
            await File.WriteAllTextAsync(path, json);
        }
        
        /// <summary>
        /// 保存版本信息到本地
        /// </summary>
        private async UniTask SaveVersionAsync(VersionInfo version)
        {
            var path = Path.Combine(m_Config.GetPersistentDataPath(), m_Config.VersionFileName);
            var json = JsonUtility.ToJson(version);
            await File.WriteAllTextAsync(path, json);
        }
        
        #endregion
        
        #region 更新检测
        
        /// <summary>
        /// 检查更新
        /// </summary>
        public async UniTask<UpdateCheckResult> CheckUpdateAsync()
        {
            var result = new UpdateCheckResult();
            
            try
            {
                m_RemoteVersion = await LoadRemoteVersionAsync();
                
                if (m_RemoteVersion == null)
                {
                    result.Status = UpdateStatus.CheckFailed;
                    result.Error = "Failed to load remote version";
                    return result;
                }
                
                if (m_LocalVersion?.Version == m_RemoteVersion.Version)
                {
                    result.Status = UpdateStatus.NoUpdate;
                    return result;
                }
                
                // 检查是否强制更新
                if (m_RemoteVersion.ForceUpdate)
                {
                    result.Status = UpdateStatus.ForceUpdate;
                }
                else
                {
                    result.Status = UpdateStatus.OptionalUpdate;
                }
                
                result.LocalVersion = m_LocalVersion?.Version;
                result.RemoteVersion = m_RemoteVersion.Version;
                
                // 加载远程清单计算下载大小
                var remoteManifest = await LoadRemoteManifestAsync();
                var downloadBundles = await GetBundlesToDownloadAsync(null, remoteManifest);
                result.DownloadSize = downloadBundles.Sum(b => b.Size);
                result.DownloadCount = downloadBundles.Count;
            }
            catch (Exception ex)
            {
                result.Status = UpdateStatus.CheckFailed;
                result.Error = ex.Message;
            }
            
            return result;
        }
        
        /// <summary>
        /// 获取需要下载的Bundle大小
        /// </summary>
        public async UniTask<long> GetDownloadSizeAsync(string[] tags = null)
        {
            var bundles = await GetBundlesToDownloadAsync(tags);
            return bundles.Sum(b => b.Size);
        }
        
        /// <summary>
        /// 获取需要下载的Bundle列表
        /// </summary>
        public async UniTask<List<BundleInfo>> GetBundlesToDownloadAsync(string[] tags = null, 
            BundleManifest manifest = null)
        {
            if (manifest == null)
            {
                manifest = m_RemoteManifest ?? m_LocalManifest;
            }
            
            if (manifest == null)
            {
                return new List<BundleInfo>();
            }
            
            var bundlesToDownload = new List<BundleInfo>();
            var bundlesToCheck = tags != null && tags.Length > 0
                ? tags.SelectMany(t => manifest.GetBundlesByTag(t)).Distinct().ToList()
                : manifest.Bundles;
            
            foreach (var bundle in bundlesToCheck)
            {
                // 检查缓存
                if (m_CacheManager.ValidateBundle(bundle))
                {
                    continue;
                }
                
                // 检查StreamingAssets
                var streamingPath = Path.Combine(m_Config.GetStreamingAssetsPath(), bundle.GetFileName());
                if (File.Exists(streamingPath))
                {
                    continue;
                }
                
                bundlesToDownload.Add(bundle);
            }
            
            await UniTask.CompletedTask;
            return bundlesToDownload;
        }
        
        #endregion
    }
    
    /// <summary>
    /// 更新状态
    /// </summary>
    public enum UpdateStatus
    {
        NoUpdate,
        OptionalUpdate,
        ForceUpdate,
        CheckFailed
    }
    
    /// <summary>
    /// 更新检查结果
    /// </summary>
    public struct UpdateCheckResult
    {
        public UpdateStatus Status;
        public string LocalVersion;
        public string RemoteVersion;
        public long DownloadSize;
        public int DownloadCount;
        public string Error;
    }
}
