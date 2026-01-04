#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using LWAssets;

namespace LWAssets.Editor
{
    /// <summary>
    /// LWAssets主窗口
    /// </summary>
    public class TestWindow : EditorWindow
    {
        private int _selectedTypeIndex = 0;
        private Type[] _handleTypes;
        private string[] _handleTypeNames;

        private Type _selectedType;
        private object _selectedInstance;

        private readonly Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();
        [MenuItem("LWAssets/TestWindow")]
        public static void ShowWindow()
        {
            var window = GetWindow<TestWindow>();
            window.minSize = new Vector2(600, 400);
        }

        /// <summary>
        /// 窗口启用时初始化句柄类型列表
        /// </summary>
        private void OnEnable()
        {
            RefreshHandleTypes();
            SyncSelection();
        }

        /// <summary>
        /// 将下拉框索引同步为当前类型/实例
        /// </summary>
        private void SyncSelection()
        {
            if (_handleTypes == null || _handleTypes.Length == 0)
            {
                _selectedType = null;
                _selectedInstance = null;
                return;
            }

            if (_selectedTypeIndex < 0 || _selectedTypeIndex >= _handleTypes.Length)
                _selectedTypeIndex = 0;

            _selectedType = _handleTypes[_selectedTypeIndex];
            EnsureInstance();
        }

        /// <summary>
        /// 刷新所有 HandleBase 派生类型列表
        /// </summary>
        private void RefreshHandleTypes()
        {
            var typeList = new List<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types = null;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                if (types == null)
                    continue;

                foreach (var type in types)
                {
                    if (type == null)
                        continue;

                    if (type.IsAbstract)
                        continue;

                    if (!typeof(MyClass).IsAssignableFrom(type))
                        continue;

                    typeList.Add(type);
                }
            }

            _handleTypes = typeList.OrderBy(t => t.FullName ?? t.Name).ToArray();
            _handleTypeNames = _handleTypes.Select(t => t.FullName ?? t.Name).ToArray();

            if (_handleTypeNames.Length == 0)
            {
                _selectedTypeIndex = 0;
            }
            else if (_selectedTypeIndex >= _handleTypeNames.Length)
            {
                _selectedTypeIndex = 0;
            }

            SyncSelection();
        }

        /// <summary>
        /// 确保已创建当前选中类型的实例
        /// </summary>
        private void EnsureInstance()
        {
            if (_selectedType == null)
            {
                _selectedInstance = null;
                return;
            }

            if (_selectedInstance != null && _selectedInstance.GetType() == _selectedType)
                return;

            try
            {
                _selectedInstance = Activator.CreateInstance(_selectedType);
            }
            catch (Exception e)
            {
                _selectedInstance = null;
                Debug.LogError($"[TestWindow] 无法创建实例: {_selectedType.FullName}, 错误: {e.Message}");
            }
        }

        /// <summary>
        /// 绘制窗口界面
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField("输入类型");

            if (_handleTypeNames == null || _handleTypeNames.Length == 0)
            {
                EditorGUILayout.HelpBox("未找到 MyClass 派生类型", MessageType.Info);
            }
            else
            {
                var newIndex = EditorGUILayout.Popup("MyClass 类型", _selectedTypeIndex, _handleTypeNames);
                if (newIndex != _selectedTypeIndex)
                {
                    _selectedTypeIndex = newIndex;
                    SyncSelection();
                }

                DrawSelectedTypeEditor();
            }
        }

        /// <summary>
        /// 根据当前选中类型绘制对应的字段/属性输入界面
        /// </summary>
        private void DrawSelectedTypeEditor()
        {
            if (_selectedType == null)
                return;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"当前类型: {_selectedType.FullName}");

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("重置", GUILayout.Width(80)))
                    {
                        _selectedInstance = null;
                        EnsureInstance();
                    }
                }

                if (_selectedInstance == null)
                {
                    EditorGUILayout.HelpBox("当前类型无法实例化（可能没有无参构造函数）", MessageType.Warning);
                    return;
                }

                DrawObjectMembers(_selectedInstance);
            }
        }

        /// <summary>
        /// 反射绘制对象的可编辑字段/属性
        /// </summary>
        private void DrawObjectMembers(object target)
        {
            if (target == null)
                return;

            var type = target.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public;

            var fields = type.GetFields(flags);
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (field.IsInitOnly || field.IsLiteral)
                    continue;

                var oldValue = field.GetValue(target);
                var newValue = DrawValueField(field.Name, field.FieldType, oldValue);
                if (!ReferenceEquals(oldValue, newValue) && (oldValue == null || !oldValue.Equals(newValue)))
                {
                    try
                    {
                        field.SetValue(target, newValue);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[TestWindow] 设置字段失败: {type.FullName}.{field.Name}, 错误: {e.Message}");
                    }
                }
            }

            var properties = type.GetProperties(flags);
            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                if (!property.CanRead || !property.CanWrite)
                    continue;
                if (property.GetIndexParameters().Length > 0)
                    continue;

                object oldValue;
                try
                {
                    oldValue = property.GetValue(target);
                }
                catch
                {
                    continue;
                }

                var newValue = DrawValueField(property.Name, property.PropertyType, oldValue);
                if (!ReferenceEquals(oldValue, newValue) && (oldValue == null || !oldValue.Equals(newValue)))
                {
                    try
                    {
                        property.SetValue(target, newValue);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[TestWindow] 设置属性失败: {type.FullName}.{property.Name}, 错误: {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 根据值类型绘制对应的编辑控件，并返回编辑后的值
        /// </summary>
        private object DrawValueField(string label, Type valueType, object value)
        {
            if (valueType == typeof(string))
            {
                if (string.Equals(label, "FoldPath", StringComparison.Ordinal))
                {
                    var current = value as string ?? string.Empty;
                    var newValue = current;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PrefixLabel(label);
                        newValue = EditorGUILayout.TextField(newValue);
                        if (GUILayout.Button("浏览", GUILayout.Width(60)))
                        {
                            var startFolder = GetAbsoluteFolderPath(newValue);
                            var selected = EditorUtility.OpenFolderPanel("选择文件夹", startFolder, string.Empty);
                            if (!string.IsNullOrEmpty(selected))
                            {
                                newValue = ToProjectRelativePath(selected);
                                GUI.FocusControl(null);
                            }
                        }
                    }

                    return newValue;
                }

                return EditorGUILayout.TextField(label, value as string ?? string.Empty);
            }

            if (valueType == typeof(int))
                return EditorGUILayout.IntField(label, value is int v ? v : 0);

            if (valueType == typeof(float))
                return EditorGUILayout.FloatField(label, value is float v ? v : 0f);

            if (valueType == typeof(double))
            {
                var newValue = EditorGUILayout.DoubleField(label, value is double v ? v : 0d);
                return newValue;
            }

            if (valueType == typeof(long))
                return EditorGUILayout.LongField(label, value is long v ? v : 0L);

            if (valueType == typeof(bool))
                return EditorGUILayout.Toggle(label, value is bool v && v);

            if (valueType.IsEnum)
            {
                var enumValue = value as Enum;
                if (enumValue == null)
                {
                    var values = Enum.GetValues(valueType);
                    enumValue = values.Length > 0 ? (Enum)values.GetValue(0) : null;
                }

                return enumValue != null ? EditorGUILayout.EnumPopup(label, enumValue) : value;
            }

            if (IsStringListType(valueType))
                return DrawStringList(label, value as List<string>);

            EditorGUILayout.LabelField(label, $"不支持类型: {valueType.Name}");
            return value;
        }

        /// <summary>
        /// 判断类型是否为 List&lt;string&gt;
        /// </summary>
        private static bool IsStringListType(Type valueType)
        {
            if (valueType == null)
                return false;

            if (!valueType.IsGenericType)
                return false;

            if (valueType.GetGenericTypeDefinition() != typeof(List<>))
                return false;

            var args = valueType.GetGenericArguments();
            return args.Length == 1 && args[0] == typeof(string);
        }

        /// <summary>
        /// 绘制 List&lt;string&gt; 的编辑UI（支持展开、增删、逐项编辑）
        /// </summary>
        private List<string> DrawStringList(string label, List<string> list)
        {
            var foldoutKey = _selectedType != null ? $"{_selectedType.FullName}.{label}" : label;

            if (!_foldoutStates.TryGetValue(foldoutKey, out var isExpanded))
                isExpanded = true;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    isExpanded = EditorGUILayout.Foldout(isExpanded, $"{label} {(list != null ? $"[{list.Count}]" : "[null]")}", true);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("+", GUILayout.Width(28)))
                    {
                        if (list == null)
                            list = new List<string>();
                        list.Add(string.Empty);
                    }
                }

                _foldoutStates[foldoutKey] = isExpanded;
                if (!isExpanded)
                    return list;

                if (list == null)
                {
                    EditorGUILayout.HelpBox("列表为空，点击右侧 + 创建并添加元素", MessageType.Info);
                    return null;
                }

                for (int i = 0; i < list.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        list[i] = EditorGUILayout.TextField($"元素 {i}", list[i] ?? string.Empty);
                        if (GUILayout.Button("-", GUILayout.Width(28)))
                        {
                            list.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }

            return list;
        }

        private static string GetAbsoluteFolderPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;

            path = path.Replace('\\', '/');
            if (path.StartsWith("Assets/", StringComparison.Ordinal) || string.Equals(path, "Assets", StringComparison.Ordinal))
            {
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                if (!string.IsNullOrEmpty(projectRoot))
                {
                    var combined = Path.Combine(projectRoot, path);
                    return combined;
                }
            }

            return path;
        }

        private static string ToProjectRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return string.Empty;

            absolutePath = absolutePath.Replace('\\', '/');
            var dataPath = Application.dataPath.Replace('\\', '/');

            if (absolutePath.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                var relative = absolutePath.Substring(dataPath.Length);
                if (relative.StartsWith("/", StringComparison.Ordinal))
                    relative = relative.Substring(1);
                return string.IsNullOrEmpty(relative) ? "Assets" : ("Assets/" + relative);
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (!string.IsNullOrEmpty(projectRoot))
            {
                projectRoot = projectRoot.Replace('\\', '/');
                if (absolutePath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                {
                    var relative = absolutePath.Substring(projectRoot.Length);
                    if (relative.StartsWith("/", StringComparison.Ordinal))
                        relative = relative.Substring(1);
                    return relative;
                }
            }

            return absolutePath;
        }


    }
}

public class MyClass
{
    public string Name;
}
public class Child : MyClass
{
    public int Age;
    public List<string> Items;
}
public class Child2 : MyClass
{
    public string Sex;
    public string Adress;
    public string FoldPath;
}
#endif
