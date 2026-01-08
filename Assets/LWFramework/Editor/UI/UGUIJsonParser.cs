using UnityEditor;
using UnityEngine;
using LitJson;
using System.IO;
using UnityEngine.UI;

/// <summary>
/// UGUI JSON 解析器窗口：从 JSON 构建 UGUI 树，并支持从选中对象导出 JSON。
/// </summary>
public class UGUIJsonParser : EditorWindow
{
    private const string PREF_KEY_PARENT_TRANSFORM = "UGUIJsonParser.ParentTransformID";

    private string m_JsonFilePath = "";
    private TextAsset m_JsonFile;
    private Transform m_ParentTransform;

    /// <summary>
    /// 窗口启用时调用，用于恢复上次的状态。
    /// </summary>
    private void OnEnable()
    {
        // 尝试恢复上一次记录的 Parent Transform
        if (m_ParentTransform == null && EditorPrefs.HasKey(PREF_KEY_PARENT_TRANSFORM))
        {
            int id = EditorPrefs.GetInt(PREF_KEY_PARENT_TRANSFORM);
            Object obj = EditorUtility.InstanceIDToObject(id);
            if (obj != null && obj is Transform)
            {
                m_ParentTransform = (Transform)obj;
            }
        }
    }

    /// <summary>
    /// 打开编辑器窗口。
    /// </summary>
    [MenuItem("Tools/UGUI JSON Parser")]
    public static void ShowWindow()
    {
        GetWindow<UGUIJsonParser>("UGUI JSON Parser");
    }
    /// <summary>
    /// 根据当前选中的 UGUI 根节点导出 JSON 文件。
    /// </summary>
    [MenuItem("GameObject/UIFramework/Create UGUI JSON ")]
    public static void CreateUGUIJson()
    {
        if (Selection.activeGameObject == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a GameObject!", "OK");
            return;
        }
        if (Selection.activeGameObject.GetComponent<CanvasGroup>() == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a GameObject with CanvasGroup component!", "OK");
            return;
        }
        string jsonContent = ParseUI(Selection.activeGameObject);
        string fileName = Selection.activeGameObject.name + ".json";
        string filePath = Path.Combine(Application.dataPath, fileName);
        File.WriteAllText(filePath, jsonContent);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    /// <summary>
    /// 将选中节点解析为 JSON 字符串。
    /// </summary>
    /// <param name="uiElement">要解析的 UI 根节点</param>
    /// <returns>JSON 字符串</returns>
    private static string ParseUI(GameObject uiElement)
    {
        return UguiUguiToJsonExporter.CreateDefault().ExportToJsonString(uiElement);
    }

    /// <summary>
    /// 绘制编辑器 UI，并触发解析/创建。
    /// </summary>
    private void OnGUI()
    {
        GUILayout.Label("UGUI JSON Parser", EditorStyles.boldLabel);
        GUILayout.Space(10);

        m_JsonFile = (TextAsset)EditorGUILayout.ObjectField("JSON File", m_JsonFile, typeof(TextAsset), false);

        // 记录修改前的 Transform，以便检测变化
        EditorGUI.BeginChangeCheck();
        m_ParentTransform = (Transform)EditorGUILayout.ObjectField("Parent Transform (Optional)", m_ParentTransform, typeof(Transform), true);

        // 如果 Transform 发生了变化，则保存其 InstanceID
        if (EditorGUI.EndChangeCheck())
        {
            if (m_ParentTransform != null)
            {
                EditorPrefs.SetInt(PREF_KEY_PARENT_TRANSFORM, m_ParentTransform.GetInstanceID());
            }
            else
            {
                EditorPrefs.DeleteKey(PREF_KEY_PARENT_TRANSFORM);
            }
        }

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
            if (!jsonData.ContainsKey(UguiJsonSchema.KEY_ROOT))
            {
                EditorUtility.DisplayDialog("Error", "Invalid JSON format: 'Root' not found!", "OK");
                return;
            }

            UguiJsonToUguiBuilder.CreateDefault().Build(jsonData[UguiJsonSchema.KEY_ROOT], m_ParentTransform);
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to parse JSON: {e.Message}", "OK");
            Debug.LogError(e);
        }
    }
}
