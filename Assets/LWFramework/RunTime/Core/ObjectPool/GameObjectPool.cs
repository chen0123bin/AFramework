using System.Collections.Generic;
using UnityEngine;

namespace LWCore
{
    public sealed class GameObjectPool<T> where T : class, IPoolGameObject, new()
    {
        public int PoolMaxSize { get; private set; }

        private readonly Stack<T> m_InactiveStack;
        private readonly HashSet<T> m_InactiveSet;
        private readonly List<T> m_ActiveList;
        private readonly GameObject m_Template;
        private readonly Transform m_Parent;
        private readonly bool m_OwnsTemplate;

        /// <summary>
        /// 获取当前未激活（在池中）的对象数量
        /// </summary>
        public int InactiveCount
        {
            get { return m_InactiveStack.Count; }
        }
        /// <summary>
        /// 获取当前激活（借出中）的对象数量
        /// </summary>
        public int ActiveCount
        {
            get { return m_ActiveList.Count; }
        }

        /// <summary>
        /// 获取当前所有对象数量（包括池中的和正在使用中的）
        /// </summary>
        public int AllCount
        {
            get { return InactiveCount + ActiveCount; }
        }
        /// <summary>
        /// 创建一个 GameObject 对象池（T 负责管理 GameObject 实例）。
        /// </summary>
        /// <param name="poolMaxSize">对象池最大容量</param>
        /// <param name="template">模板 GameObject</param>
        /// <param name="parent">父对象（可选）</param>
        /// <param name="ownsTemplate">是否由对象池管理模板 GameObject 的生命周期（可选）</param>
        public GameObjectPool(int poolMaxSize, GameObject template, Transform parent = null, bool ownsTemplate = true)
        {
            PoolMaxSize = Mathf.Max(0, poolMaxSize);

            m_Template = template;
            if (m_Template != null)
            {
                m_Template.SetActive(false);
            }

            m_Parent = parent != null ? parent : (m_Template != null ? m_Template.transform.parent : null);
            m_OwnsTemplate = ownsTemplate;

            m_InactiveStack = new Stack<T>(PoolMaxSize);
            m_InactiveSet = new HashSet<T>();
            m_ActiveList = new List<T>(PoolMaxSize);
        }

        /// <summary>
        /// 从对象池获取一个对象，必要时会创建新实例。
        /// </summary>
        public T Spawn()
        {
            T instance = null;
            while (m_InactiveStack.Count > 0 && instance == null)
            {
                T candidate = m_InactiveStack.Pop();
                m_InactiveSet.Remove(candidate);

                if (candidate != null && candidate.IsValid())
                {
                    instance = candidate;
                }
                else
                {
                    if (candidate != null)
                    {
                        candidate.OnRelease();
                    }
                }
            }

            if (instance == null)
            {
                instance = CreateNewInstance();
            }

            if (instance == null)
            {
                return null;
            }

            if (!m_ActiveList.Contains(instance))
            {
                m_ActiveList.Add(instance);
            }

            instance.SetActive(true);
            instance.OnSpawn();
            return instance;
        }

        /// <summary>
        /// 回收对象进对象池；超出容量时会直接释放。
        /// </summary>
        public void Unspawn(T obj)
        {
            if (obj == null)
            {
                return;
            }

            if (m_InactiveSet.Contains(obj))
            {
                return;
            }

            bool removedFromUseList = m_ActiveList.Remove(obj);
            if (!removedFromUseList)
            {
                return;
            }

            if (m_InactiveStack.Count >= PoolMaxSize)
            {
                obj.OnUnSpawn();
                obj.OnRelease();
                return;
            }

            obj.OnUnSpawn();
            obj.SetActive(false);

            if (m_InactiveSet.Add(obj))
            {
                m_InactiveStack.Push(obj);
            }
        }

        /// <summary>
        /// 回收所有已借出对象。
        /// </summary>
        public void UnspawnAll()
        {
            while (m_ActiveList.Count > 0)
            {
                T obj = m_ActiveList[m_ActiveList.Count - 1];
                Unspawn(obj);
            }
        }

        /// <summary>
        /// 判断对象是否已在对象池中。
        /// </summary>
        public bool IsInPool(T obj)
        {
            if (obj == null)
            {
                return false;
            }

            return m_InactiveSet.Contains(obj);
        }

        /// <summary>
        /// 调整对象池容量，缩容会释放超出部分。
        /// </summary>
        public void ChangeSize(int poolMaxSize)
        {
            PoolMaxSize = Mathf.Max(0, poolMaxSize);

            while (m_InactiveStack.Count > PoolMaxSize)
            {
                T obj = m_InactiveStack.Pop();
                m_InactiveSet.Remove(obj);
                if (obj != null)
                {
                    obj.OnRelease();
                }
            }
        }

        /// <summary>
        /// 清空对象池，可选释放已借出对象。
        /// </summary>
        public void Clear(bool releaseInUseObjects = true)
        {
            while (m_InactiveStack.Count > 0)
            {
                T obj = m_InactiveStack.Pop();
                if (obj != null)
                {
                    obj.OnRelease();
                }
            }

            m_InactiveSet.Clear();

            if (releaseInUseObjects)
            {
                for (int i = 0; i < m_ActiveList.Count; i++)
                {
                    T obj = m_ActiveList[i];
                    if (obj != null)
                    {
                        obj.OnUnSpawn();
                        obj.OnRelease();
                    }
                }
            }

            m_ActiveList.Clear();

            if (m_OwnsTemplate && m_Template != null)
            {
                Object.Destroy(m_Template);
            }
        }

        /// <summary>
        /// 创建一个新的池对象实例，并实例化对应的 GameObject。
        /// </summary>
        private T CreateNewInstance()
        {
            if (m_Template == null)
            {
                return null;
            }

            T instance = new T();
            GameObject go = Object.Instantiate(m_Template, m_Parent, false);
            instance.Create(go);
            return instance;
        }
    }
}
