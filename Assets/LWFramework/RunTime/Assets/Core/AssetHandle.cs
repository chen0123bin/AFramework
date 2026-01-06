using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LWAssets
{
    /// <summary>
    /// 资源句柄基类
    /// </summary>
    public abstract class HandleBase : IDisposable
    {
        protected string m_Path;
        protected bool m_IsDisposed;

        protected bool m_IsDone;
        protected float m_Progress;
        protected Exception m_Error;

        private long m_FileSizeBytes;
        private double m_LastLoadTimeMs;
        private double m_TotalLoadTimeMs;



        public abstract bool IsValid { get; }

        public string BundleName { get; internal set; }
        public int RefCount { get; protected set; }
        public bool IsDisposed => m_IsDisposed;
        public string Path => m_Path;
        public bool IsDone => m_IsDone;
        public float Progress => m_Progress;
        public Exception Error => m_Error;
        public bool HasError => m_Error != null;
        public long FileSizeBytes => m_FileSizeBytes;
        public double LastLoadTimeMs => m_LastLoadTimeMs;
        public double TotalLoadTimeMs => m_TotalLoadTimeMs;
        public event Action OnComplete;

        protected HandleBase(string path)
        {
            m_Path = path;
        }

        internal void SetLoadInfo(long fileSizeBytes, double loadTimeMs)
        {
            m_FileSizeBytes = fileSizeBytes;
            m_LastLoadTimeMs = loadTimeMs;
            m_TotalLoadTimeMs += loadTimeMs;
        }

        /// <summary>
        /// 触发完成回调
        /// </summary>
        protected void InvokeComplete()
        {
            OnComplete?.Invoke();
        }

        /// <summary>
        /// 增加引用计数
        /// </summary>
        public void Retain()
        {
            if (m_IsDisposed)
            {
                Debug.LogWarning($"[LWAssets] Cannot retain disposed handle: {m_Path}");
                return;
            }
            RefCount++;
        }

        /// <summary>
        /// 减少引用计数，归零后自动释放
        /// </summary>
        public void Release()
        {
            if (m_IsDisposed) return;

            RefCount--;
            if (RefCount <= 0)
            {
                RefCount = 0;
                //Dispose();
            }
        }

        public void Dispose()
        {
            if (m_IsDisposed) return;
            m_IsDisposed = true;
            m_FileSizeBytes = 0;
            m_LastLoadTimeMs = 0;
            m_TotalLoadTimeMs = 0;

            OnDispose();
        }

        protected abstract void OnDispose();
    }

    /// <summary>
    /// 资源句柄（非泛型，用于缓存/调试）
    /// </summary>
    public class AssetHandle : HandleBase
    {
        protected UnityEngine.Object m_Asset;
        public string AssetType { get; internal set; }
        public UnityEngine.Object AssetObject => m_Asset;
        public override bool IsValid => m_Asset != null;

        public AssetHandle(string assetPath) : base(assetPath)
        {
            m_Progress = 0f;
            m_IsDone = true;
        }

        /// <summary>
        /// 设置资源对象并更新统计
        /// </summary>
        internal void SetAssetObject(UnityEngine.Object asset, string bundleName, double loadTimeMs)
        {
            m_Asset = asset;
            m_Progress = 1f;
            m_IsDone = true;
            BundleName = bundleName;
            AssetType = asset != null ? asset.GetType().Name : null;
            SetLoadInfo(0, loadTimeMs);
            InvokeComplete();
        }


        /// <summary>
        /// 设置加载进度
        /// </summary>
        internal void SetProgress(float progress)
        {
            m_Progress = Mathf.Clamp01(progress);
        }

        /// <summary>
        /// 设置错误并标记完成
        /// </summary>
        internal void SetError(Exception error)
        {
            m_Error = error;
            m_IsDone = true;
            InvokeComplete();
        }

        protected override void OnDispose()
        {
            m_Asset = null;
        }
    }

    /// <summary>
    /// 泛型资源句柄（用于对外API）
    /// </summary>
    public class AssetHandle<T> : AssetHandle where T : UnityEngine.Object
    {
        public T Asset => m_Asset as T;

        public AssetHandle(string assetPath) : base(assetPath)
        {
        }

        /// <summary>
        /// 设置资源对象（可选写入Bundle与耗时统计）
        /// </summary>
        internal void SetAsset(T asset, string bundleName = null, double loadTimeMs = 0)
        {
            SetAssetObject(asset, bundleName, loadTimeMs);
        }

        /// <summary>
        /// 隐式转换为资源类型
        /// </summary>
        public static implicit operator T(AssetHandle<T> handle)
        {
            return handle?.Asset;
        }
    }

    /// <summary>
    /// 场景句柄
    /// </summary>
    public class SceneHandle : HandleBase
    {
        private UnityEngine.SceneManagement.Scene m_Scene;
        public string AssetType { get; internal set; }
        public UnityEngine.SceneManagement.Scene Scene => m_Scene;
        public override bool IsValid => m_Scene.IsValid();


        public SceneHandle(string scenePath) : base(scenePath)
        {
        }

        internal void SetScene(UnityEngine.SceneManagement.Scene scene, string bundleName = null, double loadTimeMs = 0)
        {
            m_Scene = scene;
            m_Progress = 1f;
            m_IsDone = true;
            BundleName = bundleName;
            AssetType = "Scene";
            SetLoadInfo(0, loadTimeMs);
            InvokeComplete();
        }

        internal void SetProgress(float progress)
        {
            m_Progress = Mathf.Clamp01(progress);
        }

        internal void SetError(Exception error)
        {
            m_Error = error;
            m_IsDone = true;
            InvokeComplete();
        }

        /// <summary>
        /// 卸载场景
        /// </summary>
        public async UniTask UnloadAsync()
        {
            if (!m_Scene.IsValid()) return;
            var op = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(m_Scene);
            if (op != null)
            {
                await op;
            }
        }

        protected override void OnDispose()
        {
            UnloadAsync().Forget();
        }
    }

    public class RawFileHandle : HandleBase
    {
        private byte[] m_Data;

        public byte[] Data => m_Data;
        public string Text => m_Data != null ? System.Text.Encoding.UTF8.GetString(m_Data) : null;
        public override bool IsValid => m_Data != null;

        public RawFileHandle(string assetPath) : base(assetPath)
        {
        }

        internal void SetData(byte[] data, string bundleName, long fileSizeBytes, double loadTimeMs)
        {
            m_Data = data;
            m_Progress = 1f;
            m_IsDone = true;
            BundleName = bundleName;
            SetLoadInfo(fileSizeBytes, loadTimeMs);
            InvokeComplete();
        }

        internal void SetError(Exception error)
        {
            m_Error = error;
            m_IsDone = true;
            InvokeComplete();
        }

        protected override void OnDispose()
        {
            m_Data = null;
        }
    }
}
