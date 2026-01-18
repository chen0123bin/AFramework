using UnityEngine;
namespace LWCore
{
    public class PoolGameObject : IPoolGameObject
    {

        protected GameObject m_Entity;
        /// <summary>
        /// 创建GameObject实体
        /// </summary>
        /// <param name="gameObject"></param>
        public virtual void Create(GameObject gameObject)
        {
            m_Entity = gameObject;
        }
        public virtual void OnUnSpawn()
        {
        }
        public virtual void OnSpawn()
        {
        }
        public bool IsValid()
        {
            return m_Entity;
        }
        /// <summary>
        /// 释放引用，删除gameobject
        /// </summary>
        public virtual void OnRelease()
        {
            GameObject.Destroy(m_Entity);
        }

        public bool IsActive()
        {
            return m_Entity != null && m_Entity.activeInHierarchy;
        }

        public void SetActive(bool active)
        {
            if (m_Entity != null)
            {
                m_Entity.SetActive(active);
            }
        }
    }
}

