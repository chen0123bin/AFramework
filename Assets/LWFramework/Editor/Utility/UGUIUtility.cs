using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
/// <summary>
/// UGUI 工具类
/// </summary>
public class UGUIUtility
{
    /// <summary>
    /// 绘制值字段，根据值类型使用不同的编辑器控件
    /// </summary>
    /// <param name="label">字段标签</param>
    /// <param name="valueType">值类型</param>
    /// <param name="value">当前值</param>
    /// <returns>绘制后的新值</returns>
    public static object DrawValueField(string label, Type valueType, object value)
    {
        if (valueType == typeof(string))
        {
            string current = value as string;
            if (current == null)
            {
                current = string.Empty;
            }
            return EditorGUILayout.TextField(label, current);
        }

        if (valueType == typeof(int))
        {
            int current = value is int ? (int)value : 0;
            return EditorGUILayout.IntField(label, current);
        }

        if (valueType == typeof(float))
        {
            float current = value is float ? (float)value : 0f;
            return EditorGUILayout.FloatField(label, current);
        }

        if (valueType == typeof(double))
        {
            double current = value is double ? (double)value : 0d;
            return EditorGUILayout.DoubleField(label, current);
        }

        if (valueType == typeof(long))
        {
            long current = value is long ? (long)value : 0L;
            return EditorGUILayout.LongField(label, current);
        }

        if (valueType == typeof(bool))
        {
            bool current = value is bool && (bool)value;
            return EditorGUILayout.Toggle(label, current);
        }

        if (valueType.IsEnum)
        {
            Enum current = value as Enum;
            if (current == null)
            {
                Array values = Enum.GetValues(valueType);
                if (values != null && values.Length > 0)
                {
                    current = (Enum)values.GetValue(0);
                }
            }
            if (current != null)
            {
                return EditorGUILayout.EnumPopup(label, current);
            }
            return value;
        }

        EditorGUILayout.LabelField(label, "不支持类型: " + valueType.Name);
        return value;
    }

    /// <summary>
    /// 尝试将编辑器输入的字符串解析为指定类型的值
    /// </summary>
    /// <param name="rawValue">编辑器输入的原始字符串值</param>
    /// <param name="valueType">目标值类型</param>
    /// <param name="parsedValue">解析后的目标值</param>
    /// <returns>是否成功解析</returns>
    public static bool TryParseEditorValue(string rawValue, Type valueType, out object parsedValue)
    {
        parsedValue = null;
        if (valueType == typeof(string))
        {
            parsedValue = rawValue != null ? rawValue : string.Empty;
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

        if (valueType != null && valueType.IsEnum)
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
}
