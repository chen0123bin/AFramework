using LWUI;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace LWFramework.Editor.UI
{
    [CustomEditor(typeof(RoundedImage), true)]
    [CanEditMultipleObjects]
    public class RoundedImageEditor : ImageEditor
    {
        private SerializedProperty m_IsIndependentCorners;
        private SerializedProperty m_CornerRadius;
        private SerializedProperty m_TopLeftRadius;
        private SerializedProperty m_TopRightRadius;
        private SerializedProperty m_BottomRightRadius;
        private SerializedProperty m_BottomLeftRadius;
        private SerializedProperty m_IsHollowAreaRaycastEnabled;
        private SerializedProperty m_IsShaderRenderingEnabled;
        private SerializedProperty m_RoundedShaderMaterial;
        private SerializedProperty m_IsBorderEnabled;
        private SerializedProperty m_BorderColor;
        private SerializedProperty m_BorderThickness;
        private SerializedProperty m_IsHollow;

        /// <summary>
        /// 缓存 RoundedImage 的序列化属性引用，避免每帧查找带来额外开销。
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            m_IsIndependentCorners = serializedObject.FindProperty("m_IsIndependentCorners");
            m_CornerRadius = serializedObject.FindProperty("m_CornerRadius");
            m_TopLeftRadius = serializedObject.FindProperty("m_TopLeftRadius");
            m_TopRightRadius = serializedObject.FindProperty("m_TopRightRadius");
            m_BottomRightRadius = serializedObject.FindProperty("m_BottomRightRadius");
            m_BottomLeftRadius = serializedObject.FindProperty("m_BottomLeftRadius");
            m_IsHollowAreaRaycastEnabled = serializedObject.FindProperty("m_IsHollowAreaRaycastEnabled");
            m_IsShaderRenderingEnabled = serializedObject.FindProperty("m_IsShaderRenderingEnabled");
            m_RoundedShaderMaterial = serializedObject.FindProperty("m_RoundedShaderMaterial");
            m_IsBorderEnabled = serializedObject.FindProperty("m_IsBorderEnabled");
            m_BorderColor = serializedObject.FindProperty("m_BorderColor");
            m_BorderThickness = serializedObject.FindProperty("m_BorderThickness");
            m_IsHollow = serializedObject.FindProperty("m_IsHollow");
        }

        /// <summary>
        /// 绘制 RoundedImage 的自定义 Inspector，并保持与 Image 默认 Inspector 兼容。
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.Space(8.0f);
            EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);

            if (m_IsShaderRenderingEnabled != null) EditorGUILayout.PropertyField(m_IsShaderRenderingEnabled, new GUIContent("Shader Rendering"));
            if (m_RoundedShaderMaterial != null) EditorGUILayout.PropertyField(m_RoundedShaderMaterial, new GUIContent("Shader Material"));

            EditorGUILayout.Space(8.0f);
            EditorGUILayout.LabelField("Rounded", EditorStyles.boldLabel);

            if (m_IsIndependentCorners != null)
            {
                EditorGUILayout.PropertyField(m_IsIndependentCorners, new GUIContent("Independent Corners"));
            }

            if (m_IsIndependentCorners != null && m_IsIndependentCorners.boolValue)
            {
                if (m_TopLeftRadius != null) EditorGUILayout.PropertyField(m_TopLeftRadius, new GUIContent("Top Left"));
                if (m_TopRightRadius != null) EditorGUILayout.PropertyField(m_TopRightRadius, new GUIContent("Top Right"));
                if (m_BottomRightRadius != null) EditorGUILayout.PropertyField(m_BottomRightRadius, new GUIContent("Bottom Right"));
                if (m_BottomLeftRadius != null) EditorGUILayout.PropertyField(m_BottomLeftRadius, new GUIContent("Bottom Left"));
            }
            else
            {
                if (m_CornerRadius != null) EditorGUILayout.PropertyField(m_CornerRadius, new GUIContent("Corner Radius"));
            }

            if (m_IsHollowAreaRaycastEnabled != null) EditorGUILayout.PropertyField(m_IsHollowAreaRaycastEnabled, new GUIContent("Hollow Area Raycast"));

            EditorGUILayout.Space(8.0f);
            EditorGUILayout.LabelField("Border", EditorStyles.boldLabel);

            if (m_IsHollow != null) EditorGUILayout.PropertyField(m_IsHollow, new GUIContent("Hollow"));
            if (m_IsBorderEnabled != null) EditorGUILayout.PropertyField(m_IsBorderEnabled, new GUIContent("Border Enabled"));
            if (m_BorderThickness != null) EditorGUILayout.PropertyField(m_BorderThickness, new GUIContent("Border Thickness"));
            if (m_BorderColor != null) EditorGUILayout.PropertyField(m_BorderColor, new GUIContent("Border Color"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
