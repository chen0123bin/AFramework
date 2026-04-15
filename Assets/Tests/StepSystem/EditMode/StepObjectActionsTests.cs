using System.Security;
using System.Collections.Generic;
using LWStep;
using LWStep.Editor.Metadata;
using NUnit.Framework;
using UnityEngine;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// 对象控制与实例化动作测试。
    /// </summary>
    public sealed class StepObjectActionsTests
    {
        private static bool? s_CanUseUnityObjects;

        /// <summary>
        /// 每条用例执行后清理临时对象，避免污染其他测试。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (!CanUseUnityObjects())
            {
                return;
            }

            DestroyIfExists("StepTarget_SetActive");
            DestroyIfExists("StepTarget_SetPosition");
            DestroyIfExists("StepTarget_Destroy");
        }

        /// <summary>
        /// Apply 应修改对象激活状态。
        /// </summary>
        [Test]
        public void StepSetActiveAction_Apply_ShouldChangeActiveState()
        {
            RequireUnityObjectAccess();
            GameObject target = new GameObject("StepTarget_SetActive");
            StepSetActiveAction action = new StepSetActiveAction();
            action.SetParameters(new Dictionary<string, string>
            {
                { "target", target.name },
                { "active", "false" }
            });

            action.Apply();

            Assert.IsFalse(target.activeSelf);
        }

        /// <summary>
        /// Apply 应写入本地位置。
        /// </summary>
        [Test]
        public void StepSetPositionAction_Apply_ShouldWriteTransformPosition()
        {
            RequireUnityObjectAccess();
            GameObject target = new GameObject("StepTarget_SetPosition");
            StepSetPositionAction action = new StepSetPositionAction();
            action.SetParameters(new Dictionary<string, string>
            {
                { "target", target.name },
                { "x", "1" },
                { "y", "2" },
                { "z", "3" },
                { "isLocal", "true" }
            });

            action.Apply();

            Assert.AreEqual(new Vector3(1f, 2f, 3f), target.transform.localPosition);
        }

        /// <summary>
        /// Apply 在编辑器中应立即销毁对象。
        /// </summary>
        [Test]
        public void StepDestroyTargetAction_Apply_ShouldDestroyTargetImmediatelyInEditor()
        {
            RequireUnityObjectAccess();
            GameObject target = new GameObject("StepTarget_Destroy");
            StepDestroyTargetAction action = new StepDestroyTargetAction();
            action.SetParameters(new Dictionary<string, string>
            {
                { "target", target.name }
            });

            action.Apply();

            Assert.IsNull(GameObject.Find("StepTarget_Destroy"));
        }

        /// <summary>
        /// 设置激活动作应暴露正确的展示与参数元数据。
        /// </summary>
        [Test]
        public void StepSetActiveAction_Metadata_ShouldMatchSpec()
        {
            StepActionDescriptor descriptor = StepActionDescriptorRegistry.GetDescriptor(typeof(StepSetActiveAction));

            Assert.AreEqual("设置激活状态", descriptor.DisplayName);
            Assert.AreEqual("对象控制", descriptor.Category);
            Assert.AreEqual("Active:{target}", descriptor.SummaryTemplate);
            Assert.AreEqual("active", descriptor.Parameters[0].Key);
            Assert.AreEqual("是否激活", descriptor.Parameters[0].Label);
            Assert.AreEqual(1, descriptor.Parameters[0].Order);
        }

        /// <summary>
        /// 实例化预制体动作应暴露正确的展示与参数元数据。
        /// </summary>
        [Test]
        public void StepInstantiatePrefabAction_Metadata_ShouldMatchSpec()
        {
            StepActionDescriptor descriptor = StepActionDescriptorRegistry.GetDescriptor(typeof(StepInstantiatePrefabAction));

            Assert.AreEqual("实例化预制体", descriptor.DisplayName);
            Assert.AreEqual("对象控制", descriptor.Category);
            Assert.AreEqual("Spawn:{prefab}", descriptor.SummaryTemplate);
            Assert.AreEqual("prefab", descriptor.Parameters[0].Key);
            Assert.AreEqual("预制体", descriptor.Parameters[0].Label);
            Assert.AreEqual(0, descriptor.Parameters[0].Order);
        }

        /// <summary>
        /// 播放粒子动作应暴露正确的展示与参数元数据。
        /// </summary>
        [Test]
        public void StepPlayParticleAction_Metadata_ShouldMatchSpec()
        {
            StepActionDescriptor descriptor = StepActionDescriptorRegistry.GetDescriptor(typeof(StepPlayParticleAction));

            Assert.AreEqual("播放粒子", descriptor.DisplayName);
            Assert.AreEqual("动画与特效", descriptor.Category);
            Assert.AreEqual("Particle:{target}", descriptor.SummaryTemplate);
            Assert.AreEqual("waitForFinish", descriptor.Parameters[0].Key);
            Assert.AreEqual("等待播放结束", descriptor.Parameters[0].Label);
            Assert.AreEqual(1, descriptor.Parameters[0].Order);
        }

        /// <summary>
        /// 若对象存在则立即销毁，避免测试残留。
        /// </summary>
        private static void DestroyIfExists(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            if (target == null)
            {
                return;
            }

            Object.DestroyImmediate(target);
        }

        /// <summary>
        /// 在当前测试宿主内探测是否可安全创建 Unity 对象。
        /// </summary>
        private static bool CanUseUnityObjects()
        {
            if (s_CanUseUnityObjects.HasValue)
            {
                return s_CanUseUnityObjects.Value;
            }

            try
            {
                GameObject probe = new GameObject("StepObjectActions_RuntimeProbe");
                Object.DestroyImmediate(probe);
                s_CanUseUnityObjects = true;
            }
            catch (SecurityException)
            {
                s_CanUseUnityObjects = false;
            }

            return s_CanUseUnityObjects.Value;
        }

        /// <summary>
        /// 当测试宿主不支持 Unity 对象内部调用时跳过对象级行为断言。
        /// </summary>
        private static void RequireUnityObjectAccess()
        {
            if (!CanUseUnityObjects())
            {
                Assert.Ignore("当前 dotnet test 宿主不支持 UnityEngine.GameObject 内部调用，跳过对象级行为断言。");
            }
        }
    }
}
