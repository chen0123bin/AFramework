#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace LWAssets.Editor
{
    /// <summary>
    /// LWAssets主窗口
    /// </summary>
    public class LWAssetsWindow : EditorWindow
    {
        private int m_SelectedTab;
        private readonly string[] m_Tabs = { "Dashboard", "Build", "Settings" };

        private LWAssetsBuildConfig m_BuildConfig;
        private LWAssetsConfig m_RuntimeConfig;
        private Vector2 m_ScrollPos;

        [MenuItem("LWAssets/Dashboard")]
        public static void ShowWindow()
        {
            var window = GetWindow<LWAssetsWindow>("LWAssets");
            window.minSize = new Vector2(600, 400);
        }

        private void OnEnable()
        {
            LoadConfigs();
        }

        private void LoadConfigs()
        {
            // 加载构建配置
            var buildConfigGuids = AssetDatabase.FindAssets("t:LWAssetsBuildConfig");
            if (buildConfigGuids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(buildConfigGuids[0]);
                m_BuildConfig = AssetDatabase.LoadAssetAtPath<LWAssetsBuildConfig>(path);
            }

            // 加载运行时配置
            m_RuntimeConfig = LWAssetsConfig.Load();
        }

        private void OnGUI()
        {
            m_SelectedTab = GUILayout.Toolbar(m_SelectedTab, m_Tabs, GUILayout.Height(30));
            EditorGUILayout.Space();

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            switch (m_SelectedTab)
            {
                case 0:
                    DrawDashboard();
                    break;
                case 1:
                    DrawBuildPanel();
                    break;
                case 2:
                    DrawSettings();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDashboard()
        {
            EditorGUILayout.LabelField("LWAssets Dashboard", EditorStyles.largeLabel);
            EditorGUILayout.Space();

            // 统计信息
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Project Statistics", EditorStyles.boldLabel);

            var assetCount = AssetDatabase.FindAssets("", new[] { "Assets" }).Length;
            var prefabCount = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }).Length;
            var textureCount = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" }).Length;
            var materialCount = AssetDatabase.FindAssets("t:Material", new[] { "Assets" }).Length;

            EditorGUILayout.LabelField($"Total Assets: {assetCount}");
            EditorGUILayout.LabelField($"Prefabs: {prefabCount}");
            EditorGUILayout.LabelField($"Textures: {textureCount}");
            EditorGUILayout.LabelField($"Materials: {materialCount}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 快捷操作
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Build AssetBundles", GUILayout.Height(40)))
            {
                if (m_BuildConfig != null)
                {
                    LWAssetsBuildPipeline.Build(m_BuildConfig);
                }
                else
                {
                    Debug.LogError("[LWAssets] Build config not found!");
                }
            }

            if (GUILayout.Button("Open Bundle Viewer", GUILayout.Height(40)))
            {
                BundleViewer.ShowWindow();
            }

            if (GUILayout.Button("Analyze Assets", GUILayout.Height(40)))
            {
                AssetAnalyzer.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Collect Shader Variants", GUILayout.Height(40)))
            {
                ShaderProcessor.CollectShaderVariants();
            }

            if (GUILayout.Button("Clear Build Cache", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Clear Build Cache",
                    "Are you sure you want to clear the build cache?", "Yes", "No"))
                {
                    ClearBuildCache();
                }
            }

            if (GUILayout.Button("Open Build Folder", GUILayout.Height(40)))
            {
                if (m_BuildConfig != null)
                {
                    var path = Path.Combine(Application.dataPath, "..", m_BuildConfig.OutputPath);
                    if (Directory.Exists(path))
                    {
                        EditorUtility.RevealInFinder(path);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Download Folder", GUILayout.Height(40)))
            {
                EditorUtility.RevealInFinder(m_RuntimeConfig.GetPersistentDataPath());
            }

            if (GUILayout.Button("Copy to StreamingAssets", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Copy to StreamingAssets",
                    "Are you sure you want to copy the build assets to StreamingAssets folder?", "Yes", "No"))
                {
                    CopyToStreamingAssets();
                }
            }


            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 构建历史
            DrawBuildHistory();
        }

        private void DrawBuildPanel()
        {
            EditorGUILayout.LabelField("Build Configuration", EditorStyles.largeLabel);
            EditorGUILayout.Space();

            // 构建配置对象
            EditorGUILayout.BeginHorizontal();
            m_BuildConfig = (LWAssetsBuildConfig)EditorGUILayout.ObjectField(
                "Build Config", m_BuildConfig, typeof(LWAssetsBuildConfig), false);

            if (GUILayout.Button("Create New", GUILayout.Width(100)))
            {
                CreateBuildConfig();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (m_BuildConfig != null)
            {
                var editor = UnityEditor.Editor.CreateEditor(m_BuildConfig);
                editor.OnInspectorGUI();

                EditorGUILayout.Space();

                // 构建按钮
                EditorGUILayout.BeginHorizontal();

                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Build", GUILayout.Height(40)))
                {
                    LWAssetsBuildPipeline.Build(m_BuildConfig);
                }
                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("Build & Copy to StreamingAssets", GUILayout.Height(40)))
                {
                    LWAssetsBuildPipeline.Build(m_BuildConfig);
                    CopyToStreamingAssets();
                }

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Please create or select a build configuration.", MessageType.Warning);
            }
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Runtime Settings", EditorStyles.largeLabel);
            EditorGUILayout.Space();

            // 运行时配置对象
            EditorGUILayout.BeginHorizontal();
            m_RuntimeConfig = (LWAssetsConfig)EditorGUILayout.ObjectField(
                "Runtime Config", m_RuntimeConfig, typeof(LWAssetsConfig), false);

            if (GUILayout.Button("Create New", GUILayout.Width(100)))
            {
                CreateRuntimeConfig();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (m_RuntimeConfig != null)
            {
                var editor = UnityEditor.Editor.CreateEditor(m_RuntimeConfig);
                editor.OnInspectorGUI();
            }
            else
            {
                EditorGUILayout.HelpBox("Runtime configuration not found. Please create one.", MessageType.Warning);
            }
        }

        private void DrawBuildHistory()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Recent Builds", EditorStyles.boldLabel);

            if (m_BuildConfig == null)
            {
                EditorGUILayout.LabelField("No build config selected.");
            }
            else
            {
                var buildPath = Path.Combine(Application.dataPath, "..", m_BuildConfig.OutputPath);

                if (Directory.Exists(buildPath))
                {
                    var platforms = Directory.GetDirectories(buildPath);

                    foreach (var platform in platforms)
                    {
                        var manifestPath = Path.Combine(platform, "manifest.json");
                        if (File.Exists(manifestPath))
                        {
                            var json = File.ReadAllText(manifestPath);
                            var manifest = BundleManifest.FromJson(json);

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(Path.GetFileName(platform), GUILayout.Width(100));
                            EditorGUILayout.LabelField($"v{manifest.Version}", GUILayout.Width(80));
                            EditorGUILayout.LabelField(manifest.BuildTime, GUILayout.Width(150));
                            EditorGUILayout.LabelField($"{manifest.Bundles.Count} bundles", GUILayout.Width(100));
                            EditorGUILayout.LabelField(FileUtility.FormatFileSize(manifest.GetTotalSize()));
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No builds found.");
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void CreateBuildConfig()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Build Config", "LWAssetsBuildConfig", "asset", "");

            if (!string.IsNullOrEmpty(path))
            {
                var config = CreateInstance<LWAssetsBuildConfig>();
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                m_BuildConfig = config;
            }
        }

        private void CreateRuntimeConfig()
        {
            var directory = "Assets/Resources/LWAssets";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var path = Path.Combine(directory, "LWAssetsConfig.asset");

            var config = CreateInstance<LWAssetsConfig>();
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            m_RuntimeConfig = config;
        }

        private void ClearBuildCache()
        {
            if (m_BuildConfig != null)
            {
                var path = Path.Combine(Application.dataPath, "..", m_BuildConfig.OutputPath);
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    Debug.Log("[LWAssets] Build cache cleared.");
                }
            }
        }

        private void CopyToStreamingAssets()
        {
            if (m_BuildConfig == null) return;

            var sourcePath = Path.Combine(Application.dataPath, "..",
                m_BuildConfig.OutputPath, LWAssetsConfig.GetPlatformName());
            var destPath = Path.Combine(Application.streamingAssetsPath,
                m_BuildConfig.OutputPath, LWAssetsConfig.GetPlatformName());

            if (!Directory.Exists(sourcePath))
            {
                Debug.LogError("[LWAssets] Build output not found!");
                return;
            }

            if (Directory.Exists(destPath))
            {
                Directory.Delete(destPath, true);
            }

            FileUtility.CopyDirectory(sourcePath, destPath);
            AssetDatabase.Refresh();

            Debug.Log($"[LWAssets] Copied to StreamingAssets: {destPath}");
        }
    }
}
#endif
