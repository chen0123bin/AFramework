using UnityEngine;

namespace LWCore
{
    /// <summary>
    /// 普通类单例基类
    /// </summary>
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {
        private static T _instance;
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new T();
                            _instance.OnSingletonInit();
                        }
                    }
                }
                return _instance;
            }
        }

        protected virtual void OnSingletonInit() { }
    }
}
