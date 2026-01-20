using System;
using System.Collections.Generic;

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
    }
}
