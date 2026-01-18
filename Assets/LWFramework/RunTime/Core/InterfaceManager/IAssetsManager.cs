using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LWAssets
{
    /// <summary>
    /// 资源管理接口，抽象 LWAssets 对外的资源管理能力
    /// </summary>
    public interface IAssetsManager
    {
        #region 属性

        /// <summary>
        /// 是否已初始化
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 当前运行模式
        /// </summary>
        PlayMode CurrentPlayMode { get; }

        /// <summary>
        /// 资源加载器
        /// </summary>
        IAssetLoader Loader { get; }

        /// <summary>
        /// 下载管理器
        /// </summary>
        DownloadManager Downloader { get; }

        /// <summary>
        /// 缓存管理器
        /// </summary>
        CacheManager Cache { get; }

        /// <summary>
        /// 预加载管理器
        /// </summary>
        PreloadManager Preloader { get; }

        /// <summary>
        /// 版本管理器
        /// </summary>
        VersionManager Version { get; }

        #endregion

        #region 初始化与销毁

        /// <summary>
        /// 初始化资源系统
        /// </summary>
        UniTask InitializeAsync(LWAssetsConfig config = null);

        /// <summary>
        /// 预热 Shader 相关资源
        /// </summary>
        UniTask WarmupShadersAsync(CancellationToken token = default);

        /// <summary>
        /// 销毁资源系统
        /// </summary>
        void Destroy();

        #endregion

        #region 清单

        /// <summary>
        /// 加载资源清单
        /// </summary>
        UniTask<BundleManifest> LoadManifestAsync();

        #endregion

        #region 同步加载API

        /// <summary>
        /// 同步加载资源
        /// </summary>
        T LoadAsset<T>(string assetPath) where T : UnityEngine.Object;

        /// <summary>
        /// 同步加载原始文件
        /// </summary>
        byte[] LoadRawFile(string assetPath);

        /// <summary>
        /// 同步加载原始文件为文本
        /// </summary>
        string LoadRawFileText(string assetPath);

        /// <summary>
        /// 同步实例化预制体
        /// </summary>
        GameObject Instantiate(string assetPath, Transform spawnPoint = null);

        #endregion

        #region 异步加载API

        /// <summary>
        /// 异步实例化预制体
        /// </summary>
        UniTask<GameObject> InstantiateAsync(string assetPath, Transform spawnPoint = null);

        /// <summary>
        /// 异步加载资源
        /// </summary>
        UniTask<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken = default)
            where T : UnityEngine.Object;

        /// <summary>
        /// 异步加载原始文件
        /// </summary>
        UniTask<byte[]> LoadRawFileAsync(string assetPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步加载原始文件文本
        /// </summary>
        UniTask<string> LoadRawFileTextAsync(string assetPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步加载场景
        /// </summary>
        UniTask<SceneHandle> LoadSceneAsync(string scenePath,
            UnityEngine.SceneManagement.LoadSceneMode mode = UnityEngine.SceneManagement.LoadSceneMode.Single,
            bool activateOnLoad = true,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default);

        UniTask UnloadSceneAsync(string scenePath, bool forceRelease = true, CancellationToken cancellationToken = default);

        UniTask UnloadSceneAsync(SceneHandle sceneHandle, bool forceRelease = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量异步加载资源
        /// </summary>
        UniTask<T[]> LoadAssetsAsync<T>(string[] assetPaths,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default) where T : UnityEngine.Object;

        #endregion

        #region 资源管理

        /// <summary>
        /// 释放资源
        /// </summary>
        void Release(UnityEngine.Object asset);

        /// <summary>
        /// 通过路径释放资源
        /// </summary>
        void Release(string assetPath);

        /// <summary>
        /// 异步卸载未使用资源
        /// </summary>
        UniTask UnloadUnusedAssetsAsync();

        /// <summary>
        /// 强制卸载所有资源
        /// </summary>
        void ForceUnloadAll();

        #endregion

        #region 下载相关

        /// <summary>
        /// 获取需要下载的资源总大小
        /// </summary>
        UniTask<long> GetDownloadSizeAsync(string[] tags = null);

        /// <summary>
        /// 下载资源包
        /// </summary>
        UniTask DownloadAsync(string[] tags = null,
            IProgress<DownloadProgress> progress = null,
            CancellationToken cancellationToken = default);

        #endregion
    }
}
