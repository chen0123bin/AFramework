#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWAssets.Editor
{

    /// <summary>
    /// 构建管线
    /// </summary>
    public static class LWAssetsBuildPipeline
    { 
        // 存储构建时的原始资源路径映射
        private static Dictionary<string, List<string>> _bundleAssetMap;
        /// <summary>
        /// 执行构建
        /// </summary>
        public static void Build(LWAssetsBuildConfig config)
        {
            if (config == null)
            {
                Debug.LogError("[LWAssets] Build config is null!");
                return;
            }

            var startTime = DateTime.Now;
            Debug.Log("[LWAssets] Build started...");

            try
            {
                // 1. 收集资源并保存原始路径映射
                _bundleAssetMap = new Dictionary<string, List<string>>();
                var bundleBuilds = CollectAssets(config);

                // 2. 处理Shader
                if (config.CollectShaders)
                {
                    var shaderBuild = CollectShaders(config);
                    if (shaderBuild.assetNames.Length > 0)
                    {
                        bundleBuilds.Add(shaderBuild);
                        // 保存Shader的原始路径
                        _bundleAssetMap[shaderBuild.assetBundleName] = shaderBuild.assetNames.ToList();
                    }
                }

                // 3. 构建AssetBundle
                var outputPath = GetOutputPath(config);
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                var manifest = BuildPipeline.BuildAssetBundles(
                    outputPath,
                    bundleBuilds.ToArray(),
                    config.BuildOptions,
                    config.BuildTarget);

                if (manifest == null)
                {
                    Debug.LogError("[LWAssets] Build failed!");
                    return;
                }

                // 4. 生成清单文件
                var bundleManifest = GenerateManifest(config, manifest, outputPath);

                // 5. 生成版本文件
                GenerateVersionFile(config, bundleManifest, outputPath);

                // 6. 清理多余文件
                CleanupOutputPath(outputPath, bundleManifest);

                // 7. 生成报告
                if (config.GenerateReport)
                {
                    GenerateBuildReport(config, bundleManifest, outputPath);
                }

                var elapsed = DateTime.Now - startTime;
                Debug.Log($"[LWAssets] Build completed in {elapsed.TotalSeconds:F2}s");

                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LWAssets] Build error: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _bundleAssetMap = null;
            }
        }

        /// <summary>
        /// 收集资源
        /// </summary>
        private static List<AssetBundleBuild> CollectAssets(LWAssetsBuildConfig config)
        {
            var builds = new List<AssetBundleBuild>();
            var processedAssets = new HashSet<string>();

            foreach (var rule in config.PackageRules)
            {
                if (string.IsNullOrEmpty(rule.FolderPath)) continue;
                if (!Directory.Exists(rule.FolderPath)) continue;

                var assets = Directory.GetFiles(rule.FolderPath, rule.FilePattern, SearchOption.AllDirectories)
                    .Where(f => !f.EndsWith(".meta"))
                    .Where(f => !processedAssets.Contains(f))
                    .Select(f => f.Replace("\\", "/"))
                    .ToList();

                var ruleBuilds = new List<AssetBundleBuild>();  
                switch (rule.Strategy)
                {
                    case PackageStrategy.ByFolder:
                        ruleBuilds = PackageByFolder(rule, assets);
                        break;

                    case PackageStrategy.ByFile:
                        ruleBuilds = PackageByFile(rule, assets);
                        break;

                    case PackageStrategy.BySize:
                      ruleBuilds = PackageBySize(rule, assets);
                        break;

                    case PackageStrategy.RawFile:
                        // 原始文件不加入AssetBundle，但需要记录
                        break;
                }
                // 保存原始路径映射
                foreach (var build in ruleBuilds)
                {
                    _bundleAssetMap[build.assetBundleName] = build.assetNames.ToList();
                    builds.Add(build);
                }
                foreach (var asset in assets)
                {
                    processedAssets.Add(asset);
                }
            }

            return builds;
        }

        /// <summary>
        /// 按文件夹分包
        /// </summary>
        private static List<AssetBundleBuild> PackageByFolder(PackageRule rule, List<string> assets)
        {
            var builds = new List<AssetBundleBuild>();
            var folderGroups = new Dictionary<string, List<string>>();

            foreach (var asset in assets)
            {
                var folder = Path.GetDirectoryName(asset).Replace("\\", "/");
                var relativePath = folder.Replace(rule.FolderPath.Replace("\\", "/"), "").Trim('/');

                if (string.IsNullOrEmpty(relativePath))
                {
                    relativePath = Path.GetFileName(rule.FolderPath);
                }

                if (!folderGroups.ContainsKey(relativePath))
                {
                    folderGroups[relativePath] = new List<string>();
                }
                folderGroups[relativePath].Add(asset);
            }

            foreach (var group in folderGroups)
            {
                var bundleName = $"{rule.Name}_{group.Key}".ToLower().Replace("/", "_").Replace(" ", "_");
                builds.Add(new AssetBundleBuild
                {
                    assetBundleName = bundleName,
                    assetNames = group.Value.ToArray()
                });
            }

            return builds;
        }

        /// <summary>
        /// 按文件分包
        /// </summary>
        private static List<AssetBundleBuild> PackageByFile(PackageRule rule, List<string> assets)
        {
            var builds = new List<AssetBundleBuild>();

            foreach (var asset in assets)
            {
                var fileName = Path.GetFileNameWithoutExtension(asset);
                var bundleName = $"{rule.Name}_{fileName}".ToLower().Replace(" ", "_");

                builds.Add(new AssetBundleBuild
                {
                    assetBundleName = bundleName,
                    assetNames = new[] { asset }
                });
            }

            return builds;
        }

        /// <summary>
        /// 按大小分包
        /// </summary>
        private static List<AssetBundleBuild> PackageBySize(PackageRule rule, List<string> assets)
        {
            var builds = new List<AssetBundleBuild>();
            var currentAssets = new List<string>();
            long currentSize = 0;
            int bundleIndex = 0;

            // 按文件大小排序
            var sortedAssets = assets
                .Select(a => new { Path = a, Size = new FileInfo(a).Length })
                .OrderByDescending(a => a.Size)
                .ToList();

            foreach (var asset in sortedAssets)
            {
                // 如果当前bundle超过大小限制，创建新bundle
                if (currentSize + asset.Size > rule.MaxBundleSize && currentAssets.Count > 0)
                {
                    builds.Add(new AssetBundleBuild
                    {
                        assetBundleName = $"{rule.Name}_{bundleIndex}".ToLower(),
                        assetNames = currentAssets.ToArray()
                    });

                    currentAssets.Clear();
                    currentSize = 0;
                    bundleIndex++;
                }

                currentAssets.Add(asset.Path);
                currentSize += asset.Size;
            }

            // 剩余资源
            if (currentAssets.Count > 0)
            {
                builds.Add(new AssetBundleBuild
                {
                    assetBundleName = $"{rule.Name}_{bundleIndex}".ToLower(),
                    assetNames = currentAssets.ToArray()
                });
            }

            return builds;
        }

        /// <summary>
        /// 收集Shader
        /// </summary>
        private static AssetBundleBuild CollectShaders(LWAssetsBuildConfig config)
        {
            var shaderPaths = new List<string>();

            // 收集所有Shader
            var guids = AssetDatabase.FindAssets("t:Shader");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                // 排除内置Shader和Package中的Shader
                if (!path.StartsWith("Assets/")) continue;
                shaderPaths.Add(path);
            }

            // 收集ShaderVariantCollection
            foreach (var svcPath in config.ShaderVariantCollections)
            {
                if (File.Exists(svcPath))
                {
                    shaderPaths.Add(svcPath);
                }
            }

            return new AssetBundleBuild
            {
                assetBundleName = config.ShaderBundleName,
                assetNames = shaderPaths.ToArray()
            };
        }

        // Editor/Build/LWAssetsBuildPipeline.cs (部分修改)

        /// <summary>
        /// 生成清单文件（优化版）
        /// </summary>
        private static BundleManifest GenerateManifest(LWAssetsBuildConfig config,
            AssetBundleManifest unityManifest, string outputPath)
        {
            var manifest = new BundleManifest
            {
                Version = PlayerSettings.bundleVersion,
                BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Platform = LWAssetsConfig.GetPlatformName()
            };

            var allBundles = unityManifest.GetAllAssetBundles();

            foreach (var bundleName in allBundles)
            {
                var bundlePath = Path.Combine(outputPath, bundleName);
                var fileInfo = new FileInfo(bundlePath);

                var bundleInfo = new BundleInfo
                {
                    BundleName = bundleName,
                    Hash = unityManifest.GetAssetBundleHash(bundleName).ToString(),
                    CRC = HashUtility.ComputeFileCRC32(bundlePath),
                    Size = fileInfo.Length,
                    Dependencies = unityManifest.GetAllDependencies(bundleName).ToList()
                };

                 // 使用保存的原始路径，而不是从Bundle获取
                if (_bundleAssetMap.TryGetValue(bundleName, out var originalAssets))
                {
                    bundleInfo.Assets = new List<string>(originalAssets);
                }
                else
                {
                    // 备用方案：如果没有映射，尝试从Bundle获取（会是小写）
                    Debug.LogWarning($"[LWAssets] No original path mapping for bundle: {bundleName}");
                    var bundle = AssetBundle.LoadFromFile(bundlePath);
                    if (bundle != null)
                    {
                        bundleInfo.Assets = bundle.GetAllAssetNames().ToList();
                        bundle.Unload(false);
                    }
                }

                // 应用分包规则的标签和优先级
                var rule = config.PackageRules.FirstOrDefault(r =>
                    bundleName.StartsWith(r.Name.ToLower()));
                if (rule != null)
                {
                    bundleInfo.Priority = rule.Priority;
                    bundleInfo.Tags.AddRange(rule.Tags);
                }

                // 应用TagRule
                ApplyTagRules(config, bundleInfo);

                manifest.Bundles.Add(bundleInfo);

                // 重命名Bundle文件
                var newBundlePath = Path.Combine(outputPath, bundleInfo.GetFileName());
                if (File.Exists(newBundlePath))
                {
                    File.Delete(newBundlePath);
                }
                File.Move(bundlePath, newBundlePath);
            }

            // 处理原始文件
            ProcessRawFiles(config, manifest, outputPath);

            // 保存清单
            var manifestPath = Path.Combine(outputPath, "manifest.json");
            File.WriteAllText(manifestPath, manifest.ToJson());

            // 构建索引（用于后续操作）
            manifest.BuildIndex();

            return manifest;
        }

        /// <summary>
        /// 处理原始文件（优化版）
        /// </summary>
        private static void ProcessRawFiles(LWAssetsBuildConfig config, BundleManifest manifest, string outputPath)
        {
            foreach (var rule in config.PackageRules.Where(r => r.Strategy == PackageStrategy.RawFile))
            {
                if (string.IsNullOrEmpty(rule.FolderPath)) continue;
                if (!Directory.Exists(rule.FolderPath)) continue;

                var files = Directory.GetFiles(rule.FolderPath, rule.FilePattern, SearchOption.AllDirectories)
                    .Where(f => !f.EndsWith(".meta"))
                    .Select(f => f.Replace("\\", "/"))
                    .ToList();

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var hash = HashUtility.ComputeFileMD5(file);
                    var fileInfo = new FileInfo(file);

                    var bundleInfo = new BundleInfo
                    {
                        BundleName = $"raw_{Path.GetFileNameWithoutExtension(fileName)}",
                        Hash = hash,
                        Size = fileInfo.Length,
                        IsRawFile = true,
                        Priority = rule.Priority,
                        Tags = new List<string>(rule.Tags),
                        Assets = new List<string> { file }  // 原始文件的Assets只有自己
                    };

                    manifest.Bundles.Add(bundleInfo);

                    // 复制到输出目录
                    var destPath = Path.Combine(outputPath, bundleInfo.GetFileName());
                    File.Copy(file, destPath, true);
                }
            }
        }

        /// <summary>
        /// 应用标签规则
        /// </summary>
        private static void ApplyTagRules(LWAssetsBuildConfig config, BundleInfo bundleInfo)
        {
            foreach (var rule in config.TagRules)
            {
                foreach (var asset in bundleInfo.Assets)
                {
                    if (asset.StartsWith(rule.FolderPath.Replace("\\", "/")))
                    {
                        foreach (var tag in rule.Tags)
                        {
                            if (!bundleInfo.Tags.Contains(tag))
                            {
                                bundleInfo.Tags.Add(tag);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 生成版本文件
        /// </summary>
        private static void GenerateVersionFile(LWAssetsBuildConfig config,
            BundleManifest manifest, string outputPath)
        {
            var manifestPath = Path.Combine(outputPath, "manifest.json");
            var manifestHash = HashUtility.ComputeFileMD5(manifestPath);
            var manifestSize = new FileInfo(manifestPath).Length;

            var version = new VersionInfo
            {
                Version = PlayerSettings.bundleVersion,
                ManifestHash = manifestHash,
                ManifestSize = manifestSize,
                BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                MinAppVersion = Application.version,
                ForceUpdate = false
            };

            var versionPath = Path.Combine(outputPath, "version.json");
            File.WriteAllText(versionPath, JsonUtility.ToJson(version, true));
        }

        /// <summary>
        /// 清理输出目录
        /// </summary>
        private static void CleanupOutputPath(string outputPath, BundleManifest manifest)
        {
            var validFiles = new HashSet<string>();
            validFiles.Add("manifest.json");
            validFiles.Add("version.json");

            foreach (var bundle in manifest.Bundles)
            {
                validFiles.Add(bundle.GetFileName());
            }

            foreach (var file in Directory.GetFiles(outputPath))
            {
                var fileName = Path.GetFileName(file);
                if (!validFiles.Contains(fileName))
                {
                    File.Delete(file);
                }
            }
        }

        /// <summary>
        /// 生成构建报告
        /// </summary>
        private static void GenerateBuildReport(LWAssetsBuildConfig config,
            BundleManifest manifest, string outputPath)
        {
            var report = new System.Text.StringBuilder();

            report.AppendLine("=== LWAssets Build Report ===");
            report.AppendLine($"Build Time: {DateTime.Now}");
            report.AppendLine($"Platform: {config.BuildTarget}");
            report.AppendLine($"Version: {PlayerSettings.bundleVersion}");
            report.AppendLine();

            report.AppendLine("=== Bundle Summary ===");
            report.AppendLine($"Total Bundles: {manifest.Bundles.Count}");
            report.AppendLine($"Total Size: {FileUtility.FormatFileSize(manifest.GetTotalSize())}");
            report.AppendLine($"Total Assets: {manifest.GetAssetCount()}");
            report.AppendLine();

            report.AppendLine("=== Bundle Details ===");
            foreach (var bundle in manifest.Bundles.OrderByDescending(b => b.Size))
            {
                report.AppendLine($"  {bundle.BundleName}");
                report.AppendLine($"    Size: {FileUtility.FormatFileSize(bundle.Size)}");
                report.AppendLine($"    Assets: {bundle.Assets.Count}");
                report.AppendLine($"    Dependencies: {bundle.Dependencies.Count}");
                report.AppendLine($"    Tags: {string.Join(", ", bundle.Tags)}");
            }

            report.AppendLine();
            report.AppendLine("=== Tag Summary ===");
            var tagGroups = manifest.Bundles
                .SelectMany(b => b.Tags.Select(t => new { Tag = t, Bundle = b }))
                .GroupBy(x => x.Tag);

            foreach (var group in tagGroups)
            {
                var totalSize = group.Sum(x => x.Bundle.Size);
                report.AppendLine($"  {group.Key}: {group.Count()} bundles, {FileUtility.FormatFileSize(totalSize)}");
            }

            var reportPath = Path.Combine(outputPath, "build_report.txt");
            File.WriteAllText(reportPath, report.ToString());

            Debug.Log($"[LWAssets] Build report saved to: {reportPath}");
        }

        /// <summary>
        /// 获取输出路径
        /// </summary>
        private static string GetOutputPath(LWAssetsBuildConfig config)
        {
            var platform = config.BuildTarget switch
            {
                BuildTarget.Android => "Android",
                BuildTarget.iOS => "iOS",
                BuildTarget.WebGL => "WebGL",
                BuildTarget.StandaloneWindows64 => "Windows",
                BuildTarget.StandaloneOSX => "MacOS",
                BuildTarget.StandaloneLinux64 => "Linux",
                _ => "Unknown"
            };

            return Path.Combine(Application.dataPath, "..", config.OutputPath, platform);
        }
    }
}
#endif
