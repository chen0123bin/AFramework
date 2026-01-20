using System;
using System.Collections.Generic;
using LitJson;
using LWCore;
using UnityEngine;

namespace LWStep
{
    /// <summary>
    /// 步骤运行时上下文（用于保存过程结果与共享数据）
    /// </summary>
    public class StepContext
    {
        private Dictionary<string, object> m_Data;
        private Dictionary<string, Type> m_Types;

        public struct StepContextKey<T>
        {
            public string Name { get; private set; }

            public StepContextKey(string name)
            {
                Name = name;
            }
        }

        /// <summary>
        /// 创建上下文
        /// </summary>
        public StepContext()
        {
            m_Data = new Dictionary<string, object>();
            m_Types = new Dictionary<string, Type>();
        }

        /// <summary>
        /// 设置上下文数据
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            if (m_Data.ContainsKey(key))
            {
                m_Data[key] = value;
                m_Types[key] = typeof(T);
                return;
            }
            m_Data.Add(key, value);
            m_Types.Add(key, typeof(T));
        }

        public void SetValue<T>(StepContextKey<T> key, T value)
        {
            SetValue(key.Name, value);
        }

        /// <summary>
        /// 获取上下文数据
        /// </summary>
        public T GetValue<T>(string key, T defaultValue = default(T))
        {
            object value;
            if (m_Data.TryGetValue(key, out value))
            {
                if (value is T)
                {
                    return (T)value;
                }
            }
            return defaultValue;
        }

        public T GetValue<T>(StepContextKey<T> key, T defaultValue = default(T))
        {
            return GetValue(key.Name, defaultValue);
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            object rawValue;
            if (m_Data.TryGetValue(key, out rawValue))
            {
                if (rawValue is T)
                {
                    value = (T)rawValue;
                    return true;
                }
            }
            value = default(T);
            return false;
        }

        public bool TryGetValue<T>(StepContextKey<T> key, out T value)
        {
            return TryGetValue(key.Name, out value);
        }

        /// <summary>
        /// 是否包含指定Key
        /// </summary>
        public bool HasKey(string key)
        {
            return m_Data.ContainsKey(key);
        }

        public bool HasKey<T>(StepContextKey<T> key)
        {
            return HasKey(key.Name);
        }

        public bool RemoveKey(string key)
        {
            bool removed = m_Data.Remove(key);
            m_Types.Remove(key);
            return removed;
        }

        public bool RemoveKey<T>(StepContextKey<T> key)
        {
            return RemoveKey(key.Name);
        }

        public bool TryGetRawValue(string key, out object value)
        {
            return m_Data.TryGetValue(key, out value);
        }

        /// <summary>
        /// 清空上下文数据
        /// </summary>
        public void Clear()
        {
            m_Data.Clear();
            m_Types.Clear();
        }

        /// <summary>
        /// 克隆上下文（浅拷贝）
        /// </summary>
        public Dictionary<string, object> CloneData()
        {
            return new Dictionary<string, object>(m_Data);
        }

        public StepContextSnapshot CreateSnapshot()
        {
            return new StepContextSnapshot(CloneData());
        }

        public string ToJson()
        {
            StepContextPersistData data = new StepContextPersistData();
            data.Entries = new List<StepContextPersistEntry>();
            foreach (KeyValuePair<string, object> kvp in m_Data)
            {
                string key = kvp.Key;
                object value = kvp.Value;
                Type valueType;
                if (!m_Types.TryGetValue(key, out valueType) || valueType == null)
                {
                    if (value != null)
                    {
                        valueType = value.GetType();
                    }
                }

                if (valueType == null)
                {
                    LWDebug.LogWarning("步骤上下文序列化失败：类型为空，Key=" + key);
                    continue;
                }

                if (!IsSerializableType(valueType))
                {
                    LWDebug.LogWarning("步骤上下文序列化失败：类型不支持，Key=" + key + "，类型=" + valueType.FullName);
                    continue;
                }

                string valueJson;
                try
                {
                    valueJson = JsonMapper.ToJson(value);
                }
                catch (Exception e)
                {
                    LWDebug.LogWarning("步骤上下文序列化失败：" + key + "，原因=" + e.Message);
                    continue;
                }

                StepContextPersistEntry entry = new StepContextPersistEntry();
                entry.Key = key;
                entry.TypeName = valueType.AssemblyQualifiedName;
                entry.ValueJson = valueJson;
                data.Entries.Add(entry);
            }
            return JsonMapper.ToJson(data);
        }

        public void LoadFromJson(string json)
        {
            Clear();
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            StepContextPersistData data;
            try
            {
                data = JsonMapper.ToObject<StepContextPersistData>(json);
            }
            catch (Exception e)
            {
                LWDebug.LogWarning("步骤上下文反序列化失败：" + e.Message);
                return;
            }

            if (data == null || data.Entries == null)
            {
                return;
            }

            for (int i = 0; i < data.Entries.Count; i++)
            {
                StepContextPersistEntry entry = data.Entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.Key) || string.IsNullOrEmpty(entry.TypeName))
                {
                    continue;
                }

                Type valueType = Type.GetType(entry.TypeName);
                if (valueType == null)
                {
                    LWDebug.LogWarning("步骤上下文反序列化失败：类型不存在，Key=" + entry.Key + "，类型=" + entry.TypeName);
                    continue;
                }

                if (!IsSerializableType(valueType))
                {
                    LWDebug.LogWarning("步骤上下文反序列化失败：类型不支持，Key=" + entry.Key + "，类型=" + valueType.FullName);
                    continue;
                }

                object value;
                try
                {
                    value = JsonMapper.ToObject(entry.ValueJson, valueType);
                }
                catch (Exception e)
                {
                    LWDebug.LogWarning("步骤上下文反序列化失败：" + entry.Key + "，原因=" + e.Message);
                    continue;
                }

                m_Data[entry.Key] = value;
                m_Types[entry.Key] = valueType;
            }
        }

        private bool IsSerializableType(Type valueType)
        {
            if (valueType == null)
            {
                return false;
            }

            Type unityObjectType = typeof(UnityEngine.Object);
            if (unityObjectType.IsAssignableFrom(valueType))
            {
                return false;
            }
            return true;
        }

        private class StepContextPersistData
        {
            public List<StepContextPersistEntry> Entries { get; set; }
        }

        private class StepContextPersistEntry
        {
            public string Key { get; set; }
            public string TypeName { get; set; }
            public string ValueJson { get; set; }
        }
    }
}
