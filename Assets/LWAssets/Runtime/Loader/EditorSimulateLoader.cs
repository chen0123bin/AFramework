#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWAssets
{
    /// <summary>
    /// 编辑器模拟加载器 - 直接从AssetDatabase加载
    /// </summary>
    public class EditorSimulateLoader : AssetLoaderBase
    {
        public EditorSimulateLoader(LWAssetsConfig config) : base(config)
        {
        }
        
        public override async UniTask InitializeAsync(BundleManifest manifest)
        {
            _manifest = manifest;
            await UniTask.CompletedTask;
            UnityEngine.Debug.Log("[LWAssets] EditorSimulateLoader initialized");
        }
        
        #region 同步加载
        
        /// <summary>
        /// 编辑器模拟：同步加载资源，并记录加载耗时/引用信息
        /// </summary>
        public override T LoadAsset<T>(string assetPath)
        {
            var sw = Stopwatch.StartNew();
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            sw.Stop();
            if (asset == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Asset not found: {assetPath}");
            }
            else
            {
                TrackAsset(assetPath, asset, "editor_simulate", sw.Elapsed.TotalMilliseconds);
            }
            return asset;
        }
        
        /// <summary>
        /// 编辑器模拟：同步加载资源句柄，并记录加载耗时/引用信息
        /// </summary>
        // public override AssetHandle<T> LoadAssetWithHandle<T>(string assetPath)
        // {
        //     var handle = new AssetHandle<T>(assetPath);
        //     var sw = Stopwatch.StartNew();
        //     var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        //     sw.Stop();
        //     if (asset != null)
        //     {
        //         TrackAsset(assetPath, asset, "editor_simulate", sw.Elapsed.TotalMilliseconds);
        //         handle.SetAsset(asset, "editor_simulate", sw.Elapsed.TotalMilliseconds);
        //     }
        //     else
        //     {
        //         handle.SetError(new FileNotFoundException($"Asset not found: {assetPath}"));
        //     }
        //     return handle;
        // }
        
        public override byte[] LoadRawFile(string assetPath)
        {
            var fullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), assetPath);
            if (File.Exists(fullPath))
            {
                return File.ReadAllBytes(fullPath);
            }
            
            UnityEngine.Debug.LogError($"[LWAssets] Raw file not found: {assetPath}");
            return null;
        }
        
        public override string LoadRawFileText(string assetPath)
        {
            var fullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), assetPath);
            if (File.Exists(fullPath))
            {
                return File.ReadAllText(fullPath);
            }
            
            UnityEngine.Debug.LogError($"[LWAssets] Raw file not found: {assetPath}");
            return null;
        }
        
        #endregion
        
        #region 异步加载
        
        /// <summary>
        /// 编辑器模拟：异步加载资源，并记录加载耗时/引用信息
        /// </summary>
        public override async UniTask<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken = default)
        {
            // 模拟异步延迟
            await UniTask.Yield(cancellationToken);
            
            var sw = Stopwatch.StartNew();
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            sw.Stop();
            if (asset == null)
            {
                UnityEngine.Debug.LogError($"[LWAssets] Asset not found: {assetPath}");
            }
            else
            {
                TrackAsset(assetPath, asset, "editor_simulate", sw.Elapsed.TotalMilliseconds);
            }
            return asset;
        }
        
        /// <summary>
        /// 编辑器模拟：异步加载资源句柄，并记录加载耗时/引用信息
        /// </summary>
        // public override async UniTask<AssetHandle<T>> LoadAssetWithHandleAsync<T>(string assetPath, 
        //     CancellationToken cancellationToken = default)
        // {
        //     var handle = new AssetHandle<T>(assetPath);
            
        //     await UniTask.Yield(cancellationToken);
            
        //     var sw = Stopwatch.StartNew();
        //     var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        //     sw.Stop();
        //     if (asset != null)
        //     {
        //         TrackAsset(assetPath, asset, "editor_simulate", sw.Elapsed.TotalMilliseconds);
        //         handle.SetAsset(asset, "editor_simulate", sw.Elapsed.TotalMilliseconds);
        //     }
        //     else
        //     {
        //         handle.SetError(new FileNotFoundException($"Asset not found: {assetPath}"));
        //     }
            
        //     return handle;
        // }
        
        public override async UniTask<byte[]> LoadRawFileAsync(string assetPath, CancellationToken cancellationToken = default)
        {
            await UniTask.Yield(cancellationToken);
            return LoadRawFile(assetPath);
        }
        
        public override async UniTask<SceneHandle> LoadSceneAsync(string scenePath, LoadSceneMode mode, 
            bool activateOnLoad, CancellationToken cancellationToken = default)
        {
            var handle = new SceneHandle(scenePath);
            
            try
            {
                var loadParams = new LoadSceneParameters(mode);
                var op = EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, loadParams);
                
                while (!op.isDone)
                {
                    handle.SetProgress(op.progress);
                    await UniTask.Yield(cancellationToken);
                }
                
                var scene = SceneManager.GetSceneByPath(scenePath);
                handle.SetScene(scene);
            }
            catch (Exception ex)
            {
                handle.SetError(ex);
            }
            
            return handle;
        }
        
        #endregion
        
        protected override UniTask<AssetBundle> LoadBundleFromSourceAsync(BundleInfo bundleInfo, 
            CancellationToken cancellationToken = default)
        {
            // 编辑器模式不需要实际加载Bundle
            return UniTask.FromResult<AssetBundle>(null);
        }
        
        public override void Release(UnityEngine.Object asset)
        {
            base.Release(asset);
        }
        
        public override async UniTask UnloadUnusedAssetsAsync()
        {
            await base.UnloadUnusedAssetsAsync();
            await Resources.UnloadUnusedAssets();
        }
        
        public override void ForceUnloadAll()
        {
            base.ForceUnloadAll();
            Resources.UnloadUnusedAssets();
        }
    }
    
    /// <summary>
    /// 编辑器清单构建器
    /// </summary>
    public static class EditorManifestBuilder
    {
        public static async UniTask<BundleManifest> BuildAsync()
        {
            var manifest = new BundleManifest
            {
                Version = "0.0.0",
                BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Platform = LWAssetsConfig.GetPlatformName()
            };
            
            // 收集所有资源
            var assetPaths = AssetDatabase.GetAllAssetPaths();
            foreach (var path in assetPaths)
            {
                if (!path.StartsWith("Assets/")) continue;
                if (path.EndsWith(".cs") || path.EndsWith(".meta")) continue;
                
                // manifest.Assets.Add(new AssetInfo
                // {
                //     AssetPath = path,
                //     AssetType = AssetDatabase.GetMainAssetTypeAtPath(path)?.Name ?? "Unknown",
                //     BundleName = "editor_simulate",
                //     IsRawFile = IsRawFile(path)
                // });
            }
            
            manifest.BuildIndex();
            await UniTask.CompletedTask;
            return manifest;
        }
        
    }
}
#endif
