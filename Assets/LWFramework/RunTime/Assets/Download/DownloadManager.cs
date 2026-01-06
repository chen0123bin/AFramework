using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LWAssets
{
    /// <summary>
    /// 下载进度
    /// </summary>
    public struct DownloadProgress
    {
        public int TotalCount;
        public int CompletedCount;
        public long TotalBytes;
        public long DownloadedBytes;
        public float Progress => TotalBytes > 0 ? (float)DownloadedBytes / TotalBytes : 0;
        public string CurrentFile;
        public float Speed; // bytes per second
    }
    
    /// <summary>
    /// 下载任务
    /// </summary>
    public class DownloadTask
    {
        public string Url;
        public string SavePath;
        public long ExpectedSize;
        public uint ExpectedCRC;
        public int RetryCount;
        public long DownloadedBytes;
        public DownloadTaskStatus Status;
        public Exception Error;
        
        public event Action<DownloadTask> OnProgress;
        public event Action<DownloadTask> OnCompleted;
        public event Action<DownloadTask> OnFailed;
        
        internal void NotifyProgress() => OnProgress?.Invoke(this);
        internal void NotifyCompleted() => OnCompleted?.Invoke(this);
        internal void NotifyFailed() => OnFailed?.Invoke(this);
    }
    
    public enum DownloadTaskStatus
    {
        Pending,
        Downloading,
        Completed,
        Failed,
        Cancelled
    }
    
    /// <summary>
    /// 下载管理器
    /// </summary>
    public class DownloadManager : IDisposable
    {
        private readonly LWAssetsConfig m_Config;
        private readonly Queue<DownloadTask> m_PendingQueue = new Queue<DownloadTask>();
        private readonly List<DownloadTask> m_ActiveTasks = new List<DownloadTask>();
        private readonly Dictionary<string, DownloadTask> m_TaskMap = new Dictionary<string, DownloadTask>();
        
        private CancellationTokenSource m_Cts;
        private bool m_IsRunning;
        private long m_TotalDownloadedBytes;
        private DateTime m_LastSpeedCalculateTime;
        private long m_LastDownloadedBytes;
        private float m_CurrentSpeed;
        
        private readonly object m_LockObj = new object();
        
        public int PendingCount => m_PendingQueue.Count;
        public int ActiveCount => m_ActiveTasks.Count;
        public bool IsRunning => m_IsRunning;
        public float CurrentSpeed => m_CurrentSpeed;
        
        public event Action<DownloadTask> OnTaskCompleted;
        public event Action<DownloadTask> OnTaskFailed;
        public event Action OnAllCompleted;
        
        public DownloadManager(LWAssetsConfig config)
        {
            m_Config = config;
            m_Cts = new CancellationTokenSource();
        }
        
        #region 公共方法
        
        /// <summary>
        /// 下载Bundle列表
        /// </summary>
        public async UniTask DownloadAsync(IEnumerable<BundleInfo> bundles, 
            IProgress<DownloadProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            var tasks = new List<DownloadTask>();
            long totalSize = 0;
            
            foreach (var bundle in bundles)
            {
                var url = m_Config.GetRemoteURL() + bundle.GetFileName();
                var savePath = Path.Combine(m_Config.GetPersistentDataPath(), bundle.GetFileName());
                
                // 检查是否已下载
                if (File.Exists(savePath))
                {
                    var fileInfo = new FileInfo(savePath);
                    if (fileInfo.Length == bundle.Size)
                    {
                        // 验证哈希
                        if (ValidateFileCRC32(savePath, bundle.CRC))
                        {
                            continue; // 已存在且有效
                        }
                    }
                }
                
                var task = new DownloadTask
                {
                    Url = url,
                    SavePath = savePath,
                    ExpectedSize = bundle.Size,
                    ExpectedCRC = bundle.CRC,
                    Status = DownloadTaskStatus.Pending
                };
                
                tasks.Add(task);
                totalSize += bundle.Size;
            }
            
            if (tasks.Count == 0)
            {
                progress?.Report(new DownloadProgress
                {
                    TotalCount = 0,
                    CompletedCount = 0,
                    TotalBytes = 0,
                    DownloadedBytes = 0
                });
                return;
            }
            
            // 添加到队列
            foreach (var task in tasks)
            {
                EnqueueTask(task);
            }
            
            // 启动下载
            StartDownloading();
            
            // 等待所有任务完成
            var downloadProgress = new DownloadProgress
            {
                TotalCount = tasks.Count,
                TotalBytes = totalSize
            };
            
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_Cts.Token))
            {
                while (true)
                {
                    linkedCts.Token.ThrowIfCancellationRequested();
                    
                    // 计算进度
                    int completedCount = 0;
                    long downloadedBytes = 0;
                    string currentFile = null;
                    
                    foreach (var task in tasks)
                    {
                        if (task.Status == DownloadTaskStatus.Completed)
                        {
                            completedCount++;
                            downloadedBytes += task.ExpectedSize;
                        }
                        else if (task.Status == DownloadTaskStatus.Downloading)
                        {
                            currentFile = Path.GetFileName(task.SavePath);
                            downloadedBytes += task.DownloadedBytes;
                        }
                        else if (task.Status == DownloadTaskStatus.Failed)
                        {
                            throw task.Error ?? new Exception($"Download failed: {task.Url}");
                        }
                    }
                    
                    downloadProgress.CompletedCount = completedCount;
                    downloadProgress.DownloadedBytes = downloadedBytes;
                    downloadProgress.CurrentFile = currentFile;
                    downloadProgress.Speed = m_CurrentSpeed;
                    
                    progress?.Report(downloadProgress);
                    
                    if (completedCount >= tasks.Count)
                    {
                        break;
                    }
                    
                    await UniTask.Delay(100, cancellationToken: linkedCts.Token);
                }
            }
        }
        
        /// <summary>
        /// 添加下载任务
        /// </summary>
        public void EnqueueTask(DownloadTask task)
        {
            lock (m_LockObj)
            {
                if (m_TaskMap.ContainsKey(task.Url))
                {
                    return; // 避免重复
                }
                
                m_TaskMap[task.Url] = task;
                m_PendingQueue.Enqueue(task);
            }
        }
        
        /// <summary>
        /// 开始下载
        /// </summary>
        public void StartDownloading()
        {
            if (m_IsRunning) return;
            
            m_IsRunning = true;
            m_LastSpeedCalculateTime = DateTime.Now;
            m_LastDownloadedBytes = 0;
            
            ProcessQueue().Forget();
        }
        
        /// <summary>
        /// 暂停下载
        /// </summary>
        public void Pause()
        {
            m_IsRunning = false;
        }
        
        /// <summary>
        /// 取消所有下载
        /// </summary>
        public void CancelAll()
        {
            m_Cts.Cancel();
            m_Cts = new CancellationTokenSource();
            
            lock (m_LockObj)
            {
                m_PendingQueue.Clear();
                m_ActiveTasks.Clear();
                m_TaskMap.Clear();
            }
            
            m_IsRunning = false;
        }
        
        #endregion
        
        #region 内部方法
        
        /// <summary>
        /// 处理下载队列
        /// </summary>
        private async UniTaskVoid ProcessQueue()
        {
            while (m_IsRunning)
            {
                // 计算速度
                CalculateSpeed();
                
                // 填充活动任务
                while (m_ActiveTasks.Count < m_Config.MaxConcurrentDownloads)
                {
                    DownloadTask task;
                    lock (m_LockObj)
                    {
                        if (m_PendingQueue.Count == 0) break;
                        task = m_PendingQueue.Dequeue();
                        m_ActiveTasks.Add(task);
                    }
                    
                    // 启动下载
                    DownloadTaskAsync(task, m_Cts.Token).Forget();
                }
                
                // 检查是否全部完成
                lock (m_LockObj)
                {
                    if (m_PendingQueue.Count == 0 && m_ActiveTasks.Count == 0)
                    {
                        m_IsRunning = false;
                        OnAllCompleted?.Invoke();
                        break;
                    }
                }
                
                await UniTask.Delay(50);
            }
        }
        
        /// <summary>
        /// 执行单个下载任务
        /// </summary>
        private async UniTaskVoid DownloadTaskAsync(DownloadTask task, CancellationToken cancellationToken)
        {
            task.Status = DownloadTaskStatus.Downloading;
            
            try
            {
                // 确保目录存在
                var directory = Path.GetDirectoryName(task.SavePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // 检查断点续传
                long startPosition = 0;
                string tempPath = task.SavePath + ".tmp";
                
                if (m_Config.EnableBreakpointResume && File.Exists(tempPath))
                {
                    var fileInfo = new FileInfo(tempPath);
                    startPosition = fileInfo.Length;
                    task.DownloadedBytes = startPosition;
                }
                
                // 创建请求
                using (var request = new UnityWebRequest(task.Url, UnityWebRequest.kHttpVerbGET))
                {
                    // 设置断点续传
                    if (startPosition > 0)
                    {
                        request.SetRequestHeader("Range", $"bytes={startPosition}-");
                    }
                    
                    // 使用自定义下载处理器
                    var downloadHandler = new DownloadHandlerFileWithProgress(tempPath, startPosition, task);
                    request.downloadHandler = downloadHandler;
                    request.timeout = m_Config.DownloadTimeout;
                    
                    var operation = request.SendWebRequest();
                    
                    while (!operation.isDone)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        task.DownloadedBytes = startPosition + (long)(request.downloadedBytes);
                        task.NotifyProgress();
                        
                        Interlocked.Add(ref m_TotalDownloadedBytes, (long)request.downloadedBytes);
                        
                        await UniTask.Yield(cancellationToken);
                    }
                    
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        throw new Exception($"Download failed: {request.error}");
                    }
                }

                // 验证文件
                if (!ValidateFileCRC32(tempPath, task.ExpectedCRC))
                {
                    File.Delete(tempPath);
                    throw new Exception("File hash mismatch");
                }

                // 重命名临时文件
                if (File.Exists(task.SavePath))
                {
                    File.Delete(task.SavePath);
                }
                File.Move(tempPath, task.SavePath);
                
                task.Status = DownloadTaskStatus.Completed;
                task.NotifyCompleted();
                OnTaskCompleted?.Invoke(task);
            }
            catch (OperationCanceledException)
            {
                task.Status = DownloadTaskStatus.Cancelled;
            }
            catch (Exception ex)
            {
                task.Error = ex;
                task.RetryCount++;
                
                if (task.RetryCount < m_Config.MaxRetryCount)
                {
                    // 重试
                    Debug.LogWarning($"[LWAssets] Download retry {task.RetryCount}/{m_Config.MaxRetryCount}: {task.Url}");
                    await UniTask.Delay(TimeSpan.FromSeconds(m_Config.RetryDelay), cancellationToken: cancellationToken);
                    
                    lock (m_LockObj)
                    {
                        m_ActiveTasks.Remove(task);
                        m_PendingQueue.Enqueue(task);
                    }
                    return;
                }
                
                task.Status = DownloadTaskStatus.Failed;
                task.NotifyFailed();
                OnTaskFailed?.Invoke(task);
                Debug.LogError($"[LWAssets] Download failed: {task.Url}, Error: {ex.Message}");
            }
            finally
            {
                lock (m_LockObj)
                {
                    m_ActiveTasks.Remove(task);
                }
            }
        }
        
        /// <summary>
        /// 计算下载速度
        /// </summary>
        private void CalculateSpeed()
        {
            var now = DateTime.Now;
            var elapsed = (now - m_LastSpeedCalculateTime).TotalSeconds;
            
            if (elapsed >= 1.0)
            {
                var bytesDownloaded = m_TotalDownloadedBytes - m_LastDownloadedBytes;
                m_CurrentSpeed = (float)(bytesDownloaded / elapsed);
                
                m_LastSpeedCalculateTime = now;
                m_LastDownloadedBytes = m_TotalDownloadedBytes;
            }
        }
        
        /// <summary>
        /// 验证文件哈希
        /// </summary>
        private bool ValidateFileHash(string filePath, string expectedHash)
        {
            if (string.IsNullOrEmpty(expectedHash)) return true;
            
            var actualHash = HashUtility.ComputeFileMD5(filePath);
            return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>
        /// 验证文件CRC
        /// </summary>
        private bool ValidateFileCRC32(string filePath, uint expectedCRC32)
        {
            if (expectedCRC32 == 0) return true;

            var actualCRC32 = HashUtility.ComputeFileCRC32(filePath);
            return actualCRC32 == expectedCRC32;
        }
        #endregion

        public void Dispose()
        {
            CancelAll();
            m_Cts?.Dispose();
        }
    }
    
    /// <summary>
    /// 支持进度和断点续传的下载处理器
    /// </summary>
    public class DownloadHandlerFileWithProgress : DownloadHandlerScript
    {
        private readonly FileStream m_FileStream;
        private readonly DownloadTask m_Task;
        private readonly long m_StartPosition;
        
        public DownloadHandlerFileWithProgress(string path, long startPosition, DownloadTask task) 
            : base(new byte[1024 * 1024]) // 1MB buffer
        {
            m_StartPosition = startPosition;
            m_Task = task;
            
            m_FileStream = new FileStream(path, 
                startPosition > 0 ? FileMode.Append : FileMode.Create, 
                FileAccess.Write);
        }
        
        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength == 0) return false;
            
            m_FileStream.Write(data, 0, dataLength);
            return true;
        }
        
        protected override void CompleteContent()
        {
            m_FileStream?.Flush();
            m_FileStream?.Close();
        }
        
        public override void Dispose()
        {
            m_FileStream?.Dispose();
            base.Dispose();
        }
    }
}
