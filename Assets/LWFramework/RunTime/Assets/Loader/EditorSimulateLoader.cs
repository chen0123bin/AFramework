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
            m_Manifest = manifest;
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
                TrackAssetHandle(assetPath, asset, "editor_simulate", sw.Elapsed.TotalMilliseconds);
            }
            return asset;
        }

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
                TrackAssetHandle(assetPath, asset, "editor_simulate", sw.Elapsed.TotalMilliseconds);
            }
            return asset;
        }


        public override async UniTask<byte[]> LoadRawFileAsync(string assetPath, CancellationToken cancellationToken = default)
        {
            if (TryGetRawFileFromCache(assetPath, out var cached))
            {
                return cached;
            }

            await UniTask.Yield(cancellationToken);

            var fullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), assetPath);
            if (!File.Exists(fullPath))
            {
                UnityEngine.Debug.LogError($"[LWAssets] Raw file not found: {assetPath}");
                return null;
            }

            var sw = Stopwatch.StartNew();
            var data = await File.ReadAllBytesAsync(fullPath, cancellationToken);
            sw.Stop();
            TrackRawFileHandle(assetPath, data, "editor_simulate", data.LongLength, sw.Elapsed.TotalMilliseconds);
            return data;
        }

        public override async UniTask<SceneHandle> LoadSceneAsync(string scenePath, LoadSceneMode mode,
            bool activateOnLoad,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            var sceneHandle = new SceneHandle(scenePath);

            try
            {
                progress?.Report(0f);
                var loadParams = new LoadSceneParameters(mode);
                var op = EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, loadParams);
                op.allowSceneActivation = activateOnLoad;

                while (!op.isDone)
                {
                    var normalizedProgress = op.progress < 0.9f ? Mathf.Clamp01(op.progress / 0.9f) : 1f;
                    sceneHandle.SetProgress(normalizedProgress);
                    progress?.Report(normalizedProgress);
                    if (op.progress >= 0.9f && !activateOnLoad)
                    {
                        break;
                    }
                    await UniTask.Yield(cancellationToken);
                }

                var scene = SceneManager.GetSceneByPath(scenePath);
                sceneHandle.SetScene(scene);
                sceneHandle.Retain();
            }
            catch (Exception ex)
            {
                sceneHandle.SetError(ex);
            }

            return sceneHandle;
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

        public override void ForceReleaseAll()
        {
            base.ForceReleaseAll();
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
                Version = 0,
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
