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
                uiViewBase = await UIUtility.CreateViewAsync<T>();
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

        /// <summary>
        /// 打开弹窗（不等待结果）
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="isShowCancel"></param>
        /// <param name="isShowClose"></param>
        public override void OpenDialog(string title, string content, Action<bool> ResultCallback, bool isShowCancel = true, bool isShowClose = true, bool isLastSibling = true)
        {

            DialogView dialogView = OpenView<DialogView>(isLastSibling, false);
            dialogView.ShowAsync(title, content, isShowCancel, isShowClose).ContinueWith((bool result) =>
            {
                ResultCallback?.Invoke(result);
            }).Forget();
        }

        /// <summary>
        /// 打开弹窗并等待用户选择（true=确认，false=取消/关闭）
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="isShowCancel"></param>
        /// <param name="isShowClose"></param>
        /// <param name="isLastSibling"></param>
        /// <returns></returns>
        public override async UniTask<bool> OpenDialogAsync(string title, string content, bool isShowCancel = true, bool isShowClose = true, bool isLastSibling = true)
        {
            DialogView dialogView = OpenView<DialogView>(isLastSibling, false);
            if (dialogView == null)
            {
                return false;
            }

            bool result = await dialogView.ShowAsync(title, content, isShowCancel, isShowClose);
            return result;
        }

        /// <summary>
        /// 打开Loading弹窗
        /// </summary>
        /// <param name="tip"></param>
        /// <param name="isLastSibling"></param>
        public override void OpenLoadingBar(string tip = "当前正在加载...", bool isLastSibling = true)
        {
            LoadingBarView loadingBarView = GetView<LoadingBarView>();
            if (loadingBarView == null)
            {
                loadingBarView = OpenView<LoadingBarView>(isLastSibling, false);
            }
            else
            {
                if (!loadingBarView.IsOpen)
                {
                    loadingBarView.OpenView();
                }
                loadingBarView.SetViewLastSibling(isLastSibling);
            }

            if (loadingBarView != null)
            {
                loadingBarView.Tip = tip != null ? tip : string.Empty;
                loadingBarView.Progress = 0f;
            }
        }

        /// <summary>
        /// 更新Loading弹窗
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="tip"></param>
        /// <param name="isLastSibling"></param>
        public override void UpdateLoadingBar(float progress, string tip = null, bool isLastSibling = true)
        {
            LoadingBarView loadingBarView = GetView<LoadingBarView>();
            if (loadingBarView == null)
            {
                string openTip = tip != null ? tip : "当前正在加载...";
                OpenLoadingBar(openTip, isLastSibling);
                loadingBarView = GetView<LoadingBarView>();
            }
            else
            {
                if (!loadingBarView.IsOpen)
                {
                    loadingBarView.OpenView();
                }
                loadingBarView.SetViewLastSibling(isLastSibling);
            }

            if (loadingBarView != null)
            {
                loadingBarView.Progress = progress;
                if (tip != null)
                {
                    loadingBarView.Tip = tip;
                }
            }
        }

        /// <summary>
        /// 关闭Loading弹窗
        /// </summary>
        public override void CloseLoadingBar()
        {
            CloseView<LoadingBarView>(false);
        }


    }
}

