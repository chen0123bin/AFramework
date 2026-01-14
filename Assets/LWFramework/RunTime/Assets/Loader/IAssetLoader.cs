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
        /// <summary>
        /// 同步实例化资源
        /// </summary>
        /// <returns></returns>
        GameObject Instantiate(string assetPath, Transform parent = null);
        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        T LoadAsset<T>(string assetPath) where T : UnityEngine.Object;
        /// <summary>
        /// 同步加载原始文件
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        byte[] LoadRawFile(string assetPath);
        /// <summary>
        /// 同步加载原始文件文本
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        string LoadRawFileText(string assetPath);

        #endregion

        #region 异步加载
        /// <summary>
        /// 异步实例化资源
        /// </summary>
        UniTask<GameObject> InstantiateAsync(string assetPath, Transform parent = null, CancellationToken cancellationToken = default);
        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetPath"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        UniTask<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken = default) 
            where T : UnityEngine.Object;
        /// <summary>
        /// 异步加载原始文件
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        UniTask<byte[]> LoadRawFileAsync(string assetPath, CancellationToken cancellationToken = default);
        /// <summary>
        /// 异步加载原始文件文本
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        UniTask<string> LoadRawFileTextAsync(string assetPath, CancellationToken cancellationToken = default);
        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="scenePath"></param>
        /// <param name="mode"></param>
        /// <param name="activateOnLoad"></param>
        /// <param name="progress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        UniTask<SceneHandle> LoadSceneAsync(string scenePath, LoadSceneMode mode, bool activateOnLoad,
            IProgress<float> progress = null,
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
        /// 强制释放指定资源
        /// </summary>
        /// <param name="assetPath"></param>
        void ForceReleaseAsset(string assetPath);

        /// <summary>
        /// 强制卸载指定Bundle
        /// </summary>
        void ForceUnloadBundle(string bundleName);
        /// <summary>
        /// 异步卸载未使用的资源
        /// </summary>
        /// <returns></returns>
        UniTask UnloadUnusedAssetsAsync();
        /// <summary>
        /// 强制卸载所有资源
        /// </summary>
        void ForceReleaseAll();
     
    }
}
