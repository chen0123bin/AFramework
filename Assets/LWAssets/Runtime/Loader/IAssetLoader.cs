using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWAssets
{
    /// <summary>
    /// 资源加载器接口
    /// </summary>
    public interface IAssetLoader : IDisposable
    {
        /// <summary>
        /// 初始化
        /// </summary>
        UniTask InitializeAsync(BundleManifest manifest);
        
        /// <summary>
        /// 获取Bundle缓存
        /// </summary>
        Dictionary<string, BundleHandle> GetBundleCache();

        /// <summary>
        /// 获取资源引用缓存
        /// </summary>
        Dictionary<string, AssetRefInfo> GetAssetRefCache();
        #region 同步加载
        
        T LoadAsset<T>(string assetPath) where T : UnityEngine.Object;
        AssetHandle<T> LoadAssetWithHandle<T>(string assetPath) where T : UnityEngine.Object;
        byte[] LoadRawFile(string assetPath);
        string LoadRawFileText(string assetPath);
        
        #endregion
        
        #region 异步加载
        
        UniTask<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken = default) 
            where T : UnityEngine.Object;
        UniTask<AssetHandle<T>> LoadAssetWithHandleAsync<T>(string assetPath, CancellationToken cancellationToken = default) 
            where T : UnityEngine.Object;
        UniTask<byte[]> LoadRawFileAsync(string assetPath, CancellationToken cancellationToken = default);
        UniTask<string> LoadRawFileTextAsync(string assetPath, CancellationToken cancellationToken = default);
        UniTask<SceneHandle> LoadSceneAsync(string scenePath, LoadSceneMode mode, bool activateOnLoad, 
            CancellationToken cancellationToken = default);
        
        #endregion
        
        #region 资源管理
        
        void Release(UnityEngine.Object asset);
        void Release(string assetPath);

        void ForceReleaseAsset(string assetPath);
        UniTask UnloadUnusedAssetsAsync();
        void ForceUnloadAll();
        
        #endregion
    }
}
