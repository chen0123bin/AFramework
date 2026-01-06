using UnityEngine;

namespace LWCore
{
    /// <summary>
    /// 普通类单例基类
    /// </summary>
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {
        private static T m_Instance;
        private static readonly object m_LockObj = new object();

        public static T Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    lock (m_LockObj)
                    {
                        if (m_Instance == null)
                        {
                            m_Instance = new T();
                            m_Instance.OnSingletonInit();
                        }
                    }
                }
                return m_Instance;
            }
        }

        protected virtual void OnSingletonInit() { }
    }
}
