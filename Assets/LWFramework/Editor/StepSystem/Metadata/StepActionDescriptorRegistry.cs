using System;
using System.Collections.Generic;
using System.Reflection;
using LWStep.Editor;

namespace LWStep.Editor.Metadata
{
    /// <summary>
    /// 步骤动作描述注册表。
    /// </summary>
    public static class StepActionDescriptorRegistry
    {
        private static readonly Dictionary<Type, StepActionDescriptor> s_DescriptorCache = new Dictionary<Type, StepActionDescriptor>();
        private static readonly Dictionary<string, Type> s_ActionTypeByNameCache = new Dictionary<string, Type>(StringComparer.Ordinal);
        private static readonly object s_Sync = new object();

        /// <summary>
        /// 获取指定动作类型的描述并缓存结果。
        /// </summary>
        public static StepActionDescriptor GetDescriptor(Type actionType)
        {
            if (actionType == null || !typeof(BaseStepAction).IsAssignableFrom(actionType))
            {
                return null;
            }

            lock (s_Sync)
            {
                StepActionDescriptor cachedDescriptor;
                if (s_DescriptorCache.TryGetValue(actionType, out cachedDescriptor))
                {
                    return CloneDescriptor(cachedDescriptor);
                }

                StepActionDescriptor descriptor = BuildDescriptor(actionType);
                s_DescriptorCache[actionType] = descriptor;
                return CloneDescriptor(descriptor);
            }
        }

        /// <summary>
        /// 基于动作类型名与参数数据生成摘要文本。
        /// </summary>
        public static string BuildSummary(string typeName, List<StepEditorParameterData> parameters)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return string.Empty;
            }

            Type actionType = FindActionType(typeName);
            if (actionType == null)
            {
                return typeName;
            }

            StepActionDescriptor descriptor = GetDescriptor(actionType);
            if (descriptor == null)
            {
                return typeName;
            }

            string template = !string.IsNullOrEmpty(descriptor.SummaryTemplate) ? descriptor.SummaryTemplate : descriptor.DisplayName;
            if (string.IsNullOrEmpty(template))
            {
                return typeName;
            }

            List<StepActionParameterDescriptor> parameterDescriptors = descriptor.Parameters;
            if (parameterDescriptors == null || parameterDescriptors.Count == 0)
            {
                return template;
            }

            for (int i = 0; i < parameterDescriptors.Count; i++)
            {
                StepActionParameterDescriptor parameterDescriptor = parameterDescriptors[i];
                if (parameterDescriptor == null || string.IsNullOrEmpty(parameterDescriptor.Key))
                {
                    continue;
                }

                string value = GetParameterValue(parameters, parameterDescriptor.Key);
                template = template.Replace("{" + parameterDescriptor.Key + "}", value);
            }

            return template;
        }

        /// <summary>
        /// 构建动作描述信息。
        /// </summary>
        private static StepActionDescriptor BuildDescriptor(Type actionType)
        {
            StepActionDescriptor descriptor = new StepActionDescriptor();
            descriptor.ActionType = actionType;
            descriptor.TypeName = actionType.FullName ?? actionType.Name;

            StepActionInfoAttribute actionInfo = Attribute.GetCustomAttribute(actionType, typeof(StepActionInfoAttribute), false) as StepActionInfoAttribute;
            descriptor.DisplayName = actionInfo != null && !string.IsNullOrEmpty(actionInfo.DisplayName) ? actionInfo.DisplayName : actionType.Name;
            descriptor.Category = actionInfo != null ? actionInfo.Category ?? string.Empty : string.Empty;
            descriptor.SummaryTemplate = actionInfo != null ? actionInfo.SummaryTemplate ?? string.Empty : string.Empty;
            descriptor.Description = actionInfo != null ? actionInfo.Description ?? string.Empty : string.Empty;
            if (actionInfo != null && actionInfo.Keywords != null)
            {
                for (int i = 0; i < actionInfo.Keywords.Length; i++)
                {
                    string keyword = actionInfo.Keywords[i];
                    if (string.IsNullOrEmpty(keyword))
                    {
                        continue;
                    }
                    descriptor.Keywords.Add(keyword);
                }
            }

            descriptor.Parameters = BuildParameterDescriptors(actionType);
            return descriptor;
        }

        /// <summary>
        /// 构建动作参数描述并按顺序排序。
        /// </summary>
        private static List<StepActionParameterDescriptor> BuildParameterDescriptors(Type actionType)
        {
            List<StepActionParameterDescriptor> descriptors = new List<StepActionParameterDescriptor>();
            if (actionType == null)
            {
                return descriptors;
            }

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo[] fields = actionType.GetFields(flags);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field == null)
                {
                    continue;
                }

                StepParamAttribute param = Attribute.GetCustomAttribute(field, typeof(StepParamAttribute), true) as StepParamAttribute;
                if (param == null || string.IsNullOrEmpty(param.Key))
                {
                    continue;
                }

                StepActionParameterDescriptor descriptor = new StepActionParameterDescriptor();
                descriptor.Key = param.Key;
                descriptor.Label = !string.IsNullOrEmpty(param.Label) ? param.Label : param.Key;
                descriptor.TypeName = field.FieldType != null ? field.FieldType.Name : string.Empty;
                descriptor.Order = param.Order;
                descriptor.IsAdvanced = param.IsAdvanced;
                descriptors.Add(descriptor);
            }

            PropertyInfo[] properties = actionType.GetProperties(flags);
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                if (property == null || !property.CanRead || !property.CanWrite)
                {
                    continue;
                }
                if (property.GetIndexParameters() != null && property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                StepParamAttribute param = Attribute.GetCustomAttribute(property, typeof(StepParamAttribute), true) as StepParamAttribute;
                if (param == null || string.IsNullOrEmpty(param.Key))
                {
                    continue;
                }

                StepActionParameterDescriptor descriptor = new StepActionParameterDescriptor();
                descriptor.Key = param.Key;
                descriptor.Label = !string.IsNullOrEmpty(param.Label) ? param.Label : param.Key;
                descriptor.TypeName = property.PropertyType != null ? property.PropertyType.Name : string.Empty;
                descriptor.Order = param.Order;
                descriptor.IsAdvanced = param.IsAdvanced;
                descriptors.Add(descriptor);
            }

            descriptors.Sort(CompareParameterDescriptor);
            return descriptors;
        }

        /// <summary>
        /// 比较两个参数描述的展示顺序。
        /// </summary>
        private static int CompareParameterDescriptor(StepActionParameterDescriptor a, StepActionParameterDescriptor b)
        {
            if (ReferenceEquals(a, b))
            {
                return 0;
            }
            if (a == null)
            {
                return 1;
            }
            if (b == null)
            {
                return -1;
            }

            int orderCompare = a.Order.CompareTo(b.Order);
            if (orderCompare != 0)
            {
                return orderCompare;
            }

            string leftKey = a.Key ?? string.Empty;
            string rightKey = b.Key ?? string.Empty;
            return string.CompareOrdinal(leftKey, rightKey);
        }

        /// <summary>
        /// 根据类型名查找动作类型。
        /// </summary>
        private static Type FindActionType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            lock (s_Sync)
            {
                Type cached;
                if (s_ActionTypeByNameCache.TryGetValue(typeName, out cached))
                {
                    return cached;
                }

                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    Assembly assembly = assemblies[i];
                    Type[] types;
                    try
                    {
                        types = assembly.GetTypes();
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
                        Type type = types[j];
                        if (type == null || !typeof(BaseStepAction).IsAssignableFrom(type))
                        {
                            continue;
                        }

                        string fullName = type.FullName ?? type.Name;
                        if (!string.Equals(fullName, typeName, StringComparison.Ordinal) && !string.Equals(type.Name, typeName, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        s_ActionTypeByNameCache[typeName] = type;
                        return type;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 获取参数列表中指定 key 的值。
        /// </summary>
        private static string GetParameterValue(List<StepEditorParameterData> parameters, string key)
        {
            if (parameters == null || parameters.Count == 0 || string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            for (int i = 0; i < parameters.Count; i++)
            {
                StepEditorParameterData parameter = parameters[i];
                if (parameter == null || !string.Equals(parameter.Key, key, StringComparison.Ordinal))
                {
                    continue;
                }

                return parameter.Value ?? string.Empty;
            }

            return string.Empty;
        }

        /// <summary>
        /// 深拷贝动作描述，避免外部修改污染缓存对象。
        /// </summary>
        private static StepActionDescriptor CloneDescriptor(StepActionDescriptor source)
        {
            if (source == null)
            {
                return null;
            }

            StepActionDescriptor clone = new StepActionDescriptor();
            clone.ActionType = source.ActionType;
            clone.TypeName = source.TypeName ?? string.Empty;
            clone.DisplayName = source.DisplayName ?? string.Empty;
            clone.Category = source.Category ?? string.Empty;
            clone.SummaryTemplate = source.SummaryTemplate ?? string.Empty;
            clone.Description = source.Description ?? string.Empty;

            if (source.Keywords != null)
            {
                for (int i = 0; i < source.Keywords.Count; i++)
                {
                    clone.Keywords.Add(source.Keywords[i] ?? string.Empty);
                }
            }

            if (source.Parameters != null)
            {
                for (int i = 0; i < source.Parameters.Count; i++)
                {
                    StepActionParameterDescriptor parameter = source.Parameters[i];
                    clone.Parameters.Add(CloneParameterDescriptor(parameter));
                }
            }

            return clone;
        }

        /// <summary>
        /// 深拷贝动作参数描述，避免共享引用被外部篡改。
        /// </summary>
        private static StepActionParameterDescriptor CloneParameterDescriptor(StepActionParameterDescriptor source)
        {
            if (source == null)
            {
                return null;
            }

            StepActionParameterDescriptor clone = new StepActionParameterDescriptor();
            clone.Key = source.Key ?? string.Empty;
            clone.Label = source.Label ?? string.Empty;
            clone.TypeName = source.TypeName ?? string.Empty;
            clone.Order = source.Order;
            clone.IsAdvanced = source.IsAdvanced;
            return clone;
        }
    }
}
