#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LWCore.Editor
{
    public class ExecuteMenuHubView : BaseHubTreeView
    {
        private readonly string m_MenuPath;

        /// <summary>
        /// 创建菜单入口页。
        /// </summary>
        /// <param name="nodePath">左侧树节点路径。</param>
        /// <param name="iconPath">图标路径。</param>
        /// <param name="menuPath">Unity 菜单路径（用于 ExecuteMenuItem）。</param>
        public ExecuteMenuHubView(string nodePath, string iconPath, string menuPath)
            : base(nodePath, iconPath)
        {
            m_MenuPath = menuPath;
        }

        protected override void DrawContent()
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField(m_MenuPath, EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("打开/执行", GUILayout.Width(120)))
                {
                    EditorApplication.ExecuteMenuItem(m_MenuPath);
                }

                if (GUILayout.Button("复制路径", GUILayout.Width(100)))
                {
                    EditorGUIUtility.systemCopyBuffer = m_MenuPath;
                }

                GUILayout.FlexibleSpace();
            }
        }
    }

}
#endif
