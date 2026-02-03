using System.Collections;
using System.Collections.Generic;
using LWCore;
using UnityEngine;

namespace LWUI
{
    public class BaseUIItem : PoolGameObject
    {
        /// <summary>
        /// 创建GameObject实体
        /// </summary>
        /// <param name="gameObject"></param>
        public override void Create(GameObject gameObject)
        {
            base.Create(gameObject);
            //view上的组件
            ManagerUtility.UIMgr.UIUtility.SetViewElement(this, this.GetType(), gameObject);
        }
        public override void OnUnSpawn()
        {
            base.OnUnSpawn();
            m_Entity.transform.SetAsLastSibling();
        }

    }
}