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
        private readonly LWAssetsConfig m_Config;
        private MemoryState m_CurrentState = MemoryState.Normal;
        private long m_LastUsedMemory;
        private DateTime m_LastCheckTime;

        public MemoryState CurrentState => m_CurrentState;
        public long UsedMemory => m_LastUsedMemory;

        public event Action<MemoryState> OnMemoryStateChanged;
        public event Action OnMemoryWarning;
        public event Action OnMemoryCritical;

        public MemoryMonitor(LWAssetsConfig config)
        {
            m_Config = config;

            if (m_Config.EnableAutoUnload)
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
            return m_CurrentState;
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
                State = m_CurrentState
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

                var previousState = m_CurrentState;
                UpdateMemoryState();

                if (m_CurrentState != previousState)
                {
                    OnMemoryStateChanged?.Invoke(m_CurrentState);

                    if (m_CurrentState == MemoryState.Warning)
                    {
                        OnMemoryWarning?.Invoke();
                    }
                    else if (m_CurrentState == MemoryState.Critical)
                    {
                        OnMemoryCritical?.Invoke();

                        // 自动卸载
                        if (m_Config.EnableAutoUnload)
                        {
                            await LWCore.ManagerUtility.AssetsMgr.UnloadUnusedAssetsAsync();
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
            m_LastUsedMemory = Profiler.GetTotalAllocatedMemoryLong();
            m_LastCheckTime = DateTime.Now;

            if (m_LastUsedMemory >= m_Config.MemoryCriticalThreshold)
            {
                m_CurrentState = MemoryState.Critical;
            }
            else if (m_LastUsedMemory >= m_Config.MemoryWarningThreshold)
            {
                m_CurrentState = MemoryState.Warning;
            }
            else
            {
                m_CurrentState = MemoryState.Normal;
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
