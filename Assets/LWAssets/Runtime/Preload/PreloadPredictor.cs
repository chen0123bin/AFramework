using System;
using System.Collections.Generic;
using System.Linq;

namespace LWAssets
{
    /// <summary>
    /// 预加载预测器 - 基于访问模式预测可能需要的资源
    /// </summary>
    public class PreloadPredictor
    {
        /// <summary>
        /// 访问记录
        /// </summary>
        private class AccessRecord
        {
            public string AssetPath;
            public DateTime LastAccessTime;
            public int AccessCount;
            public List<string> NextAssets = new List<string>(); // 之后访问的资源
        }
        
        private readonly Dictionary<string, AccessRecord> _records = new Dictionary<string, AccessRecord>();
        private readonly List<string> _recentAccesses = new List<string>();
        private const int MaxRecentAccesses = 100;
        private const int MaxNextAssets = 20;
        
        /// <summary>
        /// 记录资源访问
        /// </summary>
        public void RecordAccess(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return;
            
            // 更新访问记录
            if (!_records.TryGetValue(assetPath, out var record))
            {
                record = new AccessRecord { AssetPath = assetPath };
                _records[assetPath] = record;
            }
            
            record.LastAccessTime = DateTime.Now;
            record.AccessCount++;
            
            // 更新之前资源的"下一个访问"
            if (_recentAccesses.Count > 0)
            {
                var lastAccess = _recentAccesses[_recentAccesses.Count - 1];
                if (_records.TryGetValue(lastAccess, out var lastRecord))
                {
                    if (!lastRecord.NextAssets.Contains(assetPath))
                    {
                        lastRecord.NextAssets.Add(assetPath);
                        if (lastRecord.NextAssets.Count > MaxNextAssets)
                        {
                            lastRecord.NextAssets.RemoveAt(0);
                        }
                    }
                }
            }
            
            // 添加到最近访问
            _recentAccesses.Add(assetPath);
            if (_recentAccesses.Count > MaxRecentAccesses)
            {
                _recentAccesses.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// 预测可能需要的资源
        /// </summary>
        public List<string> Predict(string currentAsset, int maxCount = 5)
        {
            var predictions = new Dictionary<string, float>();
            
            if (!string.IsNullOrEmpty(currentAsset) && _records.TryGetValue(currentAsset, out var record))
            {
                // 基于访问模式预测
                foreach (var next in record.NextAssets)
                {
                    if (!predictions.ContainsKey(next))
                    {
                        predictions[next] = 0;
                    }
                    predictions[next] += 1.0f;
                }
            }
            
            // 基于访问频率补充
            var frequentAssets = _records.Values
                .OrderByDescending(r => r.AccessCount)
                .Take(maxCount * 2);
            
            foreach (var r in frequentAssets)
            {
                if (r.AssetPath == currentAsset) continue;
                
                if (!predictions.ContainsKey(r.AssetPath))
                {
                    predictions[r.AssetPath] = 0;
                }
                predictions[r.AssetPath] += r.AccessCount * 0.1f;
            }
            
            // 基于时间衰减的最近访问
            var recentWeight = 1.0f;
            for (int i = _recentAccesses.Count - 1; i >= 0 && i >= _recentAccesses.Count - 10; i--)
            {
                var asset = _recentAccesses[i];
                if (asset == currentAsset) continue;
                
                if (!predictions.ContainsKey(asset))
                {
                    predictions[asset] = 0;
                }
                predictions[asset] += recentWeight * 0.5f;
                recentWeight *= 0.8f;
            }
            
            // 排序并返回
            return predictions
                .OrderByDescending(p => p.Value)
                .Take(maxCount)
                .Select(p => p.Key)
                .ToList();
        }
        
        /// <summary>
        /// 清除所有记录
        /// </summary>
        public void Clear()
        {
            _records.Clear();
            _recentAccesses.Clear();
        }
    }
}
