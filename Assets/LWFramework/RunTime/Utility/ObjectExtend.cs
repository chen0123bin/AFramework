using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
/*
*Creator:陈斌
*/
public static class ObjectExtend
{
    /// <summary>
    /// 数据是否为空
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsEmpty(this object value)
    {
        if (value != null && !string.IsNullOrEmpty(value.ParseToString()))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// 数据是否为空，或为0
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsNullOrZero(this object value)
    {
        if (value == null || value.ParseToString().Trim() == "0")
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    /// <summary>
    /// 将object转换为string，若转换失败，则返回""。不抛出异常。  
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ParseToString(this object obj)
    {
        try
        {
            if (obj == null)
            {
                return string.Empty;
            }
            else
            {
                return obj.ToString();
            }
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string ToStringRefProp<T>(this T @this)
    {
        

        PropertyInfo[] properties = @this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        StringBuilder sb = new StringBuilder();
        foreach (PropertyInfo property in properties)
        {
            sb.Append($"{property.Name}:{property.GetValue(@this)}|");
        }
        return sb.ToString();
    }
    public static string ToStringRefFields<T>(this T @this)
    {
        FieldInfo[] fields = @this.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
        StringBuilder sb = new StringBuilder();
        foreach (FieldInfo field in fields)
        {
            sb.Append($"{field.Name}:{field.GetValue(@this)}");
        }
        return sb.ToString();
    }
    public static T FromStringRefProp<T>(this string str) where T : new()
    {
        var result = new T();
        if (string.IsNullOrEmpty(str)) return result;

        var properties = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name);

        foreach (var segment in str.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = segment.Split(':', 2);
            if (kv.Length != 2 || !properties.TryGetValue(kv[0], out var prop)) continue;

            try
            {
                object value = prop.PropertyType switch
                {
                    Type t when t == typeof(DateTime) => DateTime.Parse(kv[1]),
                    Type t when t == typeof(Guid) => Guid.Parse(kv[1]),
                    Type t when t.IsEnum => Enum.Parse(t, kv[1]),
                    _ => Convert.ChangeType(kv[1], prop.PropertyType)
                };
                prop.SetValue(result, value);
            }
            catch { /* 记录日志 */ }
        }
        return result;
    }
}
