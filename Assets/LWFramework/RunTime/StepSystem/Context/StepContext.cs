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
        private List<StepContextPersistEntry> m_Entries { get; set; }
        private Dictionary<string, object> m_Data;

        /// <summary>
        /// 创建上下文
        /// </summary>
        public StepContext()
        {
            m_Data = new Dictionary<string, object>();
        }

        /// <summary>
        /// 设置上下文数据
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            if (m_Data.ContainsKey(key))
            {
                m_Data[key] = value;
                return;
            }
            m_Data.Add(key, value);
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

        /// <summary>
        /// 是否包含指定Key
        /// </summary>
        public bool HasKey(string key)
        {
            return m_Data.ContainsKey(key);
        }

        public bool RemoveKey(string key)
        {
            bool removed = m_Data.Remove(key);
            return removed;
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
            m_Entries = new List<StepContextPersistEntry>();
            foreach (KeyValuePair<string, object> kvp in m_Data)
            {
                string key = kvp.Key;
                object value = kvp.Value;
                if (value == null)
                {
                    LWDebug.LogWarning("步骤上下文序列化失败：值为空，Key=" + key);
                    continue;
                }

                Type valueType = value.GetType();
                if (!IsBasicSerializableType(valueType))
                {
                    LWDebug.LogWarning("步骤上下文序列化失败：类型不支持，Key=" + key + "，类型=" + valueType.FullName);
                    continue;
                }

                string valueString = StepUtility.ConvertToRawString(value, valueType);
                StepContextPersistEntry entry = new StepContextPersistEntry();
                entry.Key = key;
                entry.Value = valueString;
                m_Entries.Add(entry);
            }
            return JsonMapper.ToJson(m_Entries);
        }

        public void LoadFromJson(string json)
        {
            Clear();
            if (string.IsNullOrEmpty(json))
            {
                return;
            }
            try
            {
                m_Entries = JsonMapper.ToObject<List<StepContextPersistEntry>>(json);
            }
            catch (Exception e)
            {
                LWDebug.LogWarning("步骤上下文反序列化失败：" + e.Message);
                return;
            }

            if (m_Entries == null)
            {
                return;
            }

            for (int i = 0; i < m_Entries.Count; i++)
            {
                StepContextPersistEntry entry = m_Entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.Key))
                {
                    continue;
                }

                string rawValue = entry.Value;
                object parsedValue;
                if (StepUtility.TryParseBasicValue(rawValue, out parsedValue))
                {
                    m_Data[entry.Key] = parsedValue;
                }
                else
                {
                    m_Data[entry.Key] = rawValue;
                }
            }
        }

        /// <summary>
        /// 判断是否为可序列化的基础类型
        /// </summary>
        private bool IsBasicSerializableType(Type valueType)
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
            if (valueType == typeof(string) || valueType == typeof(int) || valueType == typeof(float) || valueType == typeof(double) || valueType == typeof(long) || valueType == typeof(bool))
            {
                return true;
            }
            return false;
        }

        private class StepContextPersistEntry
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }
    }
}
