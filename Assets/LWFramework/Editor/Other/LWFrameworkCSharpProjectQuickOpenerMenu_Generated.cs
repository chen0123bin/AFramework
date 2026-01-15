using UnityEditor;

public static class LWFrameworkCSharpProjectQuickOpenerMenuGenerated
{
    [MenuItem("Assets/LWFramework/C#工程/快速打开(默认：槽位1 - Trae)", false, 2000)]
    private static void QuickOpenDefault() { LWFrameworkCSharpProjectQuickOpener.QuickOpenDefaultSlot(); }
    [MenuItem("Assets/LWFramework/C#工程/快速打开(默认：槽位1 - Trae)", true)]
    private static bool QuickOpenDefaultValidate() { return !EditorApplication.isCompiling; }

    [MenuItem("Assets/LWFramework/C#工程/用槽位打开/槽位1 - Trae", false, 2001)]
    private static void OpenWithSlot0() { LWFrameworkCSharpProjectQuickOpener.OpenCSharpProjectWithSlot(0); }
    [MenuItem("Assets/LWFramework/C#工程/用槽位打开/槽位1 - Trae", true)]
    private static bool OpenWithSlot0Validate() { return LWFrameworkCSharpProjectQuickOpener.CanOpenSlot(0); }

    [MenuItem("Assets/LWFramework/C#工程/用槽位打开/槽位2 - devenv", false, 2002)]
    private static void OpenWithSlot1() { LWFrameworkCSharpProjectQuickOpener.OpenCSharpProjectWithSlot(1); }
    [MenuItem("Assets/LWFramework/C#工程/用槽位打开/槽位2 - devenv", true)]
    private static bool OpenWithSlot1Validate() { return LWFrameworkCSharpProjectQuickOpener.CanOpenSlot(1); }

    [MenuItem("Assets/LWFramework/C#工程/用槽位打开/槽位3 - Trae CN", false, 2003)]
    private static void OpenWithSlot2() { LWFrameworkCSharpProjectQuickOpener.OpenCSharpProjectWithSlot(2); }
    [MenuItem("Assets/LWFramework/C#工程/用槽位打开/槽位3 - Trae CN", true)]
    private static bool OpenWithSlot2Validate() { return LWFrameworkCSharpProjectQuickOpener.CanOpenSlot(2); }

    [MenuItem("Assets/LWFramework/C#工程/用槽位打开/槽位4 - 未设置", false, 2004)]
    private static void OpenWithSlot3() { LWFrameworkCSharpProjectQuickOpener.OpenCSharpProjectWithSlot(3); }
    [MenuItem("Assets/LWFramework/C#工程/用槽位打开/槽位4 - 未设置", true)]
    private static bool OpenWithSlot3Validate() { return LWFrameworkCSharpProjectQuickOpener.CanOpenSlot(3); }

    [MenuItem("Assets/LWFramework/C#工程/用槽位打开/槽位5 - 未设置", false, 2005)]
    private static void OpenWithSlot4() { LWFrameworkCSharpProjectQuickOpener.OpenCSharpProjectWithSlot(4); }
    [MenuItem("Assets/LWFramework/C#工程/用槽位打开/槽位5 - 未设置", true)]
    private static bool OpenWithSlot4Validate() { return LWFrameworkCSharpProjectQuickOpener.CanOpenSlot(4); }

    [MenuItem("Assets/LWFramework/C#工程/设为默认/槽位1 - Trae", false, 2201)]
    private static void SetDefaultSlot0() { LWFrameworkCSharpProjectQuickOpener.SetDefaultSlotIndex(0); }
    [MenuItem("Assets/LWFramework/C#工程/设为默认/槽位1 - Trae", true)]
    private static bool SetDefaultSlot0Validate() { Menu.SetChecked("Assets/LWFramework/C#工程/设为默认/槽位1 - Trae", LWFrameworkCSharpProjectQuickOpener.GetDefaultSlotIndex() == 0); return !EditorApplication.isCompiling; }

    [MenuItem("Assets/LWFramework/C#工程/设为默认/槽位2 - devenv", false, 2202)]
    private static void SetDefaultSlot1() { LWFrameworkCSharpProjectQuickOpener.SetDefaultSlotIndex(1); }
    [MenuItem("Assets/LWFramework/C#工程/设为默认/槽位2 - devenv", true)]
    private static bool SetDefaultSlot1Validate() { Menu.SetChecked("Assets/LWFramework/C#工程/设为默认/槽位2 - devenv", LWFrameworkCSharpProjectQuickOpener.GetDefaultSlotIndex() == 1); return !EditorApplication.isCompiling; }

    [MenuItem("Assets/LWFramework/C#工程/设为默认/槽位3 - Trae CN", false, 2203)]
    private static void SetDefaultSlot2() { LWFrameworkCSharpProjectQuickOpener.SetDefaultSlotIndex(2); }
    [MenuItem("Assets/LWFramework/C#工程/设为默认/槽位3 - Trae CN", true)]
    private static bool SetDefaultSlot2Validate() { Menu.SetChecked("Assets/LWFramework/C#工程/设为默认/槽位3 - Trae CN", LWFrameworkCSharpProjectQuickOpener.GetDefaultSlotIndex() == 2); return !EditorApplication.isCompiling; }

    [MenuItem("Assets/LWFramework/C#工程/设为默认/槽位4 - 未设置", false, 2204)]
    private static void SetDefaultSlot3() { LWFrameworkCSharpProjectQuickOpener.SetDefaultSlotIndex(3); }
    [MenuItem("Assets/LWFramework/C#工程/设为默认/槽位4 - 未设置", true)]
    private static bool SetDefaultSlot3Validate() { Menu.SetChecked("Assets/LWFramework/C#工程/设为默认/槽位4 - 未设置", LWFrameworkCSharpProjectQuickOpener.GetDefaultSlotIndex() == 3); return !EditorApplication.isCompiling; }

    [MenuItem("Assets/LWFramework/C#工程/设为默认/槽位5 - 未设置", false, 2205)]
    private static void SetDefaultSlot4() { LWFrameworkCSharpProjectQuickOpener.SetDefaultSlotIndex(4); }
    [MenuItem("Assets/LWFramework/C#工程/设为默认/槽位5 - 未设置", true)]
    private static bool SetDefaultSlot4Validate() { Menu.SetChecked("Assets/LWFramework/C#工程/设为默认/槽位5 - 未设置", LWFrameworkCSharpProjectQuickOpener.GetDefaultSlotIndex() == 4); return !EditorApplication.isCompiling; }

    [MenuItem("Assets/LWFramework/C#工程/清空槽位/槽位1 - Trae", false, 2301)]
    private static void ClearSlot0() { LWFrameworkCSharpProjectQuickOpener.ClearEditorSlot(0); }
    [MenuItem("Assets/LWFramework/C#工程/清空槽位/槽位1 - Trae", true)]
    private static bool ClearSlot0Validate() { return LWFrameworkCSharpProjectQuickOpener.CanClearSlot(0); }

    [MenuItem("Assets/LWFramework/C#工程/清空槽位/槽位2 - devenv", false, 2302)]
    private static void ClearSlot1() { LWFrameworkCSharpProjectQuickOpener.ClearEditorSlot(1); }
    [MenuItem("Assets/LWFramework/C#工程/清空槽位/槽位2 - devenv", true)]
    private static bool ClearSlot1Validate() { return LWFrameworkCSharpProjectQuickOpener.CanClearSlot(1); }

    [MenuItem("Assets/LWFramework/C#工程/清空槽位/槽位3 - Trae CN", false, 2303)]
    private static void ClearSlot2() { LWFrameworkCSharpProjectQuickOpener.ClearEditorSlot(2); }
    [MenuItem("Assets/LWFramework/C#工程/清空槽位/槽位3 - Trae CN", true)]
    private static bool ClearSlot2Validate() { return LWFrameworkCSharpProjectQuickOpener.CanClearSlot(2); }

    [MenuItem("Assets/LWFramework/C#工程/清空槽位/槽位4 - 未设置", false, 2304)]
    private static void ClearSlot3() { LWFrameworkCSharpProjectQuickOpener.ClearEditorSlot(3); }
    [MenuItem("Assets/LWFramework/C#工程/清空槽位/槽位4 - 未设置", true)]
    private static bool ClearSlot3Validate() { return LWFrameworkCSharpProjectQuickOpener.CanClearSlot(3); }

    [MenuItem("Assets/LWFramework/C#工程/清空槽位/槽位5 - 未设置", false, 2305)]
    private static void ClearSlot4() { LWFrameworkCSharpProjectQuickOpener.ClearEditorSlot(4); }
    [MenuItem("Assets/LWFramework/C#工程/清空槽位/槽位5 - 未设置", true)]
    private static bool ClearSlot4Validate() { return LWFrameworkCSharpProjectQuickOpener.CanClearSlot(4); }

}
