using LWCore;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LWStep
{
    [StepActionInfo("实例化预制体", Category = "对象控制", SummaryTemplate = "Spawn:{prefab}")]
    public class StepInstantiatePrefabAction : BaseStepAction
    {
        [StepParam("prefab", label: "预制体", order: 0)]
        private string m_PrefabPath;

        [StepParam("instanceName", label: "实例名称", order: 1)]
        private string m_InstanceName;

        [StepParam("parent", label: "父节点", order: 2)]
        private string m_ParentName;

        private GameObject m_Instance;

        /// <summary>
        /// 进入动作时加载并实例化预制体，然后立即完成。
        /// </summary>
        protected override void OnEnter()
        {
            InstantiatePrefab();
            Finish();
        }

        /// <summary>
        /// 更新动作：该动作为瞬时动作，无需额外更新。
        /// </summary>
        protected override void OnUpdate()
        {
        }

        /// <summary>
        /// 退出动作时清理运行期缓存引用。
        /// </summary>
        protected override void OnExit()
        {
            m_Instance = null;
        }

        /// <summary>
        /// 快速应用时加载并实例化预制体。
        /// </summary>
        protected override void OnApply()
        {
            InstantiatePrefab();
        }

        /// <summary>
        /// 根据参数加载预制体并实例化到指定父节点下。
        /// </summary>
        private void InstantiatePrefab()
        {
            if (m_Instance != null)
            {
                return;
            }

            if (string.IsNullOrEmpty(m_PrefabPath))
            {
                LWDebug.LogWarning("步骤动作-实例化预制体：prefab 为空");
                return;
            }

            Transform parentTransform = ResolveParentTransform();
            GameObject instance = InstantiateWithAssetsManager(parentTransform);
            if (instance == null)
            {
                instance = InstantiateWithFallback(parentTransform);
            }

            if (instance == null)
            {
                LWDebug.LogWarning("步骤动作-实例化预制体：实例化失败 " + m_PrefabPath);
                return;
            }

            if (!string.IsNullOrEmpty(m_InstanceName))
            {
                instance.name = m_InstanceName;
            }

            m_Instance = instance;
        }

        /// <summary>
        /// 优先通过框架资源管理器实例化预制体。
        /// </summary>
        private GameObject InstantiateWithAssetsManager(Transform parentTransform)
        {
            if (ManagerUtility.AssetsMgr == null)
            {
                return null;
            }

            return ManagerUtility.AssetsMgr.Instantiate(m_PrefabPath, parentTransform);
        }

        /// <summary>
        /// 在资源管理器不可用时使用编辑器或 Resources 兜底加载。
        /// </summary>
        private GameObject InstantiateWithFallback(Transform parentTransform)
        {
#if UNITY_EDITOR
            if (m_PrefabPath.StartsWith("Assets/"))
            {
                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPath);
                if (prefabAsset != null)
                {
                    return Object.Instantiate(prefabAsset, parentTransform);
                }
            }
#endif

            string resourcePath = NormalizeResourcesPath(m_PrefabPath);
            if (string.IsNullOrEmpty(resourcePath))
            {
                return null;
            }

            GameObject resourcePrefab = Resources.Load<GameObject>(resourcePath);
            if (resourcePrefab == null)
            {
                return null;
            }

            return Object.Instantiate(resourcePrefab, parentTransform);
        }

        /// <summary>
        /// 解析父节点名称对应的 Transform；名称为空时返回空表示挂到根节点。
        /// </summary>
        private Transform ResolveParentTransform()
        {
            if (string.IsNullOrEmpty(m_ParentName))
            {
                return null;
            }

            GameObject parentObject = GameObject.Find(m_ParentName);
            if (parentObject == null)
            {
                LWDebug.LogWarning("步骤动作-实例化预制体：未找到父对象 " + m_ParentName);
                return null;
            }

            return parentObject.transform;
        }

        /// <summary>
        /// 将可能的资源路径标准化为 Resources.Load 可识别的路径。
        /// </summary>
        private static string NormalizeResourcesPath(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath))
            {
                return string.Empty;
            }

            string normalizedPath = prefabPath.Replace('\\', '/');
            const string resourcesMarker = "/Resources/";
            int resourcesIndex = normalizedPath.IndexOf(resourcesMarker);
            if (resourcesIndex >= 0)
            {
                normalizedPath = normalizedPath.Substring(resourcesIndex + resourcesMarker.Length);
            }

            if (normalizedPath.EndsWith(".prefab"))
            {
                normalizedPath = normalizedPath.Substring(0, normalizedPath.Length - ".prefab".Length);
            }

            if (normalizedPath.StartsWith("Resources/"))
            {
                normalizedPath = normalizedPath.Substring("Resources/".Length);
            }

            return normalizedPath;
        }
    }
}
