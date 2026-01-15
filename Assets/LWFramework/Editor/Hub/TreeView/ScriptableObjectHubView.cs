#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LWCore.Editor
{
    public class ScriptableObjectHubView : BaseHubTreeView
    {
        private readonly Type m_AssetType;
        private UnityEngine.Object m_Asset;
        private UnityEditor.Editor m_Inspector;
        private Vector2 m_Scroll;

        /// <summary>
        /// 创建 ScriptableObject 配置页（按类型查找/创建并内嵌 Inspector）。
        /// </summary>
        /// <param name="nodePath">左侧树节点路径。</param>
        /// <param name="iconPath">图标路径。</param>
        /// <param name="assetType">ScriptableObject 类型。</param>
        public ScriptableObjectHubView(string nodePath, string iconPath, Type assetType)
            : base(nodePath, iconPath)
        {
            m_AssetType = assetType;
            FindFirstAsset();
        }

        public override void OnDeselected()
        {
            if (m_Inspector != null)
            {
                GameObject.DestroyImmediate(m_Inspector);
                m_Inspector = null;
            }
        }

        protected override void DrawContent()
        {
            GUILayout.Space(8);
            string title = m_AssetType != null ? m_AssetType.Name : "Config";
            EditorGUILayout.LabelField(title, EditorStyles.largeLabel);
            EditorGUILayout.Space(6);

            using (new EditorGUILayout.HorizontalScope())
            {
                m_Asset = EditorGUILayout.ObjectField("资源", m_Asset, m_AssetType, false);

                if (GUILayout.Button("查找", GUILayout.Width(60)))
                {
                    FindFirstAsset();
                }

                using (new EditorGUI.DisabledScope(m_AssetType == null || !typeof(ScriptableObject).IsAssignableFrom(m_AssetType)))
                {
                    if (GUILayout.Button("创建", GUILayout.Width(60)))
                    {
                        CreateAsset();
                    }
                }

                using (new EditorGUI.DisabledScope(m_Asset == null))
                {
                    if (GUILayout.Button("定位", GUILayout.Width(60)))
                    {
                        EditorGUIUtility.PingObject(m_Asset);
                        Selection.activeObject = m_Asset;
                    }
                }
            }

            EditorGUILayout.Space(6);

            if (m_Asset == null)
            {
                EditorGUILayout.HelpBox("未绑定资源，点击“查找”自动定位一个同类型资源，或点击“创建”生成新的资产。", MessageType.Info);
                return;
            }

            if (m_Inspector == null || m_Inspector.target != m_Asset)
            {
                if (m_Inspector != null)
                {
                    GameObject.DestroyImmediate(m_Inspector);
                    m_Inspector = null;
                }

                UnityEditor.Editor.CreateCachedEditor(m_Asset, null, ref m_Inspector);
            }

            using (EditorGUILayout.ScrollViewScope scrollViewScope = new EditorGUILayout.ScrollViewScope(m_Scroll))
            {
                m_Scroll = scrollViewScope.scrollPosition;
                if (m_Inspector != null)
                {
                    m_Inspector.OnInspectorGUI();
                }
            }
        }

        /// <summary>
        /// 查找同类型的第一个资源并绑定。
        /// </summary>
        private void FindFirstAsset()
        {
            if (m_AssetType == null)
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets($"t:{m_AssetType.Name}");
            if (guids == null || guids.Length <= 0)
            {
                m_Asset = null;
                return;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            m_Asset = AssetDatabase.LoadAssetAtPath(assetPath, m_AssetType);
        }

        /// <summary>
        /// 创建新的 ScriptableObject 资产。
        /// </summary>
        private void CreateAsset()
        {
            if (m_AssetType == null || !typeof(ScriptableObject).IsAssignableFrom(m_AssetType))
            {
                return;
            }

            string defaultName = m_AssetType.Name;
            string path = EditorUtility.SaveFilePanelInProject("Create Config", defaultName, "asset", string.Empty);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            ScriptableObject asset = ScriptableObject.CreateInstance(m_AssetType);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            m_Asset = asset;
            EditorGUIUtility.PingObject(m_Asset);
            Selection.activeObject = m_Asset;
        }
    }
}
#endif
