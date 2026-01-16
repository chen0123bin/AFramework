using System;
using System.Text;
using LitJson;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using LWUI;
#if TMPRO
using TMPro;
#endif

/// <summary>
/// 将场景中的 UGUI 树导出为约定格式的 JSON（结构对齐 UGUITemp.txt）。
/// </summary>
internal sealed class UguiUguiToJsonExporter
{
    /// <summary>
    /// 创建默认导出器实例。
    /// </summary>
    public static UguiUguiToJsonExporter CreateDefault()
    {
        return new UguiUguiToJsonExporter();
    }

    private UguiUguiToJsonExporter()
    {
    }

    /// <summary>
    /// 将指定的 UGUI 根节点导出为 JSON 字符串（包含 Root 包装层）。
    /// </summary>
    public string ExportToJsonString(GameObject uiRoot)
    {
        JsonData wrapper = ExportToJsonData(uiRoot);
        return ToPrettyJson(wrapper);
    }

    /// <summary>
    /// 将指定的 UGUI 根节点导出为 JsonData（包含 Root 包装层）。
    /// </summary>
    public JsonData ExportToJsonData(GameObject uiRoot)
    {
        JsonData wrapper = NewObject();
        wrapper[UguiJsonSchema.KEY_ROOT] = ExportRootNode(uiRoot);
        return wrapper;
    }

    /// <summary>
    /// 生成 Root 节点数据：只包含 name/rectTransform/children（与 UGUITemp 对齐）。
    /// </summary>
    private JsonData ExportRootNode(GameObject uiRoot)
    {
        JsonData root = NewObject();
        if (uiRoot == null)
            return root;

        root[UguiJsonSchema.KEY_NAME] = uiRoot.name;

        RectTransform rectTransform = uiRoot.GetComponent<RectTransform>();
        if (rectTransform != null)
            root[UguiJsonSchema.KEY_RECT_TRANSFORM] = ExportRectTransform(rectTransform);

        JsonData children = ExportChildren(uiRoot.transform);
        if (children != null)
            root[UguiJsonSchema.KEY_CHILDREN] = children;

        return root;
    }

    /// <summary>
    /// 导出 Transform 的所有子节点（每个子节点为一个元素节点）。
    /// </summary>
    private JsonData ExportChildren(Transform parent)
    {
        if (parent == null)
            return null;

        int childCount = parent.childCount;
        if (childCount <= 0)
            return null;

        JsonData list = NewArray();
        for (int i = 0; i < childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == null)
                continue;

            JsonData element = ExportElementNode(child.gameObject);
            list.Add(element);
        }

        return list;
    }

    /// <summary>
    /// 导出一个元素节点：name/active/rectTransform/components/children。
    /// </summary>
    private JsonData ExportElementNode(GameObject element)
    {
        JsonData node = NewObject();
        if (element == null)
            return node;

        node[UguiJsonSchema.KEY_NAME] = element.name;
        node[UguiJsonSchema.KEY_ACTIVE] = element.activeSelf;

        RectTransform rectTransform = element.GetComponent<RectTransform>();
        if (rectTransform != null)
            node[UguiJsonSchema.KEY_RECT_TRANSFORM] = ExportRectTransform(rectTransform);

        JsonData components = ExportComponents(element);
        if (components != null && components.IsArray && components.Count > 0)
            node[UguiJsonSchema.KEY_COMPONENTS] = components;

        JsonData children = ExportChildren(element.transform);
        if (children != null)
            node[UguiJsonSchema.KEY_CHILDREN] = children;

        return node;
    }

    /// <summary>
    /// 导出 RectTransform：anchorMin/anchorMax/pivot/anchoredPosition/sizeDelta/rotation/scale。
    /// </summary>
    private JsonData ExportRectTransform(RectTransform rect)
    {
        JsonData data = NewObject();
        if (rect == null)
            return data;

        data[UguiJsonSchema.KEY_ANCHOR_MIN] = NewVector2(rect.anchorMin);
        data[UguiJsonSchema.KEY_ANCHOR_MAX] = NewVector2(rect.anchorMax);
        data[UguiJsonSchema.KEY_PIVOT] = NewVector2(rect.pivot);
        data[UguiJsonSchema.KEY_ANCHORED_POSITION] = NewVector2(rect.anchoredPosition);
        data[UguiJsonSchema.KEY_SIZE_DELTA] = NewVector2(rect.sizeDelta);
        data[UguiJsonSchema.KEY_ROTATION] = NewVector3(rect.localEulerAngles);
        data[UguiJsonSchema.KEY_SCALE] = NewVector3(rect.localScale);
        return data;
    }

    /// <summary>
    /// 导出组件数组：每个元素为 {type,data}。
    /// </summary>
    private JsonData ExportComponents(GameObject element)
    {
        if (element == null)
            return null;

        JsonData list = NewArray();

        CanvasGroup canvasGroup = element.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            AddComponent(list, "CanvasGroup", ExportCanvasGroup(canvasGroup));

        RoundedImage roundedImage = element.GetComponent<RoundedImage>();
        if (roundedImage != null)
        {
            AddComponent(list, "RoundedImage", ExportRoundedImage(roundedImage));
        }
        else
        {
            Image image = element.GetComponent<Image>();
            if (image != null)
                AddComponent(list, "Image", ExportImage(image));
        }

        RawImage rawImage = element.GetComponent<RawImage>();
        if (rawImage != null)
            AddComponent(list, "RawImage", ExportRawImage(rawImage));

        Text text = element.GetComponent<Text>();
        if (text != null)
            AddComponent(list, "Text", ExportText(text));

#if TMPRO
        TextMeshProUGUI tmp = element.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
            AddComponent(list, "TextMeshProUGUI", ExportTextMeshProUGUI(tmp));
#endif

        Button button = element.GetComponent<Button>();
        if (button != null)
            AddComponent(list, "Button", ExportButton(button));

        Toggle toggle = element.GetComponent<Toggle>();
        if (toggle != null)
            AddComponent(list, "Toggle", ExportToggle(toggle));

        Slider slider = element.GetComponent<Slider>();
        if (slider != null)
            AddComponent(list, "Slider", ExportSlider(slider));

        InputField inputField = element.GetComponent<InputField>();
        if (inputField != null)
            AddComponent(list, "InputField", ExportInputField(inputField));

        ScrollRect scrollRect = element.GetComponent<ScrollRect>();
        if (scrollRect != null)
            AddComponent(list, "ScrollRect", ExportScrollRect(scrollRect));

        Mask mask = element.GetComponent<Mask>();
        if (mask != null)
            AddComponent(list, "Mask", ExportMask(mask));

        RectMask2D rectMask2D = element.GetComponent<RectMask2D>();
        if (rectMask2D != null)
            AddComponent(list, "RectMask2D", ExportRectMask2D(rectMask2D));

        Dropdown dropdown = element.GetComponent<Dropdown>();
        if (dropdown != null)
            AddComponent(list, "Dropdown", ExportDropdown(dropdown));

        Scrollbar scrollbar = element.GetComponent<Scrollbar>();
        if (scrollbar != null)
            AddComponent(list, "Scrollbar", ExportScrollbar(scrollbar));

        HorizontalLayoutGroup horizontalLayoutGroup = element.GetComponent<HorizontalLayoutGroup>();
        if (horizontalLayoutGroup != null)
            AddComponent(list, "HorizontalLayoutGroup", ExportHorizontalLayoutGroup(horizontalLayoutGroup));

        VerticalLayoutGroup verticalLayoutGroup = element.GetComponent<VerticalLayoutGroup>();
        if (verticalLayoutGroup != null)
            AddComponent(list, "VerticalLayoutGroup", ExportVerticalLayoutGroup(verticalLayoutGroup));

        GridLayoutGroup gridLayoutGroup = element.GetComponent<GridLayoutGroup>();
        if (gridLayoutGroup != null)
            AddComponent(list, "GridLayoutGroup", ExportGridLayoutGroup(gridLayoutGroup));

        ContentSizeFitter contentSizeFitter = element.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
            AddComponent(list, "ContentSizeFitter", ExportContentSizeFitter(contentSizeFitter));

        LayoutElement layoutElement = element.GetComponent<LayoutElement>();
        if (layoutElement != null)
            AddComponent(list, "LayoutElement", ExportLayoutElement(layoutElement));

        AspectRatioFitter aspectRatioFitter = element.GetComponent<AspectRatioFitter>();
        if (aspectRatioFitter != null)
            AddComponent(list, "AspectRatioFitter", ExportAspectRatioFitter(aspectRatioFitter));

        Outline outline = element.GetComponent<Outline>();
        if (outline != null)
            AddComponent(list, "Outline", ExportShadow(outline));

        Shadow shadow = element.GetComponent<Shadow>();
        if (shadow != null && outline == null)
            AddComponent(list, "Shadow", ExportShadow(shadow));

        return list;
    }

    private JsonData ExportCanvasGroup(CanvasGroup canvasGroup)
    {
        JsonData data = NewObject();
        if (canvasGroup == null)
            return data;

        data["alpha"] = (double)canvasGroup.alpha;
        data["interactable"] = canvasGroup.interactable;
        data["blocksRaycasts"] = canvasGroup.blocksRaycasts;
        data["ignoreParentGroups"] = canvasGroup.ignoreParentGroups;
        return data;
    }

    /// <summary>
    /// 导出 Image 数据块。
    /// </summary>
    private JsonData ExportImage(Image image)
    {
        JsonData data = NewObject();
        if (image == null)
            return data;

        data["sprite"] = GetSpritePath(image.sprite);
        data["color"] = NewColor(image.color);
        data["material"] = GetMaterialPath(image.material);
        data["raycastTarget"] = image.raycastTarget;
        data["imageType"] = image.type.ToString();
        data["fillCenter"] = image.fillCenter;
        data["pixelsPerUnitMultiplier"] = (double)image.pixelsPerUnitMultiplier;
        data["preserveAspect"] = image.preserveAspect;
        data["fillMethod"] = image.fillMethod.ToString();
        data["fillAmount"] = (double)image.fillAmount;
        data["fillClockwise"] = image.fillClockwise;
        data["fillOrigin"] = image.fillOrigin;
        return data;
    }

    /// <summary>
    /// 导出 RoundedImage 数据块（在 Image 基础上增加圆角/边框/镂空相关字段）。
    /// </summary>
    private JsonData ExportRoundedImage(RoundedImage roundedImage)
    {
        JsonData data = ExportImage(roundedImage);
        if (roundedImage == null)
            return data;

        data["independentCorners"] = roundedImage.IsIndependentCorners;
        data["cornerRadius"] = (double)roundedImage.CornerRadius;
        data["topLeftRadius"] = (double)roundedImage.TopLeftRadius;
        data["topRightRadius"] = (double)roundedImage.TopRightRadius;
        data["bottomRightRadius"] = (double)roundedImage.BottomRightRadius;
        data["bottomLeftRadius"] = (double)roundedImage.BottomLeftRadius;

        data["borderEnabled"] = roundedImage.IsBorderEnabled;
        data["borderColor"] = NewColor(roundedImage.BorderColor);
        data["borderThickness"] = (double)roundedImage.BorderThickness;

        data["hollow"] = roundedImage.IsHollow;
        data["hollowAreaRaycastEnabled"] = roundedImage.IsHollowAreaRaycastEnabled;
        data["shaderRenderingEnabled"] = roundedImage.IsShaderRenderingEnabled;

        data["roundedShaderMaterial"] = GetMaterialPath(roundedImage.RoundedShaderMaterial);

        return data;
    }

    /// <summary>
    /// 导出 RawImage 数据块。
    /// </summary>
    private JsonData ExportRawImage(RawImage rawImage)
    {
        JsonData data = NewObject();
        if (rawImage == null)
            return data;

        data["texture"] = GetTexturePath(rawImage.texture);
        data["uvRect"] = NewRect(rawImage.uvRect);
        data["color"] = NewColor(rawImage.color);
        data["material"] = GetMaterialPath(rawImage.material);
        data["raycastTarget"] = rawImage.raycastTarget;
        return data;
    }

    /// <summary>
    /// 导出 Text 数据块。
    /// </summary>
    private JsonData ExportText(Text text)
    {
        JsonData data = NewObject();
        if (text == null)
            return data;

        data["content"] = text.text;
        data["font"] = GetFontPath(text.font);
        data["fontSize"] = text.fontSize;
        data["fontStyle"] = text.fontStyle.ToString();
        data["color"] = NewColor(text.color);
        data["alignment"] = text.alignment.ToString();
        data["horizontalOverflow"] = text.horizontalOverflow.ToString();
        data["verticalOverflow"] = text.verticalOverflow.ToString();
        data["raycastTarget"] = text.raycastTarget;
        data["supportRichText"] = text.supportRichText;
        data["lineSpacing"] = (double)text.lineSpacing;
        return data;
    }

#if TMPRO
    /// <summary>
    /// 导出 TextMeshProUGUI 数据块。
    /// </summary>
    private JsonData ExportTextMeshProUGUI(TextMeshProUGUI tmp)
    {
        JsonData data = NewObject();
        if (tmp == null)
            return data;

        data["content"] = tmp.text;
        data["font"] = GetTmpFontPath(tmp.font);
        data["fontSize"] = (double)tmp.fontSize;
        data["fontStyle"] = tmp.fontStyle.ToString();
        data["color"] = NewColor(tmp.color);
        data["alignment"] = tmp.alignment.ToString();
        data["raycastTarget"] = tmp.raycastTarget;
        data["richText"] = tmp.richText;
        return data;
    }
#endif

    /// <summary>
    /// 导出 Button 数据块。
    /// </summary>
    private JsonData ExportButton(Button button)
    {
        JsonData data = NewObject();
        if (button == null)
            return data;

        data["interactable"] = button.interactable;
        data["transition"] = button.transition.ToString();
        data["colors"] = ExportColorBlock(button.colors);
        data["navigation"] = ExportNavigation(button.navigation, button.transform);
        return data;
    }

    /// <summary>
    /// 导出 Toggle 数据块。
    /// </summary>
    private JsonData ExportToggle(Toggle toggle)
    {
        JsonData data = NewObject();
        if (toggle == null)
            return data;

        data["interactable"] = toggle.interactable;
        data["targetGraphic"] = toggle.targetGraphic != null ? GetRelativePathOrDot(toggle.transform, toggle.targetGraphic.transform) : null;
        data["transition"] = toggle.transition.ToString();
        data["isOn"] = toggle.isOn;
        data["toggleTransition"] = toggle.toggleTransition.ToString();
        data["graphic"] = toggle.graphic != null ? GetRelativePathOrDot(toggle.transform, toggle.graphic.transform) : null;
        data["group"] = toggle.group != null ? GetRelativePathOrDot(toggle.transform, toggle.group.transform) : null;
        data["colors"] = ExportColorBlock(toggle.colors);
        data["navigation"] = ExportNavigation(toggle.navigation, toggle.transform);
        return data;
    }

    /// <summary>
    /// 导出 Slider 数据块。
    /// </summary>
    private JsonData ExportSlider(Slider slider)
    {
        JsonData data = NewObject();
        if (slider == null)
            return data;

        data["interactable"] = slider.interactable;
        data["targetGraphic"] = slider.targetGraphic != null ? GetRelativePathOrDot(slider.transform, slider.targetGraphic.transform) : null;
        data["transition"] = slider.transition.ToString();
        data["fillRect"] = slider.fillRect != null ? GetRelativePathOrDot(slider.transform, slider.fillRect.transform) : null;
        data["handleRect"] = slider.handleRect != null ? GetRelativePathOrDot(slider.transform, slider.handleRect.transform) : null;
        data["direction"] = slider.direction.ToString();
        data["minValue"] = (double)slider.minValue;
        data["maxValue"] = (double)slider.maxValue;
        data["wholeNumbers"] = slider.wholeNumbers;
        data["value"] = (double)slider.value;
        data["navigation"] = ExportNavigation(slider.navigation, slider.transform);
        return data;
    }

    /// <summary>
    /// 导出 InputField 数据块。
    /// </summary>
    private JsonData ExportInputField(InputField inputField)
    {
        JsonData data = NewObject();
        if (inputField == null)
            return data;

        data["interactable"] = inputField.interactable;
        data["textComponent"] = inputField.textComponent != null ? GetRelativePathOrDot(inputField.transform, inputField.textComponent.transform) : null;
        data["text"] = inputField.text;
        data["characterLimit"] = inputField.characterLimit;
        data["contentType"] = inputField.contentType.ToString();
        data["lineType"] = inputField.lineType.ToString();
        data["placeholder"] = inputField.placeholder != null ? GetRelativePathOrDot(inputField.transform, inputField.placeholder.transform) : null;
        data["caretBlinkRate"] = (double)inputField.caretBlinkRate;
        data["caretWidth"] = inputField.caretWidth;
        data["selectionColor"] = NewColor(inputField.selectionColor);
        data["readOnly"] = inputField.readOnly;
        data["navigation"] = ExportNavigation(inputField.navigation, inputField.transform);
        return data;
    }

    /// <summary>
    /// 导出 ScrollRect 数据块。
    /// </summary>
    private JsonData ExportScrollRect(ScrollRect scrollRect)
    {
        JsonData data = NewObject();
        if (scrollRect == null)
            return data;

        data["content"] = scrollRect.content != null ? GetRelativePathOrDot(scrollRect.transform, scrollRect.content.transform) : null;
        data["horizontal"] = scrollRect.horizontal;
        data["vertical"] = scrollRect.vertical;
        data["movementType"] = scrollRect.movementType.ToString();
        data["elasticity"] = (double)scrollRect.elasticity;
        data["inertia"] = scrollRect.inertia;
        data["decelerationRate"] = (double)scrollRect.decelerationRate;
        data["scrollSensitivity"] = (double)scrollRect.scrollSensitivity;
        data["viewport"] = scrollRect.viewport != null ? GetRelativePathOrDot(scrollRect.transform, scrollRect.viewport.transform) : null;
        data["horizontalScrollbar"] = scrollRect.horizontalScrollbar != null ? GetRelativePathOrDot(scrollRect.transform, scrollRect.horizontalScrollbar.transform) : null;
        data["verticalScrollbar"] = scrollRect.verticalScrollbar != null ? GetRelativePathOrDot(scrollRect.transform, scrollRect.verticalScrollbar.transform) : null;
        return data;
    }

    /// <summary>
    /// 导出 Mask 数据块。
    /// </summary>
    private JsonData ExportMask(Mask mask)
    {
        JsonData data = NewObject();
        if (mask == null)
            return data;

        data["showMaskGraphic"] = mask.showMaskGraphic;
        return data;
    }

    /// <summary>
    /// 导出 Dropdown 数据块（对齐 UGUITemp：template/captionText/itemText/options）。
    /// </summary>
    private JsonData ExportDropdown(Dropdown dropdown)
    {
        JsonData data = NewObject();
        if (dropdown == null)
            return data;

        data["interactable"] = dropdown.interactable;
        data["targetGraphic"] = dropdown.targetGraphic != null ? GetRelativePathOrDot(dropdown.transform, dropdown.targetGraphic.transform) : null;
        data["template"] = dropdown.template != null ? GetRelativePathOrDot(dropdown.transform, dropdown.template.transform) : null;
        data["captionText"] = dropdown.captionText != null ? GetRelativePathOrDot(dropdown.transform, dropdown.captionText.transform) : null;
        data["itemText"] = dropdown.itemText != null ? GetRelativePathOrDot(dropdown.transform, dropdown.itemText.transform) : null;
        data["value"] = dropdown.value;

        JsonData options = NewArray();
        int optionCount = dropdown.options != null ? dropdown.options.Count : 0;
        for (int i = 0; i < optionCount; i++)
        {
            Dropdown.OptionData option = dropdown.options[i];
            JsonData optionData = NewObject();
            optionData["text"] = option != null ? option.text : string.Empty;
            optionData["image"] = option != null ? GetSpritePath(option.image) : null;
            options.Add(optionData);
        }
        data["options"] = options;
        data["navigation"] = ExportNavigation(dropdown.navigation, dropdown.transform);
        return data;
    }

    /// <summary>
    /// 导出 Scrollbar 数据块。
    /// </summary>
    private JsonData ExportScrollbar(Scrollbar scrollbar)
    {
        JsonData data = NewObject();
        if (scrollbar == null)
            return data;

        data["interactable"] = scrollbar.interactable;
        data["transition"] = scrollbar.transition.ToString();
        data["colors"] = ExportColorBlock(scrollbar.colors);
        data["targetGraphic"] = scrollbar.targetGraphic != null ? GetRelativePathOrDot(scrollbar.transform, scrollbar.targetGraphic.transform) : null;
        data["handleRect"] = scrollbar.handleRect != null ? GetRelativePathOrDot(scrollbar.transform, scrollbar.handleRect.transform) : null;
        data["direction"] = scrollbar.direction.ToString();
        data["value"] = (double)scrollbar.value;
        data["size"] = (double)scrollbar.size;
        data["numberOfSteps"] = scrollbar.numberOfSteps;
        data["navigation"] = ExportNavigation(scrollbar.navigation, scrollbar.transform);
        return data;
    }

    /// <summary>
    /// 导出 HorizontalLayoutGroup 数据块。
    /// </summary>
    private JsonData ExportHorizontalLayoutGroup(HorizontalLayoutGroup layoutGroup)
    {
        JsonData data = ExportLayoutGroupCommon(layoutGroup);
        if (layoutGroup != null)
            data["reverseArrangement"] = layoutGroup.reverseArrangement;
        return data;
    }

    /// <summary>
    /// 导出 VerticalLayoutGroup 数据块。
    /// </summary>
    private JsonData ExportVerticalLayoutGroup(VerticalLayoutGroup layoutGroup)
    {
        JsonData data = ExportLayoutGroupCommon(layoutGroup);
        if (layoutGroup != null)
            data["reverseArrangement"] = layoutGroup.reverseArrangement;
        return data;
    }

    /// <summary>
    /// 导出 GridLayoutGroup 数据块。
    /// </summary>
    private JsonData ExportGridLayoutGroup(GridLayoutGroup grid)
    {
        JsonData data = ExportLayoutGroupCommon(grid);
        if (grid == null)
            return data;

        data["cellSize"] = NewVector2(grid.cellSize);
        data["spacing"] = NewVector2(grid.spacing);
        data["startCorner"] = grid.startCorner.ToString();
        data["startAxis"] = grid.startAxis.ToString();
        data["constraint"] = grid.constraint.ToString();
        data["constraintCount"] = grid.constraintCount;
        return data;
    }

    /// <summary>
    /// 导出 ContentSizeFitter 数据块。
    /// </summary>
    private JsonData ExportContentSizeFitter(ContentSizeFitter fitter)
    {
        JsonData data = NewObject();
        if (fitter == null)
            return data;

        data["horizontalFit"] = fitter.horizontalFit.ToString();
        data["verticalFit"] = fitter.verticalFit.ToString();
        return data;
    }

    /// <summary>
    /// 导出 LayoutGroup 的通用字段。
    /// </summary>
    private JsonData ExportLayoutGroupCommon(LayoutGroup layoutGroup)
    {
        JsonData data = NewObject();
        if (layoutGroup == null)
            return data;

        data["padding"] = ExportRectOffset(layoutGroup.padding);
        data["childAlignment"] = layoutGroup.childAlignment.ToString();

        if (layoutGroup is HorizontalOrVerticalLayoutGroup hv)
        {
            data["spacing"] = (double)hv.spacing;
            data["childControlWidth"] = hv.childControlWidth;
            data["childControlHeight"] = hv.childControlHeight;
            data["childForceExpandWidth"] = hv.childForceExpandWidth;
            data["childForceExpandHeight"] = hv.childForceExpandHeight;
            data["childScaleWidth"] = hv.childScaleWidth;
            data["childScaleHeight"] = hv.childScaleHeight;
        }

        return data;
    }

    private JsonData ExportLayoutElement(LayoutElement layoutElement)
    {
        JsonData data = NewObject();
        if (layoutElement == null)
            return data;

        data["ignoreLayout"] = layoutElement.ignoreLayout;
        data["minWidth"] = (double)layoutElement.minWidth;
        data["minHeight"] = (double)layoutElement.minHeight;
        data["preferredWidth"] = (double)layoutElement.preferredWidth;
        data["preferredHeight"] = (double)layoutElement.preferredHeight;
        data["flexibleWidth"] = (double)layoutElement.flexibleWidth;
        data["flexibleHeight"] = (double)layoutElement.flexibleHeight;
        data["layoutPriority"] = layoutElement.layoutPriority;
        return data;
    }

    private JsonData ExportAspectRatioFitter(AspectRatioFitter fitter)
    {
        JsonData data = NewObject();
        if (fitter == null)
            return data;

        data["aspectMode"] = fitter.aspectMode.ToString();
        data["aspectRatio"] = (double)fitter.aspectRatio;
        return data;
    }

    private JsonData ExportRectMask2D(RectMask2D rectMask2D)
    {
        JsonData data = NewObject();
        if (rectMask2D == null)
            return data;

        data["padding"] = ExportRectOffset(rectMask2D.padding);
        data["softness"] = NewVector2(rectMask2D.softness);
        return data;
    }

    private JsonData ExportShadow(Shadow shadow)
    {
        JsonData data = NewObject();
        if (shadow == null)
            return data;

        data["effectColor"] = NewColor(shadow.effectColor);
        data["effectDistance"] = NewVector2(shadow.effectDistance);
        data["useGraphicAlpha"] = shadow.useGraphicAlpha;
        return data;
    }

    /// <summary>
    /// 导出 Selectable 的 ColorBlock。
    /// </summary>
    private JsonData ExportColorBlock(ColorBlock colors)
    {
        JsonData data = NewObject();
        data["normalColor"] = NewColor(colors.normalColor);
        data["highlightedColor"] = NewColor(colors.highlightedColor);
        data["pressedColor"] = NewColor(colors.pressedColor);
        data["selectedColor"] = NewColor(colors.selectedColor);
        data["disabledColor"] = NewColor(colors.disabledColor);
        data["colorMultiplier"] = (double)colors.colorMultiplier;
        data["fadeDuration"] = (double)colors.fadeDuration;
        return data;
    }

    /// <summary>
    /// 向 components 数组添加一个组件项。
    /// </summary>
    private static void AddComponent(JsonData components, string type, JsonData data)
    {
        if (components == null || !components.IsArray)
            return;
        if (string.IsNullOrEmpty(type))
            return;

        JsonData item = NewObject();
        item[UguiJsonSchema.KEY_TYPE] = type;
        item[UguiJsonSchema.KEY_DATA] = data ?? NewObject();
        components.Add(item);
    }

    /// <summary>
    /// 将 JsonData 输出为可读性更好的格式化 JSON。
    /// </summary>
    private static string ToPrettyJson(JsonData data)
    {
        StringBuilder sb = new StringBuilder(4096);
        JsonWriter writer = new JsonWriter(sb)
        {
            PrettyPrint = true,
            IndentValue = 2
        };
        data.ToJson(writer);
        return sb.ToString();
    }

    /// <summary>
    /// 创建一个 Object 类型的 JsonData。
    /// </summary>
    private static JsonData NewObject()
    {
        JsonData data = new JsonData();
        data.SetJsonType(JsonType.Object);
        return data;
    }

    /// <summary>
    /// 创建一个 Array 类型的 JsonData。
    /// </summary>
    private static JsonData NewArray()
    {
        JsonData data = new JsonData();
        data.SetJsonType(JsonType.Array);
        return data;
    }

    /// <summary>
    /// 将 Vector2 转为 JSON 数组。
    /// </summary>
    private static JsonData NewVector2(Vector2 v)
    {
        JsonData list = NewArray();
        list.Add((double)v.x);
        list.Add((double)v.y);
        return list;
    }

    /// <summary>
    /// 将 Vector3 转为 JSON 数组。
    /// </summary>
    private static JsonData NewVector3(Vector3 v)
    {
        JsonData list = NewArray();
        list.Add((double)v.x);
        list.Add((double)v.y);
        list.Add((double)v.z);
        return list;
    }

    /// <summary>
    /// 将 Color 转为 JSON 数组。
    /// </summary>
    private static JsonData NewColor(Color c)
    {
        JsonData list = NewArray();
        list.Add((double)c.r);
        list.Add((double)c.g);
        list.Add((double)c.b);
        list.Add((double)c.a);
        return list;
    }

    /// <summary>
    /// 将 Rect 转为 JSON 数组。
    /// </summary>
    private static JsonData NewRect(Rect r)
    {
        JsonData list = NewArray();
        list.Add((double)r.x);
        list.Add((double)r.y);
        list.Add((double)r.width);
        list.Add((double)r.height);
        return list;
    }

    /// <summary>
    /// 将 RectOffset 转为 JSON 对象。
    /// </summary>
    private static JsonData ExportRectOffset(RectOffset offset)
    {
        JsonData data = NewObject();
        if (offset == null)
            return data;

        data["left"] = offset.left;
        data["right"] = offset.right;
        data["top"] = offset.top;
        data["bottom"] = offset.bottom;
        return data;
    }
    /// <summary>
    /// 将 RectOffset 转为 JSON 对象。
    /// </summary>
    private static JsonData ExportRectOffset(Vector4 offset)
    {
        JsonData data = NewObject();
        if (offset == null)
            return data;

        data["left"] = offset.x;
        data["right"] = offset.y;
        data["top"] = offset.z;
        data["bottom"] = offset.w;
        return data;
    }
    /// <summary>
    /// 导出 Navigation：当前以 mode 为主，必要时补充引用。
    /// </summary>
    private static JsonData ExportNavigation(Navigation navigation, Transform owner)
    {
        JsonData data = NewObject();
        data["mode"] = navigation.mode.ToString();
        return data;
    }

    /// <summary>
    /// 获取 Sprite 的导出路径：Resources 内用相对路径，否则用 Assets 路径。
    /// </summary>
    private static string GetSpritePath(Sprite sprite)
    {
        if (sprite == null)
            return null;

        string assetPath = AssetDatabase.GetAssetPath(sprite);
        if (!string.IsNullOrEmpty(assetPath))
        {
            string resourcesPath = TryConvertToResourcesPath(assetPath);
            return !string.IsNullOrEmpty(resourcesPath) ? resourcesPath : assetPath;
        }

        return sprite.name;
    }

    /// <summary>
    /// 获取 Material 的导出路径：Resources 内用相对路径，否则用 Assets 路径。
    /// </summary>
    private static string GetMaterialPath(Material material)
    {
        if (material == null)
            return null;

        string assetPath = AssetDatabase.GetAssetPath(material);
        if (!string.IsNullOrEmpty(assetPath))
        {
            string resourcesPath = TryConvertToResourcesPath(assetPath);
            return !string.IsNullOrEmpty(resourcesPath) ? resourcesPath : assetPath;
        }

        return material.name;
    }

    /// <summary>
    /// 获取 Texture 的导出路径：Resources 内用相对路径，否则用 Assets 路径。
    /// </summary>
    private static string GetTexturePath(Texture texture)
    {
        if (texture == null)
            return null;

        string assetPath = AssetDatabase.GetAssetPath(texture);
        if (!string.IsNullOrEmpty(assetPath))
        {
            string resourcesPath = TryConvertToResourcesPath(assetPath);
            return !string.IsNullOrEmpty(resourcesPath) ? resourcesPath : assetPath;
        }

        return texture.name;
    }

    /// <summary>
    /// 获取 Font 的导出路径：Resources 内用相对路径，否则输出类似 Arial.ttf。
    /// </summary>
    private static string GetFontPath(Font font)
    {
        if (font == null)
            return null;

        string assetPath = AssetDatabase.GetAssetPath(font);
        if (!string.IsNullOrEmpty(assetPath))
        {
            string resourcesPath = TryConvertToResourcesPath(assetPath);
            return !string.IsNullOrEmpty(resourcesPath) ? resourcesPath : assetPath;
        }

        string name = font.name;
        if (name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
            return name;

        return name + ".ttf";
    }

    /// <summary>
    /// 尝试将 Assets/.../Resources/xxx.ext 转为 xxx.ext。
    /// </summary>
    private static string TryConvertToResourcesPath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return null;

        const string resourcesFlag = "/Resources/";
        int idx = assetPath.IndexOf(resourcesFlag, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return null;

        int start = idx + resourcesFlag.Length;
        if (start >= assetPath.Length)
            return null;

        return assetPath.Substring(start);
    }

    /// <summary>
    /// 从 root 到 target 生成相对路径（用 / 分隔），用于 Dropdown.itemText 这类字段。
    /// </summary>
    private static string GetRelativePath(Transform root, Transform target)
    {
        if (root == null || target == null)
            return null;

        if (root == target)
            return root.name;

        StringBuilder sb = new StringBuilder(64);
        Transform cur = target;
        while (cur != null && cur != root)
        {
            if (sb.Length == 0)
                sb.Insert(0, cur.name);
            else
                sb.Insert(0, cur.name + "/");

            cur = cur.parent;
        }

        if (cur != root)
            return null;

        return sb.ToString();
    }

    /// <summary>
    /// 获取从 root 到 target 的相对路径；若 target 为 root 则返回 "."。
    /// </summary>
    private static string GetRelativePathOrDot(Transform root, Transform target)
    {
        if (root == null || target == null)
            return null;

        if (root == target)
            return ".";

        return GetRelativePath(root, target);
    }

#if TMPRO
    /// <summary>
    /// 获取 TMP_FontAsset 的导出路径：Resources 内用相对路径，否则用 Assets 路径。
    /// </summary>
    private static string GetTmpFontPath(TMP_FontAsset font)
    {
        if (font == null)
            return null;

        string assetPath = AssetDatabase.GetAssetPath(font);
        if (!string.IsNullOrEmpty(assetPath))
        {
            string resourcesPath = TryConvertToResourcesPath(assetPath);
            return !string.IsNullOrEmpty(resourcesPath) ? resourcesPath : assetPath;
        }

        return font.name;
    }
#endif
}
