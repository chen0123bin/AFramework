#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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

        private bool m_IsBuilding;
        private bool m_CopyToStreamingAssetsAfterBuild;

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

        private void OnDisable()
        {
            EditorApplication.delayCall -= ExecuteBuild;
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
            try
            {
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
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }
        /// <summary>
        /// 请求构建
        /// </summary>
        /// <param name="copyToStreamingAssets"></param>
        private void RequestBuild(bool copyToStreamingAssets)
        {
            if (m_BuildConfig == null)
            {
                Debug.LogError("[LWAssets] Build config not found!");
                return;
            }

            if (m_IsBuilding) return;

            m_IsBuilding = true;
            m_CopyToStreamingAssetsAfterBuild = copyToStreamingAssets;

            EditorApplication.delayCall -= ExecuteBuild;
            EditorApplication.delayCall += ExecuteBuild;
            Repaint();
        }
        /// <summary>
        /// 执行构建
        /// </summary>
        private void ExecuteBuild()
        {
            EditorApplication.delayCall -= ExecuteBuild;

            try
            {
                LWAssetsBuildPipeline.Build(m_BuildConfig);

                if (m_CopyToStreamingAssetsAfterBuild)
                {
                    CopyToStreamingAssets();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                m_IsBuilding = false;
                m_CopyToStreamingAssetsAfterBuild = false;
                Repaint();
            }
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
                RequestBuild(false);
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
            if (GUILayout.Button("Copy to StreamingAssets by BuiltinTags", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Copy to StreamingAssets by BuiltinTags",
                    "Are you sure you want to copy the build assets to StreamingAssets folder?", "Yes", "No"))
                {
                    CopyToStreamingAssetsByTags();
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
                    RequestBuild(false);
                }
                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("Build & Copy to StreamingAssets", GUILayout.Height(40)))
                {
                    RequestBuild(true);
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
        /// <summary>
        /// 根据Tag复制到StreamingAssets并构建
        /// </summary>
        /// <param name="tags"></param>
        private void CopyToStreamingAssetsByTags()
        {

            if (m_BuildConfig == null)
            {
                Debug.LogError("[LWAssets] Build config not found!");
                return;
            }

            if (m_BuildConfig.BuiltinTags == null || m_BuildConfig.BuiltinTags.Count <= 0)
            {
                CopyToStreamingAssets();
                return;
            }

            var sourcePath = Path.Combine(Application.dataPath, "..",
                m_BuildConfig.OutputPath, LWAssetsConfig.GetPlatformName());
            var destPath = Path.Combine(Application.streamingAssetsPath,
                m_BuildConfig.OutputPath, LWAssetsConfig.GetPlatformName());

            if (!Directory.Exists(sourcePath))
            {
                Debug.LogError("[LWAssets] Build output not found!");
                return;
            }

            var manifestPath = Path.Combine(sourcePath, m_RuntimeConfig != null ? m_RuntimeConfig.ManifestFileName : "manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError($"[LWAssets] Manifest not found: {manifestPath}");
                return;
            }

            BundleManifest manifest;
            try
            {
                var json = File.ReadAllText(manifestPath);
                manifest = BundleManifest.FromJson(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LWAssets] Load manifest failed: {ex.Message}");
                return;
            }

            var bundleNamesToCopy = new HashSet<string>(StringComparer.Ordinal);
            var hitBundles = manifest.GetBundlesByTags(m_BuildConfig.BuiltinTags);
            foreach (var hit in hitBundles)
            {
                if (hit == null || string.IsNullOrEmpty(hit.BundleName))
                {
                    continue;
                }

                bundleNamesToCopy.Add(hit.BundleName);

                var deps = manifest.GetAllDependencies(hit.BundleName);
                for (int i = 0; i < deps.Count; i++)
                {
                    var dep = deps[i];
                    if (dep == null || string.IsNullOrEmpty(dep.BundleName))
                    {
                        continue;
                    }
                    bundleNamesToCopy.Add(dep.BundleName);
                }
            }

            if (bundleNamesToCopy.Count <= 0)
            {
                Debug.LogWarning($"[LWAssets] No bundles matched by tags: {string.Join(",", m_BuildConfig.BuiltinTags)}");
                return;
            }

            if (Directory.Exists(destPath))
            {
                Directory.Delete(destPath, true);
            }
            Directory.CreateDirectory(destPath);

            var copiedBundleNames = new HashSet<string>(StringComparer.Ordinal);
            var copiedBundleInfos = new List<BundleInfo>(bundleNamesToCopy.Count);

            foreach (var bundleName in bundleNamesToCopy)
            {
                var info = manifest.GetBundleInfo(bundleName);
                if (info == null)
                {
                    Debug.LogWarning($"[LWAssets] BundleInfo not found in manifest: {bundleName}");
                    continue;
                }

                var srcFile = Path.Combine(sourcePath, info.GetFileName());
                if (!File.Exists(srcFile))
                {
                    Debug.LogWarning($"[LWAssets] Bundle file not found: {srcFile}");
                    continue;
                }

                var dstFile = Path.Combine(destPath, info.GetFileName());
                File.Copy(srcFile, dstFile, true);

                copiedBundleNames.Add(info.BundleName);
                copiedBundleInfos.Add(info);
            }

            if (copiedBundleInfos.Count <= 0)
            {
                Debug.LogWarning("[LWAssets] No bundle files copied, skip manifest/version generation.");
                return;
            }

            var newManifest = new BundleManifest
            {
                Version = manifest.Version,
                BuildTime = manifest.BuildTime,
                Platform = manifest.Platform,
                Bundles = new List<BundleInfo>(copiedBundleInfos.Count)
            };

            for (int i = 0; i < copiedBundleInfos.Count; i++)
            {
                var srcInfo = copiedBundleInfos[i];

                var dstInfo = new BundleInfo
                {
                    BundleName = srcInfo.BundleName,
                    Hash = srcInfo.Hash,
                    CRC = srcInfo.CRC,
                    Size = srcInfo.Size,
                    IsRawFile = srcInfo.IsRawFile,
                    IsEncrypted = srcInfo.IsEncrypted,
                    Tags = srcInfo.Tags != null ? new List<string>(srcInfo.Tags) : new List<string>(),
                    Dependencies = new List<string>(),
                    Assets = srcInfo.Assets != null ? new List<string>(srcInfo.Assets) : new List<string>()
                };

                if (srcInfo.Dependencies != null)
                {
                    for (int d = 0; d < srcInfo.Dependencies.Count; d++)
                    {
                        var depName = srcInfo.Dependencies[d];
                        if (string.IsNullOrEmpty(depName))
                        {
                            continue;
                        }

                        if (copiedBundleNames.Contains(depName))
                        {
                            dstInfo.Dependencies.Add(depName);
                        }
                    }
                }

                newManifest.Bundles.Add(dstInfo);
            }

            newManifest.BuildIndex();

            var manifestFileName = m_RuntimeConfig != null ? m_RuntimeConfig.ManifestFileName : "manifest.json";
            var versionFileName = m_RuntimeConfig != null ? m_RuntimeConfig.VersionFileName : "version.json";

            var newManifestPath = Path.Combine(destPath, manifestFileName);
            File.WriteAllText(newManifestPath, newManifest.ToJson());

            var newManifestHash = HashUtility.ComputeFileMD5(newManifestPath);
            var newManifestSize = new FileInfo(newManifestPath).Length;
            var version = new VersionInfo
            {
                Version = PlayerSettings.bundleVersion,
                ManifestHash = newManifestHash,
                ManifestSize = newManifestSize,
                BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                MinAppVersion = Application.version,
                ForceUpdate = false
            };
            var newVersionPath = Path.Combine(destPath, versionFileName);
            File.WriteAllText(newVersionPath, JsonUtility.ToJson(version, true));

            AssetDatabase.Refresh();
            Debug.Log($"[LWAssets] Copied {copiedBundleInfos.Count} bundles to StreamingAssets: {destPath}");
        }
    }
}
#endif
