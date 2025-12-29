// Editor/Window/AssetLoadAnalyzerWindow.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace LWAssets.Editor
{
    /// <summary>
    /// 资源加载分析器窗口
    /// </summary>
    public class AssetLoadAnalyzerWindow : EditorWindow
    {
        private enum Tab
        {
            Assets,
            Bundles,
            Summary
        }
        
        private Tab _currentTab = Tab.Assets;
        
        private bool _isEnable = false;
        // 资源视图
        private Vector2 _assetScrollPos;
        private string _assetSearch = "";
        private string _typeFilter = "All";
        private string _bundleFilter = "All";
        private SortMode _assetSortMode = SortMode.Name;
        private bool _sortAscending = true;
        private bool _showValidOnly = true;
        
        // Bundle视图
        private Vector2 _bundleScrollPos;
        private string _bundleSearch = "";
        private SortMode _bundleSortMode = SortMode.Name;
        private bool _bundleSortAscending = true;
        private string _selectedBundleName;
        private Vector2 _bundleDetailScrollPos;
        
        // 刷新
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const float RefreshInterval = 0.5f;
        
  
      
        private string[] _typeOptions = new[] { "All" };
        private string[] _bundleOptions = new[] { "All" };
        
        private enum SortMode
        {
            Name,
            Type,
            Bundle,
            RefCount,
            Memory,
            LoadTime
        }
        
        [MenuItem("LWAssets/Asset Load Analyzer")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetLoadAnalyzerWindow>("Asset Load Analyzer");
            window.minSize = new Vector2(900, 600);
        }
        
        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }
        
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }
        
        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode || 
                state == PlayModeStateChange.ExitingPlayMode)
            {
                RefreshData();
            }
        }
        
        private void Update()
        {
            if (_autoRefresh && EditorApplication.isPlaying)
            {
                if (EditorApplication.timeSinceStartup - _lastRefreshTime > RefreshInterval)
                {
                    _lastRefreshTime = EditorApplication.timeSinceStartup;
                    RefreshData();
                    Repaint();
                }
            }
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("请进入播放模式以查看资源加载情况。", MessageType.Info);
                return;
            }
            
            if (!LWAssets.IsInitialized)
            {
                EditorGUILayout.HelpBox("LWAssets 尚未初始化。", MessageType.Warning);
                return;
            }
            
            EditorGUILayout.Space(5);
            
            // 标签页
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Toggle(_currentTab == Tab.Assets, "已加载资源", "ButtonLeft"))
                    _currentTab = Tab.Assets;
                if (GUILayout.Toggle(_currentTab == Tab.Bundles, "已加载Bundle", "ButtonMid"))
                    _currentTab = Tab.Bundles;
                if (GUILayout.Toggle(_currentTab == Tab.Summary, "统计概览", "ButtonRight"))
                    _currentTab = Tab.Summary;
            }
            
            EditorGUILayout.Space(5);
            
            switch (_currentTab)
            {
                case Tab.Assets:
                    DrawAssetsTab();
                    break;
                case Tab.Bundles:
                    DrawBundlesTab();
                    break;
                case Tab.Summary:
                    DrawSummaryTab();
                    break;
            }
        }
        
        #region Toolbar
        
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // 自动刷新
                _autoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(70));
                
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    RefreshData();
                }
                
                GUILayout.FlexibleSpace();
                
                if (EditorApplication.isPlaying && LWAssets.IsInitialized)
                {
                    // 记录器开关
                    _isEnable = GUILayout.Toggle(_isEnable, "启用记录", EditorStyles.toolbarButton, GUILayout.Width(70));
                    
                    GUILayout.Space(10);
                    
                    if (GUILayout.Button("清理无效记录", EditorStyles.toolbarButton, GUILayout.Width(90)))
                    {
                        
                        RefreshData();
                    }
                    
                    if (GUILayout.Button("卸载未使用", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    {
                        LWAssets.UnloadUnusedAssetsAsync().Forget();
                        RefreshData();
                    }
                    
                    GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                    if (GUILayout.Button("全部卸载", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    {
                        if (EditorUtility.DisplayDialog("确认", "确定要卸载所有已加载的资源和Bundle吗？", "确定", "取消"))
                        {
                            LWAssets.ForceUnloadAll();
                            RefreshData();
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }
            }
        }
        
        #endregion
        
        #region Assets Tab
        
        private void DrawAssetsTab()
        {
            // 筛选栏
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("搜索:", GUILayout.Width(40));
                var newSearch = EditorGUILayout.TextField(_assetSearch, GUILayout.Width(200));
                if (newSearch != _assetSearch)
                {
                    _assetSearch = newSearch;
                    RefreshData();
                }
                
                GUILayout.Space(10);
                
                EditorGUILayout.LabelField("类型:", GUILayout.Width(35));
                var typeIndex = Array.IndexOf(_typeOptions, _typeFilter);
                if (typeIndex < 0) typeIndex = 0;
                var newTypeIndex = EditorGUILayout.Popup(typeIndex, _typeOptions, GUILayout.Width(100));
                if (newTypeIndex != typeIndex)
                {
                    _typeFilter = _typeOptions[newTypeIndex];
                    RefreshData();
                }
                
                GUILayout.Space(10);
                
                EditorGUILayout.LabelField("Bundle:", GUILayout.Width(50));
                var bundleIndex = Array.IndexOf(_bundleOptions, _bundleFilter);
                if (bundleIndex < 0) bundleIndex = 0;
                var newBundleIndex = EditorGUILayout.Popup(bundleIndex, _bundleOptions, GUILayout.Width(150));
                if (newBundleIndex != bundleIndex)
                {
                    _bundleFilter = _bundleOptions[newBundleIndex];
                    RefreshData();
                }
                
                GUILayout.Space(10);
                
                var newShowValid = GUILayout.Toggle(_showValidOnly, "仅显示有效", GUILayout.Width(80));
                if (newShowValid != _showValidOnly)
                {
                    _showValidOnly = newShowValid;
                    RefreshData();
                }
                
                GUILayout.FlexibleSpace();
               
                EditorGUILayout.LabelField($"共 { LWAssets.Loader.GetAssetRefCache().Count} 条记录", EditorStyles.miniLabel, GUILayout.Width(100));
            }
            
            EditorGUILayout.Space(5);
            
            // 表头
            DrawAssetListHeader();
            
            // 列表
            _assetScrollPos = EditorGUILayout.BeginScrollView(_assetScrollPos);
            
            foreach (var record in LWAssets.Loader.GetAssetRefCache().Values)
            {
                DrawAssetRow(record);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawAssetListHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (DrawSortButton("资源路径", SortMode.Name, ref _assetSortMode, ref _sortAscending, 250))
                    RefreshData();
                
                if (DrawSortButton("类型", SortMode.Type, ref _assetSortMode, ref _sortAscending, 80))
                    RefreshData();
                
                if (DrawSortButton("Bundle", SortMode.Bundle, ref _assetSortMode, ref _sortAscending, 150))
                    RefreshData();
                
                if (DrawSortButton("引用", SortMode.RefCount, ref _assetSortMode, ref _sortAscending, 50))
                    RefreshData();
                
                if (DrawSortButton("内存", SortMode.Memory, ref _assetSortMode, ref _sortAscending, 70))
                    RefreshData();
                
                if (DrawSortButton("加载耗时", SortMode.LoadTime, ref _assetSortMode, ref _sortAscending, 70))
                    RefreshData();
                
                EditorGUILayout.LabelField("状态", EditorStyles.toolbarButton, GUILayout.Width(40));
                EditorGUILayout.LabelField("操作", EditorStyles.toolbarButton, GUILayout.Width(120));
            }
        }
        
        private bool DrawSortButton(string label, SortMode mode, ref SortMode currentMode, ref bool ascending, float width)
        {
            var style = EditorStyles.toolbarButton;
            var displayLabel = label;
            
            if (currentMode == mode)
            {
                displayLabel = label + (ascending ? " ▲" : " ▼");
            }
            
            if (GUILayout.Button(displayLabel, style, GUILayout.Width(width)))
            {
                if (currentMode == mode)
                {
                    ascending = !ascending;
                }
                else
                {
                    currentMode = mode;
                    ascending = true;
                }
                return true;
            }
            return false;
        }
        
        private void DrawAssetRow(AssetRefInfo record)
        {
            var isValid = record.Asset!=null;
            
            // 背景色
            var bgColor = GUI.backgroundColor;
            if (!isValid)
            {
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f, 0.3f);
            }
            else if (record.RefCount > 3)
            {
                GUI.backgroundColor = new Color(1f, 1f, 0.5f, 0.3f);
            }
            
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                GUI.backgroundColor = bgColor;
                
                // 资源路径（可点击定位）
                var pathContent = new GUIContent(TruncateStart(record.AssetPath, 40), record.AssetPath);
                if (GUILayout.Button(pathContent, EditorStyles.linkLabel, GUILayout.Width(250)))
                {
                    PingAsset(record);
                }
                
                //EditorGUILayout.LabelField(record.AssetType ?? "-", GUILayout.Width(80));
                
                var bundleContent = new GUIContent(TruncateStart(record.BundleName, 25), record.BundleName);
                EditorGUILayout.LabelField(bundleContent, GUILayout.Width(150));
                
                // 引用计数（高亮显示异常值）
                var refStyle = record.RefCount > 3 ? EditorStyles.boldLabel : EditorStyles.label;
                EditorGUILayout.LabelField(record.RefCount.ToString(), refStyle, GUILayout.Width(50));
                
                //EditorGUILayout.LabelField(FormatBytes(record.MemorySize), GUILayout.Width(70));
                //EditorGUILayout.LabelField($"{record.LoadTime:F1}ms", GUILayout.Width(70));
                
                // 状态指示
                var statusColor = isValid ? Color.green : Color.gray;
                GUI.color = statusColor;
                EditorGUILayout.LabelField(isValid ? "●" : "○", GUILayout.Width(40));
                GUI.color = Color.white;
                
                // 操作按钮
                using (new EditorGUI.DisabledGroupScope(!isValid))
                {
                    if (GUILayout.Button("定位", GUILayout.Width(40)))
                    {
                        PingAsset(record);
                    }
                    
                    if (GUILayout.Button("释放", GUILayout.Width(40)))
                    {
                        ReleaseAsset(record);
                    }
                }
                
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    RemoveAssetRecord(record);
                }
            }
        }
        
        private void PingAsset(AssetRefInfo record)
        {
            var asset = record.Asset;
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            }
            else
            {
                // 尝试从项目中定位
                var projectAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(record.AssetPath);
                if (projectAsset != null)
                {
                    EditorGUIUtility.PingObject(projectAsset);
                }
            }
        }
        
        private void ReleaseAsset(AssetRefInfo record)
        {
            if (EditorUtility.DisplayDialog("释放资源", 
                $"确定要释放资源吗？\n\n{record.AssetPath}", "确定", "取消"))
            {
                // 获取加载器并释放
                var loaderField = typeof(LWAssets).GetField("_loader", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var loader = loaderField?.GetValue(null) as AssetLoaderBase;
                loader?.ForceReleaseAsset(record.AssetPath);
                
                RefreshData();
            }
        }
        
        private void RemoveAssetRecord(AssetRefInfo record)
        {
            LWAssets.Loader.GetAssetRefCache().Remove(record.AssetPath);
            RefreshData();
        }
        
        #endregion
        
        #region Bundles Tab
        
        private void DrawBundlesTab()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // 左侧 Bundle 列表
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.55f)))
                {
                    DrawBundleListPanel();
                }
                
                // 右侧详情
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    DrawBundleDetailPanel();
                }
            }
        }
        
        private void DrawBundleListPanel()
        {
            // 搜索
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("搜索:", GUILayout.Width(40));
                var newSearch = EditorGUILayout.TextField(_bundleSearch);
                if (newSearch != _bundleSearch)
                {
                    _bundleSearch = newSearch;
                    RefreshData();
                }
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"共 {LWAssets.Loader.GetBundleCache().Count} 个Bundle", EditorStyles.miniLabel, GUILayout.Width(100));
            }
            
            EditorGUILayout.Space(5);
            
            // 表头
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (DrawSortButton("Bundle名称", SortMode.Name, ref _bundleSortMode, ref _bundleSortAscending, 180))
                    RefreshData();
                
                if (DrawSortButton("引用", SortMode.RefCount, ref _bundleSortMode, ref _bundleSortAscending, 45))
                    RefreshData();
                
                if (DrawSortButton("大小", SortMode.Memory, ref _bundleSortMode, ref _bundleSortAscending, 65))
                    RefreshData();
                
                EditorGUILayout.LabelField("资源", EditorStyles.toolbarButton, GUILayout.Width(60));
                EditorGUILayout.LabelField("状态", EditorStyles.toolbarButton, GUILayout.Width(40));
                EditorGUILayout.LabelField("操作", EditorStyles.toolbarButton, GUILayout.Width(60));
            }
            
            // 列表
            _bundleScrollPos = EditorGUILayout.BeginScrollView(_bundleScrollPos);
            
            foreach (var record in LWAssets.Loader.GetBundleCache().Values)
            {
                DrawBundleRow(record);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawBundleRow(BundleHandle record)
        {
            var isSelected = _selectedBundleName == record.BundleName;
            var isValid = record.IsValid;
            
            var bgColor = GUI.backgroundColor;
            if (isSelected)
            {
                GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f, 0.5f);
            }
            else if (!isValid)
            {
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f, 0.3f);
            }
            
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                GUI.backgroundColor = bgColor;
                
                // Bundle名称
                var nameContent = new GUIContent(TruncateStart(record.BundleName, 28), record.BundleName);
                if (GUILayout.Button(nameContent, EditorStyles.linkLabel, GUILayout.Width(180)))
                {
                    _selectedBundleName = record.BundleName;
                }
                
                var refStyle = record.ReferenceCount > 5 ? EditorStyles.boldLabel : EditorStyles.label;
                EditorGUILayout.LabelField(record.ReferenceCount.ToString(), refStyle, GUILayout.Width(45));
                
               // EditorGUILayout.LabelField(FormatBytes(record.FileSize), GUILayout.Width(65));
               // EditorGUILayout.LabelField($"{record.LoadedAssetCount}/{record.TotalAssetCount}", GUILayout.Width(60));
                
                GUI.color = isValid ? Color.green : Color.gray;
                EditorGUILayout.LabelField(isValid ? "●" : "○", GUILayout.Width(40));
                GUI.color = Color.white;
                
                using (new EditorGUI.DisabledGroupScope(!isValid))
                {
                    if (GUILayout.Button("卸载", GUILayout.Width(50)))
                    {
                        UnloadBundle(record);
                    }
                }
            }
        }
        
        private void DrawBundleDetailPanel()
        {
            EditorGUILayout.LabelField("Bundle 详情", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            if (string.IsNullOrEmpty(_selectedBundleName))
            {
                EditorGUILayout.HelpBox("选择一个Bundle查看详情", MessageType.Info);
                return;
            }
            
            var record = LWAssets.Loader.GetBundleCache().FirstOrDefault(b => b.Key == _selectedBundleName).Value;
            if (record == null)
            {
                EditorGUILayout.HelpBox("Bundle 记录不存在", MessageType.Warning);
                _selectedBundleName = null;
                return;
            }
            
            _bundleDetailScrollPos = EditorGUILayout.BeginScrollView(_bundleDetailScrollPos);
            
            // 基本信息
            EditorGUILayout.LabelField("名称:", record.BundleName, EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("状态:", record.IsValid ? "已加载" : "已卸载");
            EditorGUILayout.LabelField("引用计数:", record.ReferenceCount.ToString());
            //EditorGUILayout.LabelField("文件大小:", FormatBytes(record.FileSize));
            //EditorGUILayout.LabelField("资源数量:", $"{record.LoadedAssetCount} / {record.TotalAssetCount}");
            //EditorGUILayout.LabelField("加载耗时:", $"{record.LoadTime:F2} ms");
           // EditorGUILayout.LabelField("加载时间:", record.LoadTimeStamp);
            
            EditorGUILayout.Space(10);
            
            // 依赖
            EditorGUILayout.LabelField($"依赖 ({record.Dependencies.Count})", EditorStyles.boldLabel);
            if (record.Dependencies.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var dep in record.Dependencies)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("•", GUILayout.Width(15));
                        if (GUILayout.Button(dep.BundleName, EditorStyles.linkLabel))
                        {
                            _selectedBundleName = dep.BundleName;
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.LabelField("  无依赖");
            }
            
            EditorGUILayout.Space(10);
            
            // 已加载资源
            var bundleAssets = LWAssets.Loader.GetBundleCache().Where(a => a.Key == record.BundleName).ToList();
            EditorGUILayout.LabelField($"已加载资源 ({bundleAssets.Count})", EditorStyles.boldLabel);
            
            if (bundleAssets.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var asset in bundleAssets.Take(20))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("•", GUILayout.Width(15));
                        
                        var assetName = System.IO.Path.GetFileName(asset.AssetPath);
                        var content = new GUIContent(assetName, asset.AssetPath);
                        
                        if (GUILayout.Button(content, EditorStyles.linkLabel, GUILayout.Width(200)))
                        {
                            PingAsset(asset);
                        }
                        
                        EditorGUILayout.LabelField($"[{asset.RefCount}]", GUILayout.Width(30));
                    }
                }
                
                if (bundleAssets.Count > 20)
                {
                    EditorGUILayout.LabelField($"  ... 还有 {bundleAssets.Count - 20} 个资源");
                }
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.LabelField("  无已加载资源");
            }
            
            EditorGUILayout.Space(20);
            
            // 操作按钮
            using (new EditorGUI.DisabledGroupScope(!record.IsValid))
            {
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("卸载此Bundle", GUILayout.Height(30)))
                {
                    UnloadBundle(record);
                }
                GUI.backgroundColor = Color.white;
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void UnloadBundle(BundleRecord record)
        {
            if (EditorUtility.DisplayDialog("卸载Bundle", 
                $"确定要卸载此Bundle及其所有资源吗？\n\n{record.BundleName}", "确定", "取消"))
            {
                var loaderField = typeof(LWAssets).GetField("_loader", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var loader = loaderField?.GetValue(null) as AssetLoaderBase;
                loader?.ForceUnloadBundle(record.BundleName);
                
                if (_selectedBundleName == record.BundleName)
                {
                    _selectedBundleName = null;
                }
                
                RefreshData();
            }
        }
        
        #endregion
        
        #region Summary Tab
        
        private void DrawSummaryTab()
        {
            var recorder = AssetLoadRecorder.Instance;
            var assets = recorder.AssetRecords.Values.ToList();
            var bundles = recorder.BundleRecords.Values.ToList();
            
            var aliveAssets = assets.Where(a => a.IsValid).ToList();
            var aliveBundles = bundles.Where(b => b.IsValid).ToList();
            
            EditorGUILayout.Space(10);
            
            // 概览
            EditorGUILayout.LabelField("加载概览", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawStatCard("已加载资源", $"{aliveAssets.Count}", $"记录总数: {assets.Count}");
                DrawStatCard("已加载Bundle", $"{aliveBundles.Count}", $"记录总数: {bundles.Count}");
                DrawStatCard("资源引用总计", aliveAssets.Sum(a => a.RefCount).ToString(), "");
                DrawStatCard("估算内存占用", FormatBytes(aliveAssets.Sum(a => a.MemorySize)), "");
            }
            
            EditorGUILayout.Space(20);
            
            // 按类型统计
            EditorGUILayout.LabelField("按资源类型统计", EditorStyles.boldLabel);
            
            var typeGroups = aliveAssets
                .GroupBy(a => a.AssetType ?? "Unknown")
                .OrderByDescending(g => g.Sum(a => a.MemorySize))
                .ToList();
            
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("类型", EditorStyles.boldLabel, GUILayout.Width(120));
                    EditorGUILayout.LabelField("数量", EditorStyles.boldLabel, GUILayout.Width(60));
                    EditorGUILayout.LabelField("引用", EditorStyles.boldLabel, GUILayout.Width(60));
                    EditorGUILayout.LabelField("内存", EditorStyles.boldLabel, GUILayout.Width(80));
                    EditorGUILayout.LabelField("平均加载", EditorStyles.boldLabel, GUILayout.Width(80));
                }
                
                foreach (var group in typeGroups)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(group.Key, GUILayout.Width(120));
                        EditorGUILayout.LabelField(group.Count().ToString(), GUILayout.Width(60));
                        EditorGUILayout.LabelField(group.Sum(a => a.RefCount).ToString(), GUILayout.Width(60));
                        EditorGUILayout.LabelField(FormatBytes(group.Sum(a => a.MemorySize)), GUILayout.Width(80));
                        EditorGUILayout.LabelField($"{group.Average(a => a.LoadTime):F1}ms", GUILayout.Width(80));
                    }
                }
            }
            
            EditorGUILayout.Space(20);
            
            // 按Bundle统计
            EditorGUILayout.LabelField("按Bundle统计", EditorStyles.boldLabel);
            
            var bundleGroups = aliveAssets
                .GroupBy(a => a.BundleName ?? "Unknown")
                .OrderByDescending(g => g.Sum(a => a.MemorySize))
                .Take(10)
                .ToList();
            
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Bundle", EditorStyles.boldLabel, GUILayout.Width(200));
                    EditorGUILayout.LabelField("已加载", EditorStyles.boldLabel, GUILayout.Width(60));
                    EditorGUILayout.LabelField("引用", EditorStyles.boldLabel, GUILayout.Width(60));
                    EditorGUILayout.LabelField("内存", EditorStyles.boldLabel, GUILayout.Width(80));
                }
                
                foreach (var group in bundleGroups)
                {
                    var bundleRecord = bundles.FirstOrDefault(b => b.BundleName == group.Key);
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var nameContent = new GUIContent(TruncateStart(group.Key, 35), group.Key);
                        EditorGUILayout.LabelField(nameContent, GUILayout.Width(200));
                        EditorGUILayout.LabelField($"{group.Count()}/{bundleRecord?.TotalAssetCount ?? 0}", GUILayout.Width(60));
                        EditorGUILayout.LabelField(group.Sum(a => a.RefCount).ToString(), GUILayout.Width(60));
                        EditorGUILayout.LabelField(FormatBytes(group.Sum(a => a.MemorySize)), GUILayout.Width(80));
                    }
                }
            }
            
            EditorGUILayout.Space(20);
            
            // 加载性能
            EditorGUILayout.LabelField("加载性能", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (aliveAssets.Count > 0)
                {
                    EditorGUILayout.LabelField($"资源平均加载耗时: {aliveAssets.Average(a => a.LoadTime):F2} ms");
                    EditorGUILayout.LabelField($"资源最大加载耗时: {aliveAssets.Max(a => a.LoadTime):F2} ms");
                }
                
                if (aliveBundles.Count > 0)
                {
                    EditorGUILayout.LabelField($"Bundle平均加载耗时: {aliveBundles.Average(b => b.LoadTime):F2} ms");
                    EditorGUILayout.LabelField($"Bundle最大加载耗时: {aliveBundles.Max(b => b.LoadTime):F2} ms");
                }
                
                EditorGUILayout.LabelField($"Bundle总大小: {FormatBytes(aliveBundles.Sum(b => b.FileSize))}");
            }
        }
        
        private void DrawStatCard(string title, string value, string subtitle)
        {
            using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(180), GUILayout.Height(60)))
            {
                EditorGUILayout.LabelField(title, EditorStyles.miniLabel);
                EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
                if (!string.IsNullOrEmpty(subtitle))
                {
                    EditorGUILayout.LabelField(subtitle, EditorStyles.miniLabel);
                }
            }
        }
        
        #endregion
        
        #region Data Refresh
        
        private void RefreshData()
        {
            if (!EditorApplication.isPlaying || !LWAssets.IsInitialized) return;
            
            var recorder = AssetLoadRecorder.Instance;
            
            // 刷新资源数据
            var allAssets = recorder.AssetRecords.Values.ToList();
            
            // 筛选
            _cachedAssets = allAssets
                .Where(a => !_showValidOnly || a.IsValid)
                .Where(a => string.IsNullOrEmpty(_assetSearch) || 
                    a.AssetPath.ToLower().Contains(_assetSearch.ToLower()))
                .Where(a => _typeFilter == "All" || a.AssetType == _typeFilter)
                .Where(a => _bundleFilter == "All" || a.BundleName == _bundleFilter)
                .ToList();
            
            // 排序
            _cachedAssets = SortAssets(_cachedAssets, _assetSortMode, _sortAscending);
            
            // 刷新Bundle数据
            var allBundles = recorder.BundleRecords.Values.ToList();
            
            _cachedBundles = allBundles
                .Where(b => string.IsNullOrEmpty(_bundleSearch) || 
                    b.BundleName.ToLower().Contains(_bundleSearch.ToLower()))
                .ToList();
            
            _cachedBundles = SortBundles(_cachedBundles, _bundleSortMode, _bundleSortAscending);
            
            // 更新筛选选项
            UpdateFilterOptions(allAssets);
        }
        
        private List<AssetRecord> SortAssets(List<AssetRecord> assets, SortMode mode, bool ascending)
        {
            IOrderedEnumerable<AssetRecord> sorted = mode switch
            {
                SortMode.Name => ascending 
                    ? assets.OrderBy(a => a.AssetPath) 
                    : assets.OrderByDescending(a => a.AssetPath),
                SortMode.Type => ascending 
                    ? assets.OrderBy(a => a.AssetType) 
                    : assets.OrderByDescending(a => a.AssetType),
                SortMode.Bundle => ascending 
                    ? assets.OrderBy(a => a.BundleName) 
                    : assets.OrderByDescending(a => a.BundleName),
                SortMode.RefCount => ascending 
                    ? assets.OrderBy(a => a.RefCount) 
                    : assets.OrderByDescending(a => a.RefCount),
                SortMode.Memory => ascending 
                    ? assets.OrderBy(a => a.MemorySize) 
                    : assets.OrderByDescending(a => a.MemorySize),
                SortMode.LoadTime => ascending 
                    ? assets.OrderBy(a => a.LoadTime) 
                    : assets.OrderByDescending(a => a.LoadTime),
                _ => assets.OrderBy(a => a.AssetPath)
            };
            
            return sorted.ToList();
        }
        
        private List<BundleRecord> SortBundles(List<BundleRecord> bundles, SortMode mode, bool ascending)
        {
            IOrderedEnumerable<BundleRecord> sorted = mode switch
            {
                SortMode.Name => ascending 
                    ? bundles.OrderBy(b => b.BundleName) 
                    : bundles.OrderByDescending(b => b.BundleName),
                SortMode.RefCount => ascending 
                    ? bundles.OrderBy(b => b.RefCount) 
                    : bundles.OrderByDescending(b => b.RefCount),
                SortMode.Memory => ascending 
                    ? bundles.OrderBy(b => b.FileSize) 
                    : bundles.OrderByDescending(b => b.FileSize),
                SortMode.LoadTime => ascending 
                    ? bundles.OrderBy(b => b.LoadTime) 
                    : bundles.OrderByDescending(b => b.LoadTime),
                _ => bundles.OrderBy(b => b.BundleName)
            };
            
            return sorted.ToList();
        }
        
        private void UpdateFilterOptions(List<AssetRecord> assets)
        {
            var types = assets
                .Select(a => a.AssetType)
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct()
                .OrderBy(t => t)
                .ToList();
            types.Insert(0, "All");
            _typeOptions = types.ToArray();
            
            var bundles = assets
                .Select(a => a.BundleName)
                .Where(b => !string.IsNullOrEmpty(b))
                .Distinct()
                .OrderBy(b => b)
                .ToList();
            bundles.Insert(0, "All");
            _bundleOptions = bundles.ToArray();
        }
        
        #endregion
        
        #region Utility
        
        private string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024 * 1024)
                return $"{bytes / 1024.0 / 1024.0 / 1024.0:F2} GB";
            if (bytes >= 1024 * 1024)
                return $"{bytes / 1024.0 / 1024.0:F2} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F2} KB";
            return $"{bytes} B";
        }
        
        private string TruncateStart(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= maxLength) return text;
            return "..." + text.Substring(text.Length - maxLength + 3);
        }
        
        #endregion
    }
}
#endif