using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWUI
{
    /// <summary>
    /// 所有的UI管理器
    /// </summary>
    public class UIManager : BaseUIManager
    {



        private Canvas m_UICanvas;
        public override Canvas UICanvas
        {
            get
            {
                if (m_UICanvas == null)
                {
                    m_UICanvas = GameObject.Find("LWFramework/Canvas").GetComponent<Canvas>();
                }
                return m_UICanvas;
            }
            set
            {
                m_UICanvas = value;
            }
        }

        private Camera m_UICamera;
        public override Camera UICamera
        {
            get
            {
                if (m_UICamera == null)
                {
                    m_UICamera = GameObject.Find("LWFramework/Canvas/UICamera").GetComponent<Camera>();
                }
                return m_UICamera;
            }
        }
        #region 获取Canvas编辑节点
        private Transform _editTransform;
        private Transform EditTransform
        {
            get
            {
                if (_editTransform == null)
                {
                    _editTransform = GameObject.Find("LWFramework/Canvas/Edit").transform;
                }
                return _editTransform;
            }
        }

        #endregion
        public override void Init()
        {
            base.Init();
            m_UIUtility = new UIUtility(); //ManagerUtility.HotfixMgr.Instantiate<IUIUtility>("LWFramework.UI.UIUtility");
                                           //  uiUtility = ManagerUtility.HotfixMgr.Instantiate<IUIUtility>("LWFramework.UI.UIUtility");
                                           //启动之后隐藏编辑层
            EditTransform.gameObject.SetActive(false);
        }


        /// <summary>
        /// 打开View
        /// </summary>
        /// <typeparam name="T">view的控制类</typeparam>
        /// <param name="isLastSibling">是否放置在最前面</param>
        public override T OpenView<T>(bool isLastSibling = false, bool enterStack = false)
        {
            BaseUIView uiViewBase = null;
            if (!m_UIViewDic.TryGetValue(typeof(T).ToString(), out uiViewBase))
            {
                uiViewBase = m_UIUtility.CreateView<T>();
                m_UIViewDic.Add(typeof(T).ToString(), uiViewBase);
                m_UIViewList.Add(uiViewBase);
            }
            uiViewBase.OpenView();

            uiViewBase.SetViewLastSibling(isLastSibling);
            if (enterStack)
            {
                m_UIViewStack.Push(uiViewBase);
            }
            return (T)uiViewBase;
        }

        /// <summary>
        /// 打开View
        /// </summary>
        /// <typeparam name="T">view的控制类</typeparam>
        /// <param name="viewType">view的名字，用于一个多个页面共用一个类</param>
        /// <param name="uiGameObject">view的对象，提前创建，优先级高于自己创建</param>
        /// <param name="isLastSibling">是否放置在最前面</param>
        public override BaseUIView OpenView(string viewType, GameObject uiGameObject = null, bool isLastSibling = false, bool enterStack = false)
        {
            BaseUIView uiViewBase;
            if (!m_UIViewDic.TryGetValue(viewType, out uiViewBase))
            {
                if (m_UIBindViewPath.ContainsKey(viewType))
                {
                    uiViewBase = m_UIUtility.CreateView(viewType, uiGameObject, m_UIBindViewPath[viewType]);
                }
                else
                {
                    uiViewBase = m_UIUtility.CreateView(viewType, uiGameObject);
                }
                m_UIViewDic.Add(viewType, uiViewBase);
                m_UIViewList.Add(uiViewBase);
            }
            uiViewBase.OpenView();
            uiViewBase.SetViewLastSibling(isLastSibling);
            if (enterStack)
            {
                m_UIViewStack.Push(uiViewBase);
            }
            return uiViewBase;
        }



        public override async UniTask<T> OpenViewAsync<T>(bool isLastSibling = false, bool enterStack = false)
        {
            BaseUIView uiViewBase = null;
            if (!m_UIViewDic.TryGetValue(typeof(T).ToString(), out uiViewBase))
            {
                uiViewBase = await IUIUtility.CreateViewAsync<T>();
                if (!m_UIViewDic.ContainsKey(typeof(T).ToString()))
                {
                    m_UIViewDic.Add(typeof(T).ToString(), uiViewBase);
                    m_UIViewList.Add(uiViewBase);
                }
                else
                {
                    uiViewBase = m_UIViewDic[typeof(T).ToString()];
                }

            }
            await UniTask.WaitUntil(() => uiViewBase != null);
            if (!uiViewBase.IsOpen)
                uiViewBase.OpenView();
            uiViewBase.SetViewLastSibling(isLastSibling);
            if (enterStack)
            {
                m_UIViewStack.Push(uiViewBase);
            }
            return (T)uiViewBase;
        }
        public async override UniTask PreLoadDefaultUI()
        {
            string style = GetStyle();
            await UniTask.WaitForSeconds(1); // 模拟异步操作
            //style = style.IsEmpty() ? "Default" : style;
            //加载内置UI资源

            //await ManagerUtility.AssetsMgr.LoadAsync<GameObject>($"Assets/@Resources/{LWUtility.BuildIn}/UI/{style}/LoadingBarView.prefab");
            //await ManagerUtility.AssetsMgr.LoadAsync<GameObject>($"Assets/@Resources/{LWUtility.BuildIn}/UI/{style}/LoadingView.prefab");
            //await ManagerUtility.AssetsMgr.LoadAsync<GameObject>($"Assets/@Resources/{LWUtility.BuildIn}/UI/{style}/MessageBoxView.prefab");
        }


    }
}

