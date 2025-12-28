#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWAssets.Editor
{
    /// <summary>
    /// Bundle查看器
    /// </summary>
    public class BundleViewer : EditorWindow
    {
        private Vector2 _bundleListScrollPos;
        private Vector2 _assetListScrollPos;
        private Vector2 _dependencyScrollPos;
        
        private BundleManifest _manifest;
        private string _manifestPath;
        private BundleInfo _selectedBundle;
        private string _searchText = "";
        private int _selectedTab;
        private readonly string[] _tabs = { "Bundles", "Assets", "Dependencies" };
        
        [MenuItem("LWAssets/Tools/Bundle Viewer")]
        public static void ShowWindow()
        {
            GetWindow<BundleViewer>("Bundle Viewer");
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            
            if (_manifest == null)
            {
                EditorGUILayout.HelpBox("Load a manifest file to view bundles.", MessageType.Info);
                return;
            }
            
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabs);
            EditorGUILayout.Space();
            
            switch (_selectedTab)
            {
                case 0:
                    DrawBundleList();
                    break;
                case 1:
                    DrawAssetList();
                    break;
                case 2:
                    DrawDependencyGraph();
                    break;
            }
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Load Manifest", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                var path = EditorUtility.OpenFilePanel("Select Manifest", 
                    Application.dataPath + "/../AssetBundles", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    LoadManifest(path);
                }
            }
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                if (!string.IsNullOrEmpty(_manifestPath))
                {
                    LoadManifest(_manifestPath);
                }
            }
            
            GUILayout.FlexibleSpace();
            
            _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, 
                GUILayout.Width(200));
            
            if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSeachCancelButton")))
            {
                _searchText = "";
                GUI.FocusControl(null);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 显示清单信息
            if (_manifest != null)
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField($"Version: {_manifest.Version}");
                EditorGUILayout.LabelField($"Platform: {_manifest.Platform}");
                EditorGUILayout.LabelField($"Bundles: {_manifest.Bundles.Count}");
                EditorGUILayout.LabelField($"Total Size: {FileUtility.FormatFileSize(_manifest.GetTotalSize())}");
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private void LoadManifest(string path)
        {
            try
            {
                var json = File.ReadAllText(path);
                _manifest = BundleManifest.FromJson(json);
                _manifestPath = path;
                _selectedBundle = null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LWAssets] Failed to load manifest: {ex.Message}");
            }
        }
        
        private void DrawBundleList()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Bundle列表
            EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width * 0.4f));
            EditorGUILayout.LabelField("Bundle List", EditorStyles.boldLabel);
            
            _bundleListScrollPos = EditorGUILayout.BeginScrollView(_bundleListScrollPos);
            
            var filteredBundles = _manifest.Bundles
                .Where(b => string.IsNullOrEmpty(_searchText) || 
                    b.BundleName.ToLower().Contains(_searchText.ToLower()))
                .OrderByDescending(b => b.Size);
            
            foreach (var bundle in filteredBundles)
            {
                var isSelected = _selectedBundle == bundle;
                var style = isSelected ? "selectionRect" : "box";
                
                EditorGUILayout.BeginHorizontal(style);
                
                EditorGUILayout.LabelField(bundle.BundleName, GUILayout.Width(150));
                EditorGUILayout.LabelField(FileUtility.FormatFileSize(bundle.Size), GUILayout.Width(80));
                EditorGUILayout.LabelField($"[{string.Join(",", bundle.Tags)}]");
                
                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    _selectedBundle = bundle;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            
            // Bundle详情
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Bundle Details", EditorStyles.boldLabel);
            
            if (_selectedBundle != null)
            {
                DrawBundleDetails(_selectedBundle);
            }
            else
            {
                EditorGUILayout.HelpBox("Select a bundle to view details.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawBundleDetails(BundleInfo bundle)
        {
            EditorGUILayout.LabelField("Name", bundle.BundleName);
            EditorGUILayout.LabelField("Size", FileUtility.FormatFileSize(bundle.Size));
            EditorGUILayout.LabelField("Hash", bundle.Hash);
            EditorGUILayout.LabelField("CRC", bundle.CRC.ToString());
            EditorGUILayout.LabelField("Priority", bundle.Priority.ToString());
            EditorGUILayout.LabelField("Is Raw File", bundle.IsRawFile.ToString());
            EditorGUILayout.LabelField("Tags", string.Join(", ", bundle.Tags));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Dependencies ({bundle.Dependencies.Count})", EditorStyles.boldLabel);
            
            _dependencyScrollPos = EditorGUILayout.BeginScrollView(_dependencyScrollPos, GUILayout.MaxHeight(100));
            foreach (var dep in bundle.Dependencies)
            {
                EditorGUILayout.LabelField("  • " + dep);
            }
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Assets ({bundle.Assets.Count})", EditorStyles.boldLabel);
            
            _assetListScrollPos = EditorGUILayout.BeginScrollView(_assetListScrollPos);
            foreach (var asset in bundle.Assets)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(asset);
                
                if (GUILayout.Button("Ping", GUILayout.Width(40)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(asset);
                    if (obj != null)
                    {
                        EditorGUIUtility.PingObject(obj);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawAssetList()
        {
            EditorGUILayout.LabelField($"All Assets ({_manifest.Assets.Count})", EditorStyles.boldLabel);
            
            _assetListScrollPos = EditorGUILayout.BeginScrollView(_assetListScrollPos);
            
            var filteredAssets = _manifest.Assets
                .Where(a => string.IsNullOrEmpty(_searchText) || 
                    a.AssetPath.ToLower().Contains(_searchText.ToLower()));
            
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField("Asset Path", EditorStyles.boldLabel, GUILayout.Width(300));
            EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField("Bundle", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            foreach (var asset in filteredAssets)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(asset.AssetPath, GUILayout.Width(300));
                EditorGUILayout.LabelField(asset.AssetType, GUILayout.Width(100));
                EditorGUILayout.LabelField(asset.BundleName);
                
                if (GUILayout.Button("Ping", GUILayout.Width(40)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(asset.AssetPath);
                    if (obj != null)
                    {
                        EditorGUIUtility.PingObject(obj);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawDependencyGraph()
        {
            EditorGUILayout.LabelField("Dependency Analysis", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 找出被依赖最多的Bundle
            var dependencyCount = new Dictionary<string, int>();
            foreach (var bundle in _manifest.Bundles)
            {
                foreach (var dep in bundle.Dependencies)
                {
                    if (!dependencyCount.ContainsKey(dep))
                    {
                        dependencyCount[dep] = 0;
                    }
                    dependencyCount[dep]++;
                }
            }
            
            EditorGUILayout.LabelField("Most Depended Bundles:", EditorStyles.boldLabel);
            
            var topDepended = dependencyCount
                .OrderByDescending(x => x.Value)
                .Take(10);
            
            foreach (var kvp in topDepended)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(kvp.Key);
                EditorGUILayout.LabelField($"Depended by {kvp.Value} bundles", GUILayout.Width(150));
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space();
            
            // 循环依赖检测
            EditorGUILayout.LabelField("Circular Dependencies:", EditorStyles.boldLabel);
            var circularDeps = FindCircularDependencies();
            
            if (circularDeps.Count == 0)
            {
                EditorGUILayout.HelpBox("No circular dependencies found.", MessageType.Info);
            }
            else
            {
                foreach (var cycle in circularDeps)
                {
                    EditorGUILayout.LabelField($"  • {string.Join(" -> ", cycle)}", EditorStyles.wordWrappedLabel);
                }
            }
        }
        
        private List<List<string>> FindCircularDependencies()
        {
            var cycles = new List<List<string>>();
            var visited = new HashSet<string>();
            var recStack = new HashSet<string>();
            var path = new List<string>();
            
            foreach (var bundle in _manifest.Bundles)
            {
                if (!visited.Contains(bundle.BundleName))
                {
                    FindCyclesDFS(bundle.BundleName, visited, recStack, path, cycles);
                }
            }
            
            return cycles;
        }
        
        private void FindCyclesDFS(string bundleName, HashSet<string> visited, 
            HashSet<string> recStack, List<string> path, List<List<string>> cycles)
        {
            visited.Add(bundleName);
            recStack.Add(bundleName);
            path.Add(bundleName);
            
            var bundle = _manifest.GetBundleInfo(bundleName);
            if (bundle != null)
            {
                foreach (var dep in bundle.Dependencies)
                {
                    if (!visited.Contains(dep))
                    {
                        FindCyclesDFS(dep, visited, recStack, path, cycles);
                    }
                    else if (recStack.Contains(dep))
                    {
                        // 找到循环
                        var cycleStart = path.IndexOf(dep);
                        var cycle = path.GetRange(cycleStart, path.Count - cycleStart);
                        cycle.Add(dep);
                        cycles.Add(cycle);
                    }
                }
            }
            
            path.RemoveAt(path.Count - 1);
            recStack.Remove(bundleName);
        }
    }
}
#endif
