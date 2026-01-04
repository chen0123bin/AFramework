#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWAssets.Editor
{
    /// <summary>
    /// 依赖查看器
    /// </summary>
    public class DependencyViewer : EditorWindow
    {
        private string _assetPath = "";
        private Object _selectedAsset;
        private Vector2 _scrollPos;
        private List<DependencyNode> _dependencyTree = new List<DependencyNode>();
        private bool _showIndirectDependencies = true;
        private int _maxDepth = 3;
        
        [MenuItem("LWAssets/Tools/Dependency Viewer")]
        public static void ShowWindow()
        {
            GetWindow<DependencyViewer>("Dependency Viewer");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Asset Dependency Viewer", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 资源选择
            EditorGUILayout.BeginHorizontal();
            _selectedAsset = EditorGUILayout.ObjectField("Select Asset", _selectedAsset, typeof(Object), false);
            
            if (GUILayout.Button("Analyze", GUILayout.Width(80)))
            {
                if (_selectedAsset != null)
                {
                    _assetPath = AssetDatabase.GetAssetPath(_selectedAsset);
                    AnalyzeDependencies();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 设置
            _showIndirectDependencies = EditorGUILayout.Toggle("Show Indirect Dependencies", _showIndirectDependencies);
            _maxDepth = EditorGUILayout.IntSlider("Max Depth", _maxDepth, 1, 5);
            
            EditorGUILayout.Space();
            
            // 显示依赖树
            if (_dependencyTree.Count > 0)
            {
                EditorGUILayout.LabelField($"Dependencies for: {_assetPath}", EditorStyles.boldLabel);
                EditorGUILayout.Space();
                
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                
                foreach (var node in _dependencyTree)
                {
                    DrawDependencyNode(node, 0);
                }
                
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("Select an asset and click Analyze to view dependencies.", MessageType.Info);
            }
        }
        
        private void AnalyzeDependencies()
        {
            _dependencyTree.Clear();
            
            if (string.IsNullOrEmpty(_assetPath)) return;
            
            var asset = AssetDatabase.LoadAssetAtPath<Object>(_assetPath);
            if (asset == null) return;
            
            // 获取直接依赖
            var dependencies = AssetDatabase.GetDependencies(_assetPath, false);
            
            foreach (var dep in dependencies)
            {
                if (dep == _assetPath) continue;
                
                var node = new DependencyNode
                {
                    Path = dep,
                    Type = AssetDatabase.GetMainAssetTypeAtPath(dep)?.Name ?? "Unknown",
                    IsDirect = true
                };
                
                if (_showIndirectDependencies)
                {
                    node.Children = GetIndirectDependencies(dep, 1);
                }
                
                _dependencyTree.Add(node);
            }
            
            // 统计信息
            var totalSize = CalculateTotalSize(_dependencyTree);
            Debug.Log($"[LWAssets] Found {_dependencyTree.Count} direct dependencies, total size: {FileUtility.FormatFileSize(totalSize)}");
        }
        
        private List<DependencyNode> GetIndirectDependencies(string assetPath, int currentDepth)
        {
            if (currentDepth >= _maxDepth) return new List<DependencyNode>();
            
            var dependencies = AssetDatabase.GetDependencies(assetPath, false);
            var nodes = new List<DependencyNode>();
            
            foreach (var dep in dependencies)
            {
                if (dep == assetPath) continue;
                
                var node = new DependencyNode
                {
                    Path = dep,
                    Type = AssetDatabase.GetMainAssetTypeAtPath(dep)?.Name ?? "Unknown",
                    IsDirect = false,
                    Depth = currentDepth
                };
                
                if (currentDepth < _maxDepth)
                {
                    node.Children = GetIndirectDependencies(dep, currentDepth + 1);
                }
                
                nodes.Add(node);
            }
            
            return nodes;
        }
        
        private void DrawDependencyNode(DependencyNode node, int indent)
        {
            var indentStr = new string(' ', indent * 4);
            var icon = GetIconForType(node.Type);
            
            EditorGUILayout.BeginHorizontal();
            
            if (icon != null)
            {
                GUILayout.Label(icon, GUILayout.Width(16), GUILayout.Height(16));
            }
            
            EditorGUILayout.LabelField($"{indentStr}{Path.GetFileName(node.Path)}");
            EditorGUILayout.LabelField(node.Type, GUILayout.Width(100));
            
            if (GUILayout.Button("Ping", GUILayout.Width(50)))
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(node.Path);
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 递归绘制子节点
            if (node.Children != null && node.Children.Count > 0)
            {
                foreach (var child in node.Children)
                {
                    DrawDependencyNode(child, indent + 1);
                }
            }
        }
        
        private Texture GetIconForType(string type)
        {
            return type switch
            {
                "Texture2D" => EditorGUIUtility.IconContent("Texture Icon").image,
                "Material" => EditorGUIUtility.IconContent("Material Icon").image,
                "Mesh" => EditorGUIUtility.IconContent("Mesh Icon").image,
                "GameObject" => EditorGUIUtility.IconContent("GameObject Icon").image,
                "Prefab" => EditorGUIUtility.IconContent("Prefab Icon").image,
                "Shader" => EditorGUIUtility.IconContent("Shader Icon").image,
                "ScriptableObject" => EditorGUIUtility.IconContent("ScriptableObject Icon").image,
                "AudioClip" => EditorGUIUtility.IconContent("AudioClip Icon").image,
                "AnimationClip" => EditorGUIUtility.IconContent("AnimationClip Icon").image,
                _ => null
            };
        }
        
        private long CalculateTotalSize(List<DependencyNode> nodes)
        {
            long totalSize = 0;
            
            foreach (var node in nodes)
            {
                var fileInfo = new FileInfo(node.Path);
                if (fileInfo.Exists)
                {
                    totalSize += fileInfo.Length;
                }
                
                if (node.Children != null)
                {
                    totalSize += CalculateTotalSize(node.Children);
                }
            }
            
            return totalSize;
        }
    }
    
    public class DependencyNode
    {
        public string Path;
        public string Type;
        public bool IsDirect;
        public int Depth;
        public List<DependencyNode> Children = new List<DependencyNode>();
    }
}
#endif
