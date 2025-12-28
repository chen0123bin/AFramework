using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LWAssets
{
    /// <summary>
    /// 缓存条目信息
    /// </summary>
    [Serializable]
    public class CacheEntry
    {
        public string FileName;
        public string Hash;
        public long Size;
        public DateTime LastAccessTime;
        public DateTime CreateTime;
        public int AccessCount;
    }
    
    /// <summary>
    /// 缓存索引
    /// </summary>
    [Serializable]
    public class CacheIndex
    {
        public List<CacheEntry> Entries = new List<CacheEntry>();
        public long TotalSize;
        public DateTime LastUpdateTime;
    }
    
    /// <summary>
    /// 缓存管理器
    /// </summary>
    public class CacheManager : IDisposable
    {
        private readonly LWAssetsConfig _config;
        private CacheIndex _cacheIndex;
        private readonly Dictionary<string, CacheEntry> _entryDict = new Dictionary<string, CacheEntry>();
        private readonly string _cachePath;
        private readonly string _indexPath;
        private bool _isDirty;
        
        private readonly object _lockObj = new object();
        
        public long TotalCacheSize => _cacheIndex?.TotalSize ?? 0;
        public int EntryCount => _cacheIndex?.Entries.Count ?? 0;
        public long MaxCacheSize => _config.MaxCacheSize;
        public float UsageRatio => MaxCacheSize > 0 ? (float)TotalCacheSize / MaxCacheSize : 0;
        
        public CacheManager(LWAssetsConfig config)
        {
            _config = config;
            _cachePath = config.GetPersistentDataPath();
            _indexPath = Path.Combine(_cachePath, "cache_index.json");
            
            LoadIndex();
        }
        
        #region 索引管理
        
        /// <summary>
        /// 加载缓存索引
        /// </summary>
        private void LoadIndex()
        {
            try
            {
                if (File.Exists(_indexPath))
                {
                    var json = File.ReadAllText(_indexPath);
                    _cacheIndex = JsonUtility.FromJson<CacheIndex>(json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LWAssets] Failed to load cache index: {ex.Message}");
            }
            
            if (_cacheIndex == null)
            {
                _cacheIndex = new CacheIndex();
            }
            
            // 构建字典
            _entryDict.Clear();
            foreach (var entry in _cacheIndex.Entries)
            {
                _entryDict[entry.FileName] = entry;
            }
            
            // 验证缓存文件
            ValidateCache();
        }
        
        /// <summary>
        /// 保存缓存索引
        /// </summary>
        public void SaveIndex()
        {
            if (!_isDirty) return;
            
            try
            {
                if (!Directory.Exists(_cachePath))
                {
                    Directory.CreateDirectory(_cachePath);
                }
                
                _cacheIndex.LastUpdateTime = DateTime.Now;
                var json = JsonUtility.ToJson(_cacheIndex, true);
                File.WriteAllText(_indexPath, json);
                _isDirty = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LWAssets] Failed to save cache index: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 验证缓存
        /// </summary>
        private void ValidateCache()
        {
            var toRemove = new List<string>();
            long totalSize = 0;
            
            foreach (var entry in _cacheIndex.Entries)
            {
                var filePath = Path.Combine(_cachePath, entry.FileName);
                if (!File.Exists(filePath))
                {
                    toRemove.Add(entry.FileName);
                    continue;
                }
                
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length != entry.Size)
                {
                    toRemove.Add(entry.FileName);
                    File.Delete(filePath);
                    continue;
                }
                
                totalSize += entry.Size;
            }
            
            // 移除无效条目
            foreach (var fileName in toRemove)
            {
                RemoveEntry(fileName);
            }
            
            _cacheIndex.TotalSize = totalSize;
            
            if (toRemove.Count > 0)
            {
                _isDirty = true;
                SaveIndex();
            }
        }
        
        #endregion
        
        #region 缓存操作
        
        /// <summary>
        /// 添加缓存条目
        /// </summary>
        public void AddEntry(BundleInfo bundleInfo)
        {
            lock (_lockObj)
            {
                var fileName = bundleInfo.GetFileName();
                
                if (_entryDict.ContainsKey(fileName))
                {
                    // 更新访问时间
                    var entry = _entryDict[fileName];
                    entry.LastAccessTime = DateTime.Now;
                    entry.AccessCount++;
                }
                else
                {
                    var entry = new CacheEntry
                    {
                        FileName = fileName,
                        Hash = bundleInfo.Hash,
                        Size = bundleInfo.Size,
                        CreateTime = DateTime.Now,
                        LastAccessTime = DateTime.Now,
                        AccessCount = 1
                    };
                    
                    _cacheIndex.Entries.Add(entry);
                    _entryDict[fileName] = entry;
                    _cacheIndex.TotalSize += entry.Size;
                }
                
                _isDirty = true;
                
                // 检查是否需要清理
                if (_config.EnableAutoCleanup && UsageRatio >= _config.CleanupThreshold)
                {
                    CleanupAsync().Forget();
                }
            }
        }
        
        /// <summary>
        /// 移除缓存条目
        /// </summary>
        public void RemoveEntry(string fileName)
        {
            lock (_lockObj)
            {
                if (_entryDict.TryGetValue(fileName, out var entry))
                {
                    _cacheIndex.Entries.Remove(entry);
                    _entryDict.Remove(fileName);
                    _cacheIndex.TotalSize -= entry.Size;
                    _isDirty = true;
                }
            }
        }
        
        /// <summary>
        /// 检查Bundle是否已缓存且有效
        /// </summary>
        public bool ValidateBundle(BundleInfo bundleInfo)
        {
            lock (_lockObj)
            {
                var fileName = bundleInfo.GetFileName();
                
                if (!_entryDict.TryGetValue(fileName, out var entry))
                {
                    return false;
                }
                
                // 验证哈希
                if (entry.Hash != bundleInfo.Hash)
                {
                    return false;
                }
                
                // 验证文件存在
                var filePath = Path.Combine(_cachePath, fileName);
                if (!File.Exists(filePath))
                {
                    RemoveEntry(fileName);
                    return false;
                }
                
                // 更新访问时间
                entry.LastAccessTime = DateTime.Now;
                entry.AccessCount++;
                _isDirty = true;
                
                return true;
            }
        }
        
        /// <summary>
        /// 获取缓存文件路径
        /// </summary>
        public string GetCachePath(BundleInfo bundleInfo)
        {
            return Path.Combine(_cachePath, bundleInfo.GetFileName());
        }
        
        /// <summary>
        /// 检查缓存是否存在
        /// </summary>
        public bool HasCache(string fileName)
        {
            lock (_lockObj)
            {
                return _entryDict.ContainsKey(fileName);
            }
        }
        
        #endregion
        
        #region 缓存清理
        
        /// <summary>
        /// 清理缓存
        /// </summary>
        public async UniTask CleanupAsync(long targetFreeSpace = 0)
        {
            if (targetFreeSpace <= 0)
            {
                targetFreeSpace = (long)(_config.MaxCacheSize * (1 - _config.CleanupThreshold * 0.8f));
            }
            
            Debug.Log($"[LWAssets] Starting cache cleanup, target free space: {targetFreeSpace / 1024 / 1024}MB");
            
            var freedSpace = 0L;
            var toRemove = new List<CacheEntry>();
            
            lock (_lockObj)
            {
                // 按LRU策略排序
                var sortedEntries = _cacheIndex.Entries
                    .OrderBy(e => e.LastAccessTime)
                    .ThenBy(e => e.AccessCount)
                    .ToList();
                
                foreach (var entry in sortedEntries)
                {
                    if (freedSpace >= targetFreeSpace) break;
                    
                    // 检查是否过期
                    var daysSinceAccess = (DateTime.Now - entry.LastAccessTime).TotalDays;
                    if (daysSinceAccess > _config.CacheExpirationDays || freedSpace < targetFreeSpace)
                    {
                        toRemove.Add(entry);
                        freedSpace += entry.Size;
                    }
                }
            }
            
            // 删除文件
            foreach (var entry in toRemove)
            {
                try
                {
                    var filePath = Path.Combine(_cachePath, entry.FileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    RemoveEntry(entry.FileName);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LWAssets] Failed to delete cache file: {entry.FileName}, Error: {ex.Message}");
                }
                
                await UniTask.Yield();
            }
            
            SaveIndex();
            Debug.Log($"[LWAssets] Cache cleanup completed, freed {freedSpace / 1024 / 1024}MB");
        }
        
        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void ClearAll()
        {
            lock (_lockObj)
            {
                // 删除所有缓存文件
                if (Directory.Exists(_cachePath))
                {
                    var files = Directory.GetFiles(_cachePath);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[LWAssets] Failed to delete file: {file}, Error: {ex.Message}");
                        }
                    }
                }
                
                _cacheIndex.Entries.Clear();
                _entryDict.Clear();
                _cacheIndex.TotalSize = 0;
                _isDirty = true;
                SaveIndex();
            }
            
            Debug.Log("[LWAssets] All cache cleared");
        }
        
        /// <summary>
        /// 清除指定标签的缓存
        /// </summary>
        public void ClearByTag(string tag, BundleManifest manifest)
        {
            var bundles = manifest.GetBundlesByTag(tag);
            foreach (var bundle in bundles)
            {
                var fileName = bundle.GetFileName();
                var filePath = Path.Combine(_cachePath, fileName);
                
                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch { }
                }
                
                RemoveEntry(fileName);
            }
            
            SaveIndex();
        }
        
        #endregion
        
        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            lock (_lockObj)
            {
                return new CacheStatistics
                {
                    TotalSize = _cacheIndex.TotalSize,
                    EntryCount = _cacheIndex.Entries.Count,
                    MaxSize = _config.MaxCacheSize,
                    OldestAccessTime = _cacheIndex.Entries.Count > 0 
                        ? _cacheIndex.Entries.Min(e => e.LastAccessTime) 
                        : DateTime.Now,
                    NewestAccessTime = _cacheIndex.Entries.Count > 0 
                        ? _cacheIndex.Entries.Max(e => e.LastAccessTime) 
                        : DateTime.Now
                };
            }
        }
        
        public void Dispose()
        {
            SaveIndex();
        }
    }
    
    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public struct CacheStatistics
    {
        public long TotalSize;
        public int EntryCount;
        public long MaxSize;
        public DateTime OldestAccessTime;
        public DateTime NewestAccessTime;
        public float UsageRatio => MaxSize > 0 ? (float)TotalSize / MaxSize : 0;
    }
}
