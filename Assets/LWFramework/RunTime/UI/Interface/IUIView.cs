using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LWUI
{
    public interface IUIView
    {
        GameObject Entity { get; }
        ShowHideType ShowHideType { get; set; }
        /// <summary>
        /// View的数据
        /// </summary>
        //ViewData ViewData { get; set; }
        void CreateView(GameObject gameObject);
        /// <summary>
        /// 打开view
        /// </summary>
        /// <param name="isFirstSibling">是否置于最前  默认false</param>
        void OpenView();
        //void OnCreateView();
        /// <summary>
        /// 设置页面的层级
        /// </summary>
        /// <param name="isLastSibling">是否置于最前  默认false</param>
        void SetViewLastSibling(bool isLastSibling = false);
        /// <summary>
        /// 关闭View
        /// </summary>
        void CloseView();
        /// <summary>
        /// 判断当前是否打开
        /// </summary>
        /// <returns></returns>
        bool IsOpen { get; set; }
        /// <summary>
        /// 更新View
        /// </summary>
        void UpdateView();
        /// <summary>
        /// 清空View
        /// </summary>
        void ClearView();
        /// <summary>
        /// 重置View
        /// </summary>
        void ResetView();
        /// <summary>
        /// 处理本地化
        /// </summary>
        void Localization();
    }
}