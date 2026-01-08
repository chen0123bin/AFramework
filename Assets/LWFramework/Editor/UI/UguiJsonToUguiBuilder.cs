using System;
using LitJson;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 将约定格式的 UGUI JSON 转换为场景中的 UGUI 树。
/// </summary>
internal sealed class UguiJsonToUguiBuilder
{
    private readonly UguiJsonReader m_Reader;

    /// <summary>
    /// 创建一个默认的构建器实例。
    /// </summary>
    public static UguiJsonToUguiBuilder CreateDefault()
    {
        return new UguiJsonToUguiBuilder(new UguiJsonReader());
    }

    private UguiJsonToUguiBuilder(UguiJsonReader reader)
    {
        m_Reader = reader;
    }

    /// <summary>
    /// 根据 Root 节点数据创建 UGUI 树。
    /// </summary>
    /// <param name="rootData">Root 对象节点</param>
    /// <param name="parent">可选父节点；为空则放在场景根下</param>
    public GameObject Build(JsonData rootData, Transform parent)
    {
        if (rootData == null)
            return null;

        string rootName = m_Reader.GetString(rootData, UguiJsonSchema.KEY_NAME, "UGUIRoot");
        GameObject rootGo = new GameObject(rootName, typeof(RectTransform), typeof(CanvasGroup));
        SetParentKeepLocalZero(rootGo.transform, parent);

        if (m_Reader.TryGetValue(rootData, UguiJsonSchema.KEY_RECT_TRANSFORM, out JsonData rectData))
            ApplyRectTransform(rootGo.GetComponent<RectTransform>(), rectData);

        if (m_Reader.TryGetValue(rootData, UguiJsonSchema.KEY_CHILDREN, out JsonData childrenData))
            CreateChildren(childrenData, rootGo.transform);

        return rootGo;
    }

    /// <summary>
    /// 批量创建子节点。
    /// </summary>
    private void CreateChildren(JsonData childrenData, Transform parent)
    {
        if (childrenData == null || !childrenData.IsArray)
            return;

        for (int i = 0; i < childrenData.Count; i++)
        {
            JsonData childData = childrenData[i];
            if (childData == null)
                continue;

            CreateElement(childData, parent);
        }
    }

    /// <summary>
    /// 创建单个节点，并应用 RectTransform/组件配置。
    /// </summary>
    private GameObject CreateElement(JsonData elementData, Transform parent)
    {
        string typeName = m_Reader.GetString(elementData, UguiJsonSchema.KEY_TYPE, string.Empty);
        string name = m_Reader.GetString(elementData, UguiJsonSchema.KEY_NAME, string.IsNullOrEmpty(typeName) ? "Node" : typeName);
        bool active = m_Reader.GetBool(elementData, UguiJsonSchema.KEY_ACTIVE, true);

        GameObject go = new GameObject(name, typeof(RectTransform));
        SetParentKeepLocalZero(go.transform, parent);
        go.SetActive(active);

        if (m_Reader.TryGetValue(elementData, UguiJsonSchema.KEY_RECT_TRANSFORM, out JsonData rectData))
            ApplyRectTransform(go.GetComponent<RectTransform>(), rectData);

        ApplyComponents(go, elementData);

        if (m_Reader.TryGetValue(elementData, UguiJsonSchema.KEY_CHILDREN, out JsonData childrenData))
            CreateChildren(childrenData, go.transform);

        ResolveReferences(go, elementData);
        return go;
    }

    /// <summary>
    /// 根据节点 type 与对应数据块添加/配置 UGUI 组件。
    /// </summary>
    private void ApplyComponents(GameObject go, JsonData elementData)
    {
        string typeName = m_Reader.GetString(elementData, UguiJsonSchema.KEY_TYPE, string.Empty);

        if (typeName == "Text")
        {
            ApplyText(go, elementData);
            return;
        }

        if (typeName == "Image")
        {
            ApplyImage(go, elementData);
            return;
        }

        if (typeName == "Button")
        {
            ApplyImage(go, elementData);
            ApplyButton(go, elementData);
            return;
        }

        if (typeName == "Toggle")
        {
            ApplyImage(go, elementData);
            ApplyToggle(go, elementData);
            return;
        }

        if (typeName == "Slider")
        {
            ApplyImage(go, elementData);
            ApplySlider(go, elementData);
            return;
        }

        if (typeName == "InputField")
        {
            ApplyImage(go, elementData);
            ApplyInputField(go, elementData);
            return;
        }

        if (typeName == "ScrollRect")
        {
            ApplyImage(go, elementData);
            ApplyScrollRect(go, elementData);
            return;
        }

        if (typeName == "Mask")
        {
            ApplyImage(go, elementData);
            ApplyMask(go, elementData);
            return;
        }

        if (typeName == "Dropdown")
        {
            ApplyImage(go, elementData);
            ApplyDropdown(go, elementData);
        }
    }

    /// <summary>
    /// 统一处理跨子节点的引用（如 InputField.textComponent）。
    /// </summary>
    private void ResolveReferences(GameObject go, JsonData elementData)
    {
        if (go == null)
            return;

        if (m_Reader.TryGetValue(elementData, UguiJsonSchema.KEY_INPUT_FIELD, out JsonData inputFieldData))
            ResolveInputFieldReferences(go, inputFieldData);

        if (m_Reader.TryGetValue(elementData, UguiJsonSchema.KEY_SCROLL_RECT, out JsonData scrollRectData))
            ResolveScrollRectReferences(go, scrollRectData);

        if (m_Reader.TryGetValue(elementData, UguiJsonSchema.KEY_TOGGLE, out JsonData toggleData))
            ResolveToggleReferences(go, toggleData);

        if (m_Reader.TryGetValue(elementData, UguiJsonSchema.KEY_SLIDER, out JsonData sliderData))
            ResolveSliderReferences(go, sliderData);
    }

    /// <summary>
    /// 应用 RectTransform 的常用布局字段。
    /// </summary>
    private void ApplyRectTransform(RectTransform rect, JsonData rectData)
    {
        if (rect == null || rectData == null)
            return;

        if (m_Reader.TryGetValue(rectData, UguiJsonSchema.KEY_ANCHOR_MIN, out JsonData anchorMin))
            rect.anchorMin = m_Reader.GetVector2(anchorMin, rect.anchorMin);
        if (m_Reader.TryGetValue(rectData, UguiJsonSchema.KEY_ANCHOR_MAX, out JsonData anchorMax))
            rect.anchorMax = m_Reader.GetVector2(anchorMax, rect.anchorMax);
        if (m_Reader.TryGetValue(rectData, UguiJsonSchema.KEY_PIVOT, out JsonData pivot))
            rect.pivot = m_Reader.GetVector2(pivot, rect.pivot);

        if (m_Reader.TryGetValue(rectData, UguiJsonSchema.KEY_ANCHORED_POSITION, out JsonData anchoredPos))
            rect.anchoredPosition = m_Reader.GetVector2(anchoredPos, rect.anchoredPosition);
        if (m_Reader.TryGetValue(rectData, UguiJsonSchema.KEY_SIZE_DELTA, out JsonData sizeDelta))
            rect.sizeDelta = m_Reader.GetVector2(sizeDelta, rect.sizeDelta);
        if (m_Reader.TryGetValue(rectData, UguiJsonSchema.KEY_ROTATION, out JsonData rotation))
            rect.localEulerAngles = m_Reader.GetVector3(rotation, rect.localEulerAngles);
        if (m_Reader.TryGetValue(rectData, UguiJsonSchema.KEY_SCALE, out JsonData scale))
            rect.localScale = m_Reader.GetVector3(scale, rect.localScale);
    }

    /// <summary>
    /// 为节点添加并配置 Image。
    /// </summary>
    private void ApplyImage(GameObject go, JsonData elementData)
    {
        if (!m_Reader.TryGetValue(elementData, UguiJsonSchema.KEY_IMAGE, out JsonData imageData))
            return;

        Image image = GetOrAddComponent<Image>(go);
        image.raycastTarget = m_Reader.GetBool(imageData, "raycastTarget", image.raycastTarget);

        if (m_Reader.TryGetValue(imageData, "color", out JsonData colorData))
            image.color = m_Reader.GetColor(colorData, image.color);

        string spritePath = m_Reader.GetString(imageData, "sprite", string.Empty);
        if (!string.IsNullOrEmpty(spritePath))
            image.sprite = LoadSprite(spritePath);

        string imageType = m_Reader.GetString(imageData, "imageType", string.Empty);
        if (!string.IsNullOrEmpty(imageType))
            image.type = ParseEnumSafe(imageType, image.type);

        image.fillCenter = m_Reader.GetBool(imageData, "fillCenter", image.fillCenter);
        image.pixelsPerUnitMultiplier = m_Reader.GetFloat(imageData, "pixelsPerUnitMultiplier", image.pixelsPerUnitMultiplier);

        string materialPath = m_Reader.GetString(imageData, "material", string.Empty);
        if (!string.IsNullOrEmpty(materialPath))
            image.material = LoadMaterial(materialPath);
    }

    /// <summary>
    /// 为节点添加并配置 Text。
    /// </summary>
    private void ApplyText(GameObject go, JsonData elementData)
    {
        if (!m_Reader.TryGetValue(elementData, UguiJsonSchema.KEY_TEXT, out JsonData textData))
            return;

        Text text = GetOrAddComponent<Text>(go);
        text.text = m_Reader.GetString(textData, "content", text.text);
        text.fontSize = m_Reader.GetInt(textData, "fontSize", text.fontSize);
        text.raycastTarget = m_Reader.GetBool(textData, "raycastTarget", text.raycastTarget);
        text.supportRichText = m_Reader.GetBool(textData, "supportRichText", text.supportRichText);

        if (m_Reader.TryGetValue(textData, "color", out JsonData colorData))
            text.color = m_Reader.GetColor(colorData, text.color);

        string fontNameOrPath = m_Reader.GetString(textData, "font", string.Empty);
        if (!string.IsNullOrEmpty(fontNameOrPath))
            text.font = LoadFont(fontNameOrPath);

        string fontStyle = m_Reader.GetString(textData, "fontStyle", string.Empty);
        if (!string.IsNullOrEmpty(fontStyle))
            text.fontStyle = ParseEnumSafe(fontStyle, text.fontStyle);

        string alignment = m_Reader.GetString(textData, "alignment", string.Empty);
        if (!string.IsNullOrEmpty(alignment))
            text.alignment = ParseEnumSafe(alignment, text.alignment);

        string horizontalOverflow = m_Reader.GetString(textData, "horizontalOverflow", string.Empty);
        if (!string.IsNullOrEmpty(horizontalOverflow))
            text.horizontalOverflow = ParseEnumSafe(horizontalOverflow, text.horizontalOverflow);

        string verticalOverflow = m_Reader.GetString(textData, "verticalOverflow", string.Empty);
        if (!string.IsNullOrEmpty(verticalOverflow))
            text.verticalOverflow = ParseEnumSafe(verticalOverflow, text.verticalOverflow);
    }

    /// <summary>
    /// 为节点添加并配置 Button。
    /// </summary>
    private void ApplyButton(GameObject go, JsonData elementData)
    {
        if (!m_Reader.TryGetValue(elementData, UguiJsonSchema.KEY_BUTTON, out JsonData buttonData))
            return;

        Button button = GetOrAddComponent<Button>(go);
        button.interactable = m_Reader.GetBool(buttonData, "interactable", button.interactable);

        string transition = m_Reader.GetString(buttonData, "transition", string.Empty);
        if (!string.IsNullOrEmpty(transition))
            button.transition = ParseEnumSafe(transition, button.transition);

        if (m_Reader.TryGetValue(buttonData, "colors", out JsonData colorsData))
            button.colors = ReadColorBlock(colorsData, button.colors);
    }

    /// <summary>
    /// 为节点添加并配置 Toggle。
    /// </summary>
    private void ApplyToggle(GameObject go, JsonData elementData)
    {
        if (!m_Reader.TryGetValue(elementData, UguiJsonSchema.KEY_TOGGLE, out JsonData toggleData))
            return;

        Toggle toggle = GetOrAddComponent<Toggle>(go);
        toggle.interactable = m_Reader.GetBool(toggleData, "interactable", toggle.interactable);
        toggle.isOn = m_Reader.GetBool(toggleData, "isOn", toggle.isOn);

        string transition = m_Reader.GetString(toggleData, "transition", string.Empty);
        if (!string.IsNullOrEmpty(transition))
            toggle.transition = ParseEnumSafe(transition, toggle.transition);

        string toggleTransition = m_Reader.GetString(toggleData, "toggleTransition", string.Empty);
        if (!string.IsNullOrEmpty(toggleTransition))
            toggle.toggleTransition = ParseEnumSafe(toggleTransition, toggle.toggleTransition);

        if (m_Reader.TryGetValue(toggleData, "colors", out JsonData colorsData))
            toggle.colors = ReadColorBlock(colorsData, toggle.colors);
    }

    /// <summary>
    /// 为节点添加并配置 Slider。
    /// </summary>
    private void ApplySlider(GameObject go, JsonData elementData)
    {
        if (!m_Reader.TryGetValue(elementData, UguiJsonSchema.KEY_SLIDER, out JsonData sliderData))
            return;

        Slider slider = GetOrAddComponent<Slider>(go);
        slider.interactable = m_Reader.GetBool(sliderData, "interactable", slider.interactable);

        string transition = m_Reader.GetString(sliderData, "transition", string.Empty);
        if (!string.IsNullOrEmpty(transition))
            slider.transition = ParseEnumSafe(transition, slider.transition);

        string direction = m_Reader.GetString(sliderData, "direction", string.Empty);
        if (!string.IsNullOrEmpty(direction))
            slider.direction = ParseEnumSafe(direction, slider.direction);

        slider.minValue = m_Reader.GetFloat(sliderData, "minValue", slider.minValue);
        slider.maxValue = m_Reader.GetFloat(sliderData, "maxValue", slider.maxValue);
        slider.wholeNumbers = m_Reader.GetBool(sliderData, "wholeNumbers", slider.wholeNumbers);
        slider.value = m_Reader.GetFloat(sliderData, "value", slider.value);
    }

    /// <summary>
    /// 为节点添加并配置 InputField。
    /// </summary>
    private void ApplyInputField(GameObject go, JsonData elementData)
    {
        if (!m_Reader.TryGetValue(elementData, UguiJsonSchema.KEY_INPUT_FIELD, out JsonData inputFieldData))
            return;

        InputField inputField = GetOrAddComponent<InputField>(go);
        inputField.interactable = m_Reader.GetBool(inputFieldData, "interactable", inputField.interactable);
        inputField.text = m_Reader.GetString(inputFieldData, "text", inputField.text);
        inputField.characterLimit = m_Reader.GetInt(inputFieldData, "characterLimit", inputField.characterLimit);
        inputField.readOnly = m_Reader.GetBool(inputFieldData, "readOnly", inputField.readOnly);
        inputField.caretBlinkRate = m_Reader.GetFloat(inputFieldData, "caretBlinkRate", inputField.caretBlinkRate);
        inputField.caretWidth = m_Reader.GetInt(inputFieldData, "caretWidth", inputField.caretWidth);

        if (m_Reader.TryGetValue(inputFieldData, "selectionColor", out JsonData selectionColorData))
            inputField.selectionColor = m_Reader.GetColor(selectionColorData, inputField.selectionColor);

        string contentType = m_Reader.GetString(inputFieldData, "contentType", string.Empty);
        if (!string.IsNullOrEmpty(contentType))
            inputField.contentType = ParseEnumSafe(contentType, inputField.contentType);

        string lineType = m_Reader.GetString(inputFieldData, "lineType", string.Empty);
        if (!string.IsNullOrEmpty(lineType))
            inputField.lineType = ParseEnumSafe(lineType, inputField.lineType);
    }

    /// <summary>
    /// 为节点添加并配置 ScrollRect。
    /// </summary>
    private void ApplyScrollRect(GameObject go, JsonData elementData)
    {
        if (!m_Reader.TryGetValue(elementData, UguiJsonSchema.KEY_SCROLL_RECT, out JsonData scrollRectData))
            return;

        ScrollRect scrollRect = GetOrAddComponent<ScrollRect>(go);
        scrollRect.horizontal = m_Reader.GetBool(scrollRectData, "horizontal", scrollRect.horizontal);
        scrollRect.vertical = m_Reader.GetBool(scrollRectData, "vertical", scrollRect.vertical);

        string movementType = m_Reader.GetString(scrollRectData, "movementType", string.Empty);
        if (!string.IsNullOrEmpty(movementType))
            scrollRect.movementType = ParseEnumSafe(movementType, scrollRect.movementType);

        scrollRect.elasticity = m_Reader.GetFloat(scrollRectData, "elasticity", scrollRect.elasticity);
        scrollRect.inertia = m_Reader.GetBool(scrollRectData, "inertia", scrollRect.inertia);
        scrollRect.decelerationRate = m_Reader.GetFloat(scrollRectData, "decelerationRate", scrollRect.decelerationRate);
        scrollRect.scrollSensitivity = m_Reader.GetFloat(scrollRectData, "scrollSensitivity", scrollRect.scrollSensitivity);
    }

    /// <summary>
    /// 为节点添加并配置 Mask。
    /// </summary>
    private void ApplyMask(GameObject go, JsonData elementData)
    {
        Mask mask = GetOrAddComponent<Mask>(go);
        if (m_Reader.TryGetValue(elementData, UguiJsonSchema.KEY_MASK, out JsonData maskData))
            mask.showMaskGraphic = m_Reader.GetBool(maskData, "showMaskGraphic", mask.showMaskGraphic);
    }

    /// <summary>
    /// 为节点添加并配置 Dropdown（简化版）。
    /// </summary>
    private void ApplyDropdown(GameObject go, JsonData elementData)
    {
        if (!m_Reader.TryGetValue(elementData, UguiJsonSchema.KEY_DROPDOWN, out JsonData dropdownData))
            return;

        Dropdown dropdown = GetOrAddComponent<Dropdown>(go);
        dropdown.interactable = m_Reader.GetBool(dropdownData, "interactable", dropdown.interactable);

        string transition = m_Reader.GetString(dropdownData, "transition", string.Empty);
        if (!string.IsNullOrEmpty(transition))
            dropdown.transition = ParseEnumSafe(transition, dropdown.transition);

        if (m_Reader.TryGetValue(dropdownData, "colors", out JsonData colorsData))
            dropdown.colors = ReadColorBlock(colorsData, dropdown.colors);
    }

    /// <summary>
    /// 解析并应用 Toggle 中的图形引用。
    /// </summary>
    private void ResolveToggleReferences(GameObject go, JsonData toggleData)
    {
        Toggle toggle = go.GetComponent<Toggle>();
        if (toggle == null)
            return;

        string targetGraphicName = m_Reader.GetString(toggleData, "targetGraphic", string.Empty);
        if (!string.IsNullOrEmpty(targetGraphicName))
        {
            Graphic g = FindGraphicByName(go.transform, targetGraphicName);
            if (g != null)
                toggle.targetGraphic = g;
        }

        string graphicName = m_Reader.GetString(toggleData, "graphic", string.Empty);
        if (!string.IsNullOrEmpty(graphicName))
        {
            Graphic g = FindGraphicByName(go.transform, graphicName);
            if (g != null)
                toggle.graphic = g;
        }
    }

    /// <summary>
    /// 解析并应用 Slider 中的 RectTransform 引用。
    /// </summary>
    private void ResolveSliderReferences(GameObject go, JsonData sliderData)
    {
        Slider slider = go.GetComponent<Slider>();
        if (slider == null)
            return;

        string fillRectName = m_Reader.GetString(sliderData, "fillRect", string.Empty);
        if (!string.IsNullOrEmpty(fillRectName))
        {
            RectTransform t = FindRectTransformByName(go.transform, fillRectName);
            if (t != null)
                slider.fillRect = t;
        }

        string handleRectName = m_Reader.GetString(sliderData, "handleRect", string.Empty);
        if (!string.IsNullOrEmpty(handleRectName))
        {
            RectTransform t = FindRectTransformByName(go.transform, handleRectName);
            if (t != null)
                slider.handleRect = t;
        }
    }

    /// <summary>
    /// 解析并应用 InputField 中的组件引用。
    /// </summary>
    private void ResolveInputFieldReferences(GameObject go, JsonData inputFieldData)
    {
        InputField inputField = go.GetComponent<InputField>();
        if (inputField == null)
            return;

        string textComponentName = m_Reader.GetString(inputFieldData, "textComponent", string.Empty);
        if (!string.IsNullOrEmpty(textComponentName))
        {
            Text t = FindComponentByName<Text>(go.transform, textComponentName);
            if (t != null)
                inputField.textComponent = t;
        }

        string placeholderName = m_Reader.GetString(inputFieldData, "placeholder", string.Empty);
        if (!string.IsNullOrEmpty(placeholderName))
        {
            Graphic g = FindGraphicByName(go.transform, placeholderName);
            if (g != null)
                inputField.placeholder = g;
        }
    }

    /// <summary>
    /// 解析并应用 ScrollRect 中的节点引用。
    /// </summary>
    private void ResolveScrollRectReferences(GameObject go, JsonData scrollRectData)
    {
        ScrollRect scrollRect = go.GetComponent<ScrollRect>();
        if (scrollRect == null)
            return;

        string contentName = m_Reader.GetString(scrollRectData, "content", string.Empty);
        if (!string.IsNullOrEmpty(contentName))
        {
            RectTransform t = FindRectTransformByName(go.transform, contentName);
            if (t != null)
                scrollRect.content = t;
        }

        string viewportName = m_Reader.GetString(scrollRectData, "viewport", string.Empty);
        if (!string.IsNullOrEmpty(viewportName))
        {
            RectTransform t = FindRectTransformByName(go.transform, viewportName);
            if (t != null)
                scrollRect.viewport = t;
        }

        string horizontalScrollbarName = m_Reader.GetString(scrollRectData, "horizontalScrollbar", string.Empty);
        if (!string.IsNullOrEmpty(horizontalScrollbarName))
        {
            Scrollbar sb = FindComponentByName<Scrollbar>(go.transform, horizontalScrollbarName);
            if (sb != null)
                scrollRect.horizontalScrollbar = sb;
        }

        string verticalScrollbarName = m_Reader.GetString(scrollRectData, "verticalScrollbar", string.Empty);
        if (!string.IsNullOrEmpty(verticalScrollbarName))
        {
            Scrollbar sb = FindComponentByName<Scrollbar>(go.transform, verticalScrollbarName);
            if (sb != null)
                scrollRect.verticalScrollbar = sb;
        }
    }

    /// <summary>
    /// 读取 Selectable 的 ColorBlock。
    /// </summary>
    private ColorBlock ReadColorBlock(JsonData colorsData, ColorBlock defaultValue)
    {
        ColorBlock block = defaultValue;

        if (m_Reader.TryGetValue(colorsData, "normalColor", out JsonData normal))
            block.normalColor = m_Reader.GetColor(normal, block.normalColor);
        if (m_Reader.TryGetValue(colorsData, "highlightedColor", out JsonData highlighted))
            block.highlightedColor = m_Reader.GetColor(highlighted, block.highlightedColor);
        if (m_Reader.TryGetValue(colorsData, "pressedColor", out JsonData pressed))
            block.pressedColor = m_Reader.GetColor(pressed, block.pressedColor);
        if (m_Reader.TryGetValue(colorsData, "selectedColor", out JsonData selected))
            block.selectedColor = m_Reader.GetColor(selected, block.selectedColor);
        if (m_Reader.TryGetValue(colorsData, "disabledColor", out JsonData disabled))
            block.disabledColor = m_Reader.GetColor(disabled, block.disabledColor);

        block.colorMultiplier = m_Reader.GetFloat(colorsData, "colorMultiplier", block.colorMultiplier);
        block.fadeDuration = m_Reader.GetFloat(colorsData, "fadeDuration", block.fadeDuration);
        return block;
    }

    /// <summary>
    /// 将 Transform 挂到父节点，并保持本地坐标为零。
    /// </summary>
    private void SetParentKeepLocalZero(Transform child, Transform parent)
    {
        child.SetParent(parent, false);
        RectTransform rect = child as RectTransform;
        if (rect != null)
        {
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }
        else
        {
            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// 获取或添加组件，避免重复 AddComponent。
    /// </summary>
    private static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp != null)
            return comp;
        return go.AddComponent<T>();
    }

    /// <summary>
    /// 加载 Sprite：优先按 Assets 路径，其次按 Resources。
    /// </summary>
    private static Sprite LoadSprite(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
                return AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(tex));
        }

        string resourcesKey = StripExtension(path);
        return Resources.Load<Sprite>(resourcesKey);
    }

    /// <summary>
    /// 加载 Material：优先按 Assets 路径，其次按 Resources。
    /// </summary>
    private static Material LoadMaterial(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            return AssetDatabase.LoadAssetAtPath<Material>(path);

        return Resources.Load<Material>(StripExtension(path));
    }

    /// <summary>
    /// 加载 Font：支持内置字体名/Assets 路径/Resources。
    /// </summary>
    private static Font LoadFont(string fontNameOrPath)
    {
        if (string.IsNullOrEmpty(fontNameOrPath))
            return null;

        if (fontNameOrPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            return AssetDatabase.LoadAssetAtPath<Font>(fontNameOrPath);

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    /// <summary>
    /// 安全解析枚举：解析失败则返回默认值。
    /// </summary>
    private static TEnum ParseEnumSafe<TEnum>(string value, TEnum defaultValue) where TEnum : struct
    {
        if (string.IsNullOrEmpty(value))
            return defaultValue;

        if (Enum.TryParse(value, true, out TEnum parsed))
            return parsed;

        return defaultValue;
    }

    /// <summary>
    /// 在子节点树中按名字查找指定组件。
    /// </summary>
    private static T FindComponentByName<T>(Transform root, string name) where T : Component
    {
        Transform t = FindTransformByName(root, name);
        return t != null ? t.GetComponent<T>() : null;
    }

    /// <summary>
    /// 在子节点树中按名字查找 Graphic。
    /// </summary>
    private static Graphic FindGraphicByName(Transform root, string name)
    {
        Transform t = FindTransformByName(root, name);
        return t != null ? t.GetComponent<Graphic>() : null;
    }

    /// <summary>
    /// 在子节点树中按名字查找 RectTransform。
    /// </summary>
    private static RectTransform FindRectTransformByName(Transform root, string name)
    {
        Transform t = FindTransformByName(root, name);
        return t as RectTransform;
    }

    /// <summary>
    /// 深度优先在子节点树中查找指定名字的 Transform。
    /// </summary>
    private static Transform FindTransformByName(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name))
            return null;

        if (root.name == name)
            return root;

        int childCount = root.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform found = FindTransformByName(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    /// <summary>
    /// 去掉路径的扩展名，作为 Resources Key 使用。
    /// </summary>
    private static string StripExtension(string path)
    {
        int dot = path.LastIndexOf('.');
        if (dot <= 0)
            return path;
        return path.Substring(0, dot);
    }
}

