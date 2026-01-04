using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LitJson;

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
        /// Bundle名称 -> BundleInfo 的映射
        /// </summary>
        [NonSerialized] private Dictionary<string, BundleInfo> _bundleDict;
        /// <summary>
        /// 资源路径 -> BundleInfo 的映射（运行时构建）
        /// </summary>
        [NonSerialized]
        private Dictionary<string, BundleInfo> _assetToBundleDict;

        /// <summary>
        /// 标签 -> Bundle列表 的映射
        /// </summary>
        [NonSerialized] private Dictionary<string, List<BundleInfo>> _tagBundleDict;

        /// <summary>
        /// 构建运行时索引
        /// </summary>
        public void BuildIndex()
        {


            _bundleDict = new Dictionary<string, BundleInfo>(Bundles.Count);
            _assetToBundleDict = new Dictionary<string, BundleInfo>();
            _tagBundleDict = new Dictionary<string, List<BundleInfo>>();

            foreach (var bundle in Bundles)
            {
                // 1. 构建Bundle名称索引
                _bundleDict[bundle.BundleName] = bundle;

                // 2. 构建资源路径 -> Bundle 索引
                foreach (var assetPath in bundle.Assets)
                {
                    _assetToBundleDict[assetPath] = bundle;
                }

                // 3. 构建标签索引
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
        /// 根据资源路径获取所属的BundleInfo
        /// </summary>
        public BundleInfo GetBundleByAsset(string assetPath)
        {
            if (_assetToBundleDict == null) BuildIndex();
            return _assetToBundleDict.TryGetValue(assetPath, out var info) ? info : null;
        }
        /// <summary>
        /// 获取所有的Asset的Path
        /// </summary>
        public List<string> GetAssets()
        {
            if (_assetToBundleDict == null) BuildIndex();
            return _assetToBundleDict.Keys.ToList();
        }
        /// <summary>
        /// 检查资源是否存在
        /// </summary>
        public bool ContainsAsset(string assetPath)
        {
            if (_assetToBundleDict == null) BuildIndex();
            return _assetToBundleDict.ContainsKey(assetPath);
        }

        /// <summary>
        /// 获取资源所属Bundle的名称
        /// </summary>
        public string GetBundleNameByAsset(string assetPath)
        {
            return GetBundleByAsset(assetPath)?.BundleName;
        }

        /// <summary>
        /// 检查资源是否为原始文件
        /// </summary>
        public bool IsRawFile(string assetPath)
        {
            var bundle = GetBundleByAsset(assetPath);
            return bundle?.IsRawFile ?? false;
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
        /// 获取多个标签的所有Bundle（并集）
        /// </summary>
        public List<BundleInfo> GetBundlesByTags(IEnumerable<string> tags)
        {
            if (_tagBundleDict == null) BuildIndex();
            var result = new HashSet<BundleInfo>();
            foreach (var tag in tags)
            {
                if (_tagBundleDict.TryGetValue(tag, out var list))
                {
                    foreach (var bundle in list)
                    {
                        result.Add(bundle);
                    }
                }
            }
            return result.ToList();
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

        /// <summary>
        /// 递归收集Bundle依赖
        /// </summary>
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
        /// 获取加载资源需要的所有Bundle（包括依赖）
        /// </summary>
        public List<BundleInfo> GetRequiredBundles(string assetPath)
        {
            var bundle = GetBundleByAsset(assetPath);
            if (bundle == null) return new List<BundleInfo>();

            var result = new List<BundleInfo>();
            var visited = new HashSet<string>();

            // 先收集依赖
            CollectDependencies(bundle.BundleName, result, visited);

            // 再添加自身
            result.Add(bundle);

            return result;
        }
        /// <summary>
        /// 获取所有资源的数量
        /// </summary>
        public int GetAssetCount()
        {
            return Bundles.Sum(b => b.Assets.Count);
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
            return JsonMapper.ToJson(this, true);
        }

        /// <summary>
        /// 从JSON反序列化
        /// </summary>
        public static BundleManifest FromJson(string json)
        {

            var manifest = JsonMapper.ToObject<BundleManifest>(json);
            manifest.BuildIndex();
            return manifest;
        }
    }
}
