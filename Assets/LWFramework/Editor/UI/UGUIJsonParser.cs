using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using LitJson;
using System.IO;
using System.Collections.Generic;
/// <summary>
///这是一份存储unity ugui描述的文件，根据这份规则，帮我生成一份登录界面的json文件名称为login.txt
/// </summary>
public class UGUIJsonParser : EditorWindow
{
    private const string KeyRoot = "Root";
    private const string KeyName = "name";
    private const string KeyType = "type";
    private const string KeyActive = "active";
    private const string KeyRectTransform = "rectTransform";
    private const string KeyChildren = "children";

    private const string TypeContainer = "Container";
    private const string TypeButton = "Button";
    private const string TypeImage = "Image";
    private const string TypeRawImage = "RawImage";
    private const string TypeText = "Text";
    private const string TypeInputField = "InputField";
    private const string TypeDropdown = "Dropdown";
    private const string TypeSlider = "Slider";
    private const string TypeScrollRect = "ScrollRect";
    private const string TypeScrollbar = "Scrollbar";
    private const string TypeToggle = "Toggle";
    private const string TypeMask = "Mask";

    private const string KeyImage = "image";
    private const string KeyRawImage = "rawImage";
    private const string KeyText = "text";
    private const string KeyButton = "button";
    private const string KeyInputField = "inputField";
    private const string KeyDropdown = "dropdown";
    private const string KeySlider = "slider";
    private const string KeyToggle = "toggle";
    private const string KeyScrollRect = "scrollRect";
    private const string KeyScrollbar = "scrollbar";

    private string m_JsonFilePath = "";
    private TextAsset m_JsonFile;
    private Transform m_ParentTransform;

    /// <summary>
    /// 打开编辑器窗口。
    /// </summary>
    [MenuItem("Tools/UGUI JSON Parser")]
    public static void ShowWindow()
    {
        GetWindow<UGUIJsonParser>("UGUI JSON Parser");
    }

    /// <summary>
    /// 绘制编辑器 UI，并触发解析/创建。
    /// </summary>
    private void OnGUI()
    {
        GUILayout.Label("UGUI JSON Parser", EditorStyles.boldLabel);
        GUILayout.Space(10);

        m_JsonFile = (TextAsset)EditorGUILayout.ObjectField("JSON File", m_JsonFile, typeof(TextAsset), false);
        m_ParentTransform = (Transform)EditorGUILayout.ObjectField("Parent Transform (Optional)", m_ParentTransform, typeof(Transform), true);

        GUILayout.Space(10);

        if (GUILayout.Button("Parse and Create UGUI", GUILayout.Height(40)))
        {
            if (m_JsonFile != null)
            {
                ParseAndCreateUI(m_JsonFile.text);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please select a JSON file!", "OK");
            }
        }

        if (GUILayout.Button("Parse from File Path", GUILayout.Height(40)))
        {
            m_JsonFilePath = EditorUtility.OpenFilePanel("Select JSON File", Application.dataPath, "json");
            if (!string.IsNullOrEmpty(m_JsonFilePath))
            {
                string jsonContent = File.ReadAllText(m_JsonFilePath);
                ParseAndCreateUI(jsonContent);
            }
        }
    }

    /// <summary>
    /// 解析 JSON 文本并创建 UGUI 树。
    /// </summary>
    private void ParseAndCreateUI(string jsonContent)
    {
        try
        {
            JsonData jsonData = JsonMapper.ToObject(jsonContent);

            if (jsonData.ContainsKey(KeyRoot))
            {
                CreateRoot(jsonData[KeyRoot]);
                //EditorUtility.DisplayDialog("Success", "UGUI created successfully!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Invalid JSON format: 'Root' not found!", "OK");
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to parse JSON: {e.Message}", "OK");
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// 创建根节点（作为 UI 容器），并递归创建其子节点。
    /// </summary>
    private GameObject CreateRoot(JsonData rootData)
    {
        GameObject rootObj = CreateUIObject(rootData, m_ParentTransform, "Root");
        rootObj.AddComponent<CanvasRenderer>();
        rootObj.AddComponent<CanvasGroup>();

        if (rootData.ContainsKey(KeyChildren))
        {
            CreateChildren(rootData[KeyChildren], rootObj.transform);
        }

        return rootObj;
    }

    /// <summary>
    /// 递归创建 children，并在子树创建完成后做“后处理挂载”。
    /// </summary>
    private void CreateChildren(JsonData childrenData, Transform parent)
    {
        if (childrenData == null || !childrenData.IsArray) return;

        for (int i = 0; i < childrenData.Count; i++)
        {
            JsonData childData = childrenData[i];
            string type = GetString(childData, KeyType, TypeContainer);
            GameObject childObj = CreateElementByType(type, childData, parent);
            if (childObj == null) continue;

            if (childData.ContainsKey(KeyChildren))
            {
                CreateChildren(childData[KeyChildren], childObj.transform);
            }

            ApplyAfterChildren(type, childObj, childData);
        }
    }

    /// <summary>
    /// 根据 type 创建节点，并应用“可立即应用”的配置（不依赖子节点引用）。
    /// </summary>
    private GameObject CreateElementByType(string type, JsonData data, Transform parent)
    {
        switch (type)
        {
            case TypeButton:
                return CreateButton(data, parent);
            case TypeImage:
                return CreateImage(data, parent);
            case TypeRawImage:
                return CreateRawImage(data, parent);
            case TypeText:
                return CreateText(data, parent);
            case TypeInputField:
                return CreateInputField(data, parent);
            case TypeDropdown:
                return CreateDropdown(data, parent);
            case TypeSlider:
                return CreateSlider(data, parent);
            case TypeScrollRect:
                return CreateScrollRect(data, parent);
            case TypeScrollbar:
                return CreateScrollbar(data, parent);
            case TypeToggle:
                return CreateToggle(data, parent);
            case TypeMask:
                return CreateMask(data, parent);
            case TypeContainer:
            default:
                return CreateContainer(data, parent);
        }
    }

    /// <summary>
    /// 子节点创建完成后的配置应用：用于挂载依赖子物体的引用（例如 Text/Fill/Handle 等）。
    /// </summary>
    private void ApplyAfterChildren(string type, GameObject obj, JsonData data)
    {
        if (obj == null || data == null) return;

        if (type == TypeInputField && data.ContainsKey(KeyInputField))
        {
            ApplyInputField(obj.GetComponent<InputField>(), data[KeyInputField]);
            return;
        }

        if (type == TypeDropdown && data.ContainsKey(KeyDropdown))
        {
            ApplyDropdown(obj.GetComponent<Dropdown>(), data[KeyDropdown]);
            return;
        }

        if (type == TypeSlider && data.ContainsKey(KeySlider))
        {
            ApplySlider(obj.GetComponent<Slider>(), data[KeySlider]);
            return;
        }

        if (type == TypeToggle && data.ContainsKey(KeyToggle))
        {
            ApplyToggle(obj.GetComponent<Toggle>(), data[KeyToggle]);
            return;
        }

        if (type == TypeScrollRect && data.ContainsKey(KeyScrollRect))
        {
            ApplyScrollRect(obj.GetComponent<ScrollRect>(), data[KeyScrollRect]);
            return;
        }

        if (type == TypeScrollbar && data.ContainsKey(KeyScrollbar))
        {
            ApplyScrollbar(obj.GetComponent<Scrollbar>(), data[KeyScrollbar]);
            return;
        }
    }

    /// <summary>
    /// 创建基础 UI 对象（name/active/RectTransform），并自动挂载到父节点。
    /// </summary>
    private GameObject CreateUIObject(JsonData data, Transform parent, string defaultName)
    {
        string name = GetString(data, KeyName, defaultName);
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.SetActive(GetBool(data, KeyActive, true));

        RectTransform rectTransform = obj.AddComponent<RectTransform>();
        if (data != null && data.ContainsKey(KeyRectTransform))
        {
            ApplyRectTransform(rectTransform, data[KeyRectTransform]);
        }

        return obj;
    }

    /// <summary>
    /// 创建 Button 节点，并应用 button/image 的配置。
    /// </summary>
    private GameObject CreateButton(JsonData data, Transform parent)
    {
        GameObject obj = CreateUIObject(data, parent, TypeButton);

        Image image = obj.AddComponent<Image>();
        if (data.ContainsKey(KeyImage))
        {
            ApplyImage(image, data[KeyImage]);
        }

        Button button = obj.AddComponent<Button>();
        if (data.ContainsKey(KeyButton))
        {
            ApplyButton(button, data[KeyButton]);
        }

        return obj;
    }

    /// <summary>
    /// 创建 Image 节点，并应用 image 配置。
    /// </summary>
    private GameObject CreateImage(JsonData data, Transform parent)
    {
        GameObject obj = CreateUIObject(data, parent, TypeImage);

        Image image = obj.AddComponent<Image>();
        if (data.ContainsKey(KeyImage))
        {
            ApplyImage(image, data[KeyImage]);
        }

        return obj;
    }

    /// <summary>
    /// 创建 RawImage 节点，并应用 rawImage 配置。
    /// </summary>
    private GameObject CreateRawImage(JsonData data, Transform parent)
    {
        GameObject obj = CreateUIObject(data, parent, TypeRawImage);

        RawImage rawImage = obj.AddComponent<RawImage>();
        if (data.ContainsKey(KeyRawImage))
        {
            ApplyRawImage(rawImage, data[KeyRawImage]);
        }

        return obj;
    }

    /// <summary>
    /// 创建 Text 节点，并应用 text 配置。
    /// </summary>
    private GameObject CreateText(JsonData data, Transform parent)
    {
        GameObject obj = CreateUIObject(data, parent, TypeText);

        Text text = obj.AddComponent<Text>();
        if (data.ContainsKey(KeyText))
        {
            ApplyText(text, data[KeyText]);
        }

        return obj;
    }

    /// <summary>
    /// 创建 InputField 节点（配置在子节点创建完成后应用）。
    /// </summary>
    private GameObject CreateInputField(JsonData data, Transform parent)
    {
        GameObject obj = CreateUIObject(data, parent, TypeInputField);

        Image image = obj.AddComponent<Image>();
        if (data.ContainsKey(KeyImage))
        {
            ApplyImage(image, data[KeyImage]);
        }

        obj.AddComponent<InputField>();
        return obj;
    }

    /// <summary>
    /// 创建 Dropdown 节点（配置在子节点创建完成后应用）。
    /// </summary>
    private GameObject CreateDropdown(JsonData data, Transform parent)
    {
        GameObject obj = CreateUIObject(data, parent, TypeDropdown);

        Image image = obj.AddComponent<Image>();
        if (data.ContainsKey(KeyImage))
        {
            ApplyImage(image, data[KeyImage]);
        }

        obj.AddComponent<Dropdown>();

        return obj;
    }

    /// <summary>
    /// 创建 Slider 节点（配置在子节点创建完成后应用）。
    /// </summary>
    private GameObject CreateSlider(JsonData data, Transform parent)
    {
        GameObject obj = CreateUIObject(data, parent, TypeSlider);
        obj.AddComponent<Slider>();
        return obj;
    }

    /// <summary>
    /// 创建 ScrollRect 节点（配置在子节点创建完成后应用）。
    /// </summary>
    private GameObject CreateScrollRect(JsonData data, Transform parent)
    {
        GameObject obj = CreateUIObject(data, parent, TypeScrollRect);

        Image image = obj.AddComponent<Image>();
        if (data.ContainsKey(KeyImage))
        {
            ApplyImage(image, data[KeyImage]);
        }

        obj.AddComponent<ScrollRect>();

        return obj;
    }

    /// <summary>
    /// 创建 Scrollbar 节点（配置在子节点创建完成后应用）。
    /// </summary>
    private GameObject CreateScrollbar(JsonData data, Transform parent)
    {
        GameObject obj = CreateUIObject(data, parent, TypeScrollbar);

        Image image = obj.AddComponent<Image>();
        if (data.ContainsKey(KeyImage))
        {
            ApplyImage(image, data[KeyImage]);
        }

        obj.AddComponent<Scrollbar>();

        return obj;
    }

    /// <summary>
    /// 创建 Toggle 节点（配置在子节点创建完成后应用）。
    /// </summary>
    private GameObject CreateToggle(JsonData data, Transform parent)
    {
        GameObject obj = CreateUIObject(data, parent, TypeToggle);
        obj.AddComponent<Toggle>();
        return obj;
    }

    /// <summary>
    /// 创建 Mask 节点。
    /// </summary>
    private GameObject CreateMask(JsonData data, Transform parent)
    {
        GameObject obj = CreateUIObject(data, parent, "Viewport");       
        Image image = obj.AddComponent<Image>();
        if (data.ContainsKey(KeyImage))
        {
            ApplyImage(image, data[KeyImage]);
        }
        obj.AddComponent<Mask>();
        return obj;
    }

    /// <summary>
    /// 创建纯容器节点（仅 RectTransform）。
    /// </summary>
    private GameObject CreateContainer(JsonData data, Transform parent)
    {
        return CreateUIObject(data, parent, TypeContainer);
    }

    /// <summary>
    /// 应用 RectTransform 数据。
    /// </summary>
    private void ApplyRectTransform(RectTransform rect, JsonData data)
    {
        if (data == null) return;

        rect.anchorMin = GetVector2(data, "anchorMin", Vector2.zero);
        rect.anchorMax = GetVector2(data, "anchorMax", Vector2.one);
        rect.pivot = GetVector2(data, "pivot", new Vector2(0.5f, 0.5f));
        rect.anchoredPosition = GetVector2(data, "anchoredPosition", Vector2.zero);
        rect.sizeDelta = GetVector2(data, "sizeDelta", Vector2.zero);
        rect.localRotation = Quaternion.Euler(GetVector3(data, "rotation", Vector3.zero));
        rect.localScale = GetVector3(data, "scale", Vector3.one);
    }

    /// <summary>
    /// 应用 Image 数据。
    /// </summary>
    private void ApplyImage(Image image, JsonData data)
    {
        if (data == null) return;

        string spritePath = GetString(data, "sprite", "");
        if (!string.IsNullOrEmpty(spritePath))
        {

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null)
            {
                image.sprite = sprite;
            }
        }

        image.color = GetColor(data, "color", Color.white);
        image.raycastTarget = GetBool(data, "raycastTarget", true);
        image.type = ParseImageType(GetString(data, "imageType", "Simple"));
    }

    /// <summary>
    /// 应用 RawImage 数据。
    /// </summary>
    private void ApplyRawImage(RawImage rawImage, JsonData data)
    {
        if (data == null) return;

        string texturePath = GetString(data, "texture", "");
        if (!string.IsNullOrEmpty(texturePath))
        {
            Texture texture = Resources.Load<Texture>(texturePath);
            if (texture != null)
            {
                rawImage.texture = texture;
            }
        }

        rawImage.color = GetColor(data, "color", Color.white);
        rawImage.raycastTarget = GetBool(data, "raycastTarget", true);

        if (data.ContainsKey("uvRect"))
        {
            rawImage.uvRect = GetRect(data["uvRect"]);
        }
    }

    /// <summary>
    /// 应用 Text 数据。
    /// </summary>
    private void ApplyText(Text text, JsonData data)
    {
        if (data == null) return;

        text.text = GetString(data, "content", "");

        string fontPath = GetString(data, "font", "");
        if (!string.IsNullOrEmpty(fontPath))
        {
            // 尝试加载字体资源
            Font font = Resources.Load<Font>(fontPath);
            if (font != null)
            {
                text.font = font;
            }
        }

        text.fontSize = GetInt(data, "fontSize", 14);
        text.fontStyle = ParseFontStyle(GetString(data, "fontStyle", "Normal"));
        text.color = GetColor(data, "color", Color.black);
        text.alignment = ParseTextAnchor(GetString(data, "alignment", "MiddleCenter"));
        text.horizontalOverflow = ParseHorizontalWrapMode(GetString(data, "horizontalOverflow", "Wrap"));
        text.verticalOverflow = ParseVerticalWrapMode(GetString(data, "verticalOverflow", "Truncate"));
        text.lineSpacing = GetFloat(data, "lineSpacing", 1f);
        text.supportRichText = GetBool(data, "richText", true);
        text.raycastTarget = GetBool(data, "raycastTarget", true);
    }

    /// <summary>
    /// 应用 Button 数据。
    /// </summary>
    private void ApplyButton(Button button, JsonData data)
    {
        if (data == null) return;

        button.interactable = GetBool(data, "interactable", true);
        button.transition = ParseTransition(GetString(data, "transition", "ColorTint"));

        if (data.ContainsKey("colors"))
        {
            ColorBlock colors = button.colors;
            JsonData colorData = data["colors"];
            colors.normalColor = GetColor(colorData, "normalColor", Color.white);
            colors.highlightedColor = GetColor(colorData, "highlightedColor", Color.white);
            colors.pressedColor = GetColor(colorData, "pressedColor", Color.white);
            colors.disabledColor = GetColor(colorData, "disabledColor", Color.gray);
            colors.colorMultiplier = GetFloat(colorData, "colorMultiplier", 1f);
            colors.fadeDuration = GetFloat(colorData, "fadeDuration", 0.1f);
            button.colors = colors;
        }
    }

    /// <summary>
    /// 应用 Toggle 数据，并挂载 graphic 引用。
    /// </summary>
    private void ApplyToggle(Toggle toggle, JsonData data)
    {
        if (data == null) return;

        toggle.interactable = GetBool(data, "interactable", true);
        string targetGraphicName = GetString(data, "targetGraphic", "");
        if (!string.IsNullOrEmpty(targetGraphicName))
        {
            Transform t = FindChildRecursive(toggle.transform, targetGraphicName);
            if (t != null) toggle.targetGraphic = t.GetComponent<Graphic>();
        }
        toggle.isOn = GetBool(data, "isOn", false);
        toggle.toggleTransition = ParseToggleTransition(GetString(data, "toggleTransition", "Fade"));

        if (data.ContainsKey("colors"))
        {
            toggle.colors = ParseColorBlock(data["colors"]);
        }

        string graphicName = GetString(data, "graphic", "");
        if (!string.IsNullOrEmpty(graphicName))
        {
            Transform t = FindChildRecursive(toggle.transform, graphicName);
            if (t != null) toggle.graphic = t.GetComponent<Graphic>();
        }
    }

    /// <summary>
    /// 应用 Dropdown 数据，并挂载 captionText/itemText/template/targetGraphic 引用。
    /// </summary>
    private void ApplyDropdown(Dropdown dropdown, JsonData data)
    {
        if (data == null) return;

        dropdown.interactable = GetBool(data, "interactable", true);
        dropdown.value = GetInt(data, "value", 0);

        // 处理 options
        if (data.ContainsKey("options") && data["options"].IsArray)
        {
            dropdown.options.Clear();
            for (int i = 0; i < data["options"].Count; i++)
            {
                JsonData optionData = data["options"][i];
                string optionText = GetString(optionData, "text", "");

                Sprite optionSprite = null;
                string spritePath = GetString(optionData, "image", "");
                if (!string.IsNullOrEmpty(spritePath))
                {
                    optionSprite = Resources.Load<Sprite>(spritePath);
                }

                dropdown.options.Add(new Dropdown.OptionData(optionText, optionSprite));
            }
        }

        // 处理引用：支持名称或路径查找
        string captionTextName = GetString(data, "captionText", "");
        if (!string.IsNullOrEmpty(captionTextName))
        {
            Transform t = FindChildRecursive(dropdown.transform, captionTextName);
            if (t != null) dropdown.captionText = t.GetComponent<Text>();
        }

        string itemTextName = GetString(data, "itemText", "");
        if (!string.IsNullOrEmpty(itemTextName))
        {
            Transform t = FindChildRecursive(dropdown.transform, itemTextName);
            if (t != null) dropdown.itemText = t.GetComponent<Text>();
        }

        string templateName = GetString(data, "template", "");
        if (!string.IsNullOrEmpty(templateName))
        {
            Transform t = FindChildRecursive(dropdown.transform, templateName);
            if (t != null) dropdown.template = t.GetComponent<RectTransform>();
        }

        string targetGraphicName = GetString(data, "targetGraphic", "");
        if (!string.IsNullOrEmpty(targetGraphicName))
        {
            Transform t = FindChildRecursive(dropdown.transform, targetGraphicName);
            if (t != null) dropdown.targetGraphic = t.GetComponent<Graphic>();
        }
    }

    /// <summary>
    /// 应用 Slider 数据，并挂载 fillRect/handleRect 引用。
    /// </summary>
    private void ApplySlider(Slider slider, JsonData data)
    {
        if (data == null) return;

        slider.interactable = GetBool(data, "interactable", true);
        slider.transition = ParseTransition(GetString(data, "transition", "ColorTint"));
        slider.direction = ParseSliderDirection(GetString(data, "direction", "LeftToRight"));
        slider.minValue = GetFloat(data, "minValue", 0);
        slider.maxValue = GetFloat(data, "maxValue", 1);
        slider.wholeNumbers = GetBool(data, "wholeNumbers", false);
        slider.value = GetFloat(data, "value", 0);

        if (data.ContainsKey("colors"))
        {
            slider.colors = ParseColorBlock(data["colors"]);
        }

        string fillRectName = GetString(data, "fillRect", "");
        if (!string.IsNullOrEmpty(fillRectName))
        {
            Transform t = FindChildRecursive(slider.transform, fillRectName);
            if (t != null) slider.fillRect = t.GetComponent<RectTransform>();
        }

        string handleRectName = GetString(data, "handleRect", "");
        if (!string.IsNullOrEmpty(handleRectName))
        {
            Transform t = FindChildRecursive(slider.transform, handleRectName);
            if (t != null)
            {
                slider.targetGraphic = t.GetComponent<Graphic>();
                slider.handleRect = t.GetComponent<RectTransform>();
            }
        }
    }

    /// <summary>
    /// 应用 ScrollRect 数据，并挂载 content/viewport/scrollbar 引用。
    /// </summary>
    private void ApplyScrollRect(ScrollRect scrollRect, JsonData data)
    {
        if (data == null) return;

        scrollRect.horizontal = GetBool(data, "horizontal", false);
        scrollRect.vertical = GetBool(data, "vertical", true);
        scrollRect.movementType = ParseMovementType(GetString(data, "movementType", "Elastic"));
        scrollRect.elasticity = GetFloat(data, "elasticity", 0.1f);
        scrollRect.inertia = GetBool(data, "inertia", true);
        scrollRect.decelerationRate = GetFloat(data, "decelerationRate", 0.135f);
        scrollRect.scrollSensitivity = GetFloat(data, "scrollSensitivity", 1f);

        string viewportName = GetString(data, "viewport", "");
        if (!string.IsNullOrEmpty(viewportName))
        {
            Transform t = FindChildRecursive(scrollRect.transform, viewportName);
            if (t != null) scrollRect.viewport = t.GetComponent<RectTransform>();
        }

        string contentName = GetString(data, "content", "");
        if (!string.IsNullOrEmpty(contentName))
        {
            Transform t = FindChildRecursive(scrollRect.transform, contentName);
            if (t != null) scrollRect.content = t.GetComponent<RectTransform>();
        }

        string hScrollbarName = GetString(data, "horizontalScrollbar", "");
        if (!string.IsNullOrEmpty(hScrollbarName))
        {
            Transform t = FindChildRecursive(scrollRect.transform, hScrollbarName);
            if (t != null) scrollRect.horizontalScrollbar = t.GetComponent<Scrollbar>();
        }

        string vScrollbarName = GetString(data, "verticalScrollbar", "");
        if (!string.IsNullOrEmpty(vScrollbarName))
        {
            Transform t = FindChildRecursive(scrollRect.transform, vScrollbarName);
            if (t != null) scrollRect.verticalScrollbar = t.GetComponent<Scrollbar>();
        }
    }

    /// <summary>
    /// 应用 Scrollbar 数据，并挂载 handleRect 引用。
    /// </summary>
    private void ApplyScrollbar(Scrollbar scrollbar, JsonData data)
    {
        if (data == null) return;

        scrollbar.direction = ParseScrollbarDirection(GetString(data, "direction", "BottomToTop"));
        scrollbar.value = GetFloat(data, "value", 1f);
        scrollbar.size = GetFloat(data, "size", 0.5f);
        scrollbar.numberOfSteps = GetInt(data, "numberOfSteps", 0);

        if (data.ContainsKey("colors"))
        {
            scrollbar.colors = ParseColorBlock(data["colors"]);
        }

        string handleRectName = GetString(data, "handleRect", "");
        if (!string.IsNullOrEmpty(handleRectName))
        {
            Transform t = FindChildRecursive(scrollbar.transform, handleRectName);
            if (t != null) scrollbar.handleRect = t.GetComponent<RectTransform>();
        }
    }

    /// <summary>
    /// 递归查找子节点（包含自身），支持通过名称或路径（如 "Path/To/Child"）查找。
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string nameOrPath)
    {
        // 1. 检查自身是否匹配
        if (parent.name == nameOrPath) return parent;

        // 2. 尝试作为路径查找（查找直接或间接子节点）
        Transform target = parent.Find(nameOrPath);
        if (target != null) return target;

        // 3. 递归在子节点中查找
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, nameOrPath);
            if (result != null) return result;
        }
        return null;
    }

    /// <summary>
    /// 将 JSON 中的 colors 映射为 Unity 的 ColorBlock。
    /// </summary>
    private ColorBlock ParseColorBlock(JsonData data)
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        if (data == null) return colors;

        colors.normalColor = GetColor(data, "normalColor", Color.white);
        colors.highlightedColor = GetColor(data, "highlightedColor", Color.white);
        colors.pressedColor = GetColor(data, "pressedColor", Color.white);
        colors.disabledColor = GetColor(data, "disabledColor", Color.gray);
        colors.colorMultiplier = GetFloat(data, "colorMultiplier", 1f);
        colors.fadeDuration = GetFloat(data, "fadeDuration", 0.1f);

        if (data.ContainsKey("selectedColor"))
            colors.selectedColor = GetColor(data, "selectedColor", Color.white);

        return colors;
    }

    /// <summary>
    /// 从 JsonData 读取 string 字段。
    /// </summary>
    private string GetString(JsonData data, string key, string defaultValue)
    {
        if (data != null && data.ContainsKey(key) && data[key] != null && data[key].IsString)
            return (string)data[key];
        return defaultValue;
    }

    /// <summary>
    /// 从 JsonData 读取 int 字段（兼容 double）。
    /// </summary>
    private int GetInt(JsonData data, string key, int defaultValue)
    {
        if (data != null && data.ContainsKey(key) && data[key] != null && data[key].IsInt)
            return (int)data[key];
        return defaultValue;
    }

    /// <summary>
    /// 从 JsonData 读取 float 字段（兼容 int/double）。
    /// </summary>
    private float GetFloat(JsonData data, string key, float defaultValue)
    {
        if (data != null && data.ContainsKey(key))
        {
            if (data[key].IsDouble)
                return (float)(double)data[key];
            if (data[key].IsInt)
                return (float)(int)data[key];
        }
        return defaultValue;
    }

    /// <summary>
    /// 从 JsonData 读取 bool 字段。
    /// </summary>
    private bool GetBool(JsonData data, string key, bool defaultValue)
    {
        if (data != null && data.ContainsKey(key) && data[key].IsBoolean)
            return (bool)data[key];
        return defaultValue;
    }

    /// <summary>
    /// 从 JsonData 读取 Vector2 数组字段。
    /// </summary>
    private Vector2 GetVector2(JsonData data, string key, Vector2 defaultValue)
    {
        if (data != null && data.ContainsKey(key) && data[key].IsArray && data[key].Count >= 2)
        {
            float x = data[key][0].IsDouble ? (float)(double)data[key][0] : (int)data[key][0];
            float y = data[key][1].IsDouble ? (float)(double)data[key][1] : (int)data[key][1];
            return new Vector2(x, y);
        }
        return defaultValue;
    }

    /// <summary>
    /// 从 JsonData 读取 Vector3 数组字段。
    /// </summary>
    private Vector3 GetVector3(JsonData data, string key, Vector3 defaultValue)
    {
        if (data != null && data.ContainsKey(key) && data[key].IsArray && data[key].Count >= 3)
        {
            float x = data[key][0].IsDouble ? (float)(double)data[key][0] : (int)data[key][0];
            float y = data[key][1].IsDouble ? (float)(double)data[key][1] : (int)data[key][1];
            float z = data[key][2].IsDouble ? (float)(double)data[key][2] : (int)data[key][2];
            return new Vector3(x, y, z);
        }
        return defaultValue;
    }

    /// <summary>
    /// 从 JsonData 读取 Color 数组字段（RGBA）。
    /// </summary>
    private Color GetColor(JsonData data, string key, Color defaultValue)
    {
        if (data != null && data.ContainsKey(key) && data[key].IsArray && data[key].Count >= 4)
        {
            float r = data[key][0].IsDouble ? (float)(double)data[key][0] : (int)data[key][0];
            float g = data[key][1].IsDouble ? (float)(double)data[key][1] : (int)data[key][1];
            float b = data[key][2].IsDouble ? (float)(double)data[key][2] : (int)data[key][2];
            float a = data[key][3].IsDouble ? (float)(double)data[key][3] : (int)data[key][3];
            return new Color(r, g, b, a);
        }
        return defaultValue;
    }

    /// <summary>
    /// 从 JsonData 读取 Rect 对象字段。
    /// </summary>
    private Rect GetRect(JsonData data)
    {
        if (data != null)
        {
            float x = GetFloat(data, "x", 0);
            float y = GetFloat(data, "y", 0);
            float width = GetFloat(data, "width", 1);
            float height = GetFloat(data, "height", 1);
            return new Rect(x, y, width, height);
        }
        return new Rect(0, 0, 1, 1);
    }

    /// <summary>
    /// 解析 RenderMode。
    /// </summary>
    private RenderMode ParseRenderMode(string value)
    {
        switch (value)
        {
            case "ScreenSpaceOverlay": return RenderMode.ScreenSpaceOverlay;
            case "ScreenSpaceCamera": return RenderMode.ScreenSpaceCamera;
            case "WorldSpace": return RenderMode.WorldSpace;
            default: return RenderMode.ScreenSpaceOverlay;
        }
    }

    /// <summary>
    /// 解析 CanvasScaler.ScaleMode。
    /// </summary>
    private CanvasScaler.ScaleMode ParseUIScaleMode(string value)
    {
        switch (value)
        {
            case "ConstantPixelSize": return CanvasScaler.ScaleMode.ConstantPixelSize;
            case "ScaleWithScreenSize": return CanvasScaler.ScaleMode.ScaleWithScreenSize;
            case "ConstantPhysicalSize": return CanvasScaler.ScaleMode.ConstantPhysicalSize;
            default: return CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }
    }

    /// <summary>
    /// 解析 Image.Type。
    /// </summary>
    private Image.Type ParseImageType(string value)
    {
        switch (value)
        {
            case "Simple": return Image.Type.Simple;
            case "Sliced": return Image.Type.Sliced;
            case "Tiled": return Image.Type.Tiled;
            case "Filled": return Image.Type.Filled;
            default: return Image.Type.Simple;
        }
    }

    /// <summary>
    /// 解析 FontStyle。
    /// </summary>
    private FontStyle ParseFontStyle(string value)
    {
        switch (value)
        {
            case "Normal": return FontStyle.Normal;
            case "Bold": return FontStyle.Bold;
            case "Italic": return FontStyle.Italic;
            case "BoldAndItalic": return FontStyle.BoldAndItalic;
            default: return FontStyle.Normal;
        }
    }

    /// <summary>
    /// 解析 TextAnchor。
    /// </summary>
    private TextAnchor ParseTextAnchor(string value)
    {
        switch (value)
        {
            case "UpperLeft": return TextAnchor.UpperLeft;
            case "UpperCenter": return TextAnchor.UpperCenter;
            case "UpperRight": return TextAnchor.UpperRight;
            case "MiddleLeft": return TextAnchor.MiddleLeft;
            case "MiddleCenter": return TextAnchor.MiddleCenter;
            case "MiddleRight": return TextAnchor.MiddleRight;
            case "LowerLeft": return TextAnchor.LowerLeft;
            case "LowerCenter": return TextAnchor.LowerCenter;
            case "LowerRight": return TextAnchor.LowerRight;
            default: return TextAnchor.MiddleCenter;
        }
    }

    /// <summary>
    /// 解析 HorizontalWrapMode。
    /// </summary>
    private HorizontalWrapMode ParseHorizontalWrapMode(string value)
    {
        switch (value)
        {
            case "Wrap": return HorizontalWrapMode.Wrap;
            case "Overflow": return HorizontalWrapMode.Overflow;
            default: return HorizontalWrapMode.Wrap;
        }
    }

    /// <summary>
    /// 解析 VerticalWrapMode。
    /// </summary>
    private VerticalWrapMode ParseVerticalWrapMode(string value)
    {
        switch (value)
        {
            case "Truncate": return VerticalWrapMode.Truncate;
            case "Overflow": return VerticalWrapMode.Overflow;
            default: return VerticalWrapMode.Truncate;
        }
    }

    /// <summary>
    /// 解析 Selectable.Transition。
    /// </summary>
    private Selectable.Transition ParseTransition(string value)
    {
        switch (value)
        {
            case "None": return Selectable.Transition.None;
            case "ColorTint": return Selectable.Transition.ColorTint;
            case "SpriteSwap": return Selectable.Transition.SpriteSwap;
            case "Animation": return Selectable.Transition.Animation;
            default: return Selectable.Transition.ColorTint;
        }
    }

    /// <summary>
    /// 解析 Toggle.ToggleTransition。
    /// </summary>
    private Toggle.ToggleTransition ParseToggleTransition(string value)
    {
        switch (value)
        {
            case "None": return Toggle.ToggleTransition.None;
            case "Fade": return Toggle.ToggleTransition.Fade;
            default: return Toggle.ToggleTransition.Fade;
        }
    }

    /// <summary>
    /// 解析 ScrollRect.MovementType。
    /// </summary>
    private ScrollRect.MovementType ParseMovementType(string value)
    {
        switch (value)
        {
            case "Unrestricted": return ScrollRect.MovementType.Unrestricted;
            case "Elastic": return ScrollRect.MovementType.Elastic;
            case "Clamped": return ScrollRect.MovementType.Clamped;
            default: return ScrollRect.MovementType.Elastic;
        }
    }

    /// <summary>
    /// 解析 Scrollbar.Direction。
    /// </summary>
    private Scrollbar.Direction ParseScrollbarDirection(string value)
    {
        switch (value)
        {
            case "LeftToRight": return Scrollbar.Direction.LeftToRight;
            case "RightToLeft": return Scrollbar.Direction.RightToLeft;
            case "BottomToTop": return Scrollbar.Direction.BottomToTop;
            case "TopToBottom": return Scrollbar.Direction.TopToBottom;
            default: return Scrollbar.Direction.BottomToTop;
        }

    }

    /// <summary>
    /// 解析 Slider.Direction。
    /// </summary>
    private Slider.Direction ParseSliderDirection(string value)
    {
        switch (value)
        {
            case "LeftToRight": return Slider.Direction.LeftToRight;
            case "RightToLeft": return Slider.Direction.RightToLeft;
            case "BottomToTop": return Slider.Direction.BottomToTop;
            case "TopToBottom": return Slider.Direction.TopToBottom;
            default: return Slider.Direction.LeftToRight;
        }
    }

    /// <summary>
    /// 应用 InputField 数据，并挂载 textComponent/placeholder 引用。
    /// </summary>
    private void ApplyInputField(InputField inputField, JsonData data)
    {
        if (data == null) return;

        inputField.interactable = GetBool(data, "interactable", true);
        inputField.text = GetString(data, "text", "");
        inputField.characterLimit = GetInt(data, "characterLimit", 0);
        inputField.contentType = ParseContentType(GetString(data, "contentType", "Standard"));
        inputField.lineType = ParseLineType(GetString(data, "lineType", "SingleLine"));
        inputField.readOnly = GetBool(data, "readOnly", false);

        if (data.ContainsKey("caretBlinkRate"))
            inputField.caretBlinkRate = GetFloat(data, "caretBlinkRate", 0.85f);

        if (data.ContainsKey("caretWidth"))
            inputField.caretWidth = GetInt(data, "caretWidth", 1);

        if (data.ContainsKey("selectionColor"))
            inputField.selectionColor = GetColor(data, "selectionColor", new Color(0.65f, 0.8f, 1f, 0.5f));

        string textComponentName = GetString(data, "textComponent", "");
        if (!string.IsNullOrEmpty(textComponentName))
        {
            Transform textTrans = FindChildRecursive(inputField.transform, textComponentName);
            if (textTrans != null)
            {
                inputField.textComponent = textTrans.GetComponent<Text>();
                inputField.textComponent.supportRichText = false;
            }
        }

        string placeholderName = GetString(data, "placeholder", "");
        if (!string.IsNullOrEmpty(placeholderName))
        {
            Transform placeholderTrans = FindChildRecursive(inputField.transform, placeholderName);
            if (placeholderTrans != null)
            {
                inputField.placeholder = placeholderTrans.GetComponent<Text>();
            }
        }
    }

    /// <summary>
    /// 解析 InputField.ContentType。
    /// </summary>
    private InputField.ContentType ParseContentType(string value)
    {
        switch (value)
        {
            case "Standard": return InputField.ContentType.Standard;
            case "Autocorrected": return InputField.ContentType.Autocorrected;
            case "IntegerNumber": return InputField.ContentType.IntegerNumber;
            case "DecimalNumber": return InputField.ContentType.DecimalNumber;
            case "Alphanumeric": return InputField.ContentType.Alphanumeric;
            case "Name": return InputField.ContentType.Name;
            case "EmailAddress": return InputField.ContentType.EmailAddress;
            case "Password": return InputField.ContentType.Password;
            case "Pin": return InputField.ContentType.Pin;
            case "Custom": return InputField.ContentType.Custom;
            default: return InputField.ContentType.Standard;
        }
    }

    /// <summary>
    /// 解析 InputField.LineType。
    /// </summary>
    private InputField.LineType ParseLineType(string value)
    {
        switch (value)
        {
            case "SingleLine": return InputField.LineType.SingleLine;
            case "MultiLineSubmit": return InputField.LineType.MultiLineSubmit;
            case "MultiLineNewline": return InputField.LineType.MultiLineNewline;
            default: return InputField.LineType.SingleLine;
        }
    }

}
