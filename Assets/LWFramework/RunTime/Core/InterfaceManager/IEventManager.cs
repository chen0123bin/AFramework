using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LWCore
{
    public interface IEventManager
    {
        /// <summary>
        /// 监听不需要参数传递的事件
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="callback"></param>
        public void AddListener(string eventName, Action callback);
        /// <summary>
        /// 添加事件监听（1个参数）
        /// </summary>
        /// <param name="name">事件的名字</param>
        /// <param name="action">准备用来处理事件 的委托函数</param>
        public void AddListener<T>(string eventName, Action<T> callback);
        /// <summary>
        /// 添加事件监听（2个参数）
        /// </summary>
        /// <param name="eventName">事件的名字</param>
        /// <param name="callback">准备用来处理事件 的委托函数</param>
        public void AddListener<T1, T2>(string eventName, Action<T1, T2> callback);
        /// <summary>
        /// 添加事件监听（3个参数）
        /// </summary>
        /// <param name="eventName">事件的名字</param>
        /// <param name="callback">准备用来处理事件 的委托函数</param>
        public void AddListener<T1, T2, T3>(string eventName, Action<T1, T2, T3> callback);
        /// <summary>
        /// 添加事件监听（4个参数）
        /// </summary>
        /// <param name="eventName">事件的名字</param>
        /// <param name="callback">准备用来处理事件 的委托函数</param>
        public void AddListener<T1, T2, T3, T4>(string eventName, Action<T1, T2, T3, T4> callback);


        /// <summary>
        /// 移除对应的事件监听（1个参数）
        /// </summary>
        /// <param name="eventName">事件的名字</param>
        /// <param name="callback">对应之前添加的委托函数</param>
        public void RemoveListener(string eventName, Action callback);
        /// <summary>
        /// 移除对应的事件监听（1个参数）
        /// </summary>
        /// <param name="eventName">事件的名字</param>
        /// <param name="callback">对应之前添加的委托函数</param>
        public void RemoveListener<T>(string eventName, Action<T> callback);
        /// <summary>
        /// 移除对应的事件监听（2个参数）
        /// </summary>
        /// <param name="eventName">事件的名字</param>
        /// <param name="callback">对应之前添加的委托函数</param>
        public void RemoveListener<T1, T2>(string eventName, Action<T1, T2> callback);
        /// <summary>
        /// 移除对应的事件监听（3个参数）
        /// </summary>
        /// <param name="eventName">事件的名字</param>
        /// <param name="callback">对应之前添加的委托函数</param>
        public void RemoveListener<T1, T2, T3>(string eventName, Action<T1, T2, T3> callback);
        /// <summary>
        /// 移除对应的事件监听（4个参数）
        /// </summary>
        /// <param name="eventName">事件的名字</param>
        /// <param name="callback">对应之前添加的委托函数</param>
        public void RemoveListener<T1, T2, T3, T4>(string eventName, Action<T1, T2, T3, T4> callback);
        /// <summary>
        /// 事件触发（不需要参数的）
        /// </summary>
        /// <param name="eventName"></param>
        public void DispatchEvent(string eventName);
        /// <summary>
        /// 事件触发（1个参数）
        /// </summary>
        /// <param name="eventName">哪一个名字的事件触发了</param>
        public void DispatchEvent<T>(string eventName, T info);
        /// <summary>
        /// 事件触发（2个参数）
        /// </summary>
        /// <param name="eventName">哪一个名字的事件触发了</param>
        public void DispatchEvent<T1, T2>(string eventName, T1 info1, T2 info2);
        /// <summary>
        /// 事件触发（3个参数）
        /// </summary>
        /// <param name="eventName">哪一个名字的事件触发了</param>
        public void DispatchEvent<T1, T2, T3>(string eventName, T1 info1, T2 info2, T3 info3);
        /// <summary>
        /// 事件触发（4个参数）
        /// </summary>
        /// <param name="eventName">哪一个名字的事件触发了</param>
        public void DispatchEvent<T1, T2, T3, T4>(string eventName, T1 info1, T2 info2, T3 info3, T4 info4);
        /// <summary>
        /// 清空事件中心
        /// </summary>
        public void Clear();
    }

}
