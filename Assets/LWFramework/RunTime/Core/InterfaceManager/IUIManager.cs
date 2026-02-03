using System;
using Cysharp.Threading.Tasks;
using LWUI;
using UnityEngine;

namespace LWCore
{
    public interface IUIManager
    {

        UIUtility UIUtility { get; }

        /// <summary>
        /// UI画布
        /// </summary>
        Canvas UICanvas { get; set; }
        /// <summary>
        /// UI相机
        /// </summary>
        Camera UICamera { get; }

        /// <summary>
        /// 获取View
        /// </summary>
        /// <typeparam name="T">View的类型（转换成使用typeOf转换）</typeparam>
        /// <returns></returns>
        T GetView<T>() where T : BaseUIView;

        /// <summary>
        /// 获取View
        /// </summary>
        /// <param name="viewType">View的名称</param>
        /// <returns></returns>
        BaseUIView GetView(string viewType);

        /// <summary>
        /// 获取所有的View
        /// </summary>
        /// <returns></returns>
        BaseUIView[] GetAllView();

        /// <summary>
        /// 打开View
        /// </summary>
        /// <typeparam name="T">View的类型（转换成使用typeOf转换）</typeparam>
        /// <param name="isLastSibling">是否进入最上层</param>
        /// <param name="enterStack">是否放进栈种，进栈的VIEW才能直接返回</param>
        /// <param name="data">传递的数据</param>
        T OpenView<T>(object data = null, bool isLastSibling = false, bool enterStack = false) where T : BaseUIView;

        /// <summary>
        /// 打开View
        /// </summary>
        /// <param name="viewType">viewType的名称</param>
        /// <param name="uiGameObject">View的实体对象</param>
        /// <param name="isLastSibling">是否进入最上层</param>
        /// <param name="enterStack">是否放进栈种，进栈的VIEW才能直接返回</param>
        /// <param name="data">传递的数据</param>
        BaseUIView OpenView(string viewType, object data = null, GameObject uiGameObject = null, bool isLastSibling = false, bool enterStack = false);

        /// <summary>
        /// 使用异步的方式打开UI
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="isLastSibling">是否进入最上层</param>
        /// <param name="enterStack">是否放进栈种，进栈的VIEW才能直接返回</param>
        /// <param name="data">传递的数据</param>
        UniTask<T> OpenViewAsync<T>(object data = null, bool isLastSibling = false, bool enterStack = false) where T : BaseUIView;
        /// <summary>
        /// 返回上一个页面
        /// </summary>
        /// <param name="isLastSibling">是否进入最上层</param>
        /// <returns></returns>
        BaseUIView BackView(bool isLastSibling = true);
        /// <summary>
        /// 返回两个页面
        /// </summary>
        /// <param name="isLastSibling">是否进入最上层</param>
        /// <returns></returns>
        BaseUIView BackTwiceView(bool isLastSibling = true);
        /// <summary>
        /// 从上直下全部返回，return最底部的那个View
        /// </summary>
        /// <param name="isLastSibling">是否进入最上层</param>
        /// <returns></returns>
        BaseUIView BackUntilLastView(bool isLastSibling = true);
        /// <summary>
        /// 使用异步的方式预加载UI资源，后面可以直接用Open打开
        /// </summary>
        /// <param name="loadPath">资源的加载路径</param>
        UniTask PreloadViewAsync(string loadPath);
        /// <summary>
        /// 使用异步的方式预加载UI资源，后面可以直接用Open打开
        /// </summary>
        /// <typeparam name="T">View的类型</typeparam>
        UniTask PreloadViewAsync<T>();
        /// <summary>
        /// 关闭其他的View
        /// </summary>
        /// <typeparam name="T">保留的View类型（转换成使用typeOf转换）</typeparam>
        void CloseOtherView<T>();
        /// <summary>
        /// 关闭其他的View
        /// </summary>
        /// <param name="viewTypeArray">保留的多个viewType</param>
        void CloseOtherView(params string[] viewTypeArray);

        /// <summary>
        /// 关闭View
        /// </summary>
        /// <param name="view">View 对象</param>
        void CloseView(BaseUIView view, bool enterStack = false);
        /// <summary>
        /// 关闭View
        /// </summary>
        /// <param name="viewType">View的名称</param>
        void CloseView(string viewType, bool enterStack = false);
        /// <summary>
        /// 关闭指定的View
        /// </summary>
        /// <typeparam name="T">View的类型（转换成使用typeOf转换）</typeparam>
        void CloseView<T>(bool enterStack = false);

        /// <summary>
        /// 关闭所有的View
        /// </summary>
        void CloseAllView();

        /// <summary>
        /// 清空除数组以外的View
        /// </summary>
        /// <param name="viewTypeArray"></param>
        void ClearOtherView(params string[] viewTypeArray);
        /// <summary>
        /// 清空除泛型以外的View
        /// </summary>
        void ClearOtherView<T>();
        /// <summary>
        /// 清空View
        /// </summary>
        /// <param name="viewType">View的类型</param>
        void ClearView(string viewType);
        /// <summary>
        /// 清空View
        /// </summary>
        /// <typeparam name="T">View的类型（转换成使用typeOf转换）</typeparam>
        void ClearView<T>();
        /// <summary>
        /// 清空所有的View
        /// </summary>
        void ClearAllView();
        /// <summary>
        /// 设置UI风格用于替换不同的预制体路径
        /// </summary>
        /// <param name="styleName"></param>
        void SetStyle(string styleName);
        /// <summary>
        /// 获取样式
        /// </summary>
        /// <returns></returns>
        string GetStyle();
        /// <summary>
        /// 预加载内置的UI
        /// </summary>
        UniTask PreLoadDefaultUI();
        /// <summary>
        /// 打开弹窗
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="isShowCancel"></param>
        /// <param name="isShowClose"></param>
        void OpenDialog(string title, string content, Action<bool> ResultCallback, bool isShowCancel = true, bool isShowClose = true, bool isLastSibling = true);

        /// <summary>
        /// 打开弹窗并等待用户选择（true=确认，false=取消/关闭）
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="isShowCancel"></param>
        /// <param name="isShowClose"></param>
        /// <param name="isLastSibling"></param>
        UniTask<bool> OpenDialogAsync(string title, string content, bool isShowCancel = true, bool isShowClose = true, bool isLastSibling = true);
        /// <summary>
        /// 打开Loading弹窗
        /// </summary>
        /// <param name="tip"></param>
        /// <param name="isLastSibling"></param>
        void OpenLoadingBar(string tip = "当前正在加载...", bool isLastSibling = true);

        /// <summary>
        /// 更新Loading弹窗
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="tip"></param>
        /// <param name="isLastSibling"></param>
        void UpdateLoadingBar(float progress, string tip = null, bool isLastSibling = true);

        /// <summary>
        /// 关闭Loading弹窗
        /// </summary>
        void CloseLoadingBar();
    }
}
