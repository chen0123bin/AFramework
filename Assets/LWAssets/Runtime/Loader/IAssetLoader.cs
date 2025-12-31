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
        
      
        #region 同步加载
        
        T LoadAsset<T>(string assetPath) where T : UnityEngine.Object;
        //AssetHandle<T> LoadAssetWithHandle<T>(string assetPath) where T : UnityEngine.Object;
        byte[] LoadRawFile(string assetPath);
        string LoadRawFileText(string assetPath);
        
        #endregion
        
        #region 异步加载
        
        UniTask<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken = default) 
            where T : UnityEngine.Object;
        UniTask<byte[]> LoadRawFileAsync(string assetPath, CancellationToken cancellationToken = default);
        UniTask<string> LoadRawFileTextAsync(string assetPath, CancellationToken cancellationToken = default);
        UniTask<SceneHandle> LoadSceneAsync(string scenePath, LoadSceneMode mode, bool activateOnLoad, 
            CancellationToken cancellationToken = default);
        
        #endregion
        
       /// <summary>
       /// 释放指定资源
       /// </summary>
       /// <param name="asset"></param>
        void Release(UnityEngine.Object asset);
        /// <summary>
        /// 释放指定资源路径的资源
        /// </summary>
        /// <param name="assetPath"></param>
        void Release(string assetPath);
        /// <summary>
        /// 强制释放指定资源（用于调试/编辑器工具）
        /// </summary>
        /// <param name="assetPath"></param>
        void ForceReleaseAsset(string assetPath);

        /// <summary>
        /// 强制卸载指定Bundle（用于调试/编辑器工具）
        /// </summary>
        void ForceUnloadBundle(string bundleName, bool unloadAllLoadedObjects = true);
        /// <summary>
        /// 异步卸载未使用的资源
        /// </summary>
        /// <returns></returns>
        UniTask UnloadUnusedAssetsAsync();
        /// <summary>
        /// 强制卸载所有资源（用于调试/编辑器工具）
        /// </summary>
        void ForceUnloadAll();
     
    }
}
