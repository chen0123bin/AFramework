using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateScriptByTemplate
{

    /// <summary>
    /// 新建Startup类
    /// </summary>
    [@MenuItem("Assets/Create/LWFramework/ScriptableObject", false, 11)]
    private static void CreateScriptableObject()
    {

        CreateScript("新建ScriptableObject类", "ScriptableObject", "ScriptableObjectTemp");
    }

    /// <summary>
    /// 新建Startup类
    /// </summary>
    [@MenuItem("Assets/Create/LWFramework/Startup", false, 11)]
    private static void CreateStartup()
    {

        CreateScript("新建 Startup 类", "Startup", "StartupTemp");
    }
    /// <summary>
    /// 新建Manager类
    /// </summary>
    [@MenuItem("Assets/Create/LWFramework/C# HotFixMono", false, 11)]
    private static void CreateHotFixMono()
    {
        CreateScript("新建 HotFixMono 类", "NewHotFixMono", "HotFixMonoTemp");
    }
    /// <summary>
    /// 新建Manager类
    /// </summary>
    [@MenuItem("Assets/Create/LWFramework/C# Manager", false, 11)]
    private static void CreateManager()
    {
        CreateScript("新建 Manager 类", "NewManager", "ManagerTemp");
    }

    /// <summary>
    /// 新建HotfixManager类
    /// </summary>
    [@MenuItem("Assets/Create/LWFramework/C# HotfixManager", false, 11)]
    private static void CreateHotfixManager()
    {
        CreateScript("新建 HotfixManager 类", "NewHotfixManager", "HotfixManagerTemp");
    }
    /// <summary>
    /// 新建HotfixManager类
    /// </summary>
    [@MenuItem("Assets/Create/LWFramework/C# Procedure", false, 11)]
    private static void CreateProcedure()
    {
        CreateScript("新建 Procedure 类", "NewProcedure", "ProcedureTemp");

    }

    static void CreateScript(string title, string defaultName, string tempName)
    {
        string path = EditorUtility.SaveFilePanel(title, GetSavePath(), defaultName, "cs");
        if (path != "")
        {
            string className = path.Substring(path.LastIndexOf("/") + 1).Replace(".cs", "");
            if (!File.Exists(path))
            {

                TextAsset asset = Resources.Load<TextAsset>($"Template/{tempName}");
                if (asset)
                {
                    string code = asset.text;
                    code = code.Replace("#SCRIPTNAME#", className);
                    File.AppendAllText(path, code);
                    asset = null;
                    AssetDatabase.Refresh();

                    string assetPath = path.Substring(path.LastIndexOf("Assets"));
                    TextAsset cs = AssetDatabase.LoadAssetAtPath(assetPath, typeof(TextAsset)) as TextAsset;
                    EditorGUIUtility.PingObject(cs);
                    Selection.activeObject = cs;
                    AssetDatabase.OpenAsset(cs);
                }
            }
            else
            {
                Debug.LogError($"新建{className}失败，已存在类型 {className}");
            }
        }
    }
    static string GetSavePath()
    {
        string lastPath = EditorPrefs.GetString(Application.productName + "ScriptsPath", Application.dataPath + "/Scripts");
        string savePath = EditorUtility.SaveFolderPanel("保存目录", lastPath, "");
        EditorPrefs.SetString(Application.productName + "ScriptsPath", savePath);
        return savePath;
    }
}
