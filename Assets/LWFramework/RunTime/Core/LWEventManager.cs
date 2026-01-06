using System;
using System.Collections.Generic;
using UnityEngine;
namespace LWCore
{
    public class LWEventManager : IManager, IEventManager
    {
        //key —— 事件的名字
        //value —— 对应的是 监听这个事件 对应的委托函数们
        private Dictionary<string, List<Delegate>> m_EventDic = new Dictionary<string, List<Delegate>>();
        private Dictionary<string, long> m_DispatchCountDic = new Dictionary<string, long>();

#if UNITY_EDITOR
        private readonly struct CallSiteInfo
        {
            public string Location { get; }
            public long Count { get; }

            public CallSiteInfo(string location, long count)
            {
                Location = location;
                Count = count;
            }
        }

        private Dictionary<string, List<CallSiteInfo>> m_AddCallSiteDic = new Dictionary<string, List<CallSiteInfo>>();
        private Dictionary<string, List<CallSiteInfo>> m_DispatchCallSiteDic = new Dictionary<string, List<CallSiteInfo>>();
#endif

        //派发事件过程中，回调里可能会继续 Add/Remove/Clear。
        //为了避免“遍历中修改集合”导致跳过/重复/越界等不确定行为，这里将改动延迟到派发结束后统一执行。
        private List<PendingOp> m_PendingOps = new List<PendingOp>(32);

        //当前处于派发中的层级深度（支持事件回调里嵌套触发事件）。
        //>0 表示正在派发，此时所有增删改操作进入 m_PendingOps；回到 0 时再应用。
        private int m_DispatchDepth;

        private enum PendingOpType
        {
            Add,
            Remove,
            ClearAll
        }

        private struct PendingOp
        {
            public PendingOpType m_Type;
            public string m_EventName;
            public Delegate m_Callback;
        }

        public void Init()
        {

        }

        void IManager.Update()
        {

        }

        /// <summary>
        /// 立即添加监听（仅在非派发阶段调用）。
        /// </summary>
        private void AddListenerImmediate(string eventName, Delegate callback)
        {
            if (m_EventDic.TryGetValue(eventName, out List<Delegate> eventList))
            {
                if (eventList.Count > 0)
                {
                    Type existingType = eventList[0]?.GetType();
                    Type addingType = callback.GetType();
                    if (existingType != null && existingType != addingType)
                    {
                        Debug.LogError($"[LWEventManager] AddListener 类型不匹配，eventName={eventName}, existing={existingType}, adding={addingType}");
                        return;
                    }
                }

                if (!eventList.Contains(callback))
                {
                    eventList.Add(callback);
                }
            }
            else
            {
                m_EventDic.Add(eventName, new List<Delegate>() { callback });
            }

            if (!m_DispatchCountDic.ContainsKey(eventName))
            {
                m_DispatchCountDic.Add(eventName, 0);
            }

#if UNITY_EDITOR
            RecordAddCallSite(eventName);
#endif
        }

        /// <summary>
        /// 立即移除监听（仅在非派发阶段调用）。
        /// </summary>
        private void RemoveListenerImmediate(string eventName, Delegate callback)
        {
            if (m_EventDic.TryGetValue(eventName, out List<Delegate> eventList))
            {
                eventList.Remove(callback);

                if (eventList.Count == 0)
                {
                    m_EventDic.Remove(eventName);
                    m_DispatchCountDic.Remove(eventName);
                }
            }
        }

        private void IncreaseDispatchCount(string eventName)
        {
            if (m_DispatchCountDic.TryGetValue(eventName, out long count))
            {
                m_DispatchCountDic[eventName] = count + 1;
                return;
            }

            m_DispatchCountDic[eventName] = 1;
        }

#if UNITY_EDITOR
        private static string CaptureCallSite()
        {
            var stackTrace = new System.Diagnostics.StackTrace(2, true);
            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                var frame = stackTrace.GetFrame(i);
                var method = frame?.GetMethod();
                var declaringType = method?.DeclaringType;
                if (declaringType == null)
                {
                    continue;
                }

                if (declaringType == typeof(LWEventManager))
                {
                    continue;
                }

                string typeName = declaringType.FullName;
                string methodName = method.Name;
                string fileName = frame.GetFileName();
                int line = frame.GetFileLineNumber();
                string assetPath = ToAssetPath(fileName);

                if (!string.IsNullOrEmpty(assetPath) && line > 0)
                {
                    return string.Concat(assetPath, ":", line.ToString(), "|", typeName, ".", methodName);
                }

                return string.Concat(typeName, ".", methodName);
            }

            return string.Empty;
        }

        private static string ToAssetPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return string.Empty;
            }

            filePath = filePath.Replace('\\', '/');
            int idx = filePath.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                if (filePath.EndsWith("/Assets", StringComparison.OrdinalIgnoreCase))
                {
                    return "Assets";
                }

                return string.Empty;
            }

            return filePath.Substring(idx + 1);
        }

        private void RecordCallSite(Dictionary<string, List<CallSiteInfo>> dic, string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            string location = CaptureCallSite();
            if (string.IsNullOrEmpty(location))
            {
                return;
            }

            if (!dic.TryGetValue(eventName, out List<CallSiteInfo> list))
            {
                list = new List<CallSiteInfo>();
                dic[eventName] = list;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Location == location)
                {
                    CallSiteInfo old = list[i];
                    list[i] = new CallSiteInfo(old.Location, old.Count + 1);
                    return;
                }
            }

            list.Add(new CallSiteInfo(location, 1));
        }

        private void RecordAddCallSite(string eventName)
        {
            RecordCallSite(m_AddCallSiteDic, eventName);
        }

        private void RecordDispatchCallSite(string eventName)
        {
            RecordCallSite(m_DispatchCallSiteDic, eventName);
        }
#endif

        /// <summary>
        /// 将派发阶段积累的增删改操作统一应用。
        /// 只有在 m_DispatchDepth 回到 0 时才会调用，确保遍历监听列表时不会被修改。
        /// </summary>
        private void ApplyPendingOps()
        {
            if (m_PendingOps.Count == 0)
            {
                return;
            }

            for (int i = 0; i < m_PendingOps.Count; i++)
            {
                PendingOp op = m_PendingOps[i];
                switch (op.m_Type)
                {
                    case PendingOpType.Add:
                        if (!string.IsNullOrEmpty(op.m_EventName) && op.m_Callback != null)
                        {
                            AddListenerImmediate(op.m_EventName, op.m_Callback);
                        }
                        break;
                    case PendingOpType.Remove:
                        if (!string.IsNullOrEmpty(op.m_EventName) && op.m_Callback != null)
                        {
                            RemoveListenerImmediate(op.m_EventName, op.m_Callback);
                        }
                        break;
                    case PendingOpType.ClearAll:
                        m_EventDic.Clear();
                        m_DispatchCountDic.Clear();
#if UNITY_EDITOR
                        m_AddCallSiteDic.Clear();
                        m_DispatchCallSiteDic.Clear();
#endif
                        break;
                }
            }

            m_PendingOps.Clear();
        }


        /// <summary>
        /// 监听事件
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="callback"></param>
        private void AddListenerInternal(string eventName, Delegate callback)
        {
            if (string.IsNullOrEmpty(eventName) || callback == null)
            {
                return;
            }

            if (m_DispatchDepth > 0)
            {
                //派发中不直接改动字典，先记录下来，等派发结束统一处理
                m_PendingOps.Add(new PendingOp()
                {
                    m_Type = PendingOpType.Add,
                    m_EventName = eventName,
                    m_Callback = callback
                });
                return;
            }

            AddListenerImmediate(eventName, callback);
        }
        /// <summary>
        /// 监听不需要参数传递的事件
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="callback"></param>
        public void AddListener(string eventName, Action callback)
        {
            AddListenerInternal(eventName, callback);
        }
        /// <summary>
        /// 添加事件监听（1个参数）
        /// </summary>
        /// <param name="name">事件的名字</param>
        /// <param name="action">准备用来处理事件 的委托函数</param>
        public void AddListener<T>(string eventName, Action<T> callback)
        {
            AddListenerInternal(eventName, callback);
        }
        /// <summary>
        /// 添加事件监听（2个参数）
        /// </summary>
        /// <param name="eventName">事件的名字</param>
        /// <param name="callback">准备用来处理事件 的委托函数</param>
        public void AddListener<T1, T2>(string eventName, Action<T1, T2> callback)
        {
            AddListenerInternal(eventName, callback);
        }
        /// <summary>
        /// 添加事件监听（3个参数）
        /// </summary>
        /// <param name="eventName">事件的名字</param>
        /// <param name="callback">准备用来处理事件 的委托函数</param>
        public void AddListener<T1, T2, T3>(string eventName, Action<T1, T2, T3> callback)
        {
            AddListenerInternal(eventName, callback);
        }
        /// <summary>
        /// 添加事件监听（4个参数）
        /// </summary>
        /// <param name="eventName">事件的名字</param>
        /// <param name="callback">准备用来处理事件 的委托函数</param>
        public void AddListener<T1, T2, T3, T4>(string eventName, Action<T1, T2, T3, T4> callback)
        {
            AddListenerInternal(eventName, callback);
        }
        /// <summary>
        /// 移除事件
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="callback"></param>
        private void RemoveListenerInternal(string eventName, Delegate callback)
        {
            if (string.IsNullOrEmpty(eventName) || callback == null)
            {
                return;
            }

            if (m_DispatchDepth > 0)
            {
                //派发中不直接改动字典，先记录下来，等派发结束统一处理
                m_PendingOps.Add(new PendingOp()
                {
                    m_Type = PendingOpType.Remove,
                    m_EventName = eventName,
                    m_Callback = callback
                });
                return;
            }

            RemoveListenerImmediate(eventName, callback);
        }

        /// <summary>
        /// 移除不需要参数的事件
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="callback"></param>
        public void RemoveListener(string eventName, Action callback)
        {
            RemoveListenerInternal(eventName, callback);
        }
        /// <summary>
        /// 移除对应的事件监听（1个参数）
        /// </summary>
        /// <param name="eventName">事件的名字</param>
        /// <param name="callback">对应之前添加的委托函数</param>
        public void RemoveListener<T>(string eventName, Action<T> callback)
        {
            RemoveListenerInternal(eventName, callback);
        }
        /// <summary>
        /// 移除对应的事件监听（2个参数）
        /// </summary>
        /// <param name="eventName">事件的名字</param>
        /// <param name="callback">对应之前添加的委托函数</param>
        public void RemoveListener<T1, T2>(string eventName, Action<T1, T2> callback)
        {
            RemoveListenerInternal(eventName, callback);
        }
        /// <summary>
        /// 移除对应的事件监听（3个参数）
        /// </summary>
        /// <param name="eventName">事件的名字</param>
        /// <param name="callback">对应之前添加的委托函数</param>
        public void RemoveListener<T1, T2, T3>(string eventName, Action<T1, T2, T3> callback)
        {
            RemoveListenerInternal(eventName, callback);
        }
        /// <summary>
        /// 移除对应的事件监听（4个参数）
        /// </summary>
        /// <param name="eventName">事件的名字</param>
        /// <param name="callback">对应之前添加的委托函数</param>
        public void RemoveListener<T1, T2, T3, T4>(string eventName, Action<T1, T2, T3, T4> callback)
        {
            RemoveListenerInternal(eventName, callback);
        }
        /// <summary>
        /// 事件触发（不需要参数的）
        /// </summary>
        /// <param name="eventName"></param>
        public void DispatchEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            if (!m_EventDic.TryGetValue(eventName, out List<Delegate> eventList))
            {
                return;
            }

            IncreaseDispatchCount(eventName);

#if UNITY_EDITOR
            RecordDispatchCallSite(eventName);
#endif

            m_DispatchDepth++;
            try
            {
                for (int i = 0; i < eventList.Count; i++)
                {
                    Action callback = eventList[i] as Action;
                    if (callback == null)
                    {
                        continue;
                    }

                    try
                    {
                        callback.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            finally
            {
                m_DispatchDepth--;
                if (m_DispatchDepth == 0)
                {
                    //所有嵌套派发都结束后，再统一应用派发过程中产生的增删改
                    ApplyPendingOps();
                }
            }
        }
        /// <summary>
        /// 事件触发（1个参数）
        /// </summary>
        /// <param name="eventName">哪一个名字的事件触发了</param>
        public void DispatchEvent<T>(string eventName, T info)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            if (!m_EventDic.TryGetValue(eventName, out List<Delegate> eventList))
            {
                return;
            }

            IncreaseDispatchCount(eventName);

#if UNITY_EDITOR
            RecordDispatchCallSite(eventName);
#endif

            m_DispatchDepth++;
            try
            {
                for (int i = 0; i < eventList.Count; i++)
                {
                    Action<T> callback = eventList[i] as Action<T>;
                    if (callback == null)
                    {
                        continue;
                    }

                    try
                    {
                        callback.Invoke(info);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            finally
            {
                m_DispatchDepth--;
                if (m_DispatchDepth == 0)
                {
                    ApplyPendingOps();
                }
            }
        }
        /// <summary>
        /// 事件触发（2个参数）
        /// </summary>
        /// <param name="eventName">哪一个名字的事件触发了</param>
        public void DispatchEvent<T1, T2>(string eventName, T1 info1, T2 info2)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            if (!m_EventDic.TryGetValue(eventName, out List<Delegate> eventList))
            {
                return;
            }

            IncreaseDispatchCount(eventName);

#if UNITY_EDITOR
            RecordDispatchCallSite(eventName);
#endif

            m_DispatchDepth++;
            try
            {
                for (int i = 0; i < eventList.Count; i++)
                {
                    Action<T1, T2> callback = eventList[i] as Action<T1, T2>;
                    if (callback == null)
                    {
                        continue;
                    }

                    try
                    {
                        callback.Invoke(info1, info2);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            finally
            {
                m_DispatchDepth--;
                if (m_DispatchDepth == 0)
                {
                    ApplyPendingOps();
                }
            }
        }
        /// <summary>
        /// 事件触发（3个参数）
        /// </summary>
        /// <param name="eventName">哪一个名字的事件触发了</param>
        public void DispatchEvent<T1, T2, T3>(string eventName, T1 info1, T2 info2, T3 info3)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            if (!m_EventDic.TryGetValue(eventName, out List<Delegate> eventList))
            {
                return;
            }

            IncreaseDispatchCount(eventName);

#if UNITY_EDITOR
            RecordDispatchCallSite(eventName);
#endif

            m_DispatchDepth++;
            try
            {
                for (int i = 0; i < eventList.Count; i++)
                {
                    Action<T1, T2, T3> callback = eventList[i] as Action<T1, T2, T3>;
                    if (callback == null)
                    {
                        continue;
                    }

                    try
                    {
                        callback.Invoke(info1, info2, info3);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            finally
            {
                m_DispatchDepth--;
                if (m_DispatchDepth == 0)
                {
                    ApplyPendingOps();
                }
            }
        }
        /// <summary>
        /// 事件触发（4个参数）
        /// </summary>
        /// <param name="eventName">哪一个名字的事件触发了</param>
        public void DispatchEvent<T1, T2, T3, T4>(string eventName, T1 info1, T2 info2, T3 info3, T4 info4)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            if (!m_EventDic.TryGetValue(eventName, out List<Delegate> eventList))
            {
                return;
            }

            IncreaseDispatchCount(eventName);

#if UNITY_EDITOR
            RecordDispatchCallSite(eventName);
#endif

            m_DispatchDepth++;
            try
            {
                for (int i = 0; i < eventList.Count; i++)
                {
                    Action<T1, T2, T3, T4> callback = eventList[i] as Action<T1, T2, T3, T4>;
                    if (callback == null)
                    {
                        continue;
                    }

                    try
                    {
                        callback.Invoke(info1, info2, info3, info4);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            finally
            {
                m_DispatchDepth--;
                if (m_DispatchDepth == 0)
                {
                    ApplyPendingOps();
                }
            }
        }
        /// <summary>
        /// 清空事件中心
        /// </summary>
        public void Clear()
        {
            if (m_DispatchDepth > 0)
            {
                //派发中不直接清空，先记录下来，等派发结束统一处理
                m_PendingOps.Add(new PendingOp()
                {
                    m_Type = PendingOpType.ClearAll,
                    m_EventName = null,
                    m_Callback = null
                });
                return;
            }

            m_EventDic.Clear();
            m_DispatchCountDic.Clear();

#if UNITY_EDITOR
            m_AddCallSiteDic.Clear();
            m_DispatchCallSiteDic.Clear();
#endif
        }

        public readonly struct EventRuntimeInfo
        {
            public string EventName { get; }
            public int ListenerCount { get; }
            public long DispatchCount { get; }
            public string DelegateTypeName { get; }
            public string LastAddCallSite { get; }
            public string LastDispatchCallSite { get; }

            public EventRuntimeInfo(string eventName, int listenerCount, long dispatchCount, string delegateTypeName, string lastAddCallSite, string lastDispatchCallSite)
            {
                EventName = eventName;
                ListenerCount = listenerCount;
                DispatchCount = dispatchCount;
                DelegateTypeName = delegateTypeName;
                LastAddCallSite = lastAddCallSite;
                LastDispatchCallSite = lastDispatchCallSite;
            }
        }

        public void GetRuntimeInfos(List<EventRuntimeInfo> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();

            foreach (var kv in m_EventDic)
            {
                string eventName = kv.Key;
                List<Delegate> list = kv.Value;
                int listenerCount = list?.Count ?? 0;
                string delegateTypeName = (list != null && list.Count > 0 && list[0] != null) ? list[0].GetType().Name : string.Empty;
                long dispatchCount = 0;
                m_DispatchCountDic.TryGetValue(eventName, out dispatchCount);

                string lastAdd = string.Empty;
                string lastDispatch = string.Empty;
#if UNITY_EDITOR
                if (m_AddCallSiteDic.TryGetValue(eventName, out List<CallSiteInfo> addList) && addList.Count > 0)
                {
                    lastAdd = addList[addList.Count - 1].Location;
                }
                if (m_DispatchCallSiteDic.TryGetValue(eventName, out List<CallSiteInfo> dispatchList) && dispatchList.Count > 0)
                {
                    lastDispatch = dispatchList[dispatchList.Count - 1].Location;
                }
#endif

                results.Add(new EventRuntimeInfo(eventName, listenerCount, dispatchCount, delegateTypeName, lastAdd, lastDispatch));
            }
        }

        public void ClearDispatchCounts()
        {
            m_DispatchCountDic.Clear();
        }

#if UNITY_EDITOR
        public readonly struct EventCallSiteStat
        {
            public string Location { get; }
            public long Count { get; }

            public EventCallSiteStat(string location, long count)
            {
                Location = location;
                Count = count;
            }
        }

        public void GetCallSiteStats(string eventName, List<EventCallSiteStat> addResults, List<EventCallSiteStat> dispatchResults)
        {
            if (addResults != null)
            {
                addResults.Clear();
                if (!string.IsNullOrEmpty(eventName) && m_AddCallSiteDic.TryGetValue(eventName, out List<CallSiteInfo> addList))
                {
                    for (int i = 0; i < addList.Count; i++)
                    {
                        addResults.Add(new EventCallSiteStat(addList[i].Location, addList[i].Count));
                    }
                }
            }

            if (dispatchResults != null)
            {
                dispatchResults.Clear();
                if (!string.IsNullOrEmpty(eventName) && m_DispatchCallSiteDic.TryGetValue(eventName, out List<CallSiteInfo> dispatchList))
                {
                    for (int i = 0; i < dispatchList.Count; i++)
                    {
                        dispatchResults.Add(new EventCallSiteStat(dispatchList[i].Location, dispatchList[i].Count));
                    }
                }
            }
        }
#endif
    }

}
