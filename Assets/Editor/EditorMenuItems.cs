using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EditorMenuItems  
{
   [MenuItem("Assets/复制路径(Alt+C) &c")]
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
}
