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
        protected string _path;
        protected bool _isDisposed;

        protected bool _isDone;
        protected float _progress;
        protected Exception _error;

        private long _fileSizeBytes;
        private double _lastLoadTimeMs;
        private double _totalLoadTimeMs;

      
       
        public abstract bool IsValid { get; }
      
        public string BundleName { get; internal set; }
        public int RefCount { get;protected set; }
        public bool IsDisposed => _isDisposed;
        public string Path => _path;
        public  bool IsDone => _isDone;
        public  float Progress => _progress;   
        public Exception Error => _error;
        public bool HasError => _error != null;
        public long FileSizeBytes => _fileSizeBytes;
        public double LastLoadTimeMs => _lastLoadTimeMs;
        public double TotalLoadTimeMs => _totalLoadTimeMs;
        public event Action OnComplete;
        
        protected HandleBase(string path)
        {
            _path = path;
        }
        
        internal void SetLoadInfo(long fileSizeBytes, double loadTimeMs)
        {
            _fileSizeBytes = fileSizeBytes;
            _lastLoadTimeMs = loadTimeMs;
            _totalLoadTimeMs += loadTimeMs;
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
            if (_isDisposed)
            {
                Debug.LogWarning($"[LWAssets] Cannot retain disposed handle: {_path}");
                return;
            }
            RefCount++;
        }
        
        /// <summary>
        /// 减少引用计数，归零后自动释放
        /// </summary>
        public void Release()
        {
            if (_isDisposed) return;

            RefCount--;
            if (RefCount <= 0)
            {
                RefCount = 0;
                //Dispose();
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _fileSizeBytes = 0;
            _lastLoadTimeMs = 0;
            _totalLoadTimeMs = 0;

            OnDispose();
        }

        protected abstract void OnDispose();
    }
    
    /// <summary>
    /// 资源句柄（非泛型，用于缓存/调试）
    /// </summary>
    public class AssetHandle : HandleBase
    {
        protected UnityEngine.Object _asset;
        public string AssetType { get; internal set; }
        public UnityEngine.Object AssetObject => _asset;
        public override bool IsValid => _asset != null;

        public AssetHandle(string assetPath) : base(assetPath)
        {
            _progress = 0f;
            _isDone = true;
        }

        /// <summary>
        /// 设置资源对象并更新统计
        /// </summary>
        internal void SetAssetObject(UnityEngine.Object asset, string bundleName, double loadTimeMs)
        {
            _asset = asset;
            _progress = 1f;
            _isDone = true;
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
            _progress = Mathf.Clamp01(progress);
        }
        
        /// <summary>
        /// 设置错误并标记完成
        /// </summary>
        internal void SetError(Exception error)
        {
            _error = error;
            _isDone = true;
            InvokeComplete();
        }
        
        protected override void OnDispose()
        {
            _asset = null;
        }
    }

    /// <summary>
    /// 泛型资源句柄（用于对外API）
    /// </summary>
    public class AssetHandle<T> : AssetHandle where T : UnityEngine.Object
    {
        public T Asset => _asset as T;

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
        private UnityEngine.SceneManagement.Scene _scene;
        public string AssetType { get; internal set; }
        public UnityEngine.SceneManagement.Scene Scene => _scene;
        public override bool IsValid => _scene.IsValid();
       
        
        public SceneHandle(string scenePath) : base(scenePath)
        {
        }
        
        internal void SetScene(UnityEngine.SceneManagement.Scene scene, string bundleName = null, double loadTimeMs = 0)
        {
            _scene = scene;
            _progress = 1f;
            _isDone = true;
            BundleName = bundleName;
            AssetType = "Scene";
            SetLoadInfo(0, loadTimeMs);
            InvokeComplete();
        }
        
        internal void SetProgress(float progress)
        {
            _progress = Mathf.Clamp01(progress);
        }
        
        internal void SetError(Exception error)
        {
            _error = error;
            _isDone = true;
            InvokeComplete();
        }
        
        /// <summary>
        /// 卸载场景
        /// </summary>
        public async UniTask UnloadAsync()
        {
            if (!_scene.IsValid()) return;
            var op = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(_scene);          
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
    
    /// <summary>
    /// 原始文件句柄
    /// </summary>
    public class RawFileHandle : HandleBase
    {
        private byte[] _data;
       
        public byte[] Data => _data;
        public string Text => _data != null ? System.Text.Encoding.UTF8.GetString(_data) : null;
        public override bool IsValid => _data != null;
       
        public RawFileHandle(string assetPath) : base(assetPath)
        {
        }     
        internal void SetData(byte[] data)
        {
            _data = data;
            _progress = 1f;
            _isDone = true;
            InvokeComplete();
        }
        
        internal void SetProgress(float progress)
        {
            _progress = Mathf.Clamp01(progress);
        }
        
        internal void SetError(Exception error)
        {
            _error = error;
            _isDone = true;
            InvokeComplete();
        }
        
        protected override void OnDispose()
        {
            _data = null;
        }
    }
}
