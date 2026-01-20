using System;
using System.Collections.Generic;
using System.Reflection;

namespace LWStep
{
    /// <summary>
    /// 步骤动作工厂（支持反射创建）
    /// </summary>
    public class StepActionFactory
    {
        private Dictionary<string, Func<BaseStepAction>> m_ActionCreators;

        /// <summary>
        /// 创建工厂
        /// </summary>
        public StepActionFactory()
        {
            m_ActionCreators = new Dictionary<string, Func<BaseStepAction>>();
        }

        /// <summary>
        /// 创建动作
        /// </summary>
        public BaseStepAction CreateAction(string typeName)
        {
            Func<BaseStepAction> creator;
            if (m_ActionCreators.TryGetValue(typeName, out creator))
            {
                return creator();
            }

            Type type = FindActionType(typeName);
            if (type == null)
            {
                LWDebug.LogError("步骤动作类型未找到: " + typeName);
                return null;
            }
            if (!typeof(BaseStepAction).IsAssignableFrom(type))
            {
                LWDebug.LogError("步骤动作类型非法: " + typeName);
                return null;
            }
            BaseStepAction action = Activator.CreateInstance(type) as BaseStepAction;
            return action;
        }

        private Type FindActionType(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type[] types;
                try
                {
                    types = assemblies[i].GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                if (types == null)
                {
                    continue;
                }

                for (int j = 0; j < types.Length; j++)
                {
                    Type currentType = types[j];
                    if (currentType == null)
                    {
                        continue;
                    }
                    if (currentType.FullName == typeName)
                    {
                        return currentType;
                    }
                }
            }
            return null;
        }
    }
}
