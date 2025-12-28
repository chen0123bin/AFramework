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
        protected string _assetPath;
        protected int _referenceCount;
        protected bool _isDisposed;
        
        public string AssetPath => _assetPath;
        public int ReferenceCount => _referenceCount;
        public bool IsDisposed => _isDisposed;
        public abstract bool IsValid { get; }
        public abstract bool IsDone { get; }
        public abstract float Progress { get; }
        
        public event Action OnComplete;
        
        protected void InvokeComplete()
        {
            OnComplete?.Invoke();
        }
        
        public void Retain()
        {
            if (_isDisposed)
            {
                Debug.LogWarning($"[LWAssets] Cannot retain disposed handle: {_assetPath}");
                return;
            }
            _referenceCount++;
        }
        
        public void Release()
        {
            if (_isDisposed) return;
            
            _referenceCount--;
            if (_referenceCount <= 0)
            {
                Dispose();
            }
        }
        
        public abstract void Dispose();
    }
    
    /// <summary>
    /// 泛型资源句柄
    /// </summary>
    public class AssetHandle<T> : HandleBase where T : UnityEngine.Object
    {
        private T _asset;
        private bool _isDone;
        private float _progress;
        private Exception _error;
        
        public T Asset => _asset;
        public override bool IsValid => _asset != null;
        public override bool IsDone => _isDone;
        public override float Progress => _progress;
        public Exception Error => _error;
        public bool HasError => _error != null;
        
        public AssetHandle(string assetPath)
        {
            _assetPath = assetPath;
            _referenceCount = 1;
        }
        
        internal void SetAsset(T asset)
        {
            _asset = asset;
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
        
        public override void Dispose()
        {
            if (_isDisposed) return;
            
            _isDisposed = true;
            
            if (_asset != null)
            {
                LWAssets.Release(_asset);
                _asset = null;
            }
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
        private bool _isDone;
        private float _progress;
        private Exception _error;
        
        public UnityEngine.SceneManagement.Scene Scene => _scene;
        public override bool IsValid => _scene.IsValid();
        public override bool IsDone => _isDone;
        public override float Progress => _progress;
        public Exception Error => _error;
        public bool HasError => _error != null;
        
        public SceneHandle(string scenePath)
        {
            _assetPath = scenePath;
            _referenceCount = 1;
        }
        
        internal void SetScene(UnityEngine.SceneManagement.Scene scene)
        {
            _scene = scene;
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
        
        /// <summary>
        /// 卸载场景
        /// </summary>
        public async Cysharp.Threading.Tasks.UniTask UnloadAsync()
        {
            if (!_scene.IsValid()) return;
            
            var op = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(_scene);
            await op;
        }
        
        public override void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
        }
    }
    
    /// <summary>
    /// 原始文件句柄
    /// </summary>
    public class RawFileHandle : HandleBase
    {
        private byte[] _data;
        private bool _isDone;
        private float _progress;
        private Exception _error;
        
        public byte[] Data => _data;
        public string Text => _data != null ? System.Text.Encoding.UTF8.GetString(_data) : null;
        public override bool IsValid => _data != null;
        public override bool IsDone => _isDone;
        public override float Progress => _progress;
        public Exception Error => _error;
        public bool HasError => _error != null;
        
        public RawFileHandle(string assetPath)
        {
            _assetPath = assetPath;
            _referenceCount = 1;
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
        
        public override void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _data = null;
        }
    }
}
