using System;
using LitJson;
using UnityEngine;

/// <summary>
/// LitJson 的 JsonData 读取工具：提供安全的类型转换与默认值。
/// </summary>
internal sealed class UguiJsonReader
{
    /// <summary>
    /// 从对象节点读取字符串字段。
    /// </summary>
    public string GetString(JsonData data, string key, string defaultValue = "")
    {
        if (!TryGetValue(data, key, out JsonData value))
            return defaultValue;

        if (value == null)
            return defaultValue;

        return value.ToString();
    }

    /// <summary>
    /// 从对象节点读取布尔字段。
    /// </summary>
    public bool GetBool(JsonData data, string key, bool defaultValue)
    {
        if (!TryGetValue(data, key, out JsonData value))
            return defaultValue;

        try
        {
            if (value != null && value.IsBoolean)
                return (bool)value;
            if (value != null)
                return Convert.ToBoolean(value);
        }
        catch
        {
        }

        return defaultValue;
    }

    /// <summary>
    /// 从对象节点读取整数字段。
    /// </summary>
    public int GetInt(JsonData data, string key, int defaultValue)
    {
        if (!TryGetValue(data, key, out JsonData value))
            return defaultValue;

        try
        {
            if (value != null && value.IsInt)
                return (int)value;
            if (value != null && value.IsLong)
                return (int)(long)value;
            if (value != null)
                return Convert.ToInt32(value);
        }
        catch
        {
        }

        return defaultValue;
    }

    /// <summary>
    /// 从对象节点读取浮点字段。
    /// </summary>
    public float GetFloat(JsonData data, string key, float defaultValue)
    {
        if (!TryGetValue(data, key, out JsonData value))
            return defaultValue;

        return ToFloat(value, defaultValue);
    }

    /// <summary>
    /// 从数组节点读取 Vector2。
    /// </summary>
    public Vector2 GetVector2(JsonData list, Vector2 defaultValue)
    {
        if (list == null || !list.IsArray || list.Count < 2)
            return defaultValue;

        return new Vector2(ToFloat(list[0], defaultValue.x), ToFloat(list[1], defaultValue.y));
    }
    /// <summary>
    /// 从数组节点读取 Vector2Int。
    /// </summary>
    public Vector2Int GetVector2Int(JsonData list, Vector2Int defaultValue)
    {
        if (list == null || !list.IsArray || list.Count < 2)
            return defaultValue;

        return new Vector2Int(ToInt(list[0], defaultValue.x), ToInt(list[1], defaultValue.y));
    }

    private int ToInt(JsonData jsonData, int defaultValue)
    {
        if (jsonData == null)
            return defaultValue;

        try
        {
            if (jsonData.IsInt)
                return (int)jsonData;
            if (jsonData.IsLong)
                return (int)(long)jsonData;
            return Convert.ToInt32(jsonData);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// 从数组节点读取 Vector3。
    /// </summary>
    public Vector3 GetVector3(JsonData list, Vector3 defaultValue)
    {
        if (list == null || !list.IsArray || list.Count < 3)
            return defaultValue;

        return new Vector3(
            ToFloat(list[0], defaultValue.x),
            ToFloat(list[1], defaultValue.y),
            ToFloat(list[2], defaultValue.z));
    }

    /// <summary>
    /// 从数组节点读取 Color。
    /// </summary>
    public Color GetColor(JsonData list, Color defaultValue)
    {
        if (list == null || !list.IsArray || list.Count < 4)
            return defaultValue;

        return new Color(
            ToFloat(list[0], defaultValue.r),
            ToFloat(list[1], defaultValue.g),
            ToFloat(list[2], defaultValue.b),
            ToFloat(list[3], defaultValue.a));
    }

    /// <summary>
    /// 尝试从对象节点获取指定字段。
    /// </summary>
    public bool TryGetValue(JsonData data, string key, out JsonData value)
    {
        value = null;
        if (data == null || !data.IsObject)
            return false;

        if (!data.ContainsKey(key))
            return false;

        value = data[key];
        return true;
    }

    private float ToFloat(JsonData value, float defaultValue)
    {
        if (value == null)
            return defaultValue;

        try
        {
            if (value.IsDouble)
                return (float)(double)value;
            if (value.IsInt)
                return (int)value;
            if (value.IsLong)
                return (long)value;
            return Convert.ToSingle(value);
        }
        catch
        {
            return defaultValue;
        }
    }
}

