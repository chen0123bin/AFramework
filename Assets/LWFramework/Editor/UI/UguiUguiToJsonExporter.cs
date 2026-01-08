using System;
using System.Text;
using LitJson;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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
    /// 导出一个元素节点：type/name/active/rectTransform/组件块/children。
    /// </summary>
    private JsonData ExportElementNode(GameObject element)
    {
        JsonData node = NewObject();
        if (element == null)
            return node;

        string type = GetElementType(element);
        if (!string.IsNullOrEmpty(type))
            node[UguiJsonSchema.KEY_TYPE] = type;

        node[UguiJsonSchema.KEY_NAME] = element.name;
        node[UguiJsonSchema.KEY_ACTIVE] = element.activeSelf;

        RectTransform rectTransform = element.GetComponent<RectTransform>();
        if (rectTransform != null)
            node[UguiJsonSchema.KEY_RECT_TRANSFORM] = ExportRectTransform(rectTransform);

        ExportComponentBlocks(element, node);

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
    /// 按组件类型导出对应的数据块（image/text/button/toggle/slider/inputField/scrollRect/mask/dropdown）。
    /// </summary>
    private void ExportComponentBlocks(GameObject element, JsonData node)
    {
        if (element == null || node == null)
            return;

        Image image = element.GetComponent<Image>();
        if (image != null)
            node[UguiJsonSchema.KEY_IMAGE] = ExportImage(image);

        Text text = element.GetComponent<Text>();
        if (text != null)
            node[UguiJsonSchema.KEY_TEXT] = ExportText(text);

        Button button = element.GetComponent<Button>();
        if (button != null)
            node[UguiJsonSchema.KEY_BUTTON] = ExportButton(button);

        Toggle toggle = element.GetComponent<Toggle>();
        if (toggle != null)
            node[UguiJsonSchema.KEY_TOGGLE] = ExportToggle(toggle);

        Slider slider = element.GetComponent<Slider>();
        if (slider != null)
            node[UguiJsonSchema.KEY_SLIDER] = ExportSlider(slider);

        InputField inputField = element.GetComponent<InputField>();
        if (inputField != null)
            node[UguiJsonSchema.KEY_INPUT_FIELD] = ExportInputField(inputField);

        ScrollRect scrollRect = element.GetComponent<ScrollRect>();
        if (scrollRect != null)
            node[UguiJsonSchema.KEY_SCROLL_RECT] = ExportScrollRect(scrollRect);

        Mask mask = element.GetComponent<Mask>();
        if (mask != null)
            node[UguiJsonSchema.KEY_MASK] = ExportMask(mask);

        Dropdown dropdown = element.GetComponent<Dropdown>();
        if (dropdown != null)
            node[UguiJsonSchema.KEY_DROPDOWN] = ExportDropdown(dropdown);
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
        return data;
    }

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
        data["targetGraphic"] = toggle.targetGraphic != null ? toggle.targetGraphic.gameObject.name : null;
        data["transition"] = toggle.transition.ToString();
        data["isOn"] = toggle.isOn;
        data["toggleTransition"] = toggle.toggleTransition.ToString();
        data["graphic"] = toggle.graphic != null ? toggle.graphic.gameObject.name : null;
        data["group"] = toggle.group != null ? toggle.group.gameObject.name : null;
        data["colors"] = ExportColorBlock(toggle.colors);
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
        data["transition"] = slider.transition.ToString();
        data["fillRect"] = slider.fillRect != null ? slider.fillRect.gameObject.name : null;
        data["handleRect"] = slider.handleRect != null ? slider.handleRect.gameObject.name : null;
        data["direction"] = slider.direction.ToString();
        data["minValue"] = (double)slider.minValue;
        data["maxValue"] = (double)slider.maxValue;
        data["wholeNumbers"] = slider.wholeNumbers;
        data["value"] = (double)slider.value;
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
        data["textComponent"] = inputField.textComponent != null ? inputField.textComponent.gameObject.name : null;
        data["text"] = inputField.text;
        data["characterLimit"] = inputField.characterLimit;
        data["contentType"] = inputField.contentType.ToString();
        data["lineType"] = inputField.lineType.ToString();
        data["placeholder"] = inputField.placeholder != null ? inputField.placeholder.gameObject.name : null;
        data["caretBlinkRate"] = (double)inputField.caretBlinkRate;
        data["caretWidth"] = inputField.caretWidth;
        data["selectionColor"] = NewColor(inputField.selectionColor);
        data["readOnly"] = inputField.readOnly;
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

        data["content"] = scrollRect.content != null ? scrollRect.content.gameObject.name : null;
        data["horizontal"] = scrollRect.horizontal;
        data["vertical"] = scrollRect.vertical;
        data["movementType"] = scrollRect.movementType.ToString();
        data["elasticity"] = (double)scrollRect.elasticity;
        data["inertia"] = scrollRect.inertia;
        data["decelerationRate"] = (double)scrollRect.decelerationRate;
        data["scrollSensitivity"] = (double)scrollRect.scrollSensitivity;
        data["viewport"] = scrollRect.viewport != null ? scrollRect.viewport.gameObject.name : null;
        data["horizontalScrollbar"] = scrollRect.horizontalScrollbar != null ? scrollRect.horizontalScrollbar.gameObject.name : null;
        data["verticalScrollbar"] = scrollRect.verticalScrollbar != null ? scrollRect.verticalScrollbar.gameObject.name : null;
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
        data["targetGraphic"] = dropdown.targetGraphic != null ? dropdown.targetGraphic.gameObject.name : null;
        data["template"] = dropdown.template != null ? dropdown.template.gameObject.name : null;
        data["captionText"] = dropdown.captionText != null ? dropdown.captionText.gameObject.name : null;
        data["itemText"] = dropdown.itemText != null ? GetRelativePath(dropdown.transform, dropdown.itemText.transform) : null;
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
    /// 获取当前节点对应的 UGUI 类型字符串。
    /// </summary>
    private string GetElementType(GameObject element)
    {
        if (element == null)
            return string.Empty;

        if (element.GetComponent<Button>() != null)
            return "Button";
        if (element.GetComponent<Toggle>() != null)
            return "Toggle";
        if (element.GetComponent<Slider>() != null)
            return "Slider";
        if (element.GetComponent<InputField>() != null)
            return "InputField";
        if (element.GetComponent<ScrollRect>() != null)
            return "ScrollRect";
        if (element.GetComponent<Dropdown>() != null)
            return "Dropdown";
        if (element.GetComponent<Mask>() != null)
            return "Mask";
        if (element.GetComponent<Text>() != null)
            return "Text";
        if (element.GetComponent<Image>() != null)
            return "Image";

        return string.Empty;
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
            return target.name;

        return sb.ToString();
    }
}
