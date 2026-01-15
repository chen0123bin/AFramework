#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LWCore.Editor
{
    /// <summary>
    /// LWFramework 编辑器工具 Hub：左侧树形导航，右侧显示内容/Inspector/入口按钮。
    /// </summary>
    public class WelcomeHubView : BaseHubTreeView
    {
        public WelcomeHubView(string nodePath, string iconPath)
            : base(nodePath, iconPath)
        {
        }

        protected override void DrawContent()
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("LWFramework Hub", EditorStyles.largeLabel);
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox("这是一个手动注册的 Hub 示例页。\n你可以通过 new XxxTreeView(\"模块/页面\", \"iconPath\") 把页面挂到左侧树上。", MessageType.Info);
        }
    }
}
#endif
