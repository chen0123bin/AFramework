using System;

namespace LWCore
{
    public sealed class FrameworkBootstrapSettings
    {
        public const string DEFAULT_PROCEDURE_NAME = "StartProcedure";
        public const string DEFAULT_REFLECTION_HOTFIX_DIR = "Hotfix/";

        public string ProcedureName = DEFAULT_PROCEDURE_NAME;
        public HotfixCodeRunMode HotfixMode = HotfixCodeRunMode.ByCode;
        public string ReflectionHotfixAssemblyName = string.Empty;

        public bool EnableAssets = true;
        public bool EnableEvent = true;
        public bool EnableUI = true;
        public bool EnableHotfix = true;
        public bool EnableFSM = true;
        public bool EnableAudio = false;
        public bool EnableStepSystem = false;

        /// <summary>
        /// 创建一份带有默认启动配置的实例。
        /// </summary>
        public static FrameworkBootstrapSettings CreateDefault()
        {
            return new FrameworkBootstrapSettings();
        }

        /// <summary>
        /// 判断指定模块是否经过配置启用。
        /// </summary>
        public bool IsModuleEnabled(FrameworkModuleId moduleId)
        {
            switch (moduleId)
            {
                case FrameworkModuleId.Assets:
                    return EnableAssets;
                case FrameworkModuleId.Event:
                    return EnableEvent;
                case FrameworkModuleId.UI:
                    return EnableUI;
                case FrameworkModuleId.Hotfix:
                    return EnableHotfix;
                case FrameworkModuleId.FSM:
                    return EnableFSM;
                case FrameworkModuleId.Audio:
                    return EnableAudio;
                case FrameworkModuleId.StepSystem:
                    return EnableStepSystem;
                default:
                throw new ArgumentOutOfRangeException(nameof(moduleId), moduleId, "不支持的模块");
            }
        }
    }
}
