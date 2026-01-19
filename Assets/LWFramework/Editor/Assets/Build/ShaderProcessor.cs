// Editor/Build/ShaderProcessor.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWAssets.Editor
{
    /// <summary>
    /// Shader处理器
    /// </summary>
    public static class ShaderProcessor
    {
        /// <summary>
        /// 收集项目中使用的所有Shader变体
        /// </summary>
        [MenuItem("LWFramework/Assets/Build/Shader/Collect Shader Variants")]
        public static void CollectShaderVariants()
        {
            var svc = new ShaderVariantCollection();
            var materials = CollectAllMaterials();

            foreach (var material in materials)
            {
                if (material.shader == null) continue;

                try
                {
                    // 获取Material使用的PassType
                    var passTypes = GetMaterialPassTypes(material);

                    foreach (var passType in passTypes)
                    {
                        var keywords = material.shaderKeywords;
                        var variant = new ShaderVariantCollection.ShaderVariant(
                            material.shader, passType, keywords);

                        if (!svc.Contains(variant))
                        {
                            svc.Add(variant);
                        }
                    }
                }
                catch
                {
                    // 某些Shader可能不支持
                }
            }

            // 保存
            var path = "Assets/Arts/Shaders/ShaderVariants.shadervariants";
            AssetDatabase.CreateAsset(svc, path);
            AssetDatabase.SaveAssets();

            Debug.Log($"[LWAssets] Collected {svc.variantCount} shader variants to {path}");
        }

        /// <summary>
        /// 获取所有Material
        /// </summary>
        private static List<Material> CollectAllMaterials()
        {
            var materials = new List<Material>();

            // 从Prefab中收集
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    materials.AddRange(renderer.sharedMaterials.Where(m => m != null));
                }
            }

            // 从Material资源中收集
            var materialGuids = AssetDatabase.FindAssets("t:Material");
            foreach (var guid in materialGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/")) continue;

                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null)
                {
                    materials.Add(material);
                }
            }

            return materials.Distinct().ToList();
        }

        /// <summary>
        /// 获取Material的PassType
        /// </summary>
        private static List<PassType> GetMaterialPassTypes(Material material)
        {
            var passTypes = new List<PassType>();

            // 常见的PassType
            passTypes.Add(PassType.Normal);
            passTypes.Add(PassType.ForwardBase);
            passTypes.Add(PassType.ForwardAdd);
            passTypes.Add(PassType.ShadowCaster);

            // URP特有的Pass
            passTypes.Add(PassType.ScriptableRenderPipeline);
            passTypes.Add(PassType.ScriptableRenderPipelineDefaultUnlit);

            return passTypes;
        }

        /// <summary>
        /// 分析Shader使用情况
        /// </summary>
        [MenuItem("LWFramework/Assets/Build/Shader/Analyze Shader Usage")]
        public static void AnalyzeShaderUsage()
        {
            var shaderUsage = new Dictionary<Shader, int>();
            var materials = CollectAllMaterials();

            foreach (var material in materials)
            {
                if (material.shader == null) continue;

                if (!shaderUsage.ContainsKey(material.shader))
                {
                    shaderUsage[material.shader] = 0;
                }
                shaderUsage[material.shader]++;
            }

            // 输出报告
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== Shader Usage Report ===\n");

            foreach (var kvp in shaderUsage.OrderByDescending(x => x.Value))
            {
                var shaderPath = AssetDatabase.GetAssetPath(kvp.Key);
                var isBuiltin = string.IsNullOrEmpty(shaderPath) || shaderPath.StartsWith("Resources/");

                report.AppendLine($"{kvp.Key.name}");
                report.AppendLine($"  Usage Count: {kvp.Value}");
                report.AppendLine($"  Type: {(isBuiltin ? "Built-in" : "Custom")}");
                if (!isBuiltin)
                {
                    report.AppendLine($"  Path: {shaderPath}");
                }
                report.AppendLine();
            }

            var reportPath = "Assets/Arts/Shaders/shader_usage_report.txt";
            File.WriteAllText(reportPath, report.ToString());
            AssetDatabase.Refresh();

            Debug.Log($"[LWAssets] Shader usage report saved to {reportPath}");
        }

        /// <summary>
        /// 检查Shader兼容性
        /// </summary>
        [MenuItem("LWFramework/Assets/Build/Shader/Check Shader Compatibility")]
        public static void CheckShaderCompatibility()
        {
            var issues = new List<string>();

            var shaderGuids = AssetDatabase.FindAssets("t:Shader");
            foreach (var guid in shaderGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/")) continue;

                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader == null) continue;

                // 检查是否支持目标平台
                if (!shader.isSupported)
                {
                    issues.Add($"[Unsupported] {path}");
                }

                // 检查编译错误
                var shaderContent = File.ReadAllText(path);
                if (shaderContent.Contains("#error"))
                {
                    issues.Add($"[Has #error] {path}");
                }
            }

            if (issues.Count > 0)
            {
                Debug.LogWarning($"[LWAssets] Found {issues.Count} shader issues:\n" +
                    string.Join("\n", issues));
            }
            else
            {
                Debug.Log("[LWAssets] No shader compatibility issues found.");
            }
        }
    }
}
#endif
