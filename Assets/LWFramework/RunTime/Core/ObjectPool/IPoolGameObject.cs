using UnityEngine;
namespace LWCore
{
    public interface IPoolGameObject
    {
        /// <summary>
        /// 从对象池中创建
        /// </summary>
        void OnSpawn();
        /// <summary>
        /// 回收进对象池
        /// </summary>
        void OnUnSpawn();
        /// <summary>
        /// 释放掉，完全删除
        /// </summary>
        void OnRelease();
        /// <summary>
        /// 是否为Active
        /// </summary>
        bool IsActive();
        /// <summary>
        /// 是否存在场景中
        /// </summary>
        /// <returns>是否存在场景中</returns>
        bool IsValid();
        void SetActive(bool active);
        void Create(GameObject gameObject);

    }
}

