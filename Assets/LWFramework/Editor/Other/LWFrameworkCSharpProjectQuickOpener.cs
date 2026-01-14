using System;
using System.Collections.Generic;
using System.IO;
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;

public static class LWFrameworkCSharpProjectQuickOpener
{
    private const int EDITOR_SLOT_COUNT = 5;
    private const string PREFS_PREFIX = "LWFramework.QuickOpenCSharpProject";
    private const string ASSETS_MENU_ROOT = "Assets/LWFramework/C#工程/";
    private const string CONFIGURE_SLOT_MENU_ROOT = ASSETS_MENU_ROOT + "设置槽位/";
    private const string REGENERATE_MENU_PATH = ASSETS_MENU_ROOT + "重建快捷菜单";

    [InitializeOnLoadMethod]
    private static void InitializeOnLoad()
    {
        EditorApplication.delayCall += EnsureGeneratedMenu;
    }

    private static void EnsureGeneratedMenu()
    {
        MigratePrefsIfNeeded();
        LWFrameworkCSharpProjectQuickOpenerMenuGenerator.RegenerateIfNeeded();
    }

    private static void MigratePrefsIfNeeded()
    {
        bool hasAnyGlobalConfig = false;
        if (EditorPrefs.HasKey(GetDefaultSlotPrefKey()))
        {
            hasAnyGlobalConfig = true;
        }

        if (!hasAnyGlobalConfig)
        {
            for (int i = 0; i < EDITOR_SLOT_COUNT; i++)
            {
                if (EditorPrefs.HasKey(GetEditorSlotPrefKey(i)))
                {
                    hasAnyGlobalConfig = true;
                    break;
                }
            }
        }

        if (hasAnyGlobalConfig)
        {
            return;
        }

        string oldDefaultKey = string.Format("{0}.DefaultSlot.{1}", PREFS_PREFIX, Application.dataPath);
        if (EditorPrefs.HasKey(oldDefaultKey))
        {
            int defaultSlotIndex = EditorPrefs.GetInt(oldDefaultKey, 0);
            EditorPrefs.SetInt(GetDefaultSlotPrefKey(), defaultSlotIndex);
        }

        for (int i = 0; i < EDITOR_SLOT_COUNT; i++)
        {
            string oldSlotKey = string.Format("{0}.EditorSlot.{1}.{2}", PREFS_PREFIX, Application.dataPath, i);
            if (!EditorPrefs.HasKey(oldSlotKey))
            {
                continue;
            }

            string editorPath = EditorPrefs.GetString(oldSlotKey, string.Empty);
            if (string.IsNullOrEmpty(editorPath))
            {
                continue;
            }

            EditorPrefs.SetString(GetEditorSlotPrefKey(i), editorPath);
        }
    }

    [MenuItem(REGENERATE_MENU_PATH, false, 2999)]
    private static void RegenerateMenu()
    {
        LWFrameworkCSharpProjectQuickOpenerMenuGenerator.Regenerate(force: true);
    }

    [MenuItem(REGENERATE_MENU_PATH, true)]
    private static bool RegenerateMenuValidate()
    {
        return !EditorApplication.isCompiling;
    }

    [MenuItem(CONFIGURE_SLOT_MENU_ROOT + "槽位1...", false, 2101)]
    private static void ConfigureEditorSlot1()
    {
        ConfigureEditorSlot(0, false);
    }

    [MenuItem(CONFIGURE_SLOT_MENU_ROOT + "槽位2...", false, 2102)]
    private static void ConfigureEditorSlot2()
    {
        ConfigureEditorSlot(1, false);
    }

    [MenuItem(CONFIGURE_SLOT_MENU_ROOT + "槽位3...", false, 2103)]
    private static void ConfigureEditorSlot3()
    {
        ConfigureEditorSlot(2, false);
    }

    [MenuItem(CONFIGURE_SLOT_MENU_ROOT + "槽位4...", false, 2104)]
    private static void ConfigureEditorSlot4()
    {
        ConfigureEditorSlot(3, false);
    }

    [MenuItem(CONFIGURE_SLOT_MENU_ROOT + "槽位5...", false, 2105)]
    private static void ConfigureEditorSlot5()
    {
        ConfigureEditorSlot(4, false);
    }

    [MenuItem(CONFIGURE_SLOT_MENU_ROOT + "槽位1...", true)]
    private static bool ConfigureEditorSlot1Validate()
    {
        return !EditorApplication.isCompiling;
    }

    [MenuItem(CONFIGURE_SLOT_MENU_ROOT + "槽位2...", true)]
    private static bool ConfigureEditorSlot2Validate()
    {
        return !EditorApplication.isCompiling;
    }

    [MenuItem(CONFIGURE_SLOT_MENU_ROOT + "槽位3...", true)]
    private static bool ConfigureEditorSlot3Validate()
    {
        return !EditorApplication.isCompiling;
    }

    [MenuItem(CONFIGURE_SLOT_MENU_ROOT + "槽位4...", true)]
    private static bool ConfigureEditorSlot4Validate()
    {
        return !EditorApplication.isCompiling;
    }

    [MenuItem(CONFIGURE_SLOT_MENU_ROOT + "槽位5...", true)]
    private static bool ConfigureEditorSlot5Validate()
    {
        return !EditorApplication.isCompiling;
    }

    /// <summary>
    /// 快速打开：使用默认槽位，未配置则先引导设置。
    /// </summary>
    internal static void QuickOpenDefaultSlot()
    {
        if (EditorApplication.isCompiling)
        {
            return;
        }

        int defaultSlotIndex = GetDefaultSlotIndex();
        string editorPath = GetEditorPathBySlot(defaultSlotIndex);
        if (string.IsNullOrEmpty(editorPath))
        {
            ConfigureEditorSlot(defaultSlotIndex, true);
            return;
        }

        OpenCSharpProjectWithEditorPath(editorPath);
    }

    /// <summary>
    /// 使用指定槽位打开C#工程。
    /// </summary>
    internal static void OpenCSharpProjectWithSlot(int slotIndex)
    {
        if (EditorApplication.isCompiling)
        {
            return;
        }

        string editorPath = GetEditorPathBySlot(slotIndex);
        if (string.IsNullOrEmpty(editorPath))
        {
            EditorUtility.DisplayDialog("打开失败", string.Format("槽位{0}未配置编辑器路径。\n请先在菜单：{1} 中设置。", slotIndex + 1, CONFIGURE_SLOT_MENU_ROOT), "OK");
            return;
        }

        OpenCSharpProjectWithEditorPath(editorPath);
    }

    /// <summary>
    /// 选择exe并保存到指定槽位，可选配置后立即打开工程。
    /// </summary>
    internal static void ConfigureEditorSlot(int slotIndex, bool shouldOpenProjectAfterConfigured)
    {
        if (EditorApplication.isCompiling)
        {
            return;
        }

        string currentPath = GetEditorPathBySlot(slotIndex);
        string startDirectory = string.IsNullOrEmpty(currentPath) ? string.Empty : Path.GetDirectoryName(currentPath);
        string editorPath = EditorUtility.OpenFilePanel(string.Format("设置槽位{0}脚本编辑器", slotIndex + 1), startDirectory, "exe");
        if (string.IsNullOrEmpty(editorPath))
        {
            return;
        }

        SetEditorPathBySlot(slotIndex, editorPath);
        LWFrameworkCSharpProjectQuickOpenerMenuGenerator.Regenerate(force: true);
        if (shouldOpenProjectAfterConfigured)
        {
            OpenCSharpProjectWithEditorPath(editorPath);
        }
    }

    /// <summary>
    /// 清空指定槽位。
    /// </summary>
    internal static void ClearEditorSlot(int slotIndex)
    {
        if (EditorApplication.isCompiling)
        {
            return;
        }

        string slotKey = GetEditorSlotPrefKey(slotIndex);
        EditorPrefs.DeleteKey(slotKey);

        int defaultSlotIndex = GetDefaultSlotIndex();
        if (defaultSlotIndex == slotIndex)
        {
            SetDefaultSlotIndex(0);
            return;
        }

        LWFrameworkCSharpProjectQuickOpenerMenuGenerator.Regenerate(force: true);
    }

    /// <summary>
    /// 切换External Script Editor并走Open C# Project逻辑打开工程。
    /// </summary>
    private static void OpenCSharpProjectWithEditorPath(string editorPath)
    {
        if (string.IsNullOrEmpty(editorPath))
        {
            return;
        }

        CodeEditor.SetExternalScriptEditor(editorPath);

        IExternalCodeEditor externalCodeEditor = CodeEditor.CurrentEditor;
        if (externalCodeEditor == null)
        {
            EditorUtility.DisplayDialog("打开失败", "当前没有可用的脚本编辑器集成。\n请先在 Edit > Preferences > External Tools 配置 External Script Editor。", "OK");
            return;
        }

        bool isOpened = externalCodeEditor.OpenProject(string.Empty, 0, 0);
        if (!isOpened)
        {
            Debug.LogWarning(string.Format("打开C#工程失败：{0}", editorPath));
        }
    }

    /// <summary>
    /// 根据编辑器路径解析显示名。
    /// </summary>
    internal static string GetEditorDisplayNameByPath(string editorPath)
    {
        if (string.IsNullOrEmpty(editorPath))
        {
            return string.Empty;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(editorPath);
        }
        catch
        {
            fullPath = editorPath;
        }

        IExternalCodeEditor externalCodeEditor = CodeEditor.CurrentEditor;
        if (externalCodeEditor != null)
        {
            CodeEditor.Installation[] installations = externalCodeEditor.Installations;
            if (installations != null)
            {
                for (int i = 0; i < installations.Length; i++)
                {
                    CodeEditor.Installation installation = installations[i];
                    if (string.IsNullOrEmpty(installation.Path))
                    {
                        continue;
                    }

                    string installationFullPath;
                    try
                    {
                        installationFullPath = Path.GetFullPath(installation.Path);
                    }
                    catch
                    {
                        installationFullPath = installation.Path;
                    }

                    if (string.Equals(installationFullPath, fullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(installation.Name))
                        {
                            return installation.Name;
                        }
                        break;
                    }
                }
            }
        }

        try
        {
            return Path.GetFileNameWithoutExtension(fullPath);
        }
        catch
        {
            return fullPath;
        }
    }

    /// <summary>
    /// 获取默认槽位索引。
    /// </summary>
    internal static int GetDefaultSlotIndex()
    {
        string key = GetDefaultSlotPrefKey();
        int slotIndex = EditorPrefs.GetInt(key, 0);
        if (slotIndex < 0 || slotIndex >= EDITOR_SLOT_COUNT)
        {
            slotIndex = 0;
        }
        return slotIndex;
    }

    /// <summary>
    /// 设置默认槽位索引。
    /// </summary>
    internal static void SetDefaultSlotIndex(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= EDITOR_SLOT_COUNT)
        {
            slotIndex = 0;
        }
        string key = GetDefaultSlotPrefKey();
        EditorPrefs.SetInt(key, slotIndex);

        LWFrameworkCSharpProjectQuickOpenerMenuGenerator.Regenerate(force: true);
    }

    /// <summary>
    /// 读取指定槽位的编辑器路径。
    /// </summary>
    internal static string GetEditorPathBySlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= EDITOR_SLOT_COUNT)
        {
            return string.Empty;
        }
        string key = GetEditorSlotPrefKey(slotIndex);
        return EditorPrefs.GetString(key, string.Empty);
    }

    /// <summary>
    /// 写入指定槽位的编辑器路径。
    /// </summary>
    private static void SetEditorPathBySlot(int slotIndex, string editorPath)
    {
        if (slotIndex < 0 || slotIndex >= EDITOR_SLOT_COUNT)
        {
            return;
        }
        string key = GetEditorSlotPrefKey(slotIndex);
        EditorPrefs.SetString(key, editorPath);
    }

    /// <summary>
    /// 获取默认槽位的EditorPrefs键。
    /// </summary>
    private static string GetDefaultSlotPrefKey()
    {
        return string.Format("{0}.DefaultSlot", PREFS_PREFIX);
    }

    /// <summary>
    /// 获取编辑器槽位的EditorPrefs键。
    /// </summary>
    private static string GetEditorSlotPrefKey(int slotIndex)
    {
        return string.Format("{0}.EditorSlot.{1}", PREFS_PREFIX, slotIndex);
    }

    internal static bool CanOpenSlot(int slotIndex)
    {
        if (EditorApplication.isCompiling)
        {
            return false;
        }
        string editorPath = GetEditorPathBySlot(slotIndex);
        return !string.IsNullOrEmpty(editorPath);
    }

    internal static bool CanClearSlot(int slotIndex)
    {
        return CanOpenSlot(slotIndex);
    }
}

internal static class LWFrameworkCSharpProjectQuickOpenerMenuGenerator
{
    private const string GENERATED_FILE_NAME = "LWFrameworkCSharpProjectQuickOpenerMenu_Generated.cs";

    private static string m_CachedGeneratedAssetPath;
    private static string m_CachedProjectRootPath;

    public static void RegenerateIfNeeded()
    {
        Regenerate(force: false);
    }

    public static void Regenerate(bool force)
    {
        string generatedAssetPath = GetGeneratedAssetPath();
        string generatedFullPath = GetFullPathFromAssetPath(generatedAssetPath);

        string desiredContent = BuildGeneratedContent();

        string existingContent = string.Empty;
        if (File.Exists(generatedFullPath))
        {
            existingContent = File.ReadAllText(generatedFullPath);
        }

        if (!force && string.Equals(existingContent, desiredContent, StringComparison.Ordinal))
        {
            return;
        }

        DeleteLegacyGeneratedFiles(generatedAssetPath);

        string directoryPath = Path.GetDirectoryName(generatedFullPath);
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(generatedFullPath, desiredContent);
        AssetDatabase.ImportAsset(generatedAssetPath, ImportAssetOptions.ForceUpdate);
    }

    private static string GetGeneratedAssetPath()
    {
        if (!string.IsNullOrEmpty(m_CachedGeneratedAssetPath))
        {
            return m_CachedGeneratedAssetPath;
        }

        string scriptDirectoryAssetPath = GetQuickOpenerScriptDirectoryAssetPath();
        m_CachedGeneratedAssetPath = scriptDirectoryAssetPath + "/" + GENERATED_FILE_NAME;
        return m_CachedGeneratedAssetPath;
    }

    private static string GetQuickOpenerScriptDirectoryAssetPath()
    {
        string[] guids = AssetDatabase.FindAssets("LWFrameworkCSharpProjectQuickOpener t:Script");
        if (guids != null && guids.Length > 0)
        {
            string scriptAssetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            if (!string.IsNullOrEmpty(scriptAssetPath))
            {
                string directory = Path.GetDirectoryName(scriptAssetPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    return directory.Replace("\\", "/");
                }
            }
        }

        return "Assets/LWFramework/Editor/Other";
    }

    private static string GetProjectRootPath()
    {
        if (!string.IsNullOrEmpty(m_CachedProjectRootPath))
        {
            return m_CachedProjectRootPath;
        }

        string assetsPath = Application.dataPath;
        string rootPath = Directory.GetParent(assetsPath).FullName;
        m_CachedProjectRootPath = rootPath;
        return m_CachedProjectRootPath;
    }

    private static string GetFullPathFromAssetPath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return string.Empty;
        }

        string projectRootPath = GetProjectRootPath();
        string fullPath = Path.Combine(projectRootPath, assetPath);
        return Path.GetFullPath(fullPath);
    }

    private static void DeleteLegacyGeneratedFiles(string newGeneratedAssetPath)
    {
        string legacyAssetPath = "Assets/LWFramework/Editor/Other/" + GENERATED_FILE_NAME;
        if (string.Equals(legacyAssetPath, newGeneratedAssetPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<MonoScript>(legacyAssetPath) != null)
        {
            AssetDatabase.DeleteAsset(legacyAssetPath);
        }
    }

    private static string BuildGeneratedContent()
    {
        int defaultSlotIndex = LWFrameworkCSharpProjectQuickOpener.GetDefaultSlotIndex();
        string quickOpenLabel = BuildQuickOpenMenuLabel(defaultSlotIndex);

        string[] openMenuPaths = BuildSlotMenuPaths("用槽位打开/");
        string[] defaultMenuPaths = BuildSlotMenuPaths("设为默认/");
        string[] clearMenuPaths = BuildSlotMenuPaths("清空槽位/");

        System.Text.StringBuilder sb = new System.Text.StringBuilder(4096);
        sb.AppendLine("using UnityEditor;");
        sb.AppendLine();
        sb.AppendLine("public static class LWFrameworkCSharpProjectQuickOpenerMenuGenerated");
        sb.AppendLine("{");
        sb.AppendLine(string.Format("    [MenuItem(\"{0}\", false, 2000)]", quickOpenLabel));
        sb.AppendLine("    private static void QuickOpenDefault() { LWFrameworkCSharpProjectQuickOpener.QuickOpenDefaultSlot(); }");
        sb.AppendLine(string.Format("    [MenuItem(\"{0}\", true)]", quickOpenLabel));
        sb.AppendLine("    private static bool QuickOpenDefaultValidate() { return !EditorApplication.isCompiling; }");
        sb.AppendLine();

        for (int i = 0; i < openMenuPaths.Length; i++)
        {
            sb.AppendLine(string.Format("    [MenuItem(\"{0}\", false, {1})]", openMenuPaths[i], 2001 + i));
            sb.AppendLine(string.Format("    private static void OpenWithSlot{0}() {{ LWFrameworkCSharpProjectQuickOpener.OpenCSharpProjectWithSlot({0}); }}", i));
            sb.AppendLine(string.Format("    [MenuItem(\"{0}\", true)]", openMenuPaths[i]));
            sb.AppendLine(string.Format("    private static bool OpenWithSlot{0}Validate() {{ return LWFrameworkCSharpProjectQuickOpener.CanOpenSlot({0}); }}", i));
            sb.AppendLine();
        }

        for (int i = 0; i < defaultMenuPaths.Length; i++)
        {
            sb.AppendLine(string.Format("    [MenuItem(\"{0}\", false, {1})]", defaultMenuPaths[i], 2201 + i));
            sb.AppendLine(string.Format("    private static void SetDefaultSlot{0}() {{ LWFrameworkCSharpProjectQuickOpener.SetDefaultSlotIndex({0}); }}", i));
            sb.AppendLine(string.Format("    [MenuItem(\"{0}\", true)]", defaultMenuPaths[i]));
            sb.AppendLine(string.Format("    private static bool SetDefaultSlot{0}Validate() {{ Menu.SetChecked(\"{1}\", LWFrameworkCSharpProjectQuickOpener.GetDefaultSlotIndex() == {0}); return !EditorApplication.isCompiling; }}", i, defaultMenuPaths[i]));
            sb.AppendLine();
        }

        for (int i = 0; i < clearMenuPaths.Length; i++)
        {
            sb.AppendLine(string.Format("    [MenuItem(\"{0}\", false, {1})]", clearMenuPaths[i], 2301 + i));
            sb.AppendLine(string.Format("    private static void ClearSlot{0}() {{ LWFrameworkCSharpProjectQuickOpener.ClearEditorSlot({0}); }}", i));
            sb.AppendLine(string.Format("    [MenuItem(\"{0}\", true)]", clearMenuPaths[i]));
            sb.AppendLine(string.Format("    private static bool ClearSlot{0}Validate() {{ return LWFrameworkCSharpProjectQuickOpener.CanClearSlot({0}); }}", i));
            sb.AppendLine();
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string BuildQuickOpenMenuLabel(int defaultSlotIndex)
    {
        string editorPath = LWFrameworkCSharpProjectQuickOpener.GetEditorPathBySlot(defaultSlotIndex);
        string editorName = LWFrameworkCSharpProjectQuickOpener.GetEditorDisplayNameByPath(editorPath);
        string safeName = SanitizeMenuPart(string.IsNullOrEmpty(editorName) ? "未设置" : editorName);
        return string.Format("Assets/LWFramework/C#工程/快速打开(默认：槽位{0} - {1})", defaultSlotIndex + 1, safeName);
    }

    private static string[] BuildSlotMenuPaths(string subMenu)
    {
        string[] paths = new string[5];
        for (int i = 0; i < paths.Length; i++)
        {
            string editorPath = LWFrameworkCSharpProjectQuickOpener.GetEditorPathBySlot(i);
            string editorName = LWFrameworkCSharpProjectQuickOpener.GetEditorDisplayNameByPath(editorPath);
            string safeName = SanitizeMenuPart(string.IsNullOrEmpty(editorName) ? "未设置" : editorName);
            paths[i] = string.Format("Assets/LWFramework/C#工程/{0}槽位{1} - {2}", subMenu, i + 1, safeName);
        }
        return paths;
    }

    private static string SanitizeMenuPart(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        string result = text
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace("&", "＆")
            .Replace("%", "％")
            .Replace("#", "＃")
            .Replace("_", "＿");

        int maxLen = 40;
        if (result.Length > maxLen)
        {
            result = result.Substring(0, maxLen);
        }
        return result;
    }
}
