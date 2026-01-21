using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace LWStep
{
    /// <summary>
    /// 步骤动作参数绑定特性：用于将 XML 参数 key 绑定到字段/属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class StepParamAttribute : Attribute
    {
        public string Key { get; private set; }

        /// <summary>
        /// 创建参数绑定特性
        /// </summary>
        public StepParamAttribute(string key)
        {
            Key = key;
        }
    }

    public interface IStepBaselineStateAction
    {
        void CaptureBaselineState();
        void RestoreBaselineState();
    }

    /// <summary>
    /// 步骤动作基类（可继承扩展）
    /// </summary>
    public abstract class BaseStepAction
    {
        private class StepParamBinding
        {
            public string Key;
            public Type ValueType;
            public FieldInfo Field;
            public PropertyInfo Property;
        }

        private static Dictionary<Type, List<StepParamBinding>> s_ParamBindingsCache = new Dictionary<Type, List<StepParamBinding>>();

        private bool m_IsFinished;
        private bool m_HasEntered;
        private bool m_HasExited;
        private StepContext m_Context;
        private Dictionary<string, string> m_Parameters;

        /// <summary>
        /// 动作是否完成
        /// </summary>
        public bool IsFinished
        {
            get { return m_IsFinished; }
        }

        /// <summary>
        /// 设置上下文
        /// </summary>
        public void SetContext(StepContext context)
        {
            m_Context = context;
        }

        /// <summary>
        /// 获取上下文
        /// </summary>
        protected StepContext GetContext()
        {
            return m_Context;
        }

        /// <summary>
        /// 设置动作参数
        /// </summary>
        public void SetParameters(Dictionary<string, string> parameters)
        {
            m_Parameters = parameters;
            BindParametersToMembers();
        }

        /// <summary>
        /// 获取动作参数
        /// </summary>
        protected Dictionary<string, string> GetParameters()
        {
            return m_Parameters;
        }

        /// <summary>
        /// 将参数字典绑定到带 StepParamAttribute 的字段/属性
        /// </summary>
        private void BindParametersToMembers()
        {
            if (m_Parameters == null || m_Parameters.Count == 0)
            {
                return;
            }

            Type actionType = GetType();
            List<StepParamBinding> bindings = GetOrCreateParamBindings(actionType);
            if (bindings == null || bindings.Count == 0)
            {
                return;
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                StepParamBinding binding = bindings[i];
                if (binding == null || string.IsNullOrEmpty(binding.Key) || binding.ValueType == null)
                {
                    continue;
                }

                string rawValue;
                if (!m_Parameters.TryGetValue(binding.Key, out rawValue))
                {
                    continue;
                }

                object parsedValue;
                if (!TryParseValue(rawValue, binding.ValueType, out parsedValue))
                {
                    continue;
                }

                try
                {
                    if (binding.Field != null)
                    {
                        binding.Field.SetValue(this, parsedValue);
                    }
                    else if (binding.Property != null && binding.Property.CanWrite)
                    {
                        binding.Property.SetValue(this, parsedValue, null);
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// 获取或创建指定动作类型的参数绑定缓存
        /// </summary>
        private static List<StepParamBinding> GetOrCreateParamBindings(Type actionType)
        {
            if (actionType == null)
            {
                return null;
            }

            List<StepParamBinding> cached;
            if (s_ParamBindingsCache.TryGetValue(actionType, out cached))
            {
                return cached;
            }

            List<StepParamBinding> bindings = new List<StepParamBinding>();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            FieldInfo[] fields = actionType.GetFields(flags);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field == null)
                {
                    continue;
                }

                StepParamAttribute attr = Attribute.GetCustomAttribute(field, typeof(StepParamAttribute), true) as StepParamAttribute;
                if (attr == null || string.IsNullOrEmpty(attr.Key))
                {
                    continue;
                }

                StepParamBinding binding = new StepParamBinding();
                binding.Key = attr.Key;
                binding.ValueType = field.FieldType;
                binding.Field = field;
                binding.Property = null;
                bindings.Add(binding);
            }

            PropertyInfo[] properties = actionType.GetProperties(flags);
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                if (property == null || !property.CanWrite)
                {
                    continue;
                }

                if (property.GetIndexParameters() != null && property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                StepParamAttribute attr = Attribute.GetCustomAttribute(property, typeof(StepParamAttribute), true) as StepParamAttribute;
                if (attr == null || string.IsNullOrEmpty(attr.Key))
                {
                    continue;
                }

                StepParamBinding binding = new StepParamBinding();
                binding.Key = attr.Key;
                binding.ValueType = property.PropertyType;
                binding.Field = null;
                binding.Property = property;
                bindings.Add(binding);
            }

            s_ParamBindingsCache[actionType] = bindings;
            return bindings;
        }

        /// <summary>
        /// 尝试将字符串解析为目标类型
        /// </summary>
        private static bool TryParseValue(string rawValue, Type valueType, out object parsedValue)
        {
            parsedValue = null;
            if (valueType == typeof(string))
            {
                parsedValue = rawValue;
                return true;
            }

            if (valueType == typeof(int))
            {
                int intValue;
                if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue))
                {
                    parsedValue = intValue;
                    return true;
                }
                return false;
            }

            if (valueType == typeof(float))
            {
                float floatValue;
                if (float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out floatValue))
                {
                    parsedValue = floatValue;
                    return true;
                }
                return false;
            }

            if (valueType == typeof(double))
            {
                double doubleValue;
                if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue))
                {
                    parsedValue = doubleValue;
                    return true;
                }
                return false;
            }

            if (valueType == typeof(long))
            {
                long longValue;
                if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
                {
                    parsedValue = longValue;
                    return true;
                }
                return false;
            }

            if (valueType == typeof(bool))
            {
                bool boolValue;
                if (bool.TryParse(rawValue, out boolValue))
                {
                    parsedValue = boolValue;
                    return true;
                }
                return false;
            }

            if (valueType.IsEnum)
            {
                try
                {
                    object enumValue = Enum.Parse(valueType, rawValue, true);
                    parsedValue = enumValue;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// 重置动作状态
        /// </summary>
        public void Reset()
        {
            m_IsFinished = false;
            m_HasEntered = false;
            m_HasExited = false;
        }

        /// <summary>
        /// 进入动作
        /// </summary>
        public void Enter()
        {
            if (m_HasEntered)
            {
                return;
            }
            m_HasEntered = true;
            OnEnter();
        }

        /// <summary>
        /// 更新动作
        /// </summary>
        public void Update()
        {
            if (m_IsFinished)
            {
                return;
            }
            OnUpdate();
        }

        /// <summary>
        /// 退出动作
        /// </summary>
        public void Exit()
        {
            if (m_HasExited)
            {
                return;
            }
            m_HasExited = true;
            OnExit();
        }

        /// <summary>
        /// 快速应用动作（用于跳转补齐）
        /// </summary>
        public void Apply()
        {
            if (!m_HasEntered)
            {
                Enter();
            }
            OnApply();
            if (!m_IsFinished)
            {
                Finish();
            }
            Exit();
        }

        /// <summary>
        /// 标记动作完成
        /// </summary>
        protected void Finish()
        {
            m_IsFinished = true;
        }

        /// <summary>
        /// 动作进入时调用
        /// </summary>
        protected abstract void OnEnter();

        /// <summary>
        /// 动作更新时调用
        /// </summary>
        protected abstract void OnUpdate();

        /// <summary>
        /// 动作退出时调用
        /// </summary>
        protected abstract void OnExit();

        /// <summary>
        /// 动作快速应用时调用
        /// </summary>
        protected abstract void OnApply();
    }
}
