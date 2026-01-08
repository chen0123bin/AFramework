
using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
namespace LWAssets.Editor
{

    /// <summary>
    /// 构建配置
    /// </summary>
    [CreateAssetMenu(fileName = "LWAssetsBuildConfig", menuName = "LWAssets/Build Config")]
    public class LWAssetsBuildConfig : ScriptableObject
    {
        [Header("基础设置")]
        public string OutputPath = "AssetBundles";
        //public BuildTarget BuildTarget = BuildTarget.StandaloneWindows64;
        public BuildAssetBundleOptions BuildOptions = BuildAssetBundleOptions.ChunkBasedCompression;

        [Header("分包规则")]
        public List<PackageRule> PackageRules = new List<PackageRule>();

        [Header("Shader处理")]
        public bool CollectShaders = true;
        public string ShaderBundleName = "shaders";
        public List<string> ShaderVariantCollections = new List<string>();

        [Header("标签设置")]
        public List<TagRule> TagRules = new List<TagRule>();

        [Header("高级设置")]
        public bool EnableEncryption = false;
        public string EncryptionKey = "";
        public bool GenerateReport = true;
    }

    /// <summary>
    /// 分包规则
    /// </summary>

    [Serializable]
    public class PackageRule
    {
        public string Name;                    // 规则名称
        public PackageStrategy Strategy;       // 分包策略
        public string FolderPath;              // 资源文件夹路径
        public string FilePattern = "*";       // 文件匹配模式
        public long MaxBundleSize = 10 * 1024 * 1024; // 10MB
        public int Priority = 0;               // 优先级
        public List<string> Tags = new List<string>();  // 标签列表 ⬅️
    }
    /// <summary>
    /// 分包策略
    /// </summary>
    public enum PackageStrategy
    {
        /// <summary>
        /// 按最顶层文件夹打包
        /// </summary>
        ByTopFolder,
        /// <summary>
        /// 按文件夹打包
        /// </summary>
        ByFolder,

        /// <summary>
        /// 每个文件单独打包
        /// </summary>
        ByFile,

        /// <summary>
        /// 按大小分包
        /// </summary>
        BySize,

        /// <summary>
        /// 按优先级分包
        /// </summary>
        ByPriority,

        /// <summary>
        /// 原始文件（不打包）
        /// </summary>
        RawFile
    }

    /// <summary>
    /// 标签规则
    /// </summary>
    [Serializable]
    public class TagRule
    {
        public string FolderPath;
        public List<string> Tags = new List<string>();
    }
}