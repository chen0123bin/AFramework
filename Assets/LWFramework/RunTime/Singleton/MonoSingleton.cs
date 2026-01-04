using UnityEngine;

namespace LWCore
{
    /// <summary>
    /// 单例基类
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        /// <summary>
        /// 是否跨场景保留
        /// </summary>
        protected virtual bool IsPersistent => true;
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed. Returning null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();

                        if (_instance == null)
                        {
                            GameObject singletonObject = new GameObject();
                            _instance = singletonObject.AddComponent<T>();
                            singletonObject.name = $"[Singleton] {typeof(T)}";

                            if (_instance.IsPersistent)
                            {
                                DontDestroyOnLoad(singletonObject);
                            }
                        }
                    }
                    return _instance;
                }
            }
        }



        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                if (IsPersistent)
                {
                    DontDestroyOnLoad(gameObject);
                }
                OnSingletonInit();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnSingletonInit() { }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _applicationIsQuitting = true;
            }
        }
    }
}
