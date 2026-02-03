using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using UnityEngine.UI;
using System.Text;

public class EditorMenuItems
{
    /// <summary>
    /// 复制选中资产的路径到剪贴板
    /// </summary>
    [MenuItem("Assets/LWFramework/复制路径(Alt+C) &c")]
    static void CopyAssetPath()
    {
        if (EditorApplication.isCompiling)
        {
            return;
        }
        string path = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
        GUIUtility.systemCopyBuffer = path;
        Debug.Log(string.Format("systemCopyBuffer: {0}", path));
    }
    [MenuItem("Assets/LWFramework/生成截图 _F12")]
    /// <summary>
    /// 生成截图并保存到项目根目录下的 Screenshots 文件夹，文件名使用当前日期时间。
    /// </summary>
    static void CaptureScreenshot()
    {
        if (EditorApplication.isCompiling)
        {
            return;
        }

        string projectRootPath = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRootPath))
        {
            return;
        }

        string screenshotsDirectoryPath = Path.Combine(projectRootPath, "Screenshots");
        Directory.CreateDirectory(screenshotsDirectoryPath);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        string screenshotFilePath = Path.Combine(screenshotsDirectoryPath, $"图_{timestamp}.png");

        ScreenCapture.CaptureScreenshot(screenshotFilePath);
    }

    [MenuItem("GameObject/UIFramework/改变界面状态(Shift+T)  #t", false, -101)]
    static void ChangeViewState()
    {
        GameObject view = Selection.activeObject as GameObject;
        CanvasGroup canvasGroup = view.GetComponent<CanvasGroup>();
        canvasGroup.SetActive(canvasGroup.alpha == 0);
    }
    [MenuItem("GameObject/UIFramework/复制路径(Shift+C) #c", false, -101)]
    static void CopyParents()
    {
        if (EditorApplication.isCompiling)
        {
            return;
        }
        string path = GetParentPath(null, Selection.activeObject as GameObject, "");
        if (path.Contains("View/"))
        {
            int index = path.IndexOf("View/") + 5;
            path = path.Substring(index, path.Length - index);
        }
        GUIUtility.systemCopyBuffer = path;
        Debug.Log(string.Format("systemCopyBuffer: {0}", path));
    }
    [MenuItem("GameObject/UIFramework/创建脚本/View", false, -20)]
    static void CreateScriptBtn()
    {
        Object[] array = Selection.objects;
        if (!CheckView(array[0]))
        {
            return;
        }
        List<GameObject> chooseGameObjectList;
        GameObject gameObject;
        GetGameObject(array, out gameObject, out chooseGameObjectList);
        string savePath = GetSavePath();
        string viewName = gameObject.name;
        CreateView(viewName, savePath, gameObject, chooseGameObjectList);

    }
    [MenuItem("GameObject/UIFramework/创建脚本/Item", false, -19)]
    static void CreateItemScriptBtn()
    {
        Object[] array = Selection.objects;
        if (!CheckItem(array[0]))
        {
            return;
        }
        List<GameObject> chooseGameObjectList;
        GameObject gameObject;
        GetGameObject(array, out gameObject, out chooseGameObjectList);

        string savePath = GetSavePath();
        string behaviourName = gameObject.name;
        string generateFilePath = savePath + "/" + behaviourName + ".cs";
        var sw = new StreamWriter(generateFilePath, false, Encoding.UTF8);
        var strBuilder = new StringBuilder();

        strBuilder.AppendLine("using LWUI;");
        strBuilder.AppendLine("using UnityEngine.UI;");
        strBuilder.AppendLine("using UnityEngine;");
        strBuilder.AppendLine();
        strBuilder.AppendFormat("public class {0} : BaseUIItem ", behaviourName);
        strBuilder.AppendLine();
        strBuilder.AppendLine("{");
        //获取view上的组建
        foreach (var item in chooseGameObjectList)
        {
            string childName = item.name;
            string componentName = GetComponetName(item);
            strBuilder.AppendFormat("\t[UIElement(\"{0}\")]", GetParentPath(gameObject, item, ""));
            strBuilder.AppendLine();
            strBuilder.AppendFormat("\tprivate {0} {1};", componentName, ConvertName(childName));
            strBuilder.AppendLine();
        }
        strBuilder.AppendLine("\tpublic override  void Create(GameObject gameObject)");
        strBuilder.AppendLine("\t{");
        strBuilder.AppendLine("\t\tbase.Create(gameObject);");
        List<string> buttons = new List<string>();
        //获取ui控件
        foreach (var item in chooseGameObjectList)
        {
            string childName = item.name;
            string componentName = GetComponetName(item);

            //添加按钮点击事件监听
            if (componentName == "Button")
            {
                strBuilder.AppendFormat("\t\t{0}.onClick.AddListener(() => ", ConvertName(childName));
                strBuilder.AppendLine("\t\t{");
                strBuilder.AppendLine();
                strBuilder.AppendLine("\t\t});");
                strBuilder.AppendLine();
                buttons.Add(childName);
            }
        }

        strBuilder.AppendLine("\t}");
        strBuilder.AppendLine("\tpublic override void OnUnSpawn()");
        strBuilder.AppendLine("\t{");
        strBuilder.AppendLine("\t\tbase.OnUnSpawn();");
        strBuilder.AppendLine("\t}");
        strBuilder.AppendLine("\tpublic override void OnRelease()");
        strBuilder.AppendLine("\t{");
        strBuilder.AppendLine("\t\tbase.OnRelease();");
        strBuilder.AppendLine("\t}");

        strBuilder.AppendLine("}");
        sw.Write(strBuilder);
        sw.Flush();
        sw.Close();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    [MenuItem("GameObject/UIFramework/创建脚本/CopyComponet", false, -18)]
    static void CopyComponet()
    {
        Object[] array = Selection.objects;
        List<GameObject> chooseGameObjectList;
        GameObject gameObject;
        GetGameObject(array, out gameObject, out chooseGameObjectList);
        var strBuilder = new StringBuilder();
        //获取view上的组建
        foreach (var item in chooseGameObjectList)
        {
            string childName = item.name;
            string componentName = GetComponetName(item);
            strBuilder.AppendFormat("\t[UIElement(\"{0}\")]", GetParentPath(gameObject, item, ""));
            strBuilder.AppendLine();
            strBuilder.AppendFormat("\tprivate {0} {1};", componentName, ConvertName(childName));
            strBuilder.AppendLine();
        }
        GUIUtility.systemCopyBuffer = strBuilder.ToString();
        Debug.Log(strBuilder.ToString());
    }
    static bool CheckItem(Object obj)
    {
        bool ret = true;
        if (!obj.name.Contains("Item"))
        {
            Debug.LogError($"{obj} 名称中必须包含Item 否则不能创建为Item");
            ret = false;
        }
        return ret;
    }
    static bool CheckView(Object obj)
    {
        bool ret = true;
        if (!obj.name.Contains("View"))
        {
            Debug.LogError($"{obj} 名称中必须包含View 否则不能创建为View");
            ret = false;
        }
        else if (!(obj is GameObject))
        {
            Debug.LogError($"{obj} 不是GameObject对象");
            ret = false;
        }
        else if ((obj as GameObject).GetComponent<CanvasGroup>() == null)
        {
            Debug.LogError($"{obj} 没有CanvasGroup组件");
            ret = false;
        }
        return ret;
    }
    static void GetGameObject(Object[] array, out GameObject gameObject, out List<GameObject> chooseGameObjectList)
    {
        if (array.Length <= 1)
        {
            Debug.LogError($"选中的物体必须两个以上");
        }
        chooseGameObjectList = new List<GameObject>();
        for (int i = 1; i < array.Length; i++)
        {
            if (array[i] is GameObject)
            {
                chooseGameObjectList.Add(array[i] as GameObject);
            }
        }
        gameObject = array[0] as GameObject;
    }
    static string GetSavePath()
    {
        string lastPath = PlayerPrefs.GetString("LastViewSavePath");
        string savePath = EditorUtility.SaveFolderPanel("保存目录", lastPath, "");
        PlayerPrefs.SetString("LastViewSavePath", savePath);
        return savePath;
    }

    static void CreateView(string viewName, string savePath, GameObject gameObject, List<GameObject> chooseGameObjectList)
    {
        string loadPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
        string generateFilePath = savePath + "/" + viewName + ".cs";
        var sw = new StreamWriter(generateFilePath, false, Encoding.UTF8);
        var strBuilder = new StringBuilder();
        strBuilder.AppendLine("using LWUI;");
        strBuilder.AppendLine("using UnityEngine.UI;");
        strBuilder.AppendLine("using UnityEngine;");
        strBuilder.AppendLine();
        strBuilder.AppendFormat("[UIViewData(\"{0}\",(int)FindType.Name,\"LWFramework/Canvas/Normal\")]", loadPath);
        strBuilder.AppendLine();
        strBuilder.AppendFormat("public class {0} : BaseUIView ", viewName);
        strBuilder.AppendLine();
        strBuilder.AppendLine("{");
        strBuilder.AppendLine();
        //获取view上的组建
        foreach (var item in chooseGameObjectList)
        {
            string childName = item.name;
            string componentName = GetComponetName(item);
            strBuilder.AppendFormat("\t[UIElement(\"{0}\")]", GetParentPath(gameObject, item, ""));
            strBuilder.AppendLine();
            strBuilder.AppendFormat("\tprivate {0} {1};", componentName, ConvertName(childName));
            strBuilder.AppendLine();
        }

        strBuilder.AppendLine("\tpublic override  void CreateView(GameObject gameObject)");
        strBuilder.AppendLine("\t{");
        strBuilder.AppendLine("\t\tbase.CreateView(gameObject);");
        List<string> buttons = new List<string>();
        //获取ui控件
        foreach (var item in chooseGameObjectList)
        {
            string childName = item.name;
            string componentName = GetComponetName(item);

            //添加按钮点击事件监听
            if (componentName == "Button")
            {
                strBuilder.AppendFormat("\t\t{0}.onClick.AddListener(() => ", ConvertName(childName));
                strBuilder.AppendLine("\t\t{");
                strBuilder.AppendLine();
                strBuilder.AppendLine("\t\t});");
                strBuilder.AppendLine();
                buttons.Add(childName);
            }
            //添加按钮点击事件监听
            if (componentName == "Toggle")
            {
                strBuilder.AppendFormat("\t\t{0}.onValueChanged.AddListener((value) => ", ConvertName(childName));
                strBuilder.AppendLine("\t\t{");
                strBuilder.AppendLine();
                strBuilder.AppendLine("\t\t});");
                strBuilder.AppendLine();
                buttons.Add(childName);
            }
        }
        strBuilder.AppendLine("\t}");


        //类的结尾括号
        strBuilder.AppendLine("}");
        sw.Write(strBuilder);
        sw.Flush();
        sw.Close();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static string GetComponetName(GameObject gameObject)
    {
        if (null != gameObject.GetComponent<ScrollRect>())
            return "ScrollRect";
        else if (null != gameObject.GetComponent<Button>())
            return "Button";
        else if (null != gameObject.GetComponent<Slider>())
            return "Slider";
        else if (null != gameObject.GetComponent<Toggle>())
            return "Toggle";
#if TMPRO

        else if (null != gameObject.GetComponent<TMPro.TMP_InputField>())
            return "TMPro.TMP_InputField";
        else if (null != gameObject.GetComponent<TMPro.TMP_Dropdown>())
            return "TMPro.TMP_Dropdown";
        else if (null != gameObject.GetComponent<TMPro.TMP_Text>())
            return "TMPro.TMP_Text";
#else
        else if (null != gameObject.GetComponent<InputField>())
            return "InputField";
        else if (null != gameObject.GetComponent<Dropdown>())
            return "Dropdown";
        else if (null != gameObject.GetComponent<Text>())
            return "Text";
#endif       
        else if (null != gameObject.GetComponent<RawImage>())
            return "RawImage";
        else if (null != gameObject.GetComponent<Image>())
            return "Image";
        else if (null != gameObject.GetComponent<CanvasGroup>())
            return "CanvasGroup";
        return "Transform";
    }


    static string GetParentPath(GameObject gameObject, GameObject child, string str)
    {
        if (child.transform.parent == null || child.transform.parent.gameObject == gameObject)
        {
            str = child.name + str;
            return str;
        }
        else
        {
            str = "/" + child.name + str;
            return GetParentPath(gameObject, child.transform.parent.gameObject, str);
        }

    }

    static string ConvertName(string name)
    {
        return "m_" + name;
    }
}
