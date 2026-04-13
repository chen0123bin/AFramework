using System.Reflection;
using LWCore;
using LWStep;
using NUnit.Framework;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// StepManagerUtility 访问入口测试。
    /// </summary>
    public sealed class StepManagerUtilityTests
    {
        /// <summary>
        /// 每条用例执行前重置 MainManager 单例，避免跨用例污染。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            ResetMainManagerSingleton();
        }

        /// <summary>
        /// 每条用例执行后重置 MainManager 单例，避免影响其他测试。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            ResetMainManagerSingleton();
        }

        /// <summary>
        /// 未注册步骤管理器时，TryGetStepMgr 应返回 false 且输出 null。
        /// </summary>
        [Test]
        public void TryGetStepMgr_WithoutRegistration_ShouldReturnFalse()
        {
            IStepManager stepManager;

            bool isFound = StepManagerUtility.TryGetStepMgr(out stepManager);

            Assert.IsFalse(isFound);
            Assert.IsNull(stepManager);
        }

        /// <summary>
        /// 已注册步骤管理器时，TryGetStepMgr 应返回已注册实例。
        /// </summary>
        [Test]
        public void TryGetStepMgr_WithRegistration_ShouldReturnRegisteredInstance()
        {
            StepManager registeredStepManager = new StepManager();
            MainManager.Instance.AddManager(typeof(IStepManager).ToString(), registeredStepManager);

            IStepManager stepManager;
            bool isFound = StepManagerUtility.TryGetStepMgr(out stepManager);

            Assert.IsTrue(isFound);
            Assert.AreSame(registeredStepManager, stepManager);
            Assert.AreSame(registeredStepManager, StepManagerUtility.StepMgr);
        }

        /// <summary>
        /// 通过反射重置 MainManager 的单例实例，并清理已注册管理器。
        /// </summary>
        private static void ResetMainManagerSingleton()
        {
            FieldInfo singletonField = typeof(Singleton<MainManager>).GetField("m_Instance", BindingFlags.NonPublic | BindingFlags.Static);
            if (singletonField == null)
            {
                return;
            }

            MainManager mainManager = singletonField.GetValue(null) as MainManager;
            if (mainManager != null)
            {
                mainManager.ClearManager();
            }

            singletonField.SetValue(null, null);
        }
    }
}
