using System;
using LWCore;
using NUnit.Framework;

namespace LWFramework.Tests.Framework.EditMode
{
    /// <summary>
    /// FrameworkBootstrapSettings 的默认配置与模块状态验证。
    /// </summary>
    public sealed class FrameworkBootstrapSettingsTests
    {
        /// <summary>
        /// 验证默认配置只启用核心模块，Audio 与 StepSystem 保持关闭。
        /// </summary>
        [Test]
        public void DefaultSettings_OnlyCoreModulesEnabled()
        {
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();

            Assert.IsTrue(settings.IsModuleEnabled(FrameworkModuleId.Assets));
            Assert.IsTrue(settings.IsModuleEnabled(FrameworkModuleId.Event));
            Assert.IsTrue(settings.IsModuleEnabled(FrameworkModuleId.UI));
            Assert.IsTrue(settings.IsModuleEnabled(FrameworkModuleId.Hotfix));
            Assert.IsTrue(settings.IsModuleEnabled(FrameworkModuleId.FSM));
            Assert.IsFalse(settings.IsModuleEnabled(FrameworkModuleId.Audio));
            Assert.IsFalse(settings.IsModuleEnabled(FrameworkModuleId.StepSystem));
        }

        /// <summary>
        /// 验证默认启动配置字段与常量值一致。
        /// </summary>
        [Test]
        public void DefaultSettings_MatchesDefaultValues()
        {
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();

            Assert.AreEqual(FrameworkBootstrapSettings.DEFAULT_PROCEDURE_NAME, settings.ProcedureName);
            Assert.AreEqual(HotfixCodeRunMode.ByCode, settings.HotfixMode);
            Assert.AreEqual(string.Empty, settings.ReflectionHotfixAssemblyName);
            Assert.AreEqual(FrameworkBootstrapSettings.DEFAULT_REFLECTION_HOTFIX_DIR, "Hotfix/");
        }

        /// <summary>
        /// 验证未知模块的状态查询会抛出越界异常。
        /// </summary>
        [Test]
        public void DefaultSettings_UnknownModuleThrows()
        {
            FrameworkBootstrapSettings settings = FrameworkBootstrapSettings.CreateDefault();

            Assert.Throws<ArgumentOutOfRangeException>(() => settings.IsModuleEnabled((FrameworkModuleId)999));
        }
    }
}
