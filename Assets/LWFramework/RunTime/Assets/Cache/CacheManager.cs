using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using LitJson;
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
        private static readonly object m_LitJsonLock = new object();
        private static bool m_LitJsonMappersRegistered;

        private readonly LWAssetsConfig m_Config;
        private CacheIndex m_CacheIndex;
        private readonly Dictionary<string, CacheEntry> m_EntryDict = new Dictionary<string, CacheEntry>();
        private readonly string m_CachePath;
        private readonly string m_IndexPath;
        private bool m_IsDirty;

        private readonly object m_LockObj = new object();

        public long TotalCacheSize => m_CacheIndex?.TotalSize ?? 0;
        public int EntryCount => m_CacheIndex?.Entries.Count ?? 0;
        public long MaxCacheSize => m_Config.MaxCacheSize;
        public float UsageRatio => MaxCacheSize > 0 ? (float)TotalCacheSize / MaxCacheSize : 0;

        public CacheManager(LWAssetsConfig config)
        {
            m_Config = config;
            m_CachePath = config.GetPersistentDataPath();
            m_IndexPath = Path.Combine(m_CachePath, "cache_index.json");

            EnsureLitJsonMappersRegistered();

            LoadIndex();
        }

        private static void EnsureLitJsonMappersRegistered()
        {
            if (m_LitJsonMappersRegistered) return;

            lock (m_LitJsonLock)
            {
                if (m_LitJsonMappersRegistered) return;

                JsonMapper.RegisterExporter<DateTime>((dt, writer) =>
                    writer.Write(dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)));

                JsonMapper.RegisterImporter<string, DateTime>(ParseDateTime);
                JsonMapper.RegisterImporter<long, DateTime>(ticks => new DateTime(ticks));
                JsonMapper.RegisterImporter<int, DateTime>(ticks => new DateTime(ticks));

                m_LitJsonMappersRegistered = true;
            }
        }

        private static DateTime ParseDateTime(string input)
        {
            if (string.IsNullOrEmpty(input)) return default;

            if (DateTime.TryParseExact(
                    input,
                    new[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss.FFFFFFFK", "yyyy-MM-ddTHH:mm:ssK" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var dt))
            {
                return dt;
            }

            if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out dt))
            {
                return dt;
            }

            return default;
        }

        #region 索引管理

        /// <summary>
        /// 加载缓存索引
        /// </summary>
        private void LoadIndex()
        {
            try
            {
                if (File.Exists(m_IndexPath))
                {
                    var json = File.ReadAllText(m_IndexPath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        m_CacheIndex = JsonMapper.ToObject<CacheIndex>(json);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LWAssets] Failed to load cache index: {ex.Message}");
            }

            if (m_CacheIndex == null)
            {
                m_CacheIndex = new CacheIndex();
            }

            if (m_CacheIndex.Entries == null)
            {
                m_CacheIndex.Entries = new List<CacheEntry>();
            }

            // 构建字典
            m_EntryDict.Clear();
            foreach (var entry in m_CacheIndex.Entries)
            {
                m_EntryDict[entry.FileName] = entry;
            }

            // 验证缓存文件
            ValidateCache();
        }

        /// <summary>
        /// 保存缓存索引
        /// </summary>
        public void SaveIndex()
        {
            if (!m_IsDirty) return;

            try
            {
                if (!Directory.Exists(m_CachePath))
                {
                    Directory.CreateDirectory(m_CachePath);
                }

                m_CacheIndex.LastUpdateTime = DateTime.Now;
                var json = JsonMapper.ToJson(m_CacheIndex, true);
                File.WriteAllText(m_IndexPath, json);
                m_IsDirty = false;
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

            foreach (var entry in m_CacheIndex.Entries)
            {
                var filePath = Path.Combine(m_CachePath, entry.FileName);
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

            m_CacheIndex.TotalSize = totalSize;

            if (toRemove.Count > 0)
            {
                m_IsDirty = true;
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
            lock (m_LockObj)
            {
                var fileName = bundleInfo.GetFileName();

                if (m_EntryDict.ContainsKey(fileName))
                {
                    // 更新访问时间
                    var entry = m_EntryDict[fileName];
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

                    m_CacheIndex.Entries.Add(entry);
                    m_EntryDict[fileName] = entry;
                    m_CacheIndex.TotalSize += entry.Size;
                }

                m_IsDirty = true;
                SaveIndex();
                // 检查是否需要清理
                if (m_Config.EnableAutoCleanup && UsageRatio >= m_Config.CleanupThreshold)
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
            lock (m_LockObj)
            {
                if (m_EntryDict.TryGetValue(fileName, out var entry))
                {
                    m_CacheIndex.Entries.Remove(entry);
                    m_EntryDict.Remove(fileName);
                    m_CacheIndex.TotalSize -= entry.Size;
                    m_IsDirty = true;
                }
            }
        }

        /// <summary>
        /// 检查Bundle是否已缓存且有效
        /// </summary>
        public bool ValidateBundle(BundleInfo bundleInfo)
        {
            lock (m_LockObj)
            {
                var fileName = bundleInfo.GetFileName();

                if (!m_EntryDict.TryGetValue(fileName, out var entry))
                {
                    return false;
                }

                // 验证哈希
                if (entry.Hash != bundleInfo.Hash)
                {
                    return false;
                }

                // 验证文件存在
                var filePath = Path.Combine(m_CachePath, fileName);
                if (!File.Exists(filePath))
                {
                    RemoveEntry(fileName);
                    return false;
                }

                // 更新访问时间
                entry.LastAccessTime = DateTime.Now;
                entry.AccessCount++;
                m_IsDirty = true;
                SaveIndex();
                return true;
            }
        }

        /// <summary>
        /// 获取缓存文件路径
        /// </summary>
        public string GetCachePath(BundleInfo bundleInfo)
        {
            return Path.Combine(m_CachePath, bundleInfo.GetFileName());
        }

        /// <summary>
        /// 检查缓存是否存在
        /// </summary>
        public bool HasCache(string fileName)
        {
            lock (m_LockObj)
            {
                return m_EntryDict.ContainsKey(fileName);
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
                targetFreeSpace = (long)(m_Config.MaxCacheSize * (1 - m_Config.CleanupThreshold * 0.8f));
            }

            Debug.Log($"[LWAssets] Starting cache cleanup, target free space: {targetFreeSpace / 1024 / 1024}MB");

            var freedSpace = 0L;
            var toRemove = new List<CacheEntry>();

            lock (m_LockObj)
            {
                // 按LRU策略排序
                var sortedEntries = m_CacheIndex.Entries
                    .OrderBy(e => e.LastAccessTime)
                    .ThenBy(e => e.AccessCount)
                    .ToList();

                foreach (var entry in sortedEntries)
                {
                    if (freedSpace >= targetFreeSpace) break;

                    // 检查是否过期
                    var daysSinceAccess = (DateTime.Now - entry.LastAccessTime).TotalDays;
                    if (daysSinceAccess > m_Config.CacheExpirationDays || freedSpace < targetFreeSpace)
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
                    var filePath = Path.Combine(m_CachePath, entry.FileName);
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
            lock (m_LockObj)
            {
                // 删除所有缓存文件
                if (Directory.Exists(m_CachePath))
                {
                    var files = Directory.GetFiles(m_CachePath);
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

                m_CacheIndex.Entries.Clear();
                m_EntryDict.Clear();
                m_CacheIndex.TotalSize = 0;
                m_IsDirty = true;
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
                var filePath = Path.Combine(m_CachePath, fileName);

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
            lock (m_LockObj)
            {
                return new CacheStatistics
                {
                    TotalSize = m_CacheIndex.TotalSize,
                    EntryCount = m_CacheIndex.Entries.Count,
                    MaxSize = m_Config.MaxCacheSize,
                    OldestAccessTime = m_CacheIndex.Entries.Count > 0
                        ? m_CacheIndex.Entries.Min(e => e.LastAccessTime)
                        : DateTime.Now,
                    NewestAccessTime = m_CacheIndex.Entries.Count > 0
                        ? m_CacheIndex.Entries.Max(e => e.LastAccessTime)
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
