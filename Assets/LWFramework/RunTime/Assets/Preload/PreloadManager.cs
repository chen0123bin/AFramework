using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LWAssets
{
    /// <summary>
    /// 预加载优先级
    /// </summary>
    public enum PreloadPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// 预加载请求
    /// </summary>
    public class PreloadRequest
    {
        public string AssetPath;
        public Type AssetType;
        public PreloadPriority Priority;
        public DateTime RequestTime;
        public bool IsCompleted;
        public bool IsCancelled;

        internal CancellationTokenSource CTS;
    }

    /// <summary>
    /// 预加载管理器
    /// </summary>
    public class PreloadManager : IDisposable
    {
        private readonly LWAssetsConfig _config;
        private readonly MemoryMonitor _memoryMonitor;
        private readonly PreloadPredictor _predictor;

        private readonly PriorityQueue<PreloadRequest> _pendingQueue;
        private readonly List<PreloadRequest> _activeRequests;
        private readonly Dictionary<string, PreloadRequest> _requestMap;
        private readonly HashSet<string> _preloadedAssets;

        private CancellationTokenSource _cts;
        private bool _isRunning;
        private readonly object _lockObj = new object();

        public bool IsEnabled => _config.EnablePreload;
        public int PendingCount => _pendingQueue.Count;
        public int ActiveCount => _activeRequests.Count;
        public int PreloadedCount => _preloadedAssets.Count;

        public event Action<string> OnAssetPreloaded;
        public event Action<string> OnPreloadFailed;

        public PreloadManager(LWAssetsConfig config)
        {
            _config = config;
            _memoryMonitor = new MemoryMonitor(config);
            _predictor = new PreloadPredictor();

            _pendingQueue = new PriorityQueue<PreloadRequest>(
                (a, b) => b.Priority.CompareTo(a.Priority)); // 高优先级在前
            _activeRequests = new List<PreloadRequest>();
            _requestMap = new Dictionary<string, PreloadRequest>();
            _preloadedAssets = new HashSet<string>();

            _cts = new CancellationTokenSource();

            if (_config.EnablePreload)
            {
                StartPreloading();
            }
        }

        #region 公共方法

        /// <summary>
        /// 请求预加载资源
        /// </summary>
        public void RequestPreload(string assetPath, PreloadPriority priority = PreloadPriority.Normal)
        {
            if (!_config.EnablePreload) return;
            if (string.IsNullOrEmpty(assetPath)) return;

            lock (_lockObj)
            {
                // 检查是否已预加载
                if (_preloadedAssets.Contains(assetPath)) return;

                // 检查是否已在队列中
                if (_requestMap.ContainsKey(assetPath)) return;

                var request = new PreloadRequest
                {
                    AssetPath = assetPath,
                    Priority = priority,
                    RequestTime = DateTime.Now,
                    CTS = new CancellationTokenSource()
                };

                _requestMap[assetPath] = request;
                _pendingQueue.Enqueue(request);
            }
        }

        /// <summary>
        /// 批量请求预加载
        /// </summary>
        public void RequestPreload(IEnumerable<string> assetPaths, PreloadPriority priority = PreloadPriority.Normal)
        {
            foreach (var path in assetPaths)
            {
                RequestPreload(path, priority);
            }
        }

        /// <summary>
        /// 取消预加载请求
        /// </summary>
        public void CancelPreload(string assetPath)
        {
            lock (_lockObj)
            {
                if (_requestMap.TryGetValue(assetPath, out var request))
                {
                    request.IsCancelled = true;
                    request.CTS?.Cancel();
                    _requestMap.Remove(assetPath);
                }
            }
        }

        /// <summary>
        /// 取消所有预加载请求
        /// </summary>
        public void CancelAll()
        {
            lock (_lockObj)
            {
                foreach (var request in _requestMap.Values)
                {
                    request.IsCancelled = true;
                    request.CTS?.Cancel();
                }

                _pendingQueue.Clear();
                _activeRequests.Clear();
                _requestMap.Clear();
            }
        }

        /// <summary>
        /// 暂停预加载
        /// </summary>
        public void Pause()
        {
            _isRunning = false;
        }

        /// <summary>
        /// 恢复预加载
        /// </summary>
        public void Resume()
        {
            if (!_isRunning && _config.EnablePreload)
            {
                StartPreloading();
            }
        }

        /// <summary>
        /// 记录资源访问（用于智能预测）
        /// </summary>
        public void RecordAccess(string assetPath)
        {
            _predictor.RecordAccess(assetPath);
        }

        /// <summary>
        /// 获取预测的预加载资源
        /// </summary>
        public List<string> GetPredictedAssets(string currentAsset, int maxCount = 5)
        {
            return _predictor.Predict(currentAsset, maxCount);
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 开始预加载处理
        /// </summary>
        private void StartPreloading()
        {
            _isRunning = true;
            ProcessQueueAsync().Forget();
        }

        /// <summary>
        /// 处理预加载队列
        /// </summary>
        private async UniTaskVoid ProcessQueueAsync()
        {
            while (_isRunning)
            {
                // 检查内存状态
                var memoryState = _memoryMonitor.GetMemoryState();
                if (memoryState == MemoryState.Critical)
                {
                    // 内存紧张，暂停预加载
                    await UniTask.Delay(1000, cancellationToken: _cts.Token);
                    continue;
                }

                // 填充活动任务
                while (_activeRequests.Count < _config.MaxPreloadTasks)
                {
                    PreloadRequest request;
                    lock (_lockObj)
                    {
                        if (_pendingQueue.Count == 0) break;

                        // 内存警告时只处理高优先级
                        if (memoryState == MemoryState.Warning)
                        {
                            var peek = _pendingQueue.Peek();
                            if (peek.Priority < PreloadPriority.High) break;
                        }

                        request = _pendingQueue.Dequeue();
                        if (request.IsCancelled) continue;

                        _activeRequests.Add(request);
                    }

                    // 执行预加载
                    PreloadAssetAsync(request).Forget();
                }

                await UniTask.Delay(100, cancellationToken: _cts.Token);
            }
        }

        /// <summary>
        /// 预加载单个资源
        /// </summary>
        private async UniTaskVoid PreloadAssetAsync(PreloadRequest request)
        {
            try
            {
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    _cts.Token, request.CTS.Token))
                {
                    // 执行加载
                    var asset = await LWAssetsService.Assets.LoadAssetAsync<UnityEngine.Object>(
                        request.AssetPath, linkedCts.Token);

                    if (asset != null)
                    {
                        lock (_lockObj)
                        {
                            _preloadedAssets.Add(request.AssetPath);
                        }

                        request.IsCompleted = true;
                        OnAssetPreloaded?.Invoke(request.AssetPath);

                        if (_config.EnableDetailLog)
                        {
                            Debug.Log($"[LWAssets] Preloaded: {request.AssetPath}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 取消，不报错
            }
            catch (Exception ex)
            {
                OnPreloadFailed?.Invoke(request.AssetPath);

                if (_config.EnableDetailLog)
                {
                    Debug.LogWarning($"[LWAssets] Preload failed: {request.AssetPath}, Error: {ex.Message}");
                }
            }
            finally
            {
                lock (_lockObj)
                {
                    _activeRequests.Remove(request);
                    _requestMap.Remove(request.AssetPath);
                }
            }
        }

        #endregion

        public void Dispose()
        {
            _isRunning = false;
            _cts?.Cancel();
            _cts?.Dispose();
            CancelAll();
            _memoryMonitor?.Dispose();
        }
    }

    /// <summary>
    /// 简单优先级队列
    /// </summary>
    public class PriorityQueue<T>
    {
        private readonly List<T> _items = new List<T>();
        private readonly Comparison<T> _comparison;

        public int Count => _items.Count;

        public PriorityQueue(Comparison<T> comparison)
        {
            _comparison = comparison;
        }

        public void Enqueue(T item)
        {
            _items.Add(item);
            _items.Sort(_comparison);
        }

        public T Dequeue()
        {
            if (_items.Count == 0) return default;
            var item = _items[0];
            _items.RemoveAt(0);
            return item;
        }

        public T Peek()
        {
            return _items.Count > 0 ? _items[0] : default;
        }

        public void Clear()
        {
            _items.Clear();
        }
    }
}
