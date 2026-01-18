using Cysharp.Threading.Tasks;
using LWCore;
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
    public abstract class BaseUIManager : IManager, IUIManager
    {

        /// <summary>
        /// 所有的view字典
        /// </summary>
        protected Dictionary<string, BaseUIView> m_UIViewDic;
        /// <summary>
        /// 所有的view集合
        /// </summary>
        protected List<BaseUIView> m_UIViewList;
        /// <summary>
        /// 所有的view Stack集合
        /// </summary>
        protected Stack<BaseUIView> m_UIViewStack;
        /// <summary>
        /// 所有的绑定数据
        /// </summary>
        protected Dictionary<string, string> m_UIBindViewPath;



        protected UIUtility m_UIUtility;
        public UIUtility UIUtility
        {
            get => m_UIUtility;
        }

        public abstract Canvas UICanvas { get; set; }
        public abstract Camera UICamera { get; }
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
        public virtual void Init()
        {
            m_UIViewDic = new Dictionary<string, BaseUIView>();
            m_UIViewList = new List<BaseUIView>();
            m_UIViewStack = new Stack<BaseUIView>();
            m_UIBindViewPath = new Dictionary<string, string>();


        }
        /// <summary>
        /// 更新所有的View
        /// </summary>
        public void Update()
        {
            for (int i = 0; i < m_UIViewList.Count; i++)
            {
                if (m_UIViewList[i].IsOpen)
                    m_UIViewList[i].UpdateView();
            }
        }

        /// <summary>
        /// 打开View
        /// </summary>
        /// <typeparam name="T">view的控制类</typeparam>
        /// <param name="isLastSibling">是否放置在最前面</param>
        /// <param name="enterStack">是否放进栈种，进栈的VIEW才能直接返回</param>
        public abstract T OpenView<T>(bool isLastSibling = false, bool enterStack = false) where T : BaseUIView;
        /// <summary>
        /// 打开View
        /// </summary>
        /// <typeparam name="T">view的控制类</typeparam>
        /// <param name="viewType">view的名字，用于一个多个页面共用一个类</param>
        /// <param name="uiGameObject">view的对象，提前创建，优先级高于自己创建</param>
        /// <param name="isLastSibling">是否放置在最前面</param>
        /// <param name="enterStack">是否放进栈种，进栈的VIEW才能直接返回</param>
        public abstract BaseUIView OpenView(string viewType, GameObject uiGameObject = null, bool isLastSibling = false, bool enterStack = false);
        /// <summary>
        ///异步打开View
        /// </summary>
        /// <typeparam name="T">view的控制类</typeparam>
        /// <param name="isLastSibling">是否放置在最前面</param>
        /// <param name="enterStack">是否放进栈种，进栈的VIEW才能直接返回</param>
        public abstract UniTask<T> OpenViewAsync<T>(bool isLastSibling = false, bool enterStack = false) where T : BaseUIView;

        /// <summary>
        /// 打开弹窗
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="isShowCancel"></param>
        /// <param name="isShowClose"></param>
        public abstract void OpenDialog(string title, string content, Action<bool> ResultCallback, bool isShowCancel = true, bool isShowClose = true, bool isLastSibling = true);

        /// <summary>
        /// 打开弹窗并等待用户选择（true=确认，false=取消/关闭）
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="isShowCancel"></param>
        /// <param name="isShowClose"></param>
        /// <param name="isLastSibling"></param>
        /// <returns></returns>
        public abstract UniTask<bool> OpenDialogAsync(string title, string content, bool isShowCancel = true, bool isShowClose = true, bool isLastSibling = true);

        /// <summary>
        /// 打开Loading弹窗
        /// </summary>
        /// <param name="tip"></param>
        /// <param name="isLastSibling"></param>
        public abstract void OpenLoadingBar(string tip = "当前正在加载...", bool isLastSibling = true);

        /// <summary>
        /// 更新Loading弹窗
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="tip"></param>
        /// <param name="isLastSibling"></param>
        public abstract void UpdateLoadingBar(float progress, string tip = null, bool isLastSibling = true);

        /// <summary>
        /// 关闭Loading弹窗
        /// </summary>
        public abstract void CloseLoadingBar();

        public BaseUIView BackView(bool isLastSibling = true)
        {
            BaseUIView uiViewBase = null;
            if (m_UIViewStack.Count > 0)
            {
                uiViewBase = m_UIViewStack.Pop();
                if (uiViewBase.IsOpen)
                {
                    uiViewBase.CloseView();
                }
                else
                {
                    uiViewBase.OpenView();
                }

                uiViewBase.SetViewLastSibling(isLastSibling);

            }
            else
            {
                Debug.LogWarning("栈内没有View了");
            }
            return uiViewBase;
        }
        public BaseUIView BackTwiceView(bool isLastSibling = true)
        {
            BaseUIView uiViewBase = null;
            if (m_UIViewStack.Count > 1)
            {
                uiViewBase = m_UIViewStack.Pop();
                if (uiViewBase.IsOpen)
                {
                    uiViewBase.CloseView();
                }
                else
                {
                    uiViewBase.OpenView();
                }

                uiViewBase = m_UIViewStack.Pop();
                if (uiViewBase.IsOpen)
                {
                    uiViewBase.CloseView();
                }
                else
                {
                    uiViewBase.OpenView();
                }
                uiViewBase.SetViewLastSibling(isLastSibling);

            }
            else
            {
                Debug.LogWarning("栈内View不足2两个");
            }
            return uiViewBase;
        }
        public BaseUIView BackUntilLastView(bool isLastSibling = true)
        {
            BaseUIView uiViewBase = null;
            while (m_UIViewStack.Count > 0)
            {
                uiViewBase = m_UIViewStack.Pop();
                if (uiViewBase.IsOpen)
                {
                    uiViewBase.CloseView();
                }
                else
                {
                    uiViewBase.OpenView();
                }

                uiViewBase.SetViewLastSibling(isLastSibling);

            }

            return uiViewBase;
        }
        /// <summary>
        /// 获取VIEW
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetView<T>() where T : BaseUIView
        {
            BaseUIView ret = null;
            string viewName = typeof(T).ToString();
            m_UIViewDic.TryGetValue(viewName, out ret);
            return (T)ret;
        }

        /// <summary>
        /// 获取VIEW
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public BaseUIView GetView(string viewType = null)
        {
            BaseUIView ret = null;
            m_UIViewDic.TryGetValue(viewType, out ret);
            return ret;
        }
        /// <summary>
        /// 获取所有的VIEW
        /// </summary>
        /// <returns></returns>
        public BaseUIView[] GetAllView()
        {
            return m_UIViewList.ToArray<BaseUIView>();
        }


        /// <summary>
        /// 关闭View
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void CloseView<T>(bool enterStack = false)
        {
            CloseView(typeof(T).ToString(), enterStack);
        }
        /// <summary>
        /// 关闭View
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void CloseView(string viewName, bool enterStack = false)
        {
            BaseUIView uiViewBase;
            if (m_UIViewDic.TryGetValue(viewName, out uiViewBase))
            {
                if (uiViewBase.IsOpen)
                    CloseView(uiViewBase);

                if (enterStack)
                {
                    m_UIViewStack.Push(uiViewBase);
                }
            }
        }
        /// <summary>
        /// 关闭View
        /// </summary>
        /// <param name="view">View 对象</param>
        public void CloseView(BaseUIView view, bool enterStack = false)
        {
            view.CloseView();
            if (enterStack)
            {
                m_UIViewStack.Push(view);
            }
        }
        /// <summary>
        /// 关闭其他所有的View
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void CloseOtherView<T>()
        {
            CloseOtherView(typeof(T).ToString());
        }

        public void CloseOtherView(string viewName)
        {
            foreach (var item in m_UIViewDic.Keys)
            {
                if (item != viewName)
                {
                    CloseView(m_UIViewDic[item]);
                }
            }
        }
        public void CloseOtherView(string[] viewNameArray)
        {
            foreach (var item in m_UIViewDic.Keys)
            {
                bool canClose = true;
                for (int i = 0; i < viewNameArray.Length; i++)
                {
                    if (item == viewNameArray[i])
                    {
                        canClose = false;
                        break;
                        // m_UIViewDic[item].CloseView();
                    }
                }
                if (canClose)
                    CloseView(m_UIViewDic[item]);
            }
        }

        /// <summary>
        /// 关闭所有的view
        /// </summary>
        public void CloseAllView()
        {
            foreach (var item in m_UIViewDic.Values)
            {
                CloseView(item);
            }
        }
        public void ClearView(string viewName)
        {
            BaseUIView uiViewBase;
            if (m_UIViewDic.TryGetValue(viewName, out uiViewBase))
            {
                uiViewBase.CloseView();
                uiViewBase.ClearView();
                m_UIViewDic.Remove(viewName);
                if (m_UIViewList.Contains(uiViewBase))
                {
                    m_UIViewList.Remove(uiViewBase);
                }
            }
        }
        public void ClearView<T>()
        {
            ClearView(typeof(T).ToString());
        }

        public void ClearOtherView(string[] viewNameArray)
        {
            List<string> clearList = new List<string>();
            foreach (var item in m_UIViewDic.Keys)
            {
                bool canClear = true;
                for (int i = 0; i < viewNameArray.Length; i++)
                {
                    if (item == viewNameArray[i])
                    {
                        canClear = false;
                        break;
                    }
                }
                if (canClear)
                {
                    clearList.Add(item);
                }
            }
            for (int i = 0; i < clearList.Count; i++)
            {
                ClearView(clearList[i]);
            }
        }
        public void ClearOtherView<T>()
        {
            ClearOtherView(new string[] { typeof(T).ToString() });
        }


        /// <summary>
        /// 清理所有的view
        /// </summary>
        public void ClearAllView()
        {
            foreach (var item in m_UIViewDic.Values)
            {
                item.CloseView();
                item.ClearView();
            }
            m_UIViewDic.Clear();
            m_UIViewList.Clear();
        }
        public async UniTask PreloadViewAsync(string loadPath)
        {
            await ManagerUtility.UIMgr.UIUtility.PreloadViewAsync(loadPath);
        }
        public async UniTask PreloadViewAsync<T>()
        {
            await ManagerUtility.UIMgr.UIUtility.PreloadViewAsync<T>();
        }

        public abstract UniTask PreLoadDefaultUI();
        public void SetStyle(string styleName)
        {
            PlayerPrefs.SetString("UIStyle", styleName);
        }
        public string GetStyle()
        {
            return PlayerPrefs.GetString("UIStyle", "Default");
        }
    }
}

