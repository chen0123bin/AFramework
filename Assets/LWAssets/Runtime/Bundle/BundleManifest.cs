using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWAssets
{
    /// <summary>
    /// Bundle清单
    /// </summary>
    [Serializable]
    public class BundleManifest
    {
        /// <summary>
        /// 版本号
        /// </summary>
        public string Version;
        
        /// <summary>
        /// 构建时间
        /// </summary>
        public string BuildTime;
        
        /// <summary>
        /// 平台
        /// </summary>
        public string Platform;
        
        /// <summary>
        /// Bundle列表
        /// </summary>
        public List<BundleInfo> Bundles = new List<BundleInfo>();
        
        /// <summary>
        /// 资源映射（资源路径 -> 资源信息）
        /// </summary>
        public List<AssetInfo> Assets = new List<AssetInfo>();
        
        // 运行时使用的字典缓存
        [NonSerialized] private Dictionary<string, BundleInfo> _bundleDict;
        [NonSerialized] private Dictionary<string, AssetInfo> _assetDict;
        [NonSerialized] private Dictionary<string, List<BundleInfo>> _tagBundleDict;
        
        /// <summary>
        /// 构建索引
        /// </summary>
        public void BuildIndex()
        {
            _bundleDict = new Dictionary<string, BundleInfo>();
            _assetDict = new Dictionary<string, AssetInfo>();
            _tagBundleDict = new Dictionary<string, List<BundleInfo>>();
            
            foreach (var bundle in Bundles)
            {
                _bundleDict[bundle.BundleName] = bundle;
                
                foreach (var tag in bundle.Tags)
                {
                    if (!_tagBundleDict.TryGetValue(tag, out var list))
                    {
                        list = new List<BundleInfo>();
                        _tagBundleDict[tag] = list;
                    }
                    list.Add(bundle);
                }
            }
            
            foreach (var asset in Assets)
            {
                _assetDict[asset.AssetPath] = asset;
            }
        }
        
        /// <summary>
        /// 获取Bundle信息
        /// </summary>
        public BundleInfo GetBundleInfo(string bundleName)
        {
            if (_bundleDict == null) BuildIndex();
            return _bundleDict.TryGetValue(bundleName, out var info) ? info : null;
        }
        
        /// <summary>
        /// 获取资源信息
        /// </summary>
        public AssetInfo GetAssetInfo(string assetPath)
        {
            if (_assetDict == null) BuildIndex();
            return _assetDict.TryGetValue(assetPath, out var info) ? info : null;
        }
        
        /// <summary>
        /// 获取资源所在的Bundle
        /// </summary>
        public BundleInfo GetBundleByAsset(string assetPath)
        {
            var assetInfo = GetAssetInfo(assetPath);
            if (assetInfo == null) return null;
            return GetBundleInfo(assetInfo.BundleName);
        }
        
        /// <summary>
        /// 获取指定标签的所有Bundle
        /// </summary>
        public List<BundleInfo> GetBundlesByTag(string tag)
        {
            if (_tagBundleDict == null) BuildIndex();
            return _tagBundleDict.TryGetValue(tag, out var list) ? list : new List<BundleInfo>();
        }
        
        /// <summary>
        /// 获取所有Bundle的依赖（递归）
        /// </summary>
        public List<BundleInfo> GetAllDependencies(string bundleName)
        {
            var result = new List<BundleInfo>();
            var visited = new HashSet<string>();
            
            CollectDependencies(bundleName, result, visited);
            
            return result;
        }
        
        private void CollectDependencies(string bundleName, List<BundleInfo> result, HashSet<string> visited)
        {
            if (visited.Contains(bundleName)) return;
            visited.Add(bundleName);
            
            var bundle = GetBundleInfo(bundleName);
            if (bundle == null) return;
            
            foreach (var depName in bundle.Dependencies)
            {
                var depBundle = GetBundleInfo(depName);
                if (depBundle != null && !result.Contains(depBundle))
                {
                    result.Add(depBundle);
                    CollectDependencies(depName, result, visited);
                }
            }
        }
        
        /// <summary>
        /// 计算所有Bundle总大小
        /// </summary>
        public long GetTotalSize()
        {
            return Bundles.Sum(b => b.Size);
        }
        
        /// <summary>
        /// 计算指定标签Bundle的总大小
        /// </summary>
        public long GetSizeByTag(string tag)
        {
            return GetBundlesByTag(tag).Sum(b => b.Size);
        }
        
        /// <summary>
        /// 序列化为JSON
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }
        
        /// <summary>
        /// 从JSON反序列化
        /// </summary>
        public static BundleManifest FromJson(string json)
        {
            var manifest = JsonUtility.FromJson<BundleManifest>(json);
            manifest.BuildIndex();
            return manifest;
        }
    }
}
