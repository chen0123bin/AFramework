#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWAssets.Editor
{
    /// <summary>
    /// 资源分析器
    /// </summary>
    public class AssetAnalyzer : EditorWindow
    {
        private Vector2 _scrollPos;
        private string _searchPath = "Assets";
        private List<AssetAnalysisResult> _results = new List<AssetAnalysisResult>();
        private bool _showDuplicates = true;
        private bool _showLargeFiles = true;
        private bool _showMissingRefs = true;
        private long _largeFileThreshold = 10 * 1024 * 1024; // 10MB
        
        [MenuItem("LWFramework/Assets/Analyze/Asset Analyzer")]
        public static void ShowWindow()
        {
            GetWindow<AssetAnalyzer>("Asset Analyzer");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            _searchPath = EditorGUILayout.TextField("Search Path", _searchPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    _searchPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            _showDuplicates = EditorGUILayout.Toggle("Show Duplicates", _showDuplicates);
            _showLargeFiles = EditorGUILayout.Toggle("Show Large Files", _showLargeFiles);
            _showMissingRefs = EditorGUILayout.Toggle("Show Missing Refs", _showMissingRefs);
            EditorGUILayout.EndHorizontal();
            
            _largeFileThreshold = EditorGUILayout.LongField("Large File Threshold (bytes)", _largeFileThreshold);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Analyze", GUILayout.Height(30)))
            {
                AnalyzeAssets();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Found {_results.Count} issues", EditorStyles.boldLabel);
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            foreach (var result in _results)
            {
                DrawAnalysisResult(result);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void AnalyzeAssets()
        {
            _results.Clear();
            
            EditorUtility.DisplayProgressBar("Analyzing", "Collecting assets...", 0);
            
            try
            {
                var allAssets = AssetDatabase.FindAssets("", new[] { _searchPath })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => !AssetDatabase.IsValidFolder(p))
                    .ToList();
                
                // 检查重复资源
                if (_showDuplicates)
                {
                    EditorUtility.DisplayProgressBar("Analyzing", "Checking duplicates...", 0.25f);
                    CheckDuplicates(allAssets);
                }
                
                // 检查大文件
                if (_showLargeFiles)
                {
                    EditorUtility.DisplayProgressBar("Analyzing", "Checking large files...", 0.5f);
                    CheckLargeFiles(allAssets);
                }
                
                // 检查丢失引用
                if (_showMissingRefs)
                {
                    EditorUtility.DisplayProgressBar("Analyzing", "Checking missing references...", 0.75f);
                    CheckMissingReferences(allAssets);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
        
        private void CheckDuplicates(List<string> assets)
        {
            var hashGroups = new Dictionary<string, List<string>>();
            
            foreach (var path in assets)
            {
                if (path.EndsWith(".cs") || path.EndsWith(".shader")) continue;
                
                var hash = HashUtility.ComputeFileMD5(path);
                if (hash == null) continue;
                
                if (!hashGroups.ContainsKey(hash))
                {
                    hashGroups[hash] = new List<string>();
                }
                hashGroups[hash].Add(path);
            }
            
            foreach (var group in hashGroups.Where(g => g.Value.Count > 1))
            {
                _results.Add(new AssetAnalysisResult
                {
                    Type = AnalysisIssueType.Duplicate,
                    AssetPaths = group.Value,
                    Message = $"Found {group.Value.Count} duplicate files"
                });
            }
        }
        
        private void CheckLargeFiles(List<string> assets)
        {
            foreach (var path in assets)
            {
                var fileInfo = new System.IO.FileInfo(path);
                if (fileInfo.Length > _largeFileThreshold)
                {
                    _results.Add(new AssetAnalysisResult
                    {
                        Type = AnalysisIssueType.LargeFile,
                        AssetPaths = new List<string> { path },
                        Message = $"File size: {FileUtility.FormatFileSize(fileInfo.Length)}"
                    });
                }
            }
        }
        
        private void CheckMissingReferences(List<string> assets)
        {
            var prefabPaths = assets.Where(p => p.EndsWith(".prefab")).ToList();
            
            foreach (var path in prefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                
                var missingRefs = FindMissingReferences(prefab);
                if (missingRefs.Count > 0)
                {
                    _results.Add(new AssetAnalysisResult
                    {
                        Type = AnalysisIssueType.MissingReference,
                        AssetPaths = new List<string> { path },
                        Message = $"Missing references: {string.Join(", ", missingRefs)}"
                    });
                }
            }
        }
        
        private List<string> FindMissingReferences(GameObject obj)
        {
            var missing = new List<string>();
            
            var components = obj.GetComponentsInChildren<Component>(true);
            foreach (var component in components)
            {
                if (component == null)
                {
                    missing.Add("Missing Script");
                    continue;
                }
                
                var so = new SerializedObject(component);
                var sp = so.GetIterator();
                
                while (sp.NextVisible(true))
                {
                    if (sp.propertyType == SerializedPropertyType.ObjectReference &&
                        sp.objectReferenceValue == null &&
                        sp.objectReferenceInstanceIDValue != 0)
                    {
                        missing.Add(sp.propertyPath);
                    }
                }
            }
            
            return missing;
        }
        
        private void DrawAnalysisResult(AssetAnalysisResult result)
        {
            EditorGUILayout.BeginVertical("box");
            
            // 图标和类型
            var icon = result.Type switch
            {
                AnalysisIssueType.Duplicate => EditorGUIUtility.IconContent("console.warnicon.sml"),
                AnalysisIssueType.LargeFile => EditorGUIUtility.IconContent("console.infoicon.sml"),
                AnalysisIssueType.MissingReference => EditorGUIUtility.IconContent("console.erroricon.sml"),
                _ => null
            };
            
            EditorGUILayout.BeginHorizontal();
            if (icon != null)
            {
                GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
            }
            EditorGUILayout.LabelField($"[{result.Type}] {result.Message}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            // 资源列表
            foreach (var path in result.AssetPaths)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath<Object>(path), 
                    typeof(Object), false);
                
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(path);
                    EditorGUIUtility.PingObject(Selection.activeObject);
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
    }
    
    public enum AnalysisIssueType
    {
        Duplicate,
        LargeFile,
        MissingReference
    }
    
    public class AssetAnalysisResult
    {
        public AnalysisIssueType Type;
        public List<string> AssetPaths;
        public string Message;
    }
}
#endif
