using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class EditorMenuItems
{
    /// <summary>
    /// 复制选中资产的路径到剪贴板
    /// </summary>
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
    [MenuItem("Assets/生成截图 _F12")]
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
}
