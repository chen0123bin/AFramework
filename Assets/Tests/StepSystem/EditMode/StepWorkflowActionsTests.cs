using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using LWCore;
using LWStep;
using LWStep.Editor.Metadata;
using NUnit.Framework;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// 流程控制与上下文动作测试。
    /// </summary>
    public sealed class StepWorkflowActionsTests
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
        /// Apply 应将 value 解析为基础类型并写入上下文。
        /// </summary>
        [Test]
        public void StepSetContextValueAction_Apply_ShouldWriteParsedValue()
        {
            StepContext context = new StepContext();
            StepSetContextValueAction action = new StepSetContextValueAction();
            action.SetContext(context);
            action.SetParameters(new Dictionary<string, string>
            {
                { "key", "score" },
                { "value", "5" }
            });

            action.Apply();

            Assert.AreEqual(5, context.GetValue("score", 0));
        }

        /// <summary>
        /// Apply 应删除指定上下文键。
        /// </summary>
        [Test]
        public void StepRemoveContextValueAction_Apply_ShouldDeleteKey()
        {
            StepContext context = new StepContext();
            context.SetValue("mode", "A");
            StepRemoveContextValueAction action = new StepRemoveContextValueAction();
            action.SetContext(context);
            action.SetParameters(new Dictionary<string, string>
            {
                { "key", "mode" }
            });

            action.Apply();

            Assert.IsFalse(context.HasKey("mode"));
        }

        /// <summary>
        /// 等待动作在达到时长后应结束。
        /// </summary>
        [Test]
        public void StepWaitSecondsAction_Update_ShouldFinishAfterDuration()
        {
            StepWaitSecondsAction action = new StepWaitSecondsAction();
            action.SetParameters(new Dictionary<string, string>
            {
                { "seconds", "0.02" }
            });

            action.Enter();
            Thread.Sleep(40);
            action.Update();

            Assert.IsTrue(action.IsFinished);
        }

        /// <summary>
        /// Apply 应通过 EventMgr 派发指定事件名。
        /// </summary>
        [Test]
        public void StepDispatchEventAction_Apply_ShouldDispatchConfiguredEvent()
        {
            FakeEventManager fakeEventManager = new FakeEventManager();
            MainManager.Instance.AddManager(typeof(IEventManager).ToString(), fakeEventManager);
            StepDispatchEventAction action = new StepDispatchEventAction();
            action.SetParameters(new Dictionary<string, string>
            {
                { "eventName", "QuestCompleted" }
            });

            action.Apply();

            Assert.AreEqual(2, fakeEventManager.DispatchCount);
            Assert.AreEqual("QuestCompleted", fakeEventManager.LastDispatchedEventName);
        }

        /// <summary>
        /// 等待秒数动作应暴露正确的展示与参数元数据。
        /// </summary>
        [Test]
        public void StepWaitSecondsAction_Metadata_ShouldMatchSpec()
        {
            StepActionDescriptor descriptor = StepActionDescriptorRegistry.GetDescriptor(typeof(StepWaitSecondsAction));

            Assert.AreEqual("等待秒数", descriptor.DisplayName);
            Assert.AreEqual("流程控制", descriptor.Category);
            Assert.AreEqual("Wait:{seconds}s", descriptor.SummaryTemplate);
            Assert.AreEqual("seconds", descriptor.Parameters[0].Key);
            Assert.AreEqual("等待秒数", descriptor.Parameters[0].Label);
            Assert.AreEqual(0, descriptor.Parameters[0].Order);
        }

        /// <summary>
        /// 写入上下文动作应暴露正确的展示与参数元数据。
        /// </summary>
        [Test]
        public void StepSetContextValueAction_Metadata_ShouldMatchSpec()
        {
            StepActionDescriptor descriptor = StepActionDescriptorRegistry.GetDescriptor(typeof(StepSetContextValueAction));

            Assert.AreEqual("写入上下文", descriptor.DisplayName);
            Assert.AreEqual("上下文", descriptor.Category);
            Assert.AreEqual("Set:{key}", descriptor.SummaryTemplate);
            Assert.AreEqual("key", descriptor.Parameters[0].Key);
            Assert.AreEqual("键", descriptor.Parameters[0].Label);
            Assert.AreEqual(0, descriptor.Parameters[0].Order);
        }

        /// <summary>
        /// 移除上下文动作应暴露正确的展示与参数元数据。
        /// </summary>
        [Test]
        public void StepRemoveContextValueAction_Metadata_ShouldMatchSpec()
        {
            StepActionDescriptor descriptor = StepActionDescriptorRegistry.GetDescriptor(typeof(StepRemoveContextValueAction));

            Assert.AreEqual("移除上下文", descriptor.DisplayName);
            Assert.AreEqual("上下文", descriptor.Category);
            Assert.AreEqual("Remove:{key}", descriptor.SummaryTemplate);
            Assert.AreEqual("key", descriptor.Parameters[0].Key);
            Assert.AreEqual("键", descriptor.Parameters[0].Label);
            Assert.AreEqual(0, descriptor.Parameters[0].Order);
        }

        /// <summary>
        /// 派发事件动作应暴露正确的展示与参数元数据。
        /// </summary>
        [Test]
        public void StepDispatchEventAction_Metadata_ShouldMatchSpec()
        {
            StepActionDescriptor descriptor = StepActionDescriptorRegistry.GetDescriptor(typeof(StepDispatchEventAction));

            Assert.AreEqual("派发事件", descriptor.DisplayName);
            Assert.AreEqual("流程控制", descriptor.Category);
            Assert.AreEqual("Event:{eventName}", descriptor.SummaryTemplate);
            Assert.AreEqual("eventName", descriptor.Parameters[0].Key);
            Assert.AreEqual("事件名", descriptor.Parameters[0].Label);
            Assert.AreEqual(0, descriptor.Parameters[0].Order);
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

        /// <summary>
        /// 事件管理器测试替身：记录 Dispatch 调用信息。
        /// </summary>
        private sealed class FakeEventManager : IEventManager, IManager
        {
            public int DispatchCount { get; private set; }
            public string LastDispatchedEventName { get; private set; }

            /// <summary>
            /// 添加无参监听。
            /// </summary>
            public void AddListener(string eventName, System.Action callback)
            {
            }

            /// <summary>
            /// 添加单参监听。
            /// </summary>
            public void AddListener<T>(string eventName, System.Action<T> callback)
            {
            }

            /// <summary>
            /// 添加双参监听。
            /// </summary>
            public void AddListener<T1, T2>(string eventName, System.Action<T1, T2> callback)
            {
            }

            /// <summary>
            /// 添加三参监听。
            /// </summary>
            public void AddListener<T1, T2, T3>(string eventName, System.Action<T1, T2, T3> callback)
            {
            }

            /// <summary>
            /// 添加四参监听。
            /// </summary>
            public void AddListener<T1, T2, T3, T4>(string eventName, System.Action<T1, T2, T3, T4> callback)
            {
            }

            /// <summary>
            /// 移除无参监听。
            /// </summary>
            public void RemoveListener(string eventName, System.Action callback)
            {
            }

            /// <summary>
            /// 移除单参监听。
            /// </summary>
            public void RemoveListener<T>(string eventName, System.Action<T> callback)
            {
            }

            /// <summary>
            /// 移除双参监听。
            /// </summary>
            public void RemoveListener<T1, T2>(string eventName, System.Action<T1, T2> callback)
            {
            }

            /// <summary>
            /// 移除三参监听。
            /// </summary>
            public void RemoveListener<T1, T2, T3>(string eventName, System.Action<T1, T2, T3> callback)
            {
            }

            /// <summary>
            /// 移除四参监听。
            /// </summary>
            public void RemoveListener<T1, T2, T3, T4>(string eventName, System.Action<T1, T2, T3, T4> callback)
            {
            }

            /// <summary>
            /// 派发无参事件并记录调用信息。
            /// </summary>
            public void DispatchEvent(string eventName)
            {
                DispatchCount += 1;
                LastDispatchedEventName = eventName;
            }

            /// <summary>
            /// 派发单参事件。
            /// </summary>
            public void DispatchEvent<T>(string eventName, T info)
            {
            }

            /// <summary>
            /// 派发双参事件。
            /// </summary>
            public void DispatchEvent<T1, T2>(string eventName, T1 info1, T2 info2)
            {
            }

            /// <summary>
            /// 派发三参事件。
            /// </summary>
            public void DispatchEvent<T1, T2, T3>(string eventName, T1 info1, T2 info2, T3 info3)
            {
            }

            /// <summary>
            /// 派发四参事件。
            /// </summary>
            public void DispatchEvent<T1, T2, T3, T4>(string eventName, T1 info1, T2 info2, T3 info3, T4 info4)
            {
            }

            /// <summary>
            /// 清空事件中心。
            /// </summary>
            public void Clear()
            {
            }

            /// <summary>
            /// 初始化管理器。
            /// </summary>
            public void Init()
            {
            }

            /// <summary>
            /// 管理器帧更新。
            /// </summary>
            public void Update()
            {
            }
        }
    }
}
