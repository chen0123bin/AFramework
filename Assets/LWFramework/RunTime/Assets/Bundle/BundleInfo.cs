using System;
using System.Collections.Generic;

namespace LWAssets
{
    /// <summary>
    /// Bundle信息
    /// </summary>
    [Serializable]
    public class BundleInfo
    {
        /// <summary>
        /// Bundle名称
        /// </summary>
        public string BundleName;
        
        /// <summary>
        /// 文件哈希值
        /// </summary>
        public string Hash;
        
        /// <summary>
        /// 文件CRC
        /// </summary>
        public uint CRC;
        
        /// <summary>
        /// 文件大小(字节)
        /// </summary>
        public long Size;
        
        /// <summary>
        /// 是否为原始文件包
        /// </summary>
        public bool IsRawFile;
        
        /// <summary>
        /// 是否加密
        /// </summary>
        public bool IsEncrypted;
        
        /// <summary>
        /// 标签列表（用于分组下载）
        /// </summary>
        public List<string> Tags = new List<string>();
        
        /// <summary>
        /// 依赖的Bundle名称列表
        /// </summary>
        public List<string> Dependencies = new List<string>();
        
        /// <summary>
        /// 包含的资源路径列表
        /// </summary>
        public List<string> Assets = new List<string>();
        
        /// <summary>
        /// 优先级(0-10, 越高越优先)
        /// </summary>
        public int Priority;
        
        /// <summary>
        /// 获取带哈希的文件名（用于缓存和下载）
        /// </summary>
        public string GetFileName()
        {
            return $"{BundleName}_{Hash}.uab";
        }
         /// <summary>
        /// 检查是否包含指定资源
        /// </summary>
        public bool ContainsAsset(string assetPath)
        {
            return Assets.Contains(assetPath);
        }
        
        /// <summary>
        /// 检查是否有指定标签
        /// </summary>
        public bool HasTag(string tag)
        {
            return Tags.Contains(tag);
        }
        
        /// <summary>
        /// 检查是否有任意一个指定标签
        /// </summary>
        public bool HasAnyTag(IEnumerable<string> tags)
        {
            foreach (var tag in tags)
            {
                if (Tags.Contains(tag)) return true;
            }
            return false;
        }
    }
    
    // /// <summary>
    // /// 资源信息
    // /// </summary>
    // [Serializable]
    // public class AssetInfo
    // {
    //     /// <summary>
    //     /// 资源路径
    //     /// </summary>
    //     public string AssetPath;
        
    //     /// <summary>
    //     /// 资源类型
    //     /// </summary>
    //     public string AssetType;
        
    //     /// <summary>
    //     /// 所属Bundle名称
    //     /// </summary>
    //     public string BundleName;
        
    //     /// <summary>
    //     /// 是否为原始文件
    //     /// </summary>
    //     public bool IsRawFile;
    // }
}
