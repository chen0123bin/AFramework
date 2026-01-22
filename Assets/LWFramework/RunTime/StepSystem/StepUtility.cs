using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace LWStep
{
    /// <summary>
    /// Step工具类
    /// </summary>
    public class StepUtility
    {
        /// <summary>
        /// 比较两个类型的FullName
        /// </summary>
        /// <param name="a">第一个类型</param>
        /// <param name="b">第二个类型</param>
        /// <returns></returns>
        public static int CompareTypeFullName(Type a, Type b)
        {
            string aName = a != null ? (a.FullName != null ? a.FullName : a.Name) : string.Empty;
            string bName = b != null ? (b.FullName != null ? b.FullName : b.Name) : string.Empty;
            return string.CompareOrdinal(aName, bName);
        }

        /// <summary>
        /// 获取Action的参数成员列表
        /// </summary>
        /// <param name="actionType">Action类型</param>
        /// <returns></returns>
        public static List<StepParamBinding> CreateStepParamBindings(Type actionType)
        {
            List<StepParamBinding> members = new List<StepParamBinding>();
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

                StepParamBinding member = new StepParamBinding();
                member.Key = attr.Key;
                member.ValueType = field.FieldType;
                member.Field = field;
                member.Property = null;
                members.Add(member);
            }

            PropertyInfo[] properties = actionType.GetProperties(flags);
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                if (property == null || !property.CanWrite || !property.CanRead)
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

                StepParamBinding member = new StepParamBinding();
                member.Key = attr.Key;
                member.ValueType = property.PropertyType;
                member.Field = null;
                member.Property = property;
                members.Add(member);
            }

            return members;
        }


        /// <summary>
        /// 获取指定类型的默认值
        /// </summary>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static object GetDefaultValue(Type valueType)
        {
            if (valueType == typeof(string))
            {
                return string.Empty;
            }
            if (valueType == typeof(int))
            {
                return 0;
            }
            if (valueType == typeof(float))
            {
                return 0f;
            }
            if (valueType == typeof(double))
            {
                return 0d;
            }
            if (valueType == typeof(long))
            {
                return 0L;
            }
            if (valueType == typeof(bool))
            {
                return false;
            }
            if (valueType != null && valueType.IsEnum)
            {
                Array values = Enum.GetValues(valueType);
                if (values != null && values.Length > 0)
                {
                    return values.GetValue(0);
                }
            }
            return null;
        }
        /// <summary>
        /// 尝试将字符串解析为目标类型
        /// </summary>
        public static bool TryParseValue(string rawValue, Type valueType, out object parsedValue)
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
        /// 将指定值转换为编辑器输入的字符串表示
        /// </summary>
        /// <param name="value"></param>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static string ConvertToRawString(object value, Type valueType)
        {
            if (valueType == typeof(string))
            {
                return value as string ?? string.Empty;
            }

            if (valueType == typeof(int))
            {
                int intValue = value is int ? (int)value : 0;
                return intValue.ToString(CultureInfo.InvariantCulture);
            }

            if (valueType == typeof(float))
            {
                float floatValue = value is float ? (float)value : 0f;
                return floatValue.ToString(CultureInfo.InvariantCulture);
            }

            if (valueType == typeof(double))
            {
                double doubleValue = value is double ? (double)value : 0d;
                return doubleValue.ToString(CultureInfo.InvariantCulture);
            }

            if (valueType == typeof(long))
            {
                long longValue = value is long ? (long)value : 0L;
                return longValue.ToString(CultureInfo.InvariantCulture);
            }

            if (valueType == typeof(bool))
            {
                bool boolValue = value is bool && (bool)value;
                return boolValue.ToString();
            }

            if (valueType != null && valueType.IsEnum)
            {
                Enum enumValue = value as Enum;
                if (enumValue != null)
                {
                    return enumValue.ToString();
                }
                return string.Empty;
            }

            return value != null ? value.ToString() : string.Empty;
        }
    }
}

