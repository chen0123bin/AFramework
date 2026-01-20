#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using LWCore.Editor;

namespace LWAssets.Editor
{
    /// <summary>
    /// LWAssets主窗口
    /// </summary>
    public class LWAssetsTreeView : BaseHubTreeView
    {
        private LWAssetsBuildConfig m_BuildConfig;
        private LWAssetsConfig m_RuntimeConfig;
        private Vector2 m_ScrollPos;

        private bool m_IsBuilding;
        private bool m_IsBuildingPlayer;
        private bool m_CopyToStreamingAssetsAfterBuild;

        /// <summary>
        /// 创建 LWAssets Hub 页面。
        /// </summary>
        /// <param name="nodePath">左侧树节点路径。</param>
        /// <param name="iconPath">图标路径。</param>
        public LWAssetsTreeView(string nodePath, string iconPath)
            : base(nodePath, iconPath)
        {
        }

        /// <summary>
        /// 页面选中时刷新配置缓存。
        /// </summary>
        public override void OnSelected()
        {
            LoadConfigs();
        }

        /// <summary>
        /// 页面取消选中时取消延迟构建回调，避免残留任务。
        /// </summary>
        public override void OnDeselected()
        {
            EditorApplication.delayCall -= ExecuteBuildAsset;
        }

        /// <summary>
        /// 读取构建配置与运行时配置。
        /// </summary>
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

        /// <summary>
        /// Hub 右侧面板绘制入口。
        /// </summary>
        protected override void DrawContent()
        {
            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
            try
            {
                DrawDashboard();
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }


        /// <summary>
        /// 绘制总览面板：统计信息、快捷操作、构建历史。
        /// </summary>
        private void DrawDashboard()
        {
            EditorGUILayout.LabelField("LWAssets Dashboard", EditorStyles.largeLabel);
            EditorGUILayout.Space();

            // 统计信息
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Project Statistics", EditorStyles.boldLabel);

            int assetCount = AssetDatabase.FindAssets("", new[] { "Assets" }).Length;
            int prefabCount = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }).Length;
            int textureCount = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" }).Length;
            int materialCount = AssetDatabase.FindAssets("t:Material", new[] { "Assets" }).Length;

            EditorGUILayout.LabelField($"Total Assets: {assetCount}");
            EditorGUILayout.LabelField($"Prefabs: {prefabCount}");
            EditorGUILayout.LabelField($"Textures: {textureCount}");
            EditorGUILayout.LabelField($"Materials: {materialCount}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 快捷操作
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("窗口", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Bundle Viewer", GUILayout.Height(40)))
                {
                    BundleViewer.ShowWindow();
                }

                if (GUILayout.Button("Analyze Assets", GUILayout.Height(40)))
                {
                    AssetAnalyzer.ShowWindow();
                }
                if (GUILayout.Button("AssetRuntimeMonitorWindow", GUILayout.Height(40)))
                {
                    AssetRuntimeMonitorWindow.ShowWindow();
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("资源管理器", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Player Folder", GUILayout.Height(40)))
                {
                    if (m_BuildConfig != null)
                    {
                        string path = Path.Combine(Application.dataPath, "..", "BuildPlayer");
                        if (Directory.Exists(path))
                        {
                            EditorUtility.OpenWithDefaultApp(path);
                        }
                    }
                }
                if (GUILayout.Button("Open Bundle Folder", GUILayout.Height(40)))
                {
                    if (m_BuildConfig != null)
                    {
                        string path = Path.Combine(Application.dataPath, "..", m_BuildConfig.OutputPath);
                        Debug.Log($"Open Bundle Folder: {path}");
                        if (Directory.Exists(path))
                        {
                            EditorUtility.OpenWithDefaultApp(path);
                        }
                    }
                }

                if (GUILayout.Button("Open Download Folder", GUILayout.Height(40)))
                {
                    if (m_RuntimeConfig != null)
                    {
                        if (Directory.Exists(m_RuntimeConfig.GetPersistentDataPath()))
                        {
                            EditorUtility.OpenWithDefaultApp(m_RuntimeConfig.GetPersistentDataPath());
                        }
                        else
                        {
                            Debug.LogError($"Download Folder not found: {m_RuntimeConfig.GetPersistentDataPath()}");
                        }
                    }
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("构建/拷贝", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build AssetBundles", GUILayout.Height(40)))
                {
                    RequestBuildAsset(false);
                }

                if (GUILayout.Button("Copy to StreamingAssets", GUILayout.Height(40)))
                {
                    if (EditorUtility.DisplayDialog("Copy to StreamingAssets",
                        "Are you sure you want to copy the build assets to StreamingAssets folder?", "Yes", "No"))
                    {
                        LWAssetsBuildPipeline.CopyToStreamingAssets(m_BuildConfig);
                    }
                }

                if (GUILayout.Button("Copy by BuiltinTags", GUILayout.Height(40)))
                {
                    if (EditorUtility.DisplayDialog("Copy to StreamingAssets by BuiltinTags",
                        "Are you sure you want to copy the build assets to StreamingAssets folder?", "Yes", "No"))
                    {
                        LWAssetsBuildPipeline.CopyToStreamingAssetsByBuiltinTags(m_BuildConfig, m_RuntimeConfig);
                    }
                }
                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build And Copy", GUILayout.Height(40)))
                {
                    RequestBuildAsset(true);
                }

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

                GUILayout.FlexibleSpace();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build Player And Copy", GUILayout.Height(40)))
                {
                    RequestBuildPlayer();
                }

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 构建历史
            DrawBuildHistory();
        }

        /// <summary>
        /// 绘制最近构建历史（从构建输出目录读取 manifest）。
        /// </summary>
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
                    string[] platforms = Directory.GetDirectories(buildPath);

                    foreach (var platform in platforms)
                    {
                        string manifestPath = Path.Combine(platform, "manifest.json");
                        if (File.Exists(manifestPath))
                        {
                            string json = File.ReadAllText(manifestPath);
                            BundleManifest manifest = BundleManifest.FromJson(json);

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
        /// <summary>
        /// 请求构建
        /// </summary>
        /// <param name="copyToStreamingAssets"></param>
        private void RequestBuildAsset(bool copyToStreamingAssets)
        {
            if (m_BuildConfig == null)
            {
                Debug.LogError("[LWAssets] Build config not found!");
                return;
            }

            if (m_IsBuilding) return;

            m_IsBuilding = true;
            m_CopyToStreamingAssetsAfterBuild = copyToStreamingAssets;

            EditorApplication.delayCall -= ExecuteBuildAsset;
            EditorApplication.delayCall += ExecuteBuildAsset;
        }
        /// <summary>
        /// 执行构建
        /// </summary>
        private void ExecuteBuildAsset()
        {
            EditorApplication.delayCall -= ExecuteBuildAsset;

            try
            {
                LWAssetsBuildPipeline.Build(m_BuildConfig);

                if (m_CopyToStreamingAssetsAfterBuild)
                {
                    LWAssetsBuildPipeline.CopyToStreamingAssets(m_BuildConfig);
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
            }
        }
        /// <summary>
        /// 请求构建Player
        /// </summary>
        private void RequestBuildPlayer()
        {


            EditorApplication.delayCall -= ExecuteBuildPlayer;
            EditorApplication.delayCall += ExecuteBuildPlayer;
        }

        /// <summary>
        /// 执行构建Player
        /// </summary>
        private void ExecuteBuildPlayer()
        {
            EditorApplication.delayCall -= ExecuteBuildPlayer;
            try
            {
                if (m_IsBuilding || m_IsBuildingPlayer)
                {
                    return;
                }

                if (m_BuildConfig == null)
                {
                    EditorUtility.DisplayDialog("Build Player", "Build config not found!", "OK");
                    return;
                }

                string[] scenePaths = GetEnabledScenePaths();
                if (scenePaths == null || scenePaths.Length <= 0)
                {
                    EditorUtility.DisplayDialog("Build Player", "No enabled scenes in Build Settings.", "OK");
                    return;
                }

                BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
                string playerPath = GetPlayerLocationPath(buildTarget);

                if (string.IsNullOrEmpty(playerPath))
                {
                    return;
                }

                m_IsBuildingPlayer = true;
                BuildReport report = LWAssetsBuildPipeline.BuildPlayerAndCopyAsset(m_BuildConfig, m_RuntimeConfig, scenePaths, buildTarget, playerPath);
                if (report != null && report.summary.result != BuildResult.Succeeded)
                {
                    EditorUtility.DisplayDialog("Build Player", $"Build failed: {report.summary.result}", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Build Player", "Build succeeded.", "OK");
                }

            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Build Player", ex.Message, "OK");
            }
            finally
            {
                m_IsBuilding = false;
                m_IsBuildingPlayer = false;
                m_CopyToStreamingAssetsAfterBuild = false;
            }
        }
        /// <summary>
        /// 清理构建输出目录（按当前构建配置）。
        /// </summary>
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


        /// <summary>
        /// 获取 Build Settings 中启用的场景路径列表。
        /// </summary>
        private string[] GetEnabledScenePaths()
        {
            List<string> scenePaths = new List<string>();
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length <= 0)
            {
                return scenePaths.ToArray();
            }

            for (int i = 0; i < scenes.Length; i++)
            {
                EditorBuildSettingsScene scene = scenes[i];
                if (scene == null || !scene.enabled)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(scene.path))
                {
                    continue;
                }

                scenePaths.Add(scene.path);
            }

            return scenePaths.ToArray();
        }

        /// <summary>
        /// 根据平台弹出保存对话框，获取 Player 输出路径。
        /// </summary>
        /// <param name="buildTarget">当前构建目标平台。</param>
        private string GetPlayerLocationPath(BuildTarget buildTarget)
        {
            string defaultDirectory = Path.Combine(Application.dataPath, "..", "BuildPlayer");
            string productName = PlayerSettings.productName;
            if (string.IsNullOrEmpty(productName))
            {
                productName = "Game";
            }

            if (buildTarget == BuildTarget.StandaloneWindows || buildTarget == BuildTarget.StandaloneWindows64)
            {
                return EditorUtility.SaveFilePanel("Build Player", defaultDirectory, productName, "exe");
            }

            if (buildTarget == BuildTarget.Android)
            {
                return EditorUtility.SaveFilePanel("Build Player", defaultDirectory, productName, "apk");
            }

            if (buildTarget == BuildTarget.iOS || buildTarget == BuildTarget.WebGL || buildTarget == BuildTarget.StandaloneOSX)
            {
                return EditorUtility.SaveFolderPanel("Build Player", defaultDirectory, productName);
            }

            return EditorUtility.SaveFolderPanel("Build Player", defaultDirectory, productName);
        }


    }
}
#endif
