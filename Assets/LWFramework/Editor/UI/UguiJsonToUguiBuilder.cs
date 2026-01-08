using System;
using System.Collections.Generic;
using LitJson;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
#if TMPRO
using TMPro;
#endif

/// <summary>
/// 将约定格式的 UGUI JSON 转换为场景中的 UGUI 树。
/// </summary>
internal sealed class UguiJsonToUguiBuilder
{
    private readonly UguiJsonReader m_Reader;
    private readonly Dictionary<string, Action<GameObject, JsonData>> m_ComponentAppliers;

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
        m_ComponentAppliers = new Dictionary<string, Action<GameObject, JsonData>>(StringComparer.OrdinalIgnoreCase);
        RegisterDefaultComponentAppliers();
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
        if (!m_Reader.TryGetValue(elementData, UguiJsonSchema.KEY_COMPONENTS, out JsonData componentsData) || componentsData == null || !componentsData.IsArray)
            return;

        for (int i = 0; i < componentsData.Count; i++)
        {
            JsonData componentData = componentsData[i];
            if (componentData == null || !componentData.IsObject)
                continue;

            string componentType = m_Reader.GetString(componentData, UguiJsonSchema.KEY_TYPE, string.Empty);
            if (string.IsNullOrEmpty(componentType))
                continue;

            JsonData data;
            if (!m_Reader.TryGetValue(componentData, UguiJsonSchema.KEY_DATA, out data))
                data = null;

            ApplyComponent(go, componentType, data);
        }
    }

    /// <summary>
    /// 统一处理跨子节点的引用（如 InputField.textComponent）。
    /// </summary>
    private void ResolveReferences(GameObject go, JsonData elementData)
    {
        if (go == null)
            return;

        if (!m_Reader.TryGetValue(elementData, UguiJsonSchema.KEY_COMPONENTS, out JsonData componentsData) || componentsData == null || !componentsData.IsArray)
            return;

        for (int i = 0; i < componentsData.Count; i++)
        {
            JsonData componentData = componentsData[i];
            if (componentData == null || !componentData.IsObject)
                continue;

            string componentType = m_Reader.GetString(componentData, UguiJsonSchema.KEY_TYPE, string.Empty);
            if (string.IsNullOrEmpty(componentType))
                continue;

            JsonData data;
            if (!m_Reader.TryGetValue(componentData, UguiJsonSchema.KEY_DATA, out data))
                data = null;

            ResolveComponentReferences(go, componentType, data);
        }
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
    private void ApplyImage(GameObject go, JsonData imageData)
    {
        if (imageData == null)
            return;

        Image image = GetOrAddComponent<Image>(go);
        image.raycastTarget = m_Reader.GetBool(imageData, "raycastTarget", image.raycastTarget);

        if (m_Reader.TryGetValue(imageData, "color", out JsonData colorData))
            image.color = m_Reader.GetColor(colorData, image.color);

        string spritePath = NormalizeNullableString(m_Reader.GetString(imageData, "sprite", string.Empty));
        if (!string.IsNullOrEmpty(spritePath))
            image.sprite = LoadSprite(spritePath);

        string imageType = m_Reader.GetString(imageData, "imageType", string.Empty);
        if (!string.IsNullOrEmpty(imageType))
            image.type = ParseEnumSafe(imageType, image.type);

        image.fillCenter = m_Reader.GetBool(imageData, "fillCenter", image.fillCenter);
        image.pixelsPerUnitMultiplier = m_Reader.GetFloat(imageData, "pixelsPerUnitMultiplier", image.pixelsPerUnitMultiplier);
        image.preserveAspect = m_Reader.GetBool(imageData, "preserveAspect", image.preserveAspect);

        string fillMethod = m_Reader.GetString(imageData, "fillMethod", string.Empty);
        if (!string.IsNullOrEmpty(fillMethod))
            image.fillMethod = ParseEnumSafe(fillMethod, image.fillMethod);

        image.fillAmount = m_Reader.GetFloat(imageData, "fillAmount", image.fillAmount);
        image.fillClockwise = m_Reader.GetBool(imageData, "fillClockwise", image.fillClockwise);
        image.fillOrigin = m_Reader.GetInt(imageData, "fillOrigin", image.fillOrigin);

        string materialPath = NormalizeNullableString(m_Reader.GetString(imageData, "material", string.Empty));
        if (!string.IsNullOrEmpty(materialPath))
            image.material = LoadMaterial(materialPath);
    }

    /// <summary>
    /// 为节点添加并配置 Text。
    /// </summary>
    private void ApplyText(GameObject go, JsonData textData)
    {
        if (textData == null)
            return;

        Text text = GetOrAddComponent<Text>(go);
        text.text = m_Reader.GetString(textData, "content", text.text);
        text.fontSize = m_Reader.GetInt(textData, "fontSize", text.fontSize);
        text.raycastTarget = m_Reader.GetBool(textData, "raycastTarget", text.raycastTarget);
        text.supportRichText = m_Reader.GetBool(textData, "supportRichText", m_Reader.GetBool(textData, "richText", text.supportRichText));

        if (m_Reader.TryGetValue(textData, "color", out JsonData colorData))
            text.color = m_Reader.GetColor(colorData, text.color);

        string fontNameOrPath = NormalizeNullableString(m_Reader.GetString(textData, "font", string.Empty));
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

        text.lineSpacing = m_Reader.GetFloat(textData, "lineSpacing", text.lineSpacing);
    }

    private void ApplyTextMeshProUGUI(GameObject go, JsonData textData)
    {
        if (textData == null)
            return;

#if TMPRO
        TextMeshProUGUI tmp = GetOrAddComponent<TextMeshProUGUI>(go);
        tmp.text = m_Reader.GetString(textData, "content", tmp.text);
        tmp.fontSize = m_Reader.GetFloat(textData, "fontSize", tmp.fontSize);
        tmp.raycastTarget = m_Reader.GetBool(textData, "raycastTarget", tmp.raycastTarget);
        tmp.richText = m_Reader.GetBool(textData, "richText", m_Reader.GetBool(textData, "supportRichText", tmp.richText));

        if (m_Reader.TryGetValue(textData, "color", out JsonData colorData))
            tmp.color = m_Reader.GetColor(colorData, tmp.color);

        string fontNameOrPath = NormalizeNullableString(m_Reader.GetString(textData, "font", string.Empty));
        if (!string.IsNullOrEmpty(fontNameOrPath))
        {
            TMP_FontAsset font = LoadTmpFontAsset(fontNameOrPath);
            if (font != null)
                tmp.font = font;
        }

        string fontStyle = m_Reader.GetString(textData, "fontStyle", string.Empty);
        if (!string.IsNullOrEmpty(fontStyle))
            tmp.fontStyle = ParseEnumSafe(fontStyle, tmp.fontStyle);

        string alignment = m_Reader.GetString(textData, "alignment", string.Empty);
        if (!string.IsNullOrEmpty(alignment))
            tmp.alignment = ParseEnumSafe(alignment, tmp.alignment);
#else
        ApplyText(go, textData);
#endif
    }

    private void ApplyButton(GameObject go, JsonData buttonData)
    {
        if (buttonData == null)
            return;

        Button button = GetOrAddComponent<Button>(go);
        button.interactable = m_Reader.GetBool(buttonData, "interactable", button.interactable);

        string transition = m_Reader.GetString(buttonData, "transition", string.Empty);
        if (!string.IsNullOrEmpty(transition))
            button.transition = ParseEnumSafe(transition, button.transition);

        if (m_Reader.TryGetValue(buttonData, "colors", out JsonData colorsData))
            button.colors = ReadColorBlock(colorsData, button.colors);
    }

    private void ApplyToggle(GameObject go, JsonData toggleData)
    {
        if (toggleData == null)
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

    private void ApplySlider(GameObject go, JsonData sliderData)
    {
        if (sliderData == null)
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

    private void ApplyInputField(GameObject go, JsonData inputFieldData)
    {
        if (inputFieldData == null)
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

    private void ApplyScrollRect(GameObject go, JsonData scrollRectData)
    {
        if (scrollRectData == null)
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

    private void ApplyMask(GameObject go, JsonData maskData)
    {
        if (maskData == null)
            return;

        Mask mask = GetOrAddComponent<Mask>(go);
        mask.showMaskGraphic = m_Reader.GetBool(maskData, "showMaskGraphic", mask.showMaskGraphic);
    }

    private void ApplyDropdown(GameObject go, JsonData dropdownData)
    {
        if (dropdownData == null)
            return;

        Dropdown dropdown = GetOrAddComponent<Dropdown>(go);
        dropdown.interactable = m_Reader.GetBool(dropdownData, "interactable", dropdown.interactable);
        // 处理 options
        if (dropdownData.ContainsKey("options") && dropdownData["options"].IsArray)
        {
            dropdown.options.Clear();
            for (int i = 0; i < dropdownData["options"].Count; i++)
            {
                JsonData optionData = dropdownData["options"][i];
                string optionText = m_Reader.GetString(optionData, "text", "");

                Sprite optionSprite = null;
                string spritePath = NormalizeNullableString(m_Reader.GetString(optionData, "image", ""));
                if (!string.IsNullOrEmpty(spritePath))
                    optionSprite = LoadSprite(spritePath);

                dropdown.options.Add(new Dropdown.OptionData(optionText, optionSprite));
            }
        }

        dropdown.value = m_Reader.GetInt(dropdownData, "value", 0);

        string transition = m_Reader.GetString(dropdownData, "transition", string.Empty);
        if (!string.IsNullOrEmpty(transition))
            dropdown.transition = ParseEnumSafe(transition, dropdown.transition);

        if (m_Reader.TryGetValue(dropdownData, "colors", out JsonData colorsData))
            dropdown.colors = ReadColorBlock(colorsData, dropdown.colors);
    }

    private void ApplyRawImage(GameObject go, JsonData rawImageData)
    {
        if (rawImageData == null)
            return;

        RawImage rawImage = GetOrAddComponent<RawImage>(go);
        rawImage.raycastTarget = m_Reader.GetBool(rawImageData, "raycastTarget", rawImage.raycastTarget);

        if (m_Reader.TryGetValue(rawImageData, "color", out JsonData colorData))
            rawImage.color = m_Reader.GetColor(colorData, rawImage.color);

        string texturePath = NormalizeNullableString(m_Reader.GetString(rawImageData, "texture", string.Empty));
        if (!string.IsNullOrEmpty(texturePath))
            rawImage.texture = LoadTexture2D(texturePath);

        string materialPath = NormalizeNullableString(m_Reader.GetString(rawImageData, "material", string.Empty));
        if (!string.IsNullOrEmpty(materialPath))
            rawImage.material = LoadMaterial(materialPath);

        if (m_Reader.TryGetValue(rawImageData, "uvRect", out JsonData uvRectData))
            rawImage.uvRect = ReadRect(uvRectData, rawImage.uvRect);
    }

    private void ApplyContentSizeFitter(GameObject go, JsonData fitterData)
    {
        if (fitterData == null)
            return;

        ContentSizeFitter fitter = GetOrAddComponent<ContentSizeFitter>(go);

        string horizontalFit = m_Reader.GetString(fitterData, "horizontalFit", string.Empty);
        if (!string.IsNullOrEmpty(horizontalFit))
            fitter.horizontalFit = ParseEnumSafe(horizontalFit, fitter.horizontalFit);

        string verticalFit = m_Reader.GetString(fitterData, "verticalFit", string.Empty);
        if (!string.IsNullOrEmpty(verticalFit))
            fitter.verticalFit = ParseEnumSafe(verticalFit, fitter.verticalFit);
    }

    private void ApplyHorizontalLayoutGroup(GameObject go, JsonData layoutGroupData)
    {
        HorizontalLayoutGroup layoutGroup = GetOrAddComponent<HorizontalLayoutGroup>(go);
        ApplyLayoutGroupCommon(layoutGroup, layoutGroupData);
        layoutGroup.reverseArrangement = m_Reader.GetBool(layoutGroupData, "reverseArrangement", layoutGroup.reverseArrangement);
    }

    private void ApplyVerticalLayoutGroup(GameObject go, JsonData layoutGroupData)
    {
        VerticalLayoutGroup layoutGroup = GetOrAddComponent<VerticalLayoutGroup>(go);
        ApplyLayoutGroupCommon(layoutGroup, layoutGroupData);
        layoutGroup.reverseArrangement = m_Reader.GetBool(layoutGroupData, "reverseArrangement", layoutGroup.reverseArrangement);
    }

    private void ApplyGridLayoutGroup(GameObject go, JsonData layoutGroupData)
    {
        GridLayoutGroup grid = GetOrAddComponent<GridLayoutGroup>(go);
        ApplyLayoutGroupCommon(grid, layoutGroupData);

        if (layoutGroupData == null)
            return;

        if (m_Reader.TryGetValue(layoutGroupData, "cellSize", out JsonData cellSizeData))
            grid.cellSize = m_Reader.GetVector2(cellSizeData, grid.cellSize);
        if (m_Reader.TryGetValue(layoutGroupData, "spacing", out JsonData spacingData) && spacingData.IsArray)
            grid.spacing = m_Reader.GetVector2(spacingData, grid.spacing);

        string startCorner = m_Reader.GetString(layoutGroupData, "startCorner", string.Empty);
        if (!string.IsNullOrEmpty(startCorner))
            grid.startCorner = ParseEnumSafe(startCorner, grid.startCorner);

        string startAxis = m_Reader.GetString(layoutGroupData, "startAxis", string.Empty);
        if (!string.IsNullOrEmpty(startAxis))
            grid.startAxis = ParseEnumSafe(startAxis, grid.startAxis);

        string constraint = m_Reader.GetString(layoutGroupData, "constraint", string.Empty);
        if (!string.IsNullOrEmpty(constraint))
            grid.constraint = ParseEnumSafe(constraint, grid.constraint);

        grid.constraintCount = m_Reader.GetInt(layoutGroupData, "constraintCount", grid.constraintCount);
    }

    private void ApplyLayoutGroupCommon(LayoutGroup layoutGroup, JsonData layoutGroupData)
    {
        if (layoutGroupData == null)
            return;

        if (m_Reader.TryGetValue(layoutGroupData, "padding", out JsonData paddingData))
            layoutGroup.padding = ReadRectOffset(paddingData, layoutGroup.padding);

        string childAlignment = m_Reader.GetString(layoutGroupData, "childAlignment", string.Empty);
        if (!string.IsNullOrEmpty(childAlignment))
            layoutGroup.childAlignment = ParseEnumSafe(childAlignment, layoutGroup.childAlignment);

        if (layoutGroup is HorizontalOrVerticalLayoutGroup hv)
        {
            hv.spacing = m_Reader.GetFloat(layoutGroupData, "spacing", hv.spacing);
            hv.childControlWidth = m_Reader.GetBool(layoutGroupData, "childControlWidth", hv.childControlWidth);
            hv.childControlHeight = m_Reader.GetBool(layoutGroupData, "childControlHeight", hv.childControlHeight);
            hv.childForceExpandWidth = m_Reader.GetBool(layoutGroupData, "childForceExpandWidth", hv.childForceExpandWidth);
            hv.childForceExpandHeight = m_Reader.GetBool(layoutGroupData, "childForceExpandHeight", hv.childForceExpandHeight);
            hv.childScaleWidth = m_Reader.GetBool(layoutGroupData, "childScaleWidth", hv.childScaleWidth);
            hv.childScaleHeight = m_Reader.GetBool(layoutGroupData, "childScaleHeight", hv.childScaleHeight);
        }
        else if (layoutGroup is GridLayoutGroup grid)
        {
            if (m_Reader.TryGetValue(layoutGroupData, "spacing", out JsonData spacingData) && spacingData.IsArray)
                grid.spacing = m_Reader.GetVector2(spacingData, grid.spacing);
        }
    }

    private void ApplyScrollbar(GameObject go, JsonData scrollbarData)
    {
        if (scrollbarData == null)
            return;

        Scrollbar scrollbar = GetOrAddComponent<Scrollbar>(go);
        scrollbar.interactable = m_Reader.GetBool(scrollbarData, "interactable", scrollbar.interactable);

        string transition = m_Reader.GetString(scrollbarData, "transition", string.Empty);
        if (!string.IsNullOrEmpty(transition))
            scrollbar.transition = ParseEnumSafe(transition, scrollbar.transition);

        if (m_Reader.TryGetValue(scrollbarData, "colors", out JsonData colorsData))
            scrollbar.colors = ReadColorBlock(colorsData, scrollbar.colors);

        string direction = m_Reader.GetString(scrollbarData, "direction", string.Empty);
        if (!string.IsNullOrEmpty(direction))
            scrollbar.direction = ParseEnumSafe(direction, scrollbar.direction);

        scrollbar.value = m_Reader.GetFloat(scrollbarData, "value", scrollbar.value);
        scrollbar.size = m_Reader.GetFloat(scrollbarData, "size", scrollbar.size);
        scrollbar.numberOfSteps = m_Reader.GetInt(scrollbarData, "numberOfSteps", scrollbar.numberOfSteps);
    }

    private RectOffset ReadRectOffset(JsonData data, RectOffset defaultValue)
    {
        if (data == null || !data.IsObject)
            return defaultValue;

        RectOffset offset = defaultValue ?? new RectOffset();
        offset.left = m_Reader.GetInt(data, "left", offset.left);
        offset.right = m_Reader.GetInt(data, "right", offset.right);
        offset.top = m_Reader.GetInt(data, "top", offset.top);
        offset.bottom = m_Reader.GetInt(data, "bottom", offset.bottom);
        return offset;
    }
    private Vector4 ReadRectOffset(JsonData data, Vector4 defaultValue)
    {
        if (data == null || !data.IsObject)
            return defaultValue;

        Vector4 offset =  new Vector4();
        offset.x = m_Reader.GetFloat(data, "left", offset.x);
        offset.y = m_Reader.GetFloat(data, "right", offset.y);
        offset.z = m_Reader.GetFloat(data, "top", offset.z);
        offset.w = m_Reader.GetFloat(data, "bottom", offset.w);
        return offset;
    }
    private Rect ReadRect(JsonData list, Rect defaultValue)
    {
        if (list == null || !list.IsArray || list.Count < 4)
            return defaultValue;

        float x = ReadFloat(list[0], defaultValue.x);
        float y = ReadFloat(list[1], defaultValue.y);
        float w = ReadFloat(list[2], defaultValue.width);
        float h = ReadFloat(list[3], defaultValue.height);
        return new Rect(x, y, w, h);
    }

    private float ReadFloat(JsonData value, float defaultValue)
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
            Graphic g = ResolveGraphic(go.transform, targetGraphicName);
            if (g != null)
                toggle.targetGraphic = g;
        }

        string graphicName = m_Reader.GetString(toggleData, "graphic", string.Empty);
        if (!string.IsNullOrEmpty(graphicName))
        {
            Graphic g = ResolveGraphic(go.transform, graphicName);
            if (g != null)
                toggle.graphic = g;
        }

        string groupName = m_Reader.GetString(toggleData, "group", string.Empty);
        if (!string.IsNullOrEmpty(groupName))
        {
            ToggleGroup group = ResolveComponent<ToggleGroup>(go.transform, groupName);
            if (group != null)
                toggle.group = group;
        }
    }

    private void ApplyCanvasGroup(GameObject go, JsonData canvasGroupData)
    {
        if (canvasGroupData == null)
            return;

        CanvasGroup canvasGroup = GetOrAddComponent<CanvasGroup>(go);
        canvasGroup.alpha = m_Reader.GetFloat(canvasGroupData, "alpha", canvasGroup.alpha);
        canvasGroup.interactable = m_Reader.GetBool(canvasGroupData, "interactable", canvasGroup.interactable);
        canvasGroup.blocksRaycasts = m_Reader.GetBool(canvasGroupData, "blocksRaycasts", canvasGroup.blocksRaycasts);
        canvasGroup.ignoreParentGroups = m_Reader.GetBool(canvasGroupData, "ignoreParentGroups", canvasGroup.ignoreParentGroups);
    }

    private void ApplyLayoutElement(GameObject go, JsonData layoutElementData)
    {
        if (layoutElementData == null)
            return;

        LayoutElement layoutElement = GetOrAddComponent<LayoutElement>(go);
        layoutElement.ignoreLayout = m_Reader.GetBool(layoutElementData, "ignoreLayout", layoutElement.ignoreLayout);
        layoutElement.minWidth = m_Reader.GetFloat(layoutElementData, "minWidth", layoutElement.minWidth);
        layoutElement.minHeight = m_Reader.GetFloat(layoutElementData, "minHeight", layoutElement.minHeight);
        layoutElement.preferredWidth = m_Reader.GetFloat(layoutElementData, "preferredWidth", layoutElement.preferredWidth);
        layoutElement.preferredHeight = m_Reader.GetFloat(layoutElementData, "preferredHeight", layoutElement.preferredHeight);
        layoutElement.flexibleWidth = m_Reader.GetFloat(layoutElementData, "flexibleWidth", layoutElement.flexibleWidth);
        layoutElement.flexibleHeight = m_Reader.GetFloat(layoutElementData, "flexibleHeight", layoutElement.flexibleHeight);
        layoutElement.layoutPriority = m_Reader.GetInt(layoutElementData, "layoutPriority", layoutElement.layoutPriority);
    }

    private void ApplyAspectRatioFitter(GameObject go, JsonData fitterData)
    {
        if (fitterData == null)
            return;

        AspectRatioFitter fitter = GetOrAddComponent<AspectRatioFitter>(go);
        string aspectMode = m_Reader.GetString(fitterData, "aspectMode", string.Empty);
        if (!string.IsNullOrEmpty(aspectMode))
            fitter.aspectMode = ParseEnumSafe(aspectMode, fitter.aspectMode);

        fitter.aspectRatio = m_Reader.GetFloat(fitterData, "aspectRatio", fitter.aspectRatio);
    }

    private void ApplyRectMask2D(GameObject go, JsonData rectMask2DData)
    {
        if (rectMask2DData == null)
            return;

        RectMask2D rectMask2D = GetOrAddComponent<RectMask2D>(go);
        if (m_Reader.TryGetValue(rectMask2DData, "padding", out JsonData paddingData))
            rectMask2D.padding = ReadRectOffset(paddingData, rectMask2D.padding);

        if (m_Reader.TryGetValue(rectMask2DData, "softness", out JsonData softnessData))
            rectMask2D.softness = m_Reader.GetVector2Int(softnessData, rectMask2D.softness);
    }

    private void ApplyShadow(GameObject go, JsonData shadowData)
    {
        if (shadowData == null)
            return;

        Shadow shadow = GetOrAddComponent<Shadow>(go);
        if (m_Reader.TryGetValue(shadowData, "effectColor", out JsonData effectColor))
            shadow.effectColor = m_Reader.GetColor(effectColor, shadow.effectColor);
        if (m_Reader.TryGetValue(shadowData, "effectDistance", out JsonData effectDistance))
            shadow.effectDistance = m_Reader.GetVector2(effectDistance, shadow.effectDistance);
        shadow.useGraphicAlpha = m_Reader.GetBool(shadowData, "useGraphicAlpha", shadow.useGraphicAlpha);
    }

    private void ApplyOutline(GameObject go, JsonData outlineData)
    {
        if (outlineData == null)
            return;

        Outline outline = GetOrAddComponent<Outline>(go);
        if (m_Reader.TryGetValue(outlineData, "effectColor", out JsonData effectColor))
            outline.effectColor = m_Reader.GetColor(effectColor, outline.effectColor);
        if (m_Reader.TryGetValue(outlineData, "effectDistance", out JsonData effectDistance))
            outline.effectDistance = m_Reader.GetVector2(effectDistance, outline.effectDistance);
        outline.useGraphicAlpha = m_Reader.GetBool(outlineData, "useGraphicAlpha", outline.useGraphicAlpha);
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
            RectTransform t = ResolveRectTransform(go.transform, fillRectName);
            if (t != null)
                slider.fillRect = t;
        }

        string handleRectName = m_Reader.GetString(sliderData, "handleRect", string.Empty);
        if (!string.IsNullOrEmpty(handleRectName))
        {
            RectTransform t = ResolveRectTransform(go.transform, handleRectName);
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
            Text t = ResolveComponent<Text>(go.transform, textComponentName);
            if (t != null)
                inputField.textComponent = t;
        }

        string placeholderName = m_Reader.GetString(inputFieldData, "placeholder", string.Empty);
        if (!string.IsNullOrEmpty(placeholderName))
        {
            Graphic g = ResolveGraphic(go.transform, placeholderName);
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
            RectTransform t = ResolveRectTransform(go.transform, contentName);
            if (t != null)
                scrollRect.content = t;
        }

        string viewportName = m_Reader.GetString(scrollRectData, "viewport", string.Empty);
        if (!string.IsNullOrEmpty(viewportName))
        {
            RectTransform t = ResolveRectTransform(go.transform, viewportName);
            if (t != null)
                scrollRect.viewport = t;
        }

        string horizontalScrollbarName = m_Reader.GetString(scrollRectData, "horizontalScrollbar", string.Empty);
        if (!string.IsNullOrEmpty(horizontalScrollbarName))
        {
            Scrollbar sb = ResolveComponent<Scrollbar>(go.transform, horizontalScrollbarName);
            if (sb != null)
                scrollRect.horizontalScrollbar = sb;
        }

        string verticalScrollbarName = m_Reader.GetString(scrollRectData, "verticalScrollbar", string.Empty);
        if (!string.IsNullOrEmpty(verticalScrollbarName))
        {
            Scrollbar sb = ResolveComponent<Scrollbar>(go.transform, verticalScrollbarName);
            if (sb != null)
                scrollRect.verticalScrollbar = sb;
        }
    }

    /// <summary>
    /// 解析并应用 Slider 中的 RectTransform 引用。
    /// </summary>
    private void ResolveDropdownReferences(GameObject go, JsonData dropdownData)
    {
        Dropdown dropdown = go.GetComponent<Dropdown>();
        if (dropdown == null)
            return;

        string captionTextName = m_Reader.GetString(dropdownData, "captionText", "");
        if (!string.IsNullOrEmpty(captionTextName))
        {
            Text t = ResolveComponent<Text>(dropdown.transform, captionTextName);
            if (t != null)
                dropdown.captionText = t;
        }

        string itemTextName = m_Reader.GetString(dropdownData, "itemText", "");
        if (!string.IsNullOrEmpty(itemTextName))
        {
            Text t = ResolveComponent<Text>(dropdown.transform, itemTextName);
            if (t != null)
                dropdown.itemText = t;
        }

        string templateName = m_Reader.GetString(dropdownData, "template", "");
        if (!string.IsNullOrEmpty(templateName))
        {
            RectTransform t = ResolveRectTransform(dropdown.transform, templateName);
            if (t != null)
                dropdown.template = t;
        }

        string targetGraphicName = m_Reader.GetString(dropdownData, "targetGraphic", "");
        if (!string.IsNullOrEmpty(targetGraphicName))
        {
            Graphic t = ResolveGraphic(dropdown.transform, targetGraphicName);
            if (t != null)
                dropdown.targetGraphic = t;
        }
    }

    private void ResolveScrollbarReferences(GameObject go, JsonData scrollbarData)
    {
        Scrollbar scrollbar = go.GetComponent<Scrollbar>();
        if (scrollbar == null)
            return;

        string handleRectName = m_Reader.GetString(scrollbarData, "handleRect", string.Empty);
        if (!string.IsNullOrEmpty(handleRectName))
        {
            RectTransform t = ResolveRectTransform(go.transform, handleRectName);
            if (t != null)
                scrollbar.handleRect = t;
        }
    }

    private void RegisterDefaultComponentAppliers()
    {
        RegisterComponentApplier("Image", ApplyImage);
        RegisterComponentApplier("Text", ApplyText);
        RegisterComponentApplier("Button", ApplyButton);
        RegisterComponentApplier("Toggle", ApplyToggle);
        RegisterComponentApplier("Slider", ApplySlider);
        RegisterComponentApplier("InputField", ApplyInputField);
        RegisterComponentApplier("ScrollRect", ApplyScrollRect);
        RegisterComponentApplier("Mask", ApplyMask);
        RegisterComponentApplier("RectMask2D", ApplyRectMask2D);
        RegisterComponentApplier("Dropdown", ApplyDropdown);
        RegisterComponentApplier("CanvasGroup", ApplyCanvasGroup);
        RegisterComponentApplier("RawImage", ApplyRawImage);
        RegisterComponentApplier("HorizontalLayoutGroup", ApplyHorizontalLayoutGroup);
        RegisterComponentApplier("VerticalLayoutGroup", ApplyVerticalLayoutGroup);
        RegisterComponentApplier("GridLayoutGroup", ApplyGridLayoutGroup);
        RegisterComponentApplier("ContentSizeFitter", ApplyContentSizeFitter);
        RegisterComponentApplier("LayoutElement", ApplyLayoutElement);
        RegisterComponentApplier("AspectRatioFitter", ApplyAspectRatioFitter);
        RegisterComponentApplier("Scrollbar", ApplyScrollbar);
        RegisterComponentApplier("Shadow", ApplyShadow);
        RegisterComponentApplier("Outline", ApplyOutline);
        RegisterComponentApplier("TextMeshProUGUI", ApplyTextMeshProUGUI);
    }

    private void RegisterComponentApplier(string type, Action<GameObject, JsonData> applier)
    {
        if (string.IsNullOrEmpty(type) || applier == null)
            return;

        m_ComponentAppliers[type] = applier;
    }

    private void ApplyComponent(GameObject go, string componentType, JsonData data)
    {
        if (go == null || string.IsNullOrEmpty(componentType))
            return;

        if (m_ComponentAppliers.TryGetValue(componentType, out Action<GameObject, JsonData> applier))
            applier(go, data);
    }

    private void ResolveComponentReferences(GameObject go, string componentType, JsonData data)
    {
        if (string.Equals(componentType, "InputField", StringComparison.OrdinalIgnoreCase))
        {
            ResolveInputFieldReferences(go, data);
            return;
        }

        if (string.Equals(componentType, "ScrollRect", StringComparison.OrdinalIgnoreCase))
        {
            ResolveScrollRectReferences(go, data);
            return;
        }

        if (string.Equals(componentType, "Toggle", StringComparison.OrdinalIgnoreCase))
        {
            ResolveToggleReferences(go, data);
            return;
        }

        if (string.Equals(componentType, "Slider", StringComparison.OrdinalIgnoreCase))
        {
            ResolveSliderReferences(go, data);
            return;
        }

        if (string.Equals(componentType, "Dropdown", StringComparison.OrdinalIgnoreCase))
        {
            ResolveDropdownReferences(go, data);
            return;
        }

        if (string.Equals(componentType, "Scrollbar", StringComparison.OrdinalIgnoreCase))
        {
            ResolveScrollbarReferences(go, data);
        }
    }

    private static string NormalizeNullableString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return string.Equals(value, "null", StringComparison.OrdinalIgnoreCase) ? string.Empty : value;
    }

    private static Transform ResolveTransform(Transform root, string pathOrName)
    {
        if (root == null || string.IsNullOrEmpty(pathOrName))
            return null;

        if (pathOrName == ".")
            return root;

        if (pathOrName.IndexOf('/') >= 0)
            return root.Find(pathOrName);

        return FindTransformByName(root, pathOrName);
    }

    private static T ResolveComponent<T>(Transform root, string pathOrName) where T : Component
    {
        Transform t = ResolveTransform(root, pathOrName);
        return t != null ? t.GetComponent<T>() : null;
    }

    private static Graphic ResolveGraphic(Transform root, string pathOrName)
    {
        Transform t = ResolveTransform(root, pathOrName);
        return t != null ? t.GetComponent<Graphic>() : null;
    }

    private static RectTransform ResolveRectTransform(Transform root, string pathOrName)
    {
        Transform t = ResolveTransform(root, pathOrName);
        return t as RectTransform;
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

    private static Texture2D LoadTexture2D(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        return Resources.Load<Texture2D>(StripExtension(path));
    }

#if TMPRO
    private static TMP_FontAsset LoadTmpFontAsset(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);

        return Resources.Load<TMP_FontAsset>(StripExtension(path));
    }
#endif

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

