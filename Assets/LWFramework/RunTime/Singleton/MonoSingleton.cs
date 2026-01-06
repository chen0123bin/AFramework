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
        private static T m_Instance;
        private static readonly object m_LockObj = new object();

        public static T Instance
        {
            get
            {
                lock (m_LockObj)
                {
                    if (m_Instance == null)
                    {
                        m_Instance = FindObjectOfType<T>();

                        if (m_Instance == null)
                        {
                            GameObject singletonObject = new GameObject();
                            m_Instance = singletonObject.AddComponent<T>();
                            singletonObject.name = $"[Singleton] {typeof(T)}";

                            if (m_Instance.IsPersistent)
                            {
                                DontDestroyOnLoad(singletonObject);
                            }
                        }
                    }
                    return m_Instance;
                }
            }
        }



        protected virtual void Awake()
        {
            if (m_Instance == null)
            {
                m_Instance = this as T;
                if (IsPersistent)
                {
                    DontDestroyOnLoad(gameObject);
                }
                OnSingletonInit();
            }
            else if (m_Instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnSingletonInit() { }

        protected virtual void OnDestroy()
        {

        }
    }
}
