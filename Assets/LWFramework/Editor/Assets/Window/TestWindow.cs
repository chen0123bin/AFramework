#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using LWAssets;
using System.Security.Permissions;

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

        private bool _isDraggingListElement;
        private string _draggingListKey;
        private int _draggingFromIndex = -1;
        private int _draggingInsertIndex = -1;
        private int _dragHotControl;
        private Vector2 _dragStartMousePosition;
        private bool _didDragListElement;

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
            if (typeof(UnityEngine.Object).IsAssignableFrom(valueType))
            {
                return EditorGUILayout.ObjectField(label, value as UnityEngine.Object, valueType, false);
            }

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


            if (TryGetListElementType(valueType, out var elementType))
                return DrawGenericList(label, valueType, elementType, value as System.Collections.IList);

            EditorGUILayout.LabelField(label, $"不支持类型: {valueType.Name}");
            return value;
        }

        private static bool TryGetListElementType(Type valueType, out Type elementType)
        {
            elementType = null;
            if (valueType == null)
                return false;

            if (!valueType.IsGenericType)
                return false;

            if (valueType.GetGenericTypeDefinition() != typeof(List<>))
                return false;

            var args = valueType.GetGenericArguments();
            if (args.Length != 1)
                return false;

            elementType = args[0];
            return true;
        }

        private object DrawGenericList(string label, Type listType, Type elementType, System.Collections.IList list)
        {
            var foldoutKey = _selectedType != null ? $"{_selectedType.FullName}.{label}" : label;
            if (!_foldoutStates.TryGetValue(foldoutKey, out var isExpanded))
                isExpanded = true;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var countText = list != null ? $"[{list.Count}]" : "[null]";
                    isExpanded = EditorGUILayout.Foldout(isExpanded, $"{label} {countText}", true);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("+", GUILayout.Width(28)))
                    {
                        if (list == null)
                        {
                            try
                            {
                                list = Activator.CreateInstance(listType) as System.Collections.IList;
                            }
                            catch
                            {
                                list = null;
                            }
                        }

                        if (list != null)
                            list.Add(CreateDefaultElementValue(elementType));
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

                var elementHeaderRects = new List<Rect>(list.Count);

                for (int i = 0; i < list.Count; i++)
                {
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        var elementFoldoutKey = $"{foldoutKey}[{i}]";
                        if (!_foldoutStates.TryGetValue(elementFoldoutKey, out var elementExpanded))
                            elementExpanded = true;

                        var headerRect = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
                        var dragRect = new Rect(headerRect.x, headerRect.y, 18f, headerRect.height);
                        var removeRect = new Rect(headerRect.xMax - 28f, headerRect.y, 28f, headerRect.height);
                        var foldoutRect = new Rect(dragRect.xMax + 4f, headerRect.y, headerRect.width - dragRect.width - removeRect.width - 8f, headerRect.height);

                        if (_didDragListElement && _isDraggingListElement && string.Equals(_draggingListKey, foldoutKey, StringComparison.Ordinal) && _draggingFromIndex == i)
                            EditorGUI.DrawRect(headerRect, new Color(0.25f, 0.5f, 1f, 0.12f));

                        EditorGUIUtility.AddCursorRect(dragRect, MouseCursor.Pan);
                        DrawDragHandleIcon(dragRect);

                        var elementLabel = $"元素 {i} ({elementType.Name})";
                        elementExpanded = EditorGUI.Foldout(foldoutRect, elementExpanded, elementLabel, true);
                        _foldoutStates[elementFoldoutKey] = elementExpanded;

                        if (GUI.Button(removeRect, "-"))
                        {
                            list.RemoveAt(i);
                            i--;
                            continue;
                        }

                        elementHeaderRects.Add(headerRect);

                        TryStartListElementDrag(foldoutKey, i, dragRect);

                        if (i < 0 || i >= list.Count)
                            continue;

                        if (!(_foldoutStates.TryGetValue($"{foldoutKey}[{i}]", out var elementIsExpanded) && elementIsExpanded))
                            continue;

                        var element = list[i];
                        if (element == null)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField("当前元素为 null");
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("创建", GUILayout.Width(60)))
                                {
                                    var created = CreateDefaultElementValue(elementType);
                                    list[i] = created;
                                    element = created;
                                }
                            }

                            if (element == null)
                            {
                                EditorGUILayout.HelpBox("无法创建元素实例（可能没有无参构造函数）", MessageType.Warning);
                                continue;
                            }
                        }

                        if (IsDirectEditableElementType(elementType))
                        {
                            var edited = DrawValueField($"元素 {i}", elementType, element);
                            list[i] = edited;
                        }
                        else
                        {
                            EditorGUI.indentLevel++;
                            DrawObjectMembers(element);
                            EditorGUI.indentLevel--;

                            if (elementType.IsValueType)
                                list[i] = element;
                        }
                    }
                }

                HandleListDragContinueAndDrop(foldoutKey, elementHeaderRects, list);

                DrawListInsertIndicatorIfNeeded(foldoutKey, elementHeaderRects);
            }

            return list;
        }

        private void TryStartListElementDrag(string listKey, int elementIndex, Rect dragRect)
        {
            var e = Event.current;
            if (e == null)
                return;

            if (e.type == EventType.MouseDown && e.button == 0 && dragRect.Contains(e.mousePosition))
            {
                _isDraggingListElement = true;
                _draggingListKey = listKey;
                _draggingFromIndex = elementIndex;
                _draggingInsertIndex = elementIndex;
                _dragStartMousePosition = e.mousePosition;
                _didDragListElement = false;

                _dragHotControl = GUIUtility.GetControlID(FocusType.Passive);
                GUIUtility.hotControl = _dragHotControl;
                e.Use();
                Repaint();
            }
        }

        private void HandleListDragContinueAndDrop(string listKey, List<Rect> elementHeaderRects, System.Collections.IList list)
        {
            var e = Event.current;
            if (e == null)
                return;

            if (!_isDraggingListElement || !string.Equals(_draggingListKey, listKey, StringComparison.Ordinal))
                return;

            if (GUIUtility.hotControl != _dragHotControl)
                return;

            if (e.type == EventType.MouseDrag)
            {
                if (!_didDragListElement)
                {
                    var delta = e.mousePosition - _dragStartMousePosition;
                    if (delta.sqrMagnitude >= 16f)
                        _didDragListElement = true;
                }

                if (_didDragListElement)
                    _draggingInsertIndex = CalculateInsertIndex(e.mousePosition, elementHeaderRects);

                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                CancelListDrag();
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseUp)
            {
                if (_didDragListElement)
                {
                    var insertIndex = CalculateInsertIndex(e.mousePosition, elementHeaderRects);
                    TryReorderListOnDrop(list, _draggingFromIndex, insertIndex);
                }

                CancelListDrag();

                e.Use();
                Repaint();
            }
        }

        private void CancelListDrag()
        {
            _isDraggingListElement = false;
            _draggingListKey = null;
            _draggingFromIndex = -1;
            _draggingInsertIndex = -1;
            _dragHotControl = 0;
            _didDragListElement = false;
            GUIUtility.hotControl = 0;
        }

        private static void DrawDragHandleIcon(Rect rect)
        {
            var color = EditorGUIUtility.isProSkin ? new Color(0.85f, 0.85f, 0.85f, 0.9f) : new Color(0.25f, 0.25f, 0.25f, 0.9f);
            var width = Mathf.Max(0f, rect.width - 8f);
            var x = rect.x + (rect.width - width) * 0.5f;
            var centerY = rect.center.y;
            for (int i = -1; i <= 1; i++)
            {
                var y = centerY + i * 3f;
                EditorGUI.DrawRect(new Rect(x, y, width, 1f), color);
            }
        }

        private static int CalculateInsertIndex(Vector2 mousePosition, List<Rect> elementHeaderRects)
        {
            if (elementHeaderRects == null || elementHeaderRects.Count == 0)
                return 0;

            for (int i = 0; i < elementHeaderRects.Count; i++)
            {
                if (mousePosition.y < elementHeaderRects[i].center.y)
                    return i;
            }

            return elementHeaderRects.Count;
        }

        private static void TryReorderListOnDrop(System.Collections.IList list, int fromIndex, int insertIndex)
        {
            if (list == null)
                return;

            if (fromIndex < 0 || fromIndex >= list.Count)
                return;

            insertIndex = Mathf.Clamp(insertIndex, 0, list.Count);

            if (insertIndex == fromIndex || insertIndex == fromIndex + 1)
                return;

            var item = list[fromIndex];
            list.RemoveAt(fromIndex);

            if (insertIndex > fromIndex)
                insertIndex--;

            insertIndex = Mathf.Clamp(insertIndex, 0, list.Count);
            list.Insert(insertIndex, item);
        }

        private void DrawListInsertIndicatorIfNeeded(string listKey, List<Rect> elementHeaderRects)
        {
            if (!_isDraggingListElement || !_didDragListElement)
                return;

            if (!string.Equals(_draggingListKey, listKey, StringComparison.Ordinal))
                return;

            if (elementHeaderRects == null || elementHeaderRects.Count == 0)
                return;

            var insertIndex = Mathf.Clamp(_draggingInsertIndex, 0, elementHeaderRects.Count);

            float y;
            if (insertIndex >= elementHeaderRects.Count)
                y = elementHeaderRects[elementHeaderRects.Count - 1].yMax;
            else
                y = elementHeaderRects[insertIndex].yMin;

            var x = elementHeaderRects[0].x;
            var width = elementHeaderRects[0].width;
            EditorGUI.DrawRect(new Rect(x, y - 1f, width, 2f), new Color(0.25f, 0.55f, 1f, 1f));
        }

        private static bool IsDirectEditableElementType(Type elementType)
        {
            if (elementType == null)
                return true;

            if (typeof(UnityEngine.Object).IsAssignableFrom(elementType))
                return true;

            if (elementType == typeof(string))
                return true;

            if (elementType == typeof(int) || elementType == typeof(float) || elementType == typeof(double) || elementType == typeof(long) || elementType == typeof(bool))
                return true;

            if (elementType.IsEnum)
                return true;

            return false;
        }

        private static object CreateDefaultElementValue(Type elementType)
        {
            if (elementType == null)
                return null;

            if (elementType == typeof(string))
                return string.Empty;

            if (elementType.IsEnum)
            {
                var values = Enum.GetValues(elementType);
                return values.Length > 0 ? values.GetValue(0) : null;
            }

            if (elementType.IsValueType)
                return Activator.CreateInstance(elementType);

            var ctor = elementType.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
                return null;

            try
            {
                return Activator.CreateInstance(elementType);
            }
            catch
            {
                return null;
            }
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
    public List<TestData> TestDatas;
    public List<TestData2> TestDatas2;
}
public class TestData
{
    public string Description;
    public int Index;
    public bool IsChoose;
}

public class TestData2
{
    public string Description;
    public int Index;
    public bool IsChoose;
    public bool IsUp;
    public List<string> Items;
}
#endif
