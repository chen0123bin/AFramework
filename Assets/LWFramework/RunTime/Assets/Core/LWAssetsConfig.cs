using System;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("_playMode")] [SerializeField] private PlayMode m_PlayMode = PlayMode.EditorSimulate;

        [FormerlySerializedAs("_buildOutputPath")] [SerializeField] private string m_BuildOutputPath = "AssetBundles";

        [FormerlySerializedAs("_remoteURL")] [SerializeField] private string m_RemoteUrl = "http://localhost:8080/";

        [FormerlySerializedAs("_manifestFileName")] [SerializeField] private string m_ManifestFileName = "manifest.json";

        [FormerlySerializedAs("_versionFileName")] [SerializeField] private string m_VersionFileName = "version.json";

        #endregion

        #region 下载设置

        [Header("下载设置")]
        [FormerlySerializedAs("_maxConcurrentDownloads")] [SerializeField] private int m_MaxConcurrentDownloads = 5;

        [FormerlySerializedAs("_downloadTimeout")] [SerializeField] private int m_DownloadTimeout = 30;

        [FormerlySerializedAs("_maxRetryCount")] [SerializeField] private int m_MaxRetryCount = 3;

        [FormerlySerializedAs("_retryDelay")] [SerializeField] private float m_RetryDelay = 1f;

        [FormerlySerializedAs("_enableBreakpointResume")] [SerializeField] private bool m_EnableBreakpointResume = true;

        #endregion

        #region 缓存设置

        [Header("缓存设置")]
        [FormerlySerializedAs("_maxCacheSize")] [SerializeField] private long m_MaxCacheSize = 1024 * 1024 * 1024; // 1GB

        [FormerlySerializedAs("_cacheExpirationDays")] [SerializeField] private int m_CacheExpirationDays = 30;

        [FormerlySerializedAs("_enableAutoCleanup")] [SerializeField] private bool m_EnableAutoCleanup = true;

        [FormerlySerializedAs("_cleanupThreshold")] [SerializeField] private float m_CleanupThreshold = 0.9f; // 缓存使用率达到90%时清理

        #endregion

        #region 预加载设置

        [Header("预加载设置")]
        [FormerlySerializedAs("_enablePreload")] [SerializeField] private bool m_EnablePreload = true;

        [FormerlySerializedAs("_maxPreloadTasks")] [SerializeField] private int m_MaxPreloadTasks = 3;

        [FormerlySerializedAs("_maxPreloadMemory")] [SerializeField] private long m_MaxPreloadMemory = 256 * 1024 * 1024; // 256MB

        #endregion

        #region 内存设置

        [Header("内存设置")]
        [FormerlySerializedAs("_memoryWarningThreshold")] [SerializeField] private long m_MemoryWarningThreshold = 512 * 1024 * 1024; // 512MB

        [FormerlySerializedAs("_memoryCriticalThreshold")] [SerializeField] private long m_MemoryCriticalThreshold = 768 * 1024 * 1024; // 768MB

        [FormerlySerializedAs("_enableAutoUnload")] [SerializeField] private bool m_EnableAutoUnload = true;

        #endregion

        #region 调试设置

        [Header("调试设置")]
        [FormerlySerializedAs("_enableDetailLog")] [SerializeField] private bool m_EnableDetailLog = false;

        [FormerlySerializedAs("_enableProfiler")] [SerializeField] private bool m_EnableProfiler = false;

        #endregion

        #region 属性

        public PlayMode PlayMode
        {
            get => m_PlayMode;
            set => m_PlayMode = value;
        }

        public string BuildOutputPath => m_BuildOutputPath;
        public string RemoteURL => m_RemoteUrl;
        public string ManifestFileName => m_ManifestFileName;
        public string VersionFileName => m_VersionFileName;

        public int MaxConcurrentDownloads => m_MaxConcurrentDownloads;
        public int DownloadTimeout => m_DownloadTimeout;
        public int MaxRetryCount => m_MaxRetryCount;
        public float RetryDelay => m_RetryDelay;
        public bool EnableBreakpointResume => m_EnableBreakpointResume;

        public long MaxCacheSize => m_MaxCacheSize;
        public int CacheExpirationDays => m_CacheExpirationDays;
        public bool EnableAutoCleanup => m_EnableAutoCleanup;
        public float CleanupThreshold => m_CleanupThreshold;

        public bool EnablePreload => m_EnablePreload;
        public int MaxPreloadTasks => m_MaxPreloadTasks;
        public long MaxPreloadMemory => m_MaxPreloadMemory;

        public long MemoryWarningThreshold => m_MemoryWarningThreshold;
        public long MemoryCriticalThreshold => m_MemoryCriticalThreshold;
        public bool EnableAutoUnload => m_EnableAutoUnload;

        public bool EnableDetailLog => m_EnableDetailLog;
        public bool EnableProfiler => m_EnableProfiler;

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
            return Path.Combine(Application.streamingAssetsPath, m_BuildOutputPath, GetPlatformName());
        }

        /// <summary>
        /// 获取持久化数据路径
        /// </summary>
        public string GetPersistentDataPath()
        {
            return Path.Combine(Application.persistentDataPath, m_BuildOutputPath, GetPlatformName());
        }

        /// <summary>
        /// 获取远程URL
        /// </summary>
        public string GetRemoteURL()
        {
            var url = m_RemoteUrl.TrimEnd('/');
            return $"{url}/{GetPlatformName()}/";
        }

        /// <summary>
        /// 获取构建输出路径
        /// </summary>
        public string GetBuildOutputPath()
        {
            return Path.Combine(Application.dataPath, "..", m_BuildOutputPath, GetPlatformName());
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
