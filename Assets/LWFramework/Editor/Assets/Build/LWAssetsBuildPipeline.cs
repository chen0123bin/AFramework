#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LitJson;
using UnityEditor;
using UnityEditor.Build.Reporting;
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
                    EditorUserBuildSettings.activeBuildTarget);
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
        /// 构建 Player：先构建 AssetBundle，再按 BuiltinTags 复制到 StreamingAssets，最后调用 Unity BuildPlayer。
        /// </summary>
        /// <param name="buildConfig">构建配置（决定 AssetBundle 输出与 BuiltinTags）。</param>
        /// <param name="runtimeConfig">运行时配置（决定 manifest/version 文件名）。</param>
        /// <param name="scenePaths">Build Settings 中启用的场景路径。</param>
        /// <param name="buildTarget">目标平台。</param>
        /// <param name="locationPathName">Player 输出路径。</param>
        /// <returns>Unity BuildReport。</returns>
        public static BuildReport BuildPlayerAndCopyAsset(LWAssetsBuildConfig buildConfig, LWAssetsConfig runtimeConfig, string[] scenePaths, BuildTarget buildTarget, string locationPathName)
        {
            Build(buildConfig);
            //Offline 模式下，直接复制到 StreamingAssets
            if (runtimeConfig.PlayMode == PlayMode.Offline)
            {
                CopyToStreamingAssets(buildConfig);
            }
            //Online 模式下，按 BuiltinTags 复制到 StreamingAssets
            else
            {
                CopyToStreamingAssetsByBuiltinTags(buildConfig, runtimeConfig);
            }
            AssetDatabase.Refresh();

            BuildPlayerOptions options = new BuildPlayerOptions();
            options.scenes = scenePaths;
            options.locationPathName = locationPathName;
            options.target = buildTarget;
            options.options = BuildOptions.None;

            BuildReport report = BuildPipeline.BuildPlayer(options);
            return report;
        }
        /// <summary>
        /// 将构建输出完整复制到 StreamingAssets（不做标签过滤）。
        /// </summary>
        public static void CopyToStreamingAssets(LWAssetsBuildConfig buildConfig)
        {
            if (buildConfig == null) return;

            var sourcePath = Path.Combine(Application.dataPath, "..",
                buildConfig.OutputPath, LWAssetsConfig.GetPlatformName());
            var destPath = Path.Combine(Application.streamingAssetsPath,
                buildConfig.OutputPath, LWAssetsConfig.GetPlatformName());

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
        /// 按 BuiltinTags 将构建输出复制到 StreamingAssets，并生成裁剪后的 manifest/version。
        /// </summary>
        /// <param name="buildConfig">构建配置。</param>
        /// <param name="runtimeConfig">运行时配置（可为空）。</param>
        public static void CopyToStreamingAssetsByBuiltinTags(LWAssetsBuildConfig buildConfig, LWAssetsConfig runtimeConfig)
        {
            if (buildConfig == null)
            {
                Debug.LogError("[LWAssets] Build config not found!");
                return;
            }

            string outputPath = GetOutputPath(buildConfig);
            string platformName = Path.GetFileName(outputPath);
            string destPath = Path.Combine(Application.streamingAssetsPath, buildConfig.OutputPath, platformName);

            if (!Directory.Exists(outputPath))
            {
                Debug.LogError("[LWAssets] Build output not found!");
                return;
            }

            if (buildConfig.BuiltinTags == null || buildConfig.BuiltinTags.Count <= 0)
            {
                if (Directory.Exists(destPath))
                {
                    Directory.Delete(destPath, true);
                }

                FileUtility.CopyDirectory(outputPath, destPath);
                AssetDatabase.Refresh();
                Debug.Log($"[LWAssets] Copied to StreamingAssets: {destPath}");
                return;
            }

            string manifestFileName = runtimeConfig != null ? runtimeConfig.ManifestFileName : "manifest.json";
            string versionFileName = runtimeConfig != null ? runtimeConfig.VersionFileName : "version.json";
            string manifestPath = Path.Combine(outputPath, manifestFileName);

            if (!File.Exists(manifestPath))
            {
                Debug.LogError($"[LWAssets] Manifest not found: {manifestPath}");
                return;
            }

            BundleManifest manifest;
            try
            {
                string json = File.ReadAllText(manifestPath);
                manifest = BundleManifest.FromJson(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LWAssets] Load manifest failed: {ex.Message}");
                return;
            }

            HashSet<string> bundleNamesToCopy = new HashSet<string>(StringComparer.Ordinal);
            List<BundleInfo> hitBundles = manifest.GetBundlesByTags(buildConfig.BuiltinTags);
            for (int i = 0; i < hitBundles.Count; i++)
            {
                BundleInfo hit = hitBundles[i];
                if (hit == null || string.IsNullOrEmpty(hit.BundleName))
                {
                    continue;
                }

                bundleNamesToCopy.Add(hit.BundleName);

                List<BundleInfo> deps = manifest.GetAllDependencies(hit.BundleName);
                for (int d = 0; d < deps.Count; d++)
                {
                    BundleInfo dep = deps[d];
                    if (dep == null || string.IsNullOrEmpty(dep.BundleName))
                    {
                        continue;
                    }

                    bundleNamesToCopy.Add(dep.BundleName);
                }
            }

            if (bundleNamesToCopy.Count <= 0)
            {
                Debug.LogWarning($"[LWAssets] No bundles matched by tags: {string.Join(",", buildConfig.BuiltinTags)}");
                return;
            }

            if (Directory.Exists(destPath))
            {
                Directory.Delete(destPath, true);
            }
            Directory.CreateDirectory(destPath);

            HashSet<string> copiedBundleNames = new HashSet<string>(StringComparer.Ordinal);
            List<BundleInfo> copiedBundleInfos = new List<BundleInfo>(bundleNamesToCopy.Count);

            foreach (string bundleName in bundleNamesToCopy)
            {
                BundleInfo info = manifest.GetBundleInfo(bundleName);
                if (info == null)
                {
                    Debug.LogWarning($"[LWAssets] BundleInfo not found in manifest: {bundleName}");
                    continue;
                }

                string srcFile = Path.Combine(outputPath, info.GetFileName());
                if (!File.Exists(srcFile))
                {
                    Debug.LogWarning($"[LWAssets] Bundle file not found: {srcFile}");
                    continue;
                }

                string dstFile = Path.Combine(destPath, info.GetFileName());
                File.Copy(srcFile, dstFile, true);

                copiedBundleNames.Add(info.BundleName);
                copiedBundleInfos.Add(info);
            }

            if (copiedBundleInfos.Count <= 0)
            {
                Debug.LogWarning("[LWAssets] No bundle files copied, skip manifest/version generation.");
                return;
            }

            BundleManifest newManifest = new BundleManifest
            {
                Version = manifest.Version,
                BuildTime = manifest.BuildTime,
                Platform = manifest.Platform,
                Bundles = new List<BundleInfo>(copiedBundleInfos.Count)
            };

            for (int i = 0; i < copiedBundleInfos.Count; i++)
            {
                BundleInfo srcInfo = copiedBundleInfos[i];
                BundleInfo dstInfo = new BundleInfo
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
                        string depName = srcInfo.Dependencies[d];
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

            string newManifestPath = Path.Combine(destPath, manifestFileName);
            File.WriteAllText(newManifestPath, newManifest.ToJson());

            string newManifestHash = HashUtility.ComputeFileMD5(newManifestPath);
            long newManifestSize = new FileInfo(newManifestPath).Length;
            VersionInfo version = new VersionInfo
            {
                Version = manifest.Version,
                ManifestHash = newManifestHash,
                ManifestSize = newManifestSize,
                BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                MinAppVersion = Application.version,
                ForceUpdate = false
            };
            string newVersionPath = Path.Combine(destPath, versionFileName);
            File.WriteAllText(newVersionPath, JsonUtility.ToJson(version, true));

            AssetDatabase.Refresh();
            Debug.Log($"[LWAssets] Copied {copiedBundleInfos.Count} bundles to StreamingAssets: {destPath}");
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
                    case PackageStrategy.ByTopFolder:
                        ruleBuilds = PackageByTopFolder(rule, assets);
                        break;
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
        /// 按最顶层文件夹分包
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="assets"></param>
        /// <returns></returns>
        private static List<AssetBundleBuild> PackageByTopFolder(PackageRule rule, List<string> assets)
        {
            var builds = new List<AssetBundleBuild>();
            var topFolder = Path.GetFileName(rule.FolderPath);
            var bundleName = $"{rule.Name}_{topFolder}".ToLower().Replace(" ", "_");
            builds.Add(new AssetBundleBuild
            {
                assetBundleName = bundleName,
                assetNames = assets.ToArray()
            });
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

            //收集ShaderVariantCollection
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
            var buildVersion = AllocateNextBuildVersion(config);
            var manifest = new BundleManifest
            {
                Version = buildVersion,
                BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Platform = LWAssetsConfig.GetPlatformName()
            };

            var allBundles = unityManifest.GetAllAssetBundles();

            foreach (var bundleName in allBundles)
            {
                var bundlePath = Path.Combine(outputPath, bundleName);
                var fileInfo = new FileInfo(bundlePath);

                string[] unityDependencies = unityManifest.GetAllDependencies(bundleName);
                List<string> dependencies = new List<string>(unityDependencies.Length);
                foreach (string dependencyBundleName in unityDependencies)
                {
                    // 排除ShaderBundle
                    if (string.Equals(dependencyBundleName, config.ShaderBundleName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    dependencies.Add(dependencyBundleName);
                }

                var bundleInfo = new BundleInfo
                {
                    BundleName = bundleName,
                    //Hash = unityManifest.GetAssetBundleHash(bundleName).ToString(),
                    Hash = HashUtility.ComputeFileMD5(bundlePath),
                    CRC = HashUtility.ComputeFileCRC32(bundlePath),
                    Size = fileInfo.Length,
                    Dependencies = dependencies
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

                // 应用分包规则的标签
                var rule = config.PackageRules.FirstOrDefault(r =>
                    bundleName.StartsWith(r.Name.ToLower()));
                if (rule != null)
                {
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
        /// 应用标签规则：根据构建配置中的 TagRules，为当前 BundleInfo 追加标签。
        /// 
        /// 规则逻辑：
        /// 1) 遍历 config.TagRules；
        /// 2) 遍历 bundleInfo.Assets 中的每个资源路径；
        /// 3) 若资源路径位于 rule.FolderPath 目录下（以“路径前缀匹配”的方式判断），则将 rule.Tags 合并到 bundleInfo.Tags；
        /// 4) 合并时会去重，避免重复标签。
        /// 
        /// 注意：
        /// - 本方法会将 rule.FolderPath 中的 "\\" 统一替换为 "/"，以适配 Unity 资源路径常用格式。
        /// - StartsWith 属于前缀匹配，若 FolderPath 未以 "/" 结尾，可能出现“Assets/A”匹配到“Assets/AB”的情况；
        ///   这属于配置规范问题，建议在配置侧确保 FolderPath 是一个标准目录前缀（例如以 "/" 结尾）。
        /// - 本方法有副作用：会直接修改 bundleInfo.Tags。
        /// </summary>
        /// <param name="config">构建配置，包含 TagRules。</param>
        /// <param name="bundleInfo">待应用标签规则的 Bundle 信息（会被原地修改）。</param>
        private static void ApplyTagRules(LWAssetsBuildConfig config, BundleInfo bundleInfo)
        {
            // 遍历所有标签规则，将符合条件的标签合并到当前 Bundle。
            foreach (var rule in config.TagRules)
            {
                // 逐个资源路径检查是否命中该规则的目录范围。
                foreach (var asset in bundleInfo.Assets)
                {
                    // 统一路径分隔符，确保前缀匹配行为与资源路径格式一致。
                    var normalizedFolderPath = rule.FolderPath.Replace("\\", "/");

                    // 使用“目录前缀”判断该资源是否位于规则目录下。
                    // 例如：asset = "Assets/Prefabs/A.prefab"，FolderPath = "Assets/Prefabs/" => 命中。
                    if (asset.StartsWith(normalizedFolderPath))
                    {
                        // 命中规则后，将规则标签合并进 Bundle 标签列表（去重）。
                        foreach (var tag in rule.Tags)
                        {
                            // 避免重复添加同名 Tag。
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
        /// <remarks>
        /// VersionInfo.Version 使用“构建自增号”，每次成功打包后自动 +1，并持久化到项目内 JSON 文件。
        /// </remarks>
        private static void GenerateVersionFile(LWAssetsBuildConfig config,
            BundleManifest manifest, string outputPath)
        {
            var manifestPath = Path.Combine(outputPath, "manifest.json");
            var manifestHash = HashUtility.ComputeFileMD5(manifestPath);
            var manifestSize = new FileInfo(manifestPath).Length;



            var version = new VersionInfo
            {
                Version = manifest.Version,
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
        /// 分配下一次构建的版本号（并记录到 BuildVersions.json 文件中）。
        /// </summary>
        private static int AllocateNextBuildVersion(LWAssetsBuildConfig config)
        {
            string key = $"{config.OutputPath}|{EditorUserBuildSettings.activeBuildTarget}";
            string path = Path.Combine(Application.dataPath, "LWAssetsBuildVersions.json");

            Dictionary<string, int> buildVersions = new Dictionary<string, int>(StringComparer.Ordinal);
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        Dictionary<string, int> loaded = JsonMapper.ToObject<Dictionary<string, int>>(json);
                        if (loaded != null)
                        {
                            buildVersions = loaded;
                        }
                    }
                }
                catch
                {
                    buildVersions = new Dictionary<string, int>(StringComparer.Ordinal);
                }
            }

            int currentVersion;
            buildVersions.TryGetValue(key, out currentVersion);
            int nextVersion = currentVersion + 1;
            buildVersions[key] = nextVersion;

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            SortedDictionary<string, int> orderedBuildVersions = new SortedDictionary<string, int>(buildVersions, StringComparer.Ordinal);
            string outputJson = JsonMapper.ToJson(orderedBuildVersions, true);
            File.WriteAllText(path, outputJson);

            return nextVersion;
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
            report.AppendLine($"Platform: {EditorUserBuildSettings.activeBuildTarget}");
            report.AppendLine($"Version: {manifest.Version}");
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
            var platform = EditorUserBuildSettings.activeBuildTarget switch
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
