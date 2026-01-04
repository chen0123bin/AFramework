using System;
using System.IO;
using UnityEngine;

namespace LWAssets
{
    /// <summary>
    /// 运行模式
    /// </summary>
    public enum PlayMode
    {
        /// <summary>
        /// 编辑器模拟模式 - 直接从AssetDatabase加载
        /// </summary>
        EditorSimulate,

        /// <summary>
        /// 离线模式 - 从本地资源包加载
        /// </summary>
        Offline,

        /// <summary>
        /// 联机模式 - 从服务器下载资源
        /// </summary>
        Online,

        /// <summary>
        /// WebGL模式 - 特殊的WebGL平台处理
        /// </summary>
        WebGL
    }

    /// <summary>
    /// LWAssets配置
    /// </summary>
    [CreateAssetMenu(fileName = "LWAssetsConfig", menuName = "LWAssets/Config")]
    public class LWAssetsConfig : ScriptableObject
    {
        #region 基础设置

        [Header("基础设置")]
        [SerializeField] private PlayMode _playMode = PlayMode.EditorSimulate;

        [SerializeField] private string _buildOutputPath = "AssetBundles";

        [SerializeField] private string _remoteURL = "http://localhost:8080/";

        [SerializeField] private string _manifestFileName = "manifest.json";

        [SerializeField] private string _versionFileName = "version.json";

        #endregion

        #region 下载设置

        [Header("下载设置")]
        [SerializeField] private int _maxConcurrentDownloads = 5;

        [SerializeField] private int _downloadTimeout = 30;

        [SerializeField] private int _maxRetryCount = 3;

        [SerializeField] private float _retryDelay = 1f;

        [SerializeField] private bool _enableBreakpointResume = true;

        #endregion

        #region 缓存设置

        [Header("缓存设置")]
        [SerializeField] private long _maxCacheSize = 1024 * 1024 * 1024; // 1GB

        [SerializeField] private int _cacheExpirationDays = 30;

        [SerializeField] private bool _enableAutoCleanup = true;

        [SerializeField] private float _cleanupThreshold = 0.9f; // 缓存使用率达到90%时清理

        #endregion

        #region 预加载设置

        [Header("预加载设置")]
        [SerializeField] private bool _enablePreload = true;

        [SerializeField] private int _maxPreloadTasks = 3;

        [SerializeField] private long _maxPreloadMemory = 256 * 1024 * 1024; // 256MB

        #endregion

        #region 内存设置

        [Header("内存设置")]
        [SerializeField] private long _memoryWarningThreshold = 512 * 1024 * 1024; // 512MB

        [SerializeField] private long _memoryCriticalThreshold = 768 * 1024 * 1024; // 768MB

        [SerializeField] private bool _enableAutoUnload = true;

        #endregion

        #region 调试设置

        [Header("调试设置")]
        [SerializeField] private bool _enableDetailLog = false;

        [SerializeField] private bool _enableProfiler = false;

        #endregion

        #region 属性

        public PlayMode PlayMode
        {
            get => _playMode;
            set => _playMode = value;
        }

        public string BuildOutputPath => _buildOutputPath;
        public string RemoteURL => _remoteURL;
        public string ManifestFileName => _manifestFileName;
        public string VersionFileName => _versionFileName;

        public int MaxConcurrentDownloads => _maxConcurrentDownloads;
        public int DownloadTimeout => _downloadTimeout;
        public int MaxRetryCount => _maxRetryCount;
        public float RetryDelay => _retryDelay;
        public bool EnableBreakpointResume => _enableBreakpointResume;

        public long MaxCacheSize => _maxCacheSize;
        public int CacheExpirationDays => _cacheExpirationDays;
        public bool EnableAutoCleanup => _enableAutoCleanup;
        public float CleanupThreshold => _cleanupThreshold;

        public bool EnablePreload => _enablePreload;
        public int MaxPreloadTasks => _maxPreloadTasks;
        public long MaxPreloadMemory => _maxPreloadMemory;

        public long MemoryWarningThreshold => _memoryWarningThreshold;
        public long MemoryCriticalThreshold => _memoryCriticalThreshold;
        public bool EnableAutoUnload => _enableAutoUnload;

        public bool EnableDetailLog => _enableDetailLog;
        public bool EnableProfiler => _enableProfiler;

        #endregion

        #region 路径

        /// <summary>
        /// 获取平台名称
        /// </summary>
        public static string GetPlatformName()
        {
#if UNITY_ANDROID
            return "Android";
#elif UNITY_IOS
            return "iOS";
#elif UNITY_WEBGL
            return "WebGL";
#elif UNITY_STANDALONE_WIN
            return "Windows";
#elif UNITY_STANDALONE_OSX
            return "MacOS";
#elif UNITY_STANDALONE_LINUX
            return "Linux";
#else
            return "Unknown";
#endif
        }

        /// <summary>
        /// 获取StreamingAssets路径
        /// </summary>
        public string GetStreamingAssetsPath()
        {
            return Path.Combine(Application.streamingAssetsPath, _buildOutputPath, GetPlatformName());
        }

        /// <summary>
        /// 获取持久化数据路径
        /// </summary>
        public string GetPersistentDataPath()
        {
            return Path.Combine(Application.persistentDataPath, _buildOutputPath, GetPlatformName());
        }

        /// <summary>
        /// 获取远程URL
        /// </summary>
        public string GetRemoteURL()
        {
            var url = _remoteURL.TrimEnd('/');
            return $"{url}/{GetPlatformName()}/";
        }

        /// <summary>
        /// 获取构建输出路径
        /// </summary>
        public string GetBuildOutputPath()
        {
            return Path.Combine(Application.dataPath, "..", _buildOutputPath, GetPlatformName());
        }

        #endregion

        #region 加载配置

        private const string CONFIG_PATH = "LWAssetsConfig";

        /// <summary>
        /// 加载配置
        /// </summary>
        public static LWAssetsConfig Load()
        {
            var config = Resources.Load<LWAssetsConfig>(CONFIG_PATH);
            if (config == null)
            {
                Debug.LogWarning($"[LWAssets] Config not found at Resources/{CONFIG_PATH}, using default config");
                config = CreateInstance<LWAssetsConfig>();
            }
            return config;
        }

        #endregion
    }
}
