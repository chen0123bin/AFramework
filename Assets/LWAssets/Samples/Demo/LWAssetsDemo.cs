using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LWAssets.Samples
{
    /// <summary>
    /// LWAssets使用示例
    /// </summary>
    public class LWAssetsDemo : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _initButton;
        [SerializeField] private Button _loadAssetButton;
        [SerializeField] private Button _loadSceneButton;
        [SerializeField] private Button _loadScene2Button;
         [SerializeField] private Button _loadRawButton;
        [SerializeField] private Button _downloadButton;
        [SerializeField] private Button _clearCacheButton;
        [SerializeField] private Text _statusText;
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private Transform _spawnPoint;
        
        [Header("Test Assets")]
        [SerializeField] private string _testPrefabPath = "Assets/Prefabs/TestCube.prefab";
        [SerializeField] private string _testScenePath = "Assets/Scenes/TestScene.unity";

        [SerializeField] private string _test2ScenePath = "Assets/Scenes/TestScene2.unity";
        [SerializeField] private string _testRawFilePath = "Assets/Config/game_config.json";
        
        private void Start()
        {
            SetupButtons();
        }
        
        private void SetupButtons()
        {
            _initButton?.onClick.AddListener(() => InitializeAsync().Forget());
            _loadAssetButton?.onClick.AddListener(() => LoadAssetAsync().Forget());
            _loadSceneButton?.onClick.AddListener(() => LoadSceneAsync().Forget());
            _loadScene2Button?.onClick.AddListener(() => LoadScene2Async().Forget());
            
            _loadRawButton?.onClick.AddListener(() => LoadRawFileAsync().Forget());
            _downloadButton?.onClick.AddListener(() => DownloadAsync().Forget());
            _clearCacheButton?.onClick.AddListener(ClearCache);
        }
        
        /// <summary>
        /// 初始化LWAssets
        /// </summary>
        private async UniTaskVoid InitializeAsync()
        {
            try
            {
                UpdateStatusMsg("Initializing LWAssets...");
                
                // 加载配置
                var config = LWAssetsConfig.Load();
                
                // 初始化
                await LWAssets.InitializeAsync(config);
                
                UpdateStatusMsg($"Initialized! Mode: {LWAssets.CurrentPlayMode}");
            }
            catch (Exception ex)
            {
                UpdateStatusMsg($"Init failed: {ex.Message}");
                Debug.LogException(ex);
            }
        }
       
        /// <summary>
        /// 加载资源示例
        /// </summary>
        private async UniTaskVoid LoadAssetAsync()
        {
            if (!LWAssets.IsInitialized)
            {
                UpdateStatusMsg("Please initialize first!");
                return;
            }
            
            try
            {
                UpdateStatusMsg("Loading prefab...");
                
                // 方式1：直接加载
                // var prefab = await LWAssets.LoadAssetAsync<GameObject>(_testPrefabPath);
                // if (prefab != null)
                // {
                //     var instance = Instantiate(prefab, _spawnPoint);
                //     UpdateStatusMsg($"Loaded: {prefab.name}");
                // }
                
                var gameObject = await LWAssets.InstantiateAsync(_testPrefabPath, _spawnPoint);
                
                // 方式2：使用句柄
                // var handle = await LWAssets.LoadAssetWithHandleAsync<GameObject>(_testPrefabPath);
                // if (handle.IsValid)
                // {
                //     var instance = Instantiate(handle.Asset, _spawnPoint);
                //     // 使用完毕后释放
                //     // handle.Release();
                // }
            }
            catch (Exception ex)
            {
                UpdateStatusMsg($"Load failed: {ex.Message}");
                Debug.LogException(ex);
            }
        }
        
        /// <summary>
        /// 加载场景示例
        /// </summary>
        private async UniTaskVoid LoadSceneAsync()
        {
            if (!LWAssets.IsInitialized)
            {
                UpdateStatusMsg("Please initialize first!");
                return;
            }
            
            try
            {
                UpdateStatusMsg("Loading scene...");
                
                var handle = await LWAssets.LoadSceneAsync(
                    _testScenePath,
                    UnityEngine.SceneManagement.LoadSceneMode.Additive,
                    true);
                
                if (handle.IsValid)
                {
                    UpdateStatusMsg($"Scene loaded: {handle.Scene.name}");
                }
                else if (handle.HasError)
                {
                    UpdateStatusMsg($"Scene load error: {handle.Error.Message}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusMsg($"Load failed: {ex.Message}");
                Debug.LogException(ex);
            }
        }
         /// <summary>
        /// 加载场景示例
        /// </summary>
        private async UniTaskVoid LoadScene2Async()
        {
            if (!LWAssets.IsInitialized)
            {
                UpdateStatusMsg("Please initialize first!");
                return;
            }
            
            try
            {
                UpdateStatusMsg("Loading scene...");
                
                var handle = await LWAssets.LoadSceneAsync(
                    _test2ScenePath,
                    UnityEngine.SceneManagement.LoadSceneMode.Additive,
                    true);
                
                if (handle.IsValid)
                {
                    UpdateStatusMsg($"Scene loaded: {handle.Scene.name}");
                }
                else if (handle.HasError)
                {
                    UpdateStatusMsg($"Scene load error: {handle.Error.Message}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusMsg($"Load failed: {ex.Message}");
                Debug.LogException(ex);
            }
        }
        /// <summary>
        /// 加载原始文件示例
        /// </summary>
        public async UniTask<string> LoadRawFileAsync()
        {
            if (!LWAssets.IsInitialized)
            {
                await LWAssets.InitializeAsync();
            }
            
            var text = await LWAssets.LoadRawFileTextAsync(_testRawFilePath);
            Debug.Log($"Raw file content: {text}");
            return text;
        }
        
        /// <summary>
        /// 下载资源示例
        /// </summary>
        private async UniTaskVoid DownloadAsync()
        {
            if (!LWAssets.IsInitialized)
            {
                UpdateStatusMsg("Please initialize first!");
                return;
            }
            
            try
            {
                // 检查更新
                UpdateStatusMsg("Checking for updates...");
                var checkResult = await LWAssets.Version.CheckUpdateAsync();
                
                if (checkResult.Status == UpdateStatus.NoUpdate)
                {
                    UpdateStatusMsg("No updates available.");
                    return;
                }
                
                UpdateStatusMsg($"Update available: {checkResult.RemoteVersion}, Size: {FileUtility.FormatFileSize(checkResult.DownloadSize)}");
                
                // 开始下载
                var progress = new Progress<DownloadProgress>(p =>
                {
                    _progressSlider.value = p.Progress;
                    UpdateStatusMsg($"Downloading: {p.CompletedCount}/{p.TotalCount} - {FileUtility.FormatFileSize((long)p.Speed)}/s");
                });
                
                await LWAssets.DownloadAsync(null, progress);
                
                _progressSlider.value = 1f;
                UpdateStatusMsg("Download completed!");
            }
            catch (Exception ex)
            {
                UpdateStatusMsg($"Download failed: {ex.Message}");
                Debug.LogException(ex);
            }
        }
        
        /// <summary>
        /// 按标签下载示例
        /// </summary>
        public async UniTask DownloadByTagAsync(params string[] tags)
        {
            if (!LWAssets.IsInitialized)
            {
                await LWAssets.InitializeAsync();
            }
            
            // 获取下载大小
            var downloadSize = await LWAssets.GetDownloadSizeAsync(tags);
            Debug.Log($"Download size for tags [{string.Join(",", tags)}]: {FileUtility.FormatFileSize(downloadSize)}");
            
            // 下载
            await LWAssets.DownloadAsync(tags);
        }
        
        /// <summary>
        /// 清理缓存
        /// </summary>
        private void ClearCache()
        {
            if (!LWAssets.IsInitialized)
            {
                UpdateStatusMsg("Please initialize first!");
                return;
            }
            
            LWAssets.Cache.ClearAll();
            UpdateStatusMsg("Cache cleared!");
        }
        
        /// <summary>
        /// 批量加载示例
        /// </summary>
        public async UniTask BatchLoadAsync()
        {
            if (!LWAssets.IsInitialized)
            {
                await LWAssets.InitializeAsync();
            }
            
            var assetPaths = new[]
            {
                "Assets/Prefabs/Item1.prefab",
                "Assets/Prefabs/Item2.prefab",
                "Assets/Prefabs/Item3.prefab"
            };
            
            var progress = new Progress<float>(p =>
            {
                Debug.Log($"Batch load progress: {p * 100:F1}%");
            });
            
            var assets = await LWAssets.LoadAssetsAsync<GameObject>(assetPaths, progress);
            
            foreach (var asset in assets)
            {
                Debug.Log($"Loaded: {asset?.name}");
            }
        }
        
        /// <summary>
        /// 预加载示例
        /// </summary>
        public void SetupPreload()
        {
            if (!LWAssets.IsInitialized) return;
            
            // 手动请求预加载
            LWAssets.Preloader.RequestPreload("Assets/Prefabs/UI/MainMenu.prefab", PreloadPriority.High);
            LWAssets.Preloader.RequestPreload("Assets/Prefabs/UI/SettingsPanel.prefab", PreloadPriority.Normal);
            
            // 批量预加载
            var preloadList = new[]
            {
                "Assets/Prefabs/Characters/Player.prefab",
                "Assets/Prefabs/Characters/NPC1.prefab",
                "Assets/Prefabs/Effects/Explosion.prefab"
            };
            LWAssets.Preloader.RequestPreload(preloadList, PreloadPriority.Low);
            
            // 使用智能预测
            LWAssets.Preloader.RecordAccess("Assets/Scenes/Level1.unity");
            var predicted = LWAssets.Preloader.GetPredictedAssets("Assets/Scenes/Level1.unity");
            LWAssets.Preloader.RequestPreload(predicted);
        }
        
        /// <summary>
        /// 内存管理示例
        /// </summary>
        public async UniTask MemoryManagementAsync()
        {
            // 获取内存统计
            var stats = LWAssets.Preloader.GetType()
                .GetField("_memoryMonitor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(LWAssets.Preloader) as MemoryMonitor;
            
            if (stats != null)
            {
                var memStats = stats.GetStatistics();
                Debug.Log($"Memory: {memStats.GetFormattedAllocatedMemory()} / State: {memStats.State}");
            }
            
            // 手动卸载未使用资源
            await LWAssets.UnloadUnusedAssetsAsync();
            
            // 强制卸载所有
            // LWAssets.ForceUnloadAll();
        }
        
        private void UpdateStatusMsg(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }
            Debug.Log($"[LWAssetsDemo] {message}");
        }
        
        private void OnDestroy()
        {
            // 清理
            LWAssets.Destroy();
        }
    }
    
    /// <summary>
    /// 高级用法示例 - 游戏启动流程
    /// </summary>
    public class GameLauncher : MonoBehaviour
    {
        public async UniTaskVoid StartGame()
        {
            // 1. 初始化资源系统
            await LWAssets.InitializeAsync();
            
            // 2. 检查更新
            var updateResult = await LWAssets.Version.CheckUpdateAsync();
            
            if (updateResult.Status == UpdateStatus.ForceUpdate)
            {
                // 强制更新，提示用户
                ShowUpdateDialog(updateResult);
                return;
            }
            else if (updateResult.Status == UpdateStatus.OptionalUpdate)
            {
                // 可选更新，询问用户
                if (await ShowOptionalUpdateDialog(updateResult))
                {
                    await DownloadUpdates();
                }
            }
            
            // 3. 下载核心资源
            var coreSize = await LWAssets.GetDownloadSizeAsync(new[] { "core", "ui" });
            if (coreSize > 0)
            {
                await LWAssets.DownloadAsync(new[] { "core", "ui" });
            }
            
            // 4. 加载启动场景
            await LWAssets.LoadSceneAsync("Assets/Scenes/MainMenu.unity");
            
            // 5. 后台下载其他资源
            DownloadBackgroundAsync().Forget();
        }
        
        private async UniTaskVoid DownloadBackgroundAsync()
        {
            // 低优先级后台下载
            await LWAssets.DownloadAsync(new[] { "levels", "characters", "effects" });
        }
        
        private void ShowUpdateDialog(UpdateCheckResult result)
        {
            Debug.Log($"Force update required: {result.LocalVersion} -> {result.RemoteVersion}");
        }
        
        private async UniTask<bool> ShowOptionalUpdateDialog(UpdateCheckResult result)
        {
            Debug.Log($"Optional update available: {result.LocalVersion} -> {result.RemoteVersion}");
            return true; // 模拟用户确认
        }
        
        private async UniTask DownloadUpdates()
        {
            await LWAssets.DownloadAsync();
        }
    }
}
