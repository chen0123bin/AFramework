using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LWUI
{
    public interface IUIUtility
    {
        BaseUIView CreateView(string viewType, GameObject uiGameObject = null, string loadPathParam = null);
        /// <summary>
        /// IL模式下不可用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uiGameObject"></param>
        /// <param name="loadPathParam"></param>
        /// <returns></returns>
        BaseUIView CreateView<T>(GameObject uiGameObject = null, string loadPathParam = null);
        /// <summary>
        /// IL模式下不可用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uiGameObject"></param>
        /// <param name="loadPath"></param>
        /// <returns></returns>
        UniTask<BaseUIView> CreateViewAsync<T>(GameObject uiGameObject = null, string loadPath = null);
        /// <summary>
        /// 异步预加载一个对象
        /// </summary>
        /// <param name="loadPath">资源路径</param>
        /// <returns></returns>
        UniTask PreloadViewAsync(string loadPath);
        /// <summary>
        /// 异步预加载一个对象
        /// </summary>
        /// <typeparam name="T">View</typeparam>
        /// <returns></returns>
        UniTask PreloadViewAsync<T>();
        Transform GetParent(FindType findType, string param);

        void SetViewElement(object entity, Type type, GameObject uiGameObject);
        void SetViewElement(object entity, Type type, BaseUIView uiView);
    }
}