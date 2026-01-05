using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

namespace LWAssets
{
    /// <summary>
    /// LWAssets 资源管理系统主入口
    /// </summary>
    public class LWAssetsManager : IAssetsManager, IManager
    {
        #region 属性与字段

        private IAssetLoader _loader;
        private LWAssetsConfig _config;
        private DownloadManager _downloadManager;
        private CacheManager _cacheManager;
        private PreloadManager _preloadManager;
        private VersionManager _versionManager;
        private BundleManifest _manifest;

        private bool _isInitialized;
        private readonly object _lockObj = new object();

        /// <summary>
        /// 创建资源系统实例
        /// </summary>
        public LWAssetsManager()
        {
        }

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 资源加载器
        /// </summary>
        public IAssetLoader Loader => _loader;
        /// <summary>
        /// 当前运行模式
        /// </summary>
        public PlayMode CurrentPlayMode => _config?.PlayMode ?? PlayMode.EditorSimulate;

        /// <summary>
        /// 下载管理器
        /// </summary>
        public DownloadManager Downloader => _downloadManager;

        /// <summary>
        /// 缓存管理器
        /// </summary>
        public CacheManager Cache => _cacheManager;

        /// <summary>
        /// 预加载管理器
        /// </summary>
        public PreloadManager Preloader => _preloadManager;

        /// <summary>
        /// 版本管理器
        /// </summary>
        public VersionManager Version => _versionManager;

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化资源系统
        /// </summary>
        public async UniTask InitializeAsync(LWAssetsConfig config = null)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[LWAssets] Already initialized!");
                return;
            }

            lock (_lockObj)
            {
                if (_isInitialized) return;

                _config = config ?? LWAssetsConfig.Load();

                // 初始化各子系统
                _cacheManager = new CacheManager(_config);
                _versionManager = new VersionManager(_config, _cacheManager);
                _downloadManager = new DownloadManager(_config);
                _preloadManager = new PreloadManager(_config);

                // 根据运行模式创建加载器
                _loader = CreateLoader(_config.PlayMode);
            }

            // 初始化版本信息
            await _versionManager.InitializeAsync();

            // 加载清单文件
            _manifest = await LoadManifestAsync();

            // 初始化加载器
            await _loader.InitializeAsync(_manifest);

            _isInitialized = true;
            Debug.Log($"[LWAssets] Initialized with {_config.PlayMode} mode");
        }

        public async UniTask WarmupShadersAsync(CancellationToken token = default)
        {
            CheckInitialized();
            var svc = await _loader.LoadAssetAsync<ShaderVariantCollection>("shaders/variant_collection", token);
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
                    return new EditorSimulateLoader(_config);
#else
                    Debug.LogWarning("[LWAssets] EditorSimulate mode not available in build, fallback to Offline");
                    return new OfflineLoader(_config, _cacheManager, _downloadManager);
#endif

                case PlayMode.Offline:
                    return new OfflineLoader(_config, _cacheManager, _downloadManager);

                case PlayMode.Online:
                    return new OnlineLoader(_config, _cacheManager, _downloadManager, _versionManager);

                case PlayMode.WebGL:
                    return new WebGLLoader(_config, _cacheManager, _downloadManager);

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        /// <summary>
        /// 加载清单文件
        /// </summary>
        public async UniTask<BundleManifest> LoadManifestAsync()
        {
            if (_config.PlayMode == PlayMode.EditorSimulate)
            {
#if UNITY_EDITOR
                return await EditorManifestBuilder.BuildAsync();
#endif
            }

            return await _versionManager.LoadManifestAsync();
        }

        #endregion

        #region 同步加载API

        /// <summary>
        /// 同步加载资源
        /// </summary>
        public T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            CheckInitialized();
            return _loader.LoadAsset<T>(assetPath);
        }

        /// <summary>
        /// 同步加载原始文件
        /// </summary>
        public byte[] LoadRawFile(string assetPath)
        {
            CheckInitialized();
            return _loader.LoadRawFile(assetPath);
        }

        /// <summary>
        /// 同步加载原始文件为文本
        /// </summary>
        public string LoadRawFileText(string assetPath)
        {
            CheckInitialized();
            return _loader.LoadRawFileText(assetPath);
        }

        public GameObject Instantiate(string testPrefabPath, Transform spawnPoint)
        {
            CheckInitialized();
            return _loader.Instantiate(testPrefabPath, spawnPoint);
        }
        #endregion

        #region 异步加载API (UniTask)
        public async UniTask<GameObject> InstantiateAsync(string testPrefabPath, Transform spawnPoint)
        {
            CheckInitialized();
            return await _loader.InstantiateAsync(testPrefabPath, spawnPoint);
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        public async UniTask<T> LoadAssetAsync<T>(string assetPath,
            CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            CheckInitialized();
            return await _loader.LoadAssetAsync<T>(assetPath, cancellationToken);
        }



        /// <summary>
        /// 异步加载原始文件
        /// </summary>
        public async UniTask<byte[]> LoadRawFileAsync(string assetPath,
            CancellationToken cancellationToken = default)
        {
            CheckInitialized();
            return await _loader.LoadRawFileAsync(assetPath, cancellationToken);
        }

        /// <summary>
        /// 异步加载原始文件为文本
        /// </summary>
        public async UniTask<string> LoadRawFileTextAsync(string assetPath,
            CancellationToken cancellationToken = default)
        {
            CheckInitialized();
            return await _loader.LoadRawFileTextAsync(assetPath, cancellationToken);
        }

        /// <summary>
        /// 异步加载场景
        /// </summary>
        public async UniTask<SceneHandle> LoadSceneAsync(string scenePath,
            UnityEngine.SceneManagement.LoadSceneMode mode = UnityEngine.SceneManagement.LoadSceneMode.Single,
            bool activateOnLoad = true,
            CancellationToken cancellationToken = default)
        {
            CheckInitialized();
            return await _loader.LoadSceneAsync(scenePath, mode, activateOnLoad, cancellationToken);
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
            _loader.Release(asset);
        }

        public void Release(string assetPath)
        {
            CheckInitialized();
            _loader.Release(assetPath);
        }
        /// <summary>
        /// 释放所有未使用资源
        /// </summary>
        public async UniTask UnloadUnusedAssetsAsync()
        {
            CheckInitialized();
            await _loader.UnloadUnusedAssetsAsync();
            await Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        /// <summary>
        /// 强制卸载所有资源
        /// </summary>
        public void ForceUnloadAll()
        {
            CheckInitialized();
            _loader.ForceReleaseAll();
        }

        #endregion

        #region 下载相关

        /// <summary>
        /// 获取资源包下载大小
        /// </summary>
        public async UniTask<long> GetDownloadSizeAsync(string[] tags = null)
        {
            CheckInitialized();
            return await _versionManager.GetDownloadSizeAsync(tags);
        }

        /// <summary>
        /// 下载资源包
        /// </summary>
        public async UniTask DownloadAsync(string[] tags = null,
            IProgress<DownloadProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            CheckInitialized();

            var bundles = await _versionManager.GetBundlesToDownloadAsync(tags);
            await _downloadManager.DownloadAsync(bundles, progress, cancellationToken);
        }

        #endregion

        #region 工具方法

        private void CheckInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("[LWAssets] Not initialized! Call InitializeAsync first.");
            }
        }

        /// <summary>
        /// 销毁资源系统
        /// </summary>
        public void Destroy()
        {
            if (!_isInitialized) return;

            _loader?.Dispose();
            _downloadManager?.Dispose();
            _cacheManager?.Dispose();
            _preloadManager?.Dispose();

            _loader = null;
            _downloadManager = null;
            _cacheManager = null;
            _preloadManager = null;
            _versionManager = null;
            _manifest = null;
            _config = null;

            _isInitialized = false;

            Debug.Log("[LWAssets] Destroyed");
        }

        public void Init()
        {

        }

        public void Update()
        {

        }


        #endregion
    }
}
