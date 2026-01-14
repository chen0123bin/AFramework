using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using LWCore;

namespace LWAssets
{
    /// <summary>
    /// LWAssets 资源管理系统主入口
    /// </summary>
    public class LWAssetsManager : IAssetsManager, IManager
    {
        #region 属性与字段

        private IAssetLoader m_Loader;
        private LWAssetsConfig m_Config;
        private DownloadManager m_DownloadManager;
        private CacheManager m_CacheManager;
        private PreloadManager m_PreloadManager;
        private VersionManager m_VersionManager;
        private BundleManifest m_Manifest;

        private bool m_IsInitialized;
        private readonly object m_LockObj = new object();

        /// <summary>
        /// 创建资源系统实例
        /// </summary>
        public LWAssetsManager()
        {
        }

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => m_IsInitialized;

        /// <summary>
        /// 资源加载器
        /// </summary>
        public IAssetLoader Loader => m_Loader;
        /// <summary>
        /// 当前运行模式
        /// </summary>
        public PlayMode CurrentPlayMode => m_Config?.PlayMode ?? PlayMode.EditorSimulate;

        /// <summary>
        /// 下载管理器
        /// </summary>
        public DownloadManager Downloader => m_DownloadManager;

        /// <summary>
        /// 缓存管理器
        /// </summary>
        public CacheManager Cache => m_CacheManager;

        /// <summary>
        /// 预加载管理器
        /// </summary>
        public PreloadManager Preloader => m_PreloadManager;

        /// <summary>
        /// 版本管理器
        /// </summary>
        public VersionManager Version => m_VersionManager;

        #endregion


        public void Init()
        {

        }

        public void Update()
        {

        }

        #region 初始化

        /// <summary>
        /// 初始化资源系统
        /// </summary>
        public async UniTask InitializeAsync(LWAssetsConfig config = null)
        {
            if (m_IsInitialized)
            {
                Debug.LogWarning("[LWAssets] Already initialized!");
                return;
            }

            lock (m_LockObj)
            {
                if (m_IsInitialized) return;

                m_Config = config ?? LWAssetsConfig.Load();

                // 初始化各子系统
                m_CacheManager = new CacheManager(m_Config);
                m_VersionManager = new VersionManager(m_Config, m_CacheManager);
                m_DownloadManager = new DownloadManager(m_Config);
                m_PreloadManager = new PreloadManager(m_Config);

                // 根据运行模式创建加载器
                m_Loader = CreateLoader(m_Config.PlayMode);
            }

            // 初始化版本信息
            await m_VersionManager.InitializeAsync();

            // 加载清单文件
            m_Manifest = await LoadManifestAsync();

            // 初始化加载器
            await m_Loader.InitializeAsync(m_Manifest);

            m_IsInitialized = true;
            Debug.Log($"[LWAssets] Initialized with {m_Config.PlayMode} mode");
        }

        public async UniTask WarmupShadersAsync(CancellationToken token = default)
        {
            CheckInitialized();
            var svc = await m_Loader.LoadAssetAsync<ShaderVariantCollection>("shaders/variant_collection", token);
            if (svc != null)
                svc.WarmUp();
        }
        /// <summary>
        /// 创建对应模式的加载器
        /// </summary>
        private IAssetLoader CreateLoader(PlayMode mode)
        {
            switch (mode)
            {
                case PlayMode.EditorSimulate:
#if UNITY_EDITOR
                    return new EditorSimulateLoader(m_Config);
#else
                    Debug.LogWarning("[LWAssets] EditorSimulate mode not available in build, fallback to Offline");
                    return new OfflineLoader(m_Config, m_CacheManager, m_DownloadManager);
#endif

                case PlayMode.Offline:
                    return new OfflineLoader(m_Config, m_CacheManager, m_DownloadManager);

                case PlayMode.Online:
                    return new OnlineLoader(m_Config, m_CacheManager, m_DownloadManager, m_VersionManager);

                case PlayMode.WebGL:
                    return new WebGLLoader(m_Config, m_CacheManager, m_DownloadManager);

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        /// <summary>
        /// 加载清单文件
        /// </summary>
        public async UniTask<BundleManifest> LoadManifestAsync()
        {
            if (m_Config.PlayMode == PlayMode.EditorSimulate)
            {
#if UNITY_EDITOR
                return await EditorManifestBuilder.BuildAsync();
#endif
            }

            return await m_VersionManager.LoadManifestAsync();
        }

        #endregion

        #region 同步加载API

        /// <summary>
        /// 同步加载资源
        /// </summary>
        public T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            CheckInitialized();
            return m_Loader.LoadAsset<T>(assetPath);
        }

        /// <summary>
        /// 同步加载原始文件
        /// </summary>
        public byte[] LoadRawFile(string assetPath)
        {
            CheckInitialized();
            return m_Loader.LoadRawFile(assetPath);
        }

        /// <summary>
        /// 同步加载原始文件为文本
        /// </summary>
        public string LoadRawFileText(string assetPath)
        {
            CheckInitialized();
            return m_Loader.LoadRawFileText(assetPath);
        }

        public GameObject Instantiate(string testPrefabPath, Transform spawnPoint = null)
        {
            CheckInitialized();
            return m_Loader.Instantiate(testPrefabPath, spawnPoint);
        }
        #endregion

        #region 异步加载API (UniTask)
        public async UniTask<GameObject> InstantiateAsync(string testPrefabPath, Transform spawnPoint = null)
        {
            CheckInitialized();
            return await m_Loader.InstantiateAsync(testPrefabPath, spawnPoint);
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        public async UniTask<T> LoadAssetAsync<T>(string assetPath,
            CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            CheckInitialized();
            return await m_Loader.LoadAssetAsync<T>(assetPath, cancellationToken);
        }



        /// <summary>
        /// 异步加载原始文件
        /// </summary>
        public async UniTask<byte[]> LoadRawFileAsync(string assetPath,
            CancellationToken cancellationToken = default)
        {
            CheckInitialized();
            return await m_Loader.LoadRawFileAsync(assetPath, cancellationToken);
        }

        /// <summary>
        /// 异步加载原始文件为文本
        /// </summary>
        public async UniTask<string> LoadRawFileTextAsync(string assetPath,
            CancellationToken cancellationToken = default)
        {
            CheckInitialized();
            return await m_Loader.LoadRawFileTextAsync(assetPath, cancellationToken);
        }

        /// <summary>
        /// 异步加载场景
        /// </summary>
        public async UniTask<SceneHandle> LoadSceneAsync(string scenePath,
            UnityEngine.SceneManagement.LoadSceneMode mode = UnityEngine.SceneManagement.LoadSceneMode.Single,
            bool activateOnLoad = true,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            CheckInitialized();
            return await m_Loader.LoadSceneAsync(scenePath, mode, activateOnLoad, progress, cancellationToken);
        }

        /// <summary>
        /// 批量异步加载资源
        /// </summary>
        public async UniTask<T[]> LoadAssetsAsync<T>(string[] assetPaths,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            CheckInitialized();

            var results = new T[assetPaths.Length];
            var completedCount = 0;

            var tasks = new UniTask<T>[assetPaths.Length];
            for (int i = 0; i < assetPaths.Length; i++)
            {
                int index = i;
                tasks[i] = LoadAssetAsync<T>(assetPaths[i], cancellationToken)
                    .ContinueWith(asset =>
                    {
                        results[index] = asset;
                        Interlocked.Increment(ref completedCount);
                        progress?.Report((float)completedCount / assetPaths.Length);
                        return asset;
                    });
            }

            await UniTask.WhenAll(tasks);
            return results;
        }

        #endregion

        #region 资源管理

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Release(UnityEngine.Object asset)
        {
            CheckInitialized();
            m_Loader.Release(asset);
        }

        public void Release(string assetPath)
        {
            CheckInitialized();
            m_Loader.Release(assetPath);
        }
        /// <summary>
        /// 释放所有未使用资源
        /// </summary>
        public async UniTask UnloadUnusedAssetsAsync()
        {
            CheckInitialized();
            await m_Loader.UnloadUnusedAssetsAsync();
            await Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        /// <summary>
        /// 强制卸载所有资源
        /// </summary>
        public void ForceUnloadAll()
        {
            CheckInitialized();
            m_Loader.ForceReleaseAll();
        }

        #endregion

        #region 下载相关

        /// <summary>
        /// 获取资源包下载大小
        /// </summary>
        public async UniTask<long> GetDownloadSizeAsync(string[] tags = null)
        {
            CheckInitialized();
            return await m_VersionManager.GetDownloadSizeAsync(tags);
        }

        /// <summary>
        /// 下载资源包
        /// </summary>
        public async UniTask DownloadAsync(string[] tags = null,
            IProgress<DownloadProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            CheckInitialized();

            var bundles = await m_VersionManager.GetBundlesToDownloadAsync(tags);
            await m_DownloadManager.DownloadAsync(bundles, progress, cancellationToken);

            var newManifest = await m_VersionManager.ApplyRemoteAsLocalAsync();
            if (newManifest != null)
            {
                m_Manifest = newManifest;
                await m_Loader.InitializeAsync(m_Manifest);
            }
        }

        #endregion

        #region 工具方法

        private void CheckInitialized()
        {
            if (!m_IsInitialized)
            {
                throw new InvalidOperationException("[LWAssets] Not initialized! Call InitializeAsync first.");
            }
        }

        /// <summary>
        /// 销毁资源系统
        /// </summary>
        public void Destroy()
        {
            if (!m_IsInitialized) return;

            m_Loader?.Dispose();
            m_DownloadManager?.Dispose();
            m_CacheManager?.Dispose();
            m_PreloadManager?.Dispose();

            m_Loader = null;
            m_DownloadManager = null;
            m_CacheManager = null;
            m_PreloadManager = null;
            m_VersionManager = null;
            m_Manifest = null;
            m_Config = null;

            m_IsInitialized = false;

            Debug.Log("[LWAssets] Destroyed");
        }



        #endregion
    }
}
