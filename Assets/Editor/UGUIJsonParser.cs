using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using LitJson;
using System.IO;
using System.Collections.Generic;
/// <summary>
///这是一份存储unity ugui描述的文件，根据这份规则，帮我生成一份登录界面的json文件名称为login.txt
/// </summary>
public class UGUIJsonParser : EditorWindow
{
    private string jsonFilePath = "";
    private TextAsset jsonFile;
    private Transform parentTransform;

    [MenuItem("Tools/UGUI JSON Parser")]
    public static void ShowWindow()
    {
        GetWindow<UGUIJsonParser>("UGUI JSON Parser");
    }

    private void OnGUI()
    {
        GUILayout.Label("UGUI JSON Parser", EditorStyles.boldLabel);
        GUILayout.Space(10);

        jsonFile = (TextAsset)EditorGUILayout.ObjectField("JSON File", jsonFile, typeof(TextAsset), false);
        parentTransform = (Transform)EditorGUILayout.ObjectField("Parent Transform (Optional)", parentTransform, typeof(Transform), true);

        GUILayout.Space(10);

        if (GUILayout.Button("Parse and Create UGUI", GUILayout.Height(40)))
        {
            if (jsonFile != null)
            {
                ParseAndCreateUI(jsonFile.text);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please select a JSON file!", "OK");
            }
        }

        if (GUILayout.Button("Parse from File Path", GUILayout.Height(40)))
        {
            jsonFilePath = EditorUtility.OpenFilePanel("Select JSON File", Application.dataPath, "json");
            if (!string.IsNullOrEmpty(jsonFilePath))
            {
                string jsonContent = File.ReadAllText(jsonFilePath);
                ParseAndCreateUI(jsonContent);
            }
        }
    }

    private void ParseAndCreateUI(string jsonContent)
    {
        try
        {
            JsonData jsonData = JsonMapper.ToObject(jsonContent);

            if (jsonData.ContainsKey("Root"))
            {
                CreateRoot(jsonData["Root"]);
                EditorUtility.DisplayDialog("Success", "UGUI created successfully!", "OK");
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

    private GameObject CreateRoot(JsonData canvasData)
    {
        GameObject rootObj = new GameObject(GetString(canvasData, "name", "Root"));

        if (parentTransform != null)
        {
            rootObj.transform.SetParent(parentTransform);
        }

        // Add RectTransform
        RectTransform rectTransform = rootObj.AddComponent<RectTransform>();
        if (canvasData.ContainsKey("rectTransform"))
        {
            ApplyRectTransform(rectTransform, canvasData["rectTransform"]);
        }
        rootObj.AddComponent<CanvasRenderer>();
        rootObj.AddComponent<CanvasGroup>();
        
 
        // Create children
        if (canvasData.ContainsKey("children"))
        {
            CreateChildren(canvasData["children"], rootObj.transform);
        }

        return rootObj;
    }

    private void CreateChildren(JsonData childrenData, Transform parent)
    {
        if (childrenData == null || !childrenData.IsArray) return;

        for (int i = 0; i < childrenData.Count; i++)
        {
            JsonData childData = childrenData[i];
            string type = GetString(childData, "type", "Container");
            GameObject childObj = null;

            switch (type)
            {
                case "Button":
                    childObj = CreateButton(childData, parent);
                    break;
                case "Image":
                    childObj = CreateImage(childData, parent);
                    break;
                case "RawImage":
                    childObj = CreateRawImage(childData, parent);
                    break;
                case "Text":
                    childObj = CreateText(childData, parent);
                    break;
                case "InputField":
                    childObj = CreateInputField(childData, parent);
                    break;
                case "Dropdown":
                    childObj = CreateDropdown(childData, parent);
                    break;
                case "Slider":
                    childObj = CreateSlider(childData, parent);
                    break;
                case "ScrollRect":
                    childObj = CreateScrollRect(childData, parent);
                    break;
                case "Scrollbar":
                    childObj = CreateScrollbar(childData, parent);
                    break;
                case "Toggle":
                    childObj = CreateToggle(childData, parent);
                    break;
                case "RectMask2D":
                    childObj = CreateRectMask2D(childData, parent);
                    break;
                case "Container":
                default:
                    childObj = CreateContainer(childData, parent);
                    break;
            }

            if (childObj != null && childData.ContainsKey("children"))
            {
                CreateChildren(childData["children"], childObj.transform);
            }
        }
    }

    private GameObject CreateButton(JsonData data, Transform parent)
    {
        GameObject obj = new GameObject(GetString(data, "name", "Button"));
        obj.transform.SetParent(parent, false);
        obj.SetActive(GetBool(data, "active", true));

        RectTransform rectTransform = obj.AddComponent<RectTransform>();
        ApplyRectTransform(rectTransform, data["rectTransform"]);

        Image image = obj.AddComponent<Image>();
        if (data.ContainsKey("image"))
        {
            ApplyImage(image, data["image"]);
        }

        Button button = obj.AddComponent<Button>();
        if (data.ContainsKey("button"))
        {
            ApplyButton(button, data["button"]);
        }

        return obj;
    }

    private GameObject CreateImage(JsonData data, Transform parent)
    {
        GameObject obj = new GameObject(GetString(data, "name", "Image"));
        obj.transform.SetParent(parent, false);
        obj.SetActive(GetBool(data, "active", true));

        RectTransform rectTransform = obj.AddComponent<RectTransform>();
        ApplyRectTransform(rectTransform, data["rectTransform"]);

        Image image = obj.AddComponent<Image>();
        if (data.ContainsKey("image"))
        {
            ApplyImage(image, data["image"]);
        }

        return obj;
    }

    private GameObject CreateRawImage(JsonData data, Transform parent)
    {
        GameObject obj = new GameObject(GetString(data, "name", "RawImage"));
        obj.transform.SetParent(parent, false);
        obj.SetActive(GetBool(data, "active", true));

        RectTransform rectTransform = obj.AddComponent<RectTransform>();
        ApplyRectTransform(rectTransform, data["rectTransform"]);

        RawImage rawImage = obj.AddComponent<RawImage>();
        if (data.ContainsKey("rawImage"))
        {
            ApplyRawImage(rawImage, data["rawImage"]);
        }

        return obj;
    }

    private GameObject CreateText(JsonData data, Transform parent)
    {
        GameObject obj = new GameObject(GetString(data, "name", "Text"));
        obj.transform.SetParent(parent, false);
        obj.SetActive(GetBool(data, "active", true));

        RectTransform rectTransform = obj.AddComponent<RectTransform>();
        ApplyRectTransform(rectTransform, data["rectTransform"]);

        Text text = obj.AddComponent<Text>();
        if (data.ContainsKey("text"))
        {
            ApplyText(text, data["text"]);
        }

        return obj;
    }

    private GameObject CreateInputField(JsonData data, Transform parent)
    {
        GameObject obj = new GameObject(GetString(data, "name", "InputField"));
        obj.transform.SetParent(parent, false);
        obj.SetActive(GetBool(data, "active", true));

        RectTransform rectTransform = obj.AddComponent<RectTransform>();
        ApplyRectTransform(rectTransform, data["rectTransform"]);

        Image image = obj.AddComponent<Image>();
        if (data.ContainsKey("image"))
        {
            ApplyImage(image, data["image"]);
        }

        InputField inputField = obj.AddComponent<InputField>();

        // InputField settings will be applied after children are created
        return obj;
    }

    private GameObject CreateDropdown(JsonData data, Transform parent)
    {
        GameObject obj = new GameObject(GetString(data, "name", "Dropdown"));
        obj.transform.SetParent(parent, false);
        obj.SetActive(GetBool(data, "active", true));

        RectTransform rectTransform = obj.AddComponent<RectTransform>();
        ApplyRectTransform(rectTransform, data["rectTransform"]);

        Image image = obj.AddComponent<Image>();
        if (data.ContainsKey("image"))
        {
            ApplyImage(image, data["image"]);
        }

        Dropdown dropdown = obj.AddComponent<Dropdown>();
        if (data.ContainsKey("dropdown"))
        {
            ApplyDropdown(dropdown, data["dropdown"]);
        }

        return obj;
    }

    private GameObject CreateSlider(JsonData data, Transform parent)
    {
        GameObject obj = new GameObject(GetString(data, "name", "Slider"));
        obj.transform.SetParent(parent, false);
        obj.SetActive(GetBool(data, "active", true));

        RectTransform rectTransform = obj.AddComponent<RectTransform>();
        ApplyRectTransform(rectTransform, data["rectTransform"]);

        Slider slider = obj.AddComponent<Slider>();

        return obj;
    }

    private GameObject CreateScrollRect(JsonData data, Transform parent)
    {
        GameObject obj = new GameObject(GetString(data, "name", "ScrollRect"));
        obj.transform.SetParent(parent, false);
        obj.SetActive(GetBool(data, "active", true));

        RectTransform rectTransform = obj.AddComponent<RectTransform>();
        ApplyRectTransform(rectTransform, data["rectTransform"]);

        Image image = obj.AddComponent<Image>();
        if (data.ContainsKey("image"))
        {
            ApplyImage(image, data["image"]);
        }

        ScrollRect scrollRect = obj.AddComponent<ScrollRect>();
        if (data.ContainsKey("scrollRect"))
        {
            ApplyScrollRect(scrollRect, data["scrollRect"]);
        }

        return obj;
    }

    private GameObject CreateScrollbar(JsonData data, Transform parent)
    {
        GameObject obj = new GameObject(GetString(data, "name", "Scrollbar"));
        obj.transform.SetParent(parent, false);
        obj.SetActive(GetBool(data, "active", true));

        RectTransform rectTransform = obj.AddComponent<RectTransform>();
        ApplyRectTransform(rectTransform, data["rectTransform"]);

        Image image = obj.AddComponent<Image>();
        if (data.ContainsKey("image"))
        {
            ApplyImage(image, data["image"]);
        }

        Scrollbar scrollbar = obj.AddComponent<Scrollbar>();
        if (data.ContainsKey("scrollbar"))
        {
            ApplyScrollbar(scrollbar, data["scrollbar"]);
        }

        return obj;
    }

    private GameObject CreateToggle(JsonData data, Transform parent)
    {
        GameObject obj = new GameObject(GetString(data, "name", "Toggle"));
        obj.transform.SetParent(parent, false);
        obj.SetActive(GetBool(data, "active", true));

        RectTransform rectTransform = obj.AddComponent<RectTransform>();
        ApplyRectTransform(rectTransform, data["rectTransform"]);

        Toggle toggle = obj.AddComponent<Toggle>();
        if (data.ContainsKey("toggle"))
        {
            ApplyToggle(toggle, data["toggle"]);
        }

        return obj;
    }

    private GameObject CreateRectMask2D(JsonData data, Transform parent)
    {
        GameObject obj = new GameObject(GetString(data, "name", "Viewport"));
        obj.transform.SetParent(parent, false);
        obj.SetActive(GetBool(data, "active", true));

        RectTransform rectTransform = obj.AddComponent<RectTransform>();
        ApplyRectTransform(rectTransform, data["rectTransform"]);

        obj.AddComponent<RectMask2D>();

        return obj;
    }

    private GameObject CreateContainer(JsonData data, Transform parent)
    {
        GameObject obj = new GameObject(GetString(data, "name", "Container"));
        obj.transform.SetParent(parent, false);
        obj.SetActive(GetBool(data, "active", true));

        RectTransform rectTransform = obj.AddComponent<RectTransform>();
        if (data.ContainsKey("rectTransform"))
        {
            ApplyRectTransform(rectTransform, data["rectTransform"]);
        }

        return obj;
    }

    // Apply methods
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

    private void ApplyImage(Image image, JsonData data)
    {
        if (data == null) return;

        string spritePath = GetString(data, "sprite", "");
        if (!string.IsNullOrEmpty(spritePath))
        {
            Sprite sprite = Resources.Load<Sprite>(spritePath);
            if (sprite != null)
            {
                image.sprite = sprite;
            }
        }

        image.color = GetColor(data, "color", Color.white);
        image.raycastTarget = GetBool(data, "raycastTarget", true);
        image.type = ParseImageType(GetString(data, "imageType", "Simple"));
    }

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

    private void ApplyText(Text text, JsonData data)
    {
        if (data == null) return;

        text.text = GetString(data, "content", "");

        string fontPath = GetString(data, "font", "");
        if (!string.IsNullOrEmpty(fontPath))
        {
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

    private void ApplyToggle(Toggle toggle, JsonData data)
    {
        if (data == null) return;

        toggle.interactable = GetBool(data, "interactable", true);
        toggle.isOn = GetBool(data, "isOn", false);
        toggle.toggleTransition = ParseToggleTransition(GetString(data, "toggleTransition", "Fade"));

        if (data.ContainsKey("colors"))
        {
            ColorBlock colors = toggle.colors;
            JsonData colorData = data["colors"];
            colors.normalColor = GetColor(colorData, "normalColor", Color.white);
            colors.highlightedColor = GetColor(colorData, "highlightedColor", Color.white);
            colors.pressedColor = GetColor(colorData, "pressedColor", Color.white);
            colors.disabledColor = GetColor(colorData, "disabledColor", Color.gray);
            colors.colorMultiplier = GetFloat(colorData, "colorMultiplier", 1f);
            colors.fadeDuration = GetFloat(colorData, "fadeDuration", 0.1f);
            toggle.colors = colors;
        }
    }

    private void ApplyDropdown(Dropdown dropdown, JsonData data)
    {
        if (data == null) return;

        dropdown.interactable = GetBool(data, "interactable", true);
        dropdown.value = GetInt(data, "value", 0);

        if (data.ContainsKey("options") && data["options"].IsArray)
        {
            dropdown.options.Clear();
            for (int i = 0; i < data["options"].Count; i++)
            {
                JsonData optionData = data["options"][i];
                string optionText = GetString(optionData, "text", "");
                dropdown.options.Add(new Dropdown.OptionData(optionText));
            }
        }
    }

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
    }

    private void ApplyScrollbar(Scrollbar scrollbar, JsonData data)
    {
        if (data == null) return;

        scrollbar.direction = ParseScrollbarDirection(GetString(data, "direction", "BottomToTop"));
        scrollbar.value = GetFloat(data, "value", 1f);
        scrollbar.size = GetFloat(data, "size", 0.5f);
    }

    // Helper methods to get values from JsonData
    private string GetString(JsonData data, string key, string defaultValue)
    {
        if (data != null && data.ContainsKey(key) && data[key].IsString)
            return (string)data[key];
        return defaultValue;
    }

    private int GetInt(JsonData data, string key, int defaultValue)
    {
        if (data != null && data.ContainsKey(key) && data[key].IsInt)
            return (int)data[key];
        return defaultValue;
    }

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

    private bool GetBool(JsonData data, string key, bool defaultValue)
    {
        if (data != null && data.ContainsKey(key) && data[key].IsBoolean)
            return (bool)data[key];
        return defaultValue;
    }

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

    // Enum parsers
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

    private HorizontalWrapMode ParseHorizontalWrapMode(string value)
    {
        switch (value)
        {
            case "Wrap": return HorizontalWrapMode.Wrap;
            case "Overflow": return HorizontalWrapMode.Overflow;
            default: return HorizontalWrapMode.Wrap;
        }
    }

    private VerticalWrapMode ParseVerticalWrapMode(string value)
    {
        switch (value)
        {
            case "Truncate": return VerticalWrapMode.Truncate;
            case "Overflow": return VerticalWrapMode.Overflow;
            default: return VerticalWrapMode.Truncate;
        }
    }

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

    private Toggle.ToggleTransition ParseToggleTransition(string value)
    {
        switch (value)
        {
            case "None": return Toggle.ToggleTransition.None;
            case "Fade": return Toggle.ToggleTransition.Fade;
            default: return Toggle.ToggleTransition.Fade;
        }
    }

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
}