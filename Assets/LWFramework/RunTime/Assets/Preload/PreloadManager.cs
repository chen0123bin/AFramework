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
        private readonly LWAssetsConfig m_Config;
        private readonly MemoryMonitor m_MemoryMonitor;
        private readonly PreloadPredictor m_Predictor;

        private readonly PriorityQueue<PreloadRequest> m_PendingQueue;
        private readonly List<PreloadRequest> m_ActiveRequests;
        private readonly Dictionary<string, PreloadRequest> m_RequestMap;
        private readonly HashSet<string> m_PreloadedAssets;

        private CancellationTokenSource m_Cts;
        private bool m_IsRunning;
        private readonly object m_LockObj = new object();

        public bool IsEnabled => m_Config.EnablePreload;
        public int PendingCount => m_PendingQueue.Count;
        public int ActiveCount => m_ActiveRequests.Count;
        public int PreloadedCount => m_PreloadedAssets.Count;

        public event Action<string> OnAssetPreloaded;
        public event Action<string> OnPreloadFailed;

        public PreloadManager(LWAssetsConfig config)
        {
            m_Config = config;
            m_MemoryMonitor = new MemoryMonitor(config);
            m_Predictor = new PreloadPredictor();

            m_PendingQueue = new PriorityQueue<PreloadRequest>(
                (a, b) => b.Priority.CompareTo(a.Priority)); // 高优先级在前
            m_ActiveRequests = new List<PreloadRequest>();
            m_RequestMap = new Dictionary<string, PreloadRequest>();
            m_PreloadedAssets = new HashSet<string>();

            m_Cts = new CancellationTokenSource();

            if (m_Config.EnablePreload)
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
            if (!m_Config.EnablePreload) return;
            if (string.IsNullOrEmpty(assetPath)) return;

            lock (m_LockObj)
            {
                // 检查是否已预加载
                if (m_PreloadedAssets.Contains(assetPath)) return;

                // 检查是否已在队列中
                if (m_RequestMap.ContainsKey(assetPath)) return;

                var request = new PreloadRequest
                {
                    AssetPath = assetPath,
                    Priority = priority,
                    RequestTime = DateTime.Now,
                    CTS = new CancellationTokenSource()
                };

                m_RequestMap[assetPath] = request;
                m_PendingQueue.Enqueue(request);
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
            lock (m_LockObj)
            {
                if (m_RequestMap.TryGetValue(assetPath, out var request))
                {
                    request.IsCancelled = true;
                    request.CTS?.Cancel();
                    m_RequestMap.Remove(assetPath);
                }
            }
        }

        /// <summary>
        /// 取消所有预加载请求
        /// </summary>
        public void CancelAll()
        {
            lock (m_LockObj)
            {
                foreach (var request in m_RequestMap.Values)
                {
                    request.IsCancelled = true;
                    request.CTS?.Cancel();
                }

                m_PendingQueue.Clear();
                m_ActiveRequests.Clear();
                m_RequestMap.Clear();
            }
        }

        /// <summary>
        /// 暂停预加载
        /// </summary>
        public void Pause()
        {
            m_IsRunning = false;
        }

        /// <summary>
        /// 恢复预加载
        /// </summary>
        public void Resume()
        {
            if (!m_IsRunning && m_Config.EnablePreload)
            {
                StartPreloading();
            }
        }

        /// <summary>
        /// 记录资源访问（用于智能预测）
        /// </summary>
        public void RecordAccess(string assetPath)
        {
            m_Predictor.RecordAccess(assetPath);
        }

        /// <summary>
        /// 获取预测的预加载资源
        /// </summary>
        public List<string> GetPredictedAssets(string currentAsset, int maxCount = 5)
        {
            return m_Predictor.Predict(currentAsset, maxCount);
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 开始预加载处理
        /// </summary>
        private void StartPreloading()
        {
            m_IsRunning = true;
            ProcessQueueAsync().Forget();
        }

        /// <summary>
        /// 处理预加载队列
        /// </summary>
        private async UniTaskVoid ProcessQueueAsync()
        {
            while (m_IsRunning)
            {
                // 检查内存状态
                var memoryState = m_MemoryMonitor.GetMemoryState();
                if (memoryState == MemoryState.Critical)
                {
                    // 内存紧张，暂停预加载
                    await UniTask.Delay(1000, cancellationToken: m_Cts.Token);
                    continue;
                }

                // 填充活动任务
                while (m_ActiveRequests.Count < m_Config.MaxPreloadTasks)
                {
                    PreloadRequest request;
                    lock (m_LockObj)
                    {
                        if (m_PendingQueue.Count == 0) break;

                        // 内存警告时只处理高优先级
                        if (memoryState == MemoryState.Warning)
                        {
                            var peek = m_PendingQueue.Peek();
                            if (peek.Priority < PreloadPriority.High) break;
                        }

                        request = m_PendingQueue.Dequeue();
                        if (request.IsCancelled) continue;

                        m_ActiveRequests.Add(request);
                    }

                    // 执行预加载
                    PreloadAssetAsync(request).Forget();
                }

                await UniTask.Delay(100, cancellationToken: m_Cts.Token);
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
                    m_Cts.Token, request.CTS.Token))
                {
                    // 执行加载
                    var asset = await LWCore.ManagerUtility.AssetsMgr.LoadAssetAsync<UnityEngine.Object>(
                        request.AssetPath, linkedCts.Token);

                    if (asset != null)
                    {
                        lock (m_LockObj)
                        {
                            m_PreloadedAssets.Add(request.AssetPath);
                        }

                        request.IsCompleted = true;
                        OnAssetPreloaded?.Invoke(request.AssetPath);

                        if (m_Config.EnableDetailLog)
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

                if (m_Config.EnableDetailLog)
                {
                    Debug.LogWarning($"[LWAssets] Preload failed: {request.AssetPath}, Error: {ex.Message}");
                }
            }
            finally
            {
                lock (m_LockObj)
                {
                    m_ActiveRequests.Remove(request);
                    m_RequestMap.Remove(request.AssetPath);
                }
            }
        }

        #endregion

        public void Dispose()
        {
            m_IsRunning = false;
            m_Cts?.Cancel();
            m_Cts?.Dispose();
            CancelAll();
            m_MemoryMonitor?.Dispose();
        }
    }

    /// <summary>
    /// 简单优先级队列
    /// </summary>
    public class PriorityQueue<T>
    {
        private readonly List<T> m_Items = new List<T>();
        private readonly Comparison<T> m_Comparison;

        public int Count => m_Items.Count;

        public PriorityQueue(Comparison<T> comparison)
        {
            m_Comparison = comparison;
        }

        public void Enqueue(T item)
        {
            m_Items.Add(item);
            m_Items.Sort(m_Comparison);
        }

        public T Dequeue()
        {
            if (m_Items.Count == 0) return default;
            var item = m_Items[0];
            m_Items.RemoveAt(0);
            return item;
        }

        public T Peek()
        {
            return m_Items.Count > 0 ? m_Items[0] : default;
        }

        public void Clear()
        {
            m_Items.Clear();
        }
    }
}
