using System;
using UnityEngine;
using UnityEngine.Profiling;
using Cysharp.Threading.Tasks;

namespace LWAssets
{
    /// <summary>
    /// 内存状态
    /// </summary>
    public enum MemoryState
    {
        Normal,
        Warning,
        Critical
    }
    
    /// <summary>
    /// 内存监控器
    /// </summary>
    public class MemoryMonitor : IDisposable
    {
        private readonly LWAssetsConfig _config;
        private MemoryState _currentState = MemoryState.Normal;
        private long _lastUsedMemory;
        private DateTime _lastCheckTime;
        
        public MemoryState CurrentState => _currentState;
        public long UsedMemory => _lastUsedMemory;
        
        public event Action<MemoryState> OnMemoryStateChanged;
        public event Action OnMemoryWarning;
        public event Action OnMemoryCritical;
        
        public MemoryMonitor(LWAssetsConfig config)
        {
            _config = config;
            
            if (_config.EnableAutoUnload)
            {
                StartMonitoring().Forget();
            }
        }
        
        /// <summary>
        /// 获取当前内存状态
        /// </summary>
        public MemoryState GetMemoryState()
        {
            UpdateMemoryState();
            return _currentState;
        }
        
        /// <summary>
        /// 获取内存统计信息
        /// </summary>
        public MemoryStatistics GetStatistics()
        {
            return new MemoryStatistics
            {
                TotalAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong(),
                TotalReservedMemory = Profiler.GetTotalReservedMemoryLong(),
                TotalUnusedReservedMemory = Profiler.GetTotalUnusedReservedMemoryLong(),
                MonoUsedSize = Profiler.GetMonoUsedSizeLong(),
                MonoHeapSize = Profiler.GetMonoHeapSizeLong(),
                TempAllocatorSize = Profiler.GetTempAllocatorSize(),
                GraphicsMemory = Profiler.GetAllocatedMemoryForGraphicsDriver(),
                State = _currentState
            };
        }
        
        /// <summary>
        /// 开始监控
        /// </summary>
        private async UniTaskVoid StartMonitoring()
        {
            while (true)
            {
                await UniTask.Delay(1000);
                
                var previousState = _currentState;
                UpdateMemoryState();
                
                if (_currentState != previousState)
                {
                    OnMemoryStateChanged?.Invoke(_currentState);
                    
                    if (_currentState == MemoryState.Warning)
                    {
                        OnMemoryWarning?.Invoke();
                    }
                    else if (_currentState == MemoryState.Critical)
                    {
                        OnMemoryCritical?.Invoke();
                        
                        // 自动卸载
                        if (_config.EnableAutoUnload)
                        {
                            await LWAssets.UnloadUnusedAssetsAsync();
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 更新内存状态
        /// </summary>
        private void UpdateMemoryState()
        {
            _lastUsedMemory = Profiler.GetTotalAllocatedMemoryLong();
            _lastCheckTime = DateTime.Now;
            
            if (_lastUsedMemory >= _config.MemoryCriticalThreshold)
            {
                _currentState = MemoryState.Critical;
            }
            else if (_lastUsedMemory >= _config.MemoryWarningThreshold)
            {
                _currentState = MemoryState.Warning;
            }
            else
            {
                _currentState = MemoryState.Normal;
            }
        }
        
        public void Dispose()
        {
        }
    }
    
    /// <summary>
    /// 内存统计信息
    /// </summary>
    public struct MemoryStatistics
    {
        public long TotalAllocatedMemory;
        public long TotalReservedMemory;
        public long TotalUnusedReservedMemory;
        public long MonoUsedSize;
        public long MonoHeapSize;
        public long TempAllocatorSize;
        public long GraphicsMemory;
        public MemoryState State;
        
        public string GetFormattedAllocatedMemory() => FormatBytes(TotalAllocatedMemory);
        public string GetFormattedReservedMemory() => FormatBytes(TotalReservedMemory);
        public string GetFormattedMonoUsedSize() => FormatBytes(MonoUsedSize);
        
        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024 * 1024)
                return $"{bytes / 1024.0 / 1024.0 / 1024.0:F2} GB";
            if (bytes >= 1024 * 1024)
                return $"{bytes / 1024.0 / 1024.0:F2} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F2} KB";
            return $"{bytes} B";
        }
    }
}
