using System;
using System.IO;
using System.Reflection;
using Cysharp.Threading.Tasks;
using LWAssets;
using LWCore;
using UnityEngine;

namespace LWHotfix
{
    /// <summary>
    /// 反射模式热更管理器。
    /// </summary>
    public class HotFixRefManager : HotFixBaseManager, IManager
    {
        private const string RAW_FILE_ROOT = "Assets/0Res/RawFiles";

        /// <summary>
        /// 初始化反射热更管理器。
        /// </summary>
        public override void Init()
        {
        }

        /// <summary>
        /// 反射模式下每帧更新入口，当前无需额外逻辑。
        /// </summary>
        public override void Update()
        {
        }

        /// <summary>
        /// 按程序集名称加载反射热更 DLL。
        /// </summary>
        /// <param name="hotfixDllName">程序集名称，可带或不带 .dll 后缀。</param>
        /// <param name="dir">RawFile 下的相对子目录。</param>
        public override async UniTask LoadScriptAsync(string hotfixDllName, string dir = "Hotfix/")
        {
            if (string.IsNullOrEmpty(hotfixDllName))
            {
                LWDebug.LogError("反射模式加载失败，程序集名称为空");
                return;
            }

            string assemblyName = NormalizeAssemblyName(hotfixDllName);
            if (HasLoadedAssembly(assemblyName))
            {
                Debug.LogWarning("内存中已经加载了" + assemblyName);
                return;
            }

            await LoadHotFixDll(assemblyName, dir);
        }

        /// <summary>
        /// 从 LWAssets RawFile 链路中加载 DLL 与可选的 PDB 符号文件。
        /// </summary>
        /// <param name="hotfixName">程序集名称（不带 .dll 后缀）。</param>
        /// <param name="dir">RawFile 下的相对子目录。</param>
        private async UniTask LoadHotFixDll(string hotfixName, string dir = "Hotfix/")
        {
            IAssetsManager assetsManager = ManagerUtility.AssetsMgr;
            if (assetsManager == null)
            {
                LWDebug.LogError("反射模式加载失败，IAssetsManager 不存在");
                return;
            }

            if (!assetsManager.IsInitialized)
            {
                LWDebug.LogError("反射模式加载失败，资源管理器尚未初始化");
                return;
            }

            string dllAssetPath = BuildHotfixAssetPath(dir, hotfixName, ".dll.bytes");
            byte[] dllBytes = await assetsManager.LoadRawFileAsync(dllAssetPath);
            if (dllBytes == null || dllBytes.Length <= 0)
            {
                LWDebug.LogError("反射模式未找到热更 DLL: " + dllAssetPath);
                return;
            }

            byte[] pdbBytes = await LoadSymbolBytesAsync(assetsManager, dir, hotfixName);

            Assembly assembly;
            try
            {
                assembly = pdbBytes != null && pdbBytes.Length > 0
                    ? Assembly.Load(dllBytes, pdbBytes)
                    : Assembly.Load(dllBytes);
            }
            catch (Exception ex)
            {
                LWDebug.LogError("反射模式装配程序集失败: " + hotfixName + "，原因：" + ex.Message);
                return;
            }

            OnHotFixLoaded(assembly);
        }

        /// <summary>
        /// 尝试加载可选的 PDB 符号文件。
        /// </summary>
        /// <param name="assetsManager">资源管理器。</param>
        /// <param name="dir">RawFile 下的相对子目录。</param>
        /// <param name="hotfixName">程序集名称。</param>
        /// <returns>符号文件字节，不存在时返回 null。</returns>
        private async UniTask<byte[]> LoadSymbolBytesAsync(IAssetsManager assetsManager, string dir, string hotfixName)
        {
            string pdbAssetPath = BuildHotfixAssetPath(dir, hotfixName, ".pdb.bytes");

            if (assetsManager.CurrentPlayMode == LWAssets.PlayMode.EditorSimulate)
            {
                string rootPath = Application.dataPath.Replace("Assets", string.Empty);
                string fullPath = Path.Combine(rootPath, pdbAssetPath);
                if (!File.Exists(fullPath))
                {
                    return null;
                }
            }
            else
            {
                BundleManifest manifest = await assetsManager.LoadManifestAsync();
                if (manifest == null || !manifest.ContainsAsset(pdbAssetPath))
                {
                    return null;
                }
            }

            return await assetsManager.LoadRawFileAsync(pdbAssetPath);
        }

        /// <summary>
        /// 归一化程序集名称，去掉末尾扩展名。
        /// </summary>
        /// <param name="hotfixDllName">原始程序集名称。</param>
        /// <returns>归一化后的程序集名称。</returns>
        private static string NormalizeAssemblyName(string hotfixDllName)
        {
            string assemblyName = hotfixDllName.Trim();
            if (assemblyName.EndsWith(".dll.bytes", StringComparison.OrdinalIgnoreCase))
            {
                return assemblyName.Substring(0, assemblyName.Length - ".dll.bytes".Length);
            }

            if (assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return assemblyName.Substring(0, assemblyName.Length - ".dll".Length);
            }

            return assemblyName;
        }

        /// <summary>
        /// 拼装 Hotfix RawFile 资源路径。
        /// </summary>
        /// <param name="dir">RawFile 下的相对子目录。</param>
        /// <param name="assemblyName">程序集名称。</param>
        /// <param name="suffix">文件后缀。</param>
        /// <returns>规范化后的资源路径。</returns>
        private static string BuildHotfixAssetPath(string dir, string assemblyName, string suffix)
        {
            string normalizedDir = string.IsNullOrEmpty(dir) ? string.Empty : dir.Replace("\\", "/").Trim('/');
            if (string.IsNullOrEmpty(normalizedDir))
            {
                return string.Concat(RAW_FILE_ROOT, "/", assemblyName, suffix);
            }

            return string.Concat(RAW_FILE_ROOT, "/", normalizedDir, "/", assemblyName, suffix);
        }

        /// <summary>
        /// 销毁反射热更缓存。
        /// </summary>
        public override void Destroy()
        {
            base.Destroy();
        }
    }
}
