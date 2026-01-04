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
        private readonly LWAssetsConfig _config;
        private readonly CacheManager _cacheManager;
        
        private VersionInfo _localVersion;
        private VersionInfo _remoteVersion;
        private BundleManifest _localManifest;
        private BundleManifest _remoteManifest;
        
        public VersionInfo LocalVersion => _localVersion;
        public VersionInfo RemoteVersion => _remoteVersion;
        public bool HasNewVersion => _remoteVersion != null && _localVersion?.Version != _remoteVersion.Version;
        
        public VersionManager(LWAssetsConfig config, CacheManager cacheManager)
        {
            _config = config;
            _cacheManager = cacheManager;
        }
        
        #region 初始化
        
        /// <summary>
        /// 初始化版本信息
        /// </summary>
        public async UniTask InitializeAsync()
        {
            // 加载本地版本信息
            _localVersion = await LoadLocalVersionAsync();
            
            // 如果是在线模式，检查远程版本
            if (_config.PlayMode == PlayMode.Online)
            {
                try
                {
                    _remoteVersion = await LoadRemoteVersionAsync();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LWAssets] Failed to load remote version: {ex.Message}");
                }
            }
            
            Debug.Log($"[LWAssets] Version initialized - Local: {_localVersion?.Version}, Remote: {_remoteVersion?.Version}");
        }
        
        /// <summary>
        /// 加载本地版本信息
        /// </summary>
        private async UniTask<VersionInfo> LoadLocalVersionAsync()
        {
            // 优先从缓存目录加载
            var cachePath = Path.Combine(_config.GetPersistentDataPath(), _config.VersionFileName);
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
            var streamingPath = Path.Combine(_config.GetStreamingAssetsPath(), _config.VersionFileName);
            
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
            var url = _config.GetRemoteURL() + _config.VersionFileName;
            
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
            if (_config.PlayMode == PlayMode.Online && HasNewVersion)
            {
                try
                {
                    var manifest = await LoadRemoteManifestAsync();
                    if (manifest != null)
                    {
                        // 保存到本地
                        await SaveManifestAsync(manifest);
                        await SaveVersionAsync(_remoteVersion);
                        _localManifest = manifest;
                        _localVersion = _remoteVersion;
                        return manifest;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LWAssets] Failed to load remote manifest: {ex.Message}");
                }
            }
            
            // 加载本地清单
            _localManifest = await LoadLocalManifestAsync();
            return _localManifest;
        }
        
        /// <summary>
        /// 加载本地清单
        /// </summary>
        private async UniTask<BundleManifest> LoadLocalManifestAsync()
        {
            // 优先从缓存目录加载
            var cachePath = Path.Combine(_config.GetPersistentDataPath(), _config.ManifestFileName);
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
            var streamingPath = Path.Combine(_config.GetStreamingAssetsPath(), _config.ManifestFileName);
            
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
            var url = _config.GetRemoteURL() + _config.ManifestFileName;
            
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = 30;
                await request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    _remoteManifest = BundleManifest.FromJson(request.downloadHandler.text);
                    return _remoteManifest;
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
            var path = Path.Combine(_config.GetPersistentDataPath(), _config.ManifestFileName);
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
            var path = Path.Combine(_config.GetPersistentDataPath(), _config.VersionFileName);
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
                _remoteVersion = await LoadRemoteVersionAsync();
                
                if (_remoteVersion == null)
                {
                    result.Status = UpdateStatus.CheckFailed;
                    result.Error = "Failed to load remote version";
                    return result;
                }
                
                if (_localVersion?.Version == _remoteVersion.Version)
                {
                    result.Status = UpdateStatus.NoUpdate;
                    return result;
                }
                
                // 检查是否强制更新
                if (_remoteVersion.ForceUpdate)
                {
                    result.Status = UpdateStatus.ForceUpdate;
                }
                else
                {
                    result.Status = UpdateStatus.OptionalUpdate;
                }
                
                result.LocalVersion = _localVersion?.Version;
                result.RemoteVersion = _remoteVersion.Version;
                
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
                manifest = _remoteManifest ?? _localManifest;
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
                if (_cacheManager.ValidateBundle(bundle))
                {
                    continue;
                }
                
                // 检查StreamingAssets
                var streamingPath = Path.Combine(_config.GetStreamingAssetsPath(), bundle.GetFileName());
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
