
using Cysharp.Threading.Tasks;
using LWCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LWHotfix
{

    /// <summary>
    /// 热更环境初始化处理
    /// </summary>
    public abstract class HotFixBaseManager : IManager, IHotfixManager
    {
        private bool m_IsLoaded = false;
        public bool Loaded { get => m_IsLoaded; }

        protected List<Assembly> m_AssemblyList = new List<Assembly>();
        protected List<string> m_HotfixDllNameList = new List<string>();
        //热更DLL中所有的type
        protected List<Type> m_TypeHotfixList = new List<Type>();
        //管理热更中的所有的Type
        protected Dictionary<string, List<TypeAttr>> m_TypeAttrHotfixListDic = new Dictionary<string, List<TypeAttr>>();
        protected Dictionary<string, Assembly> m_AssemblyDic = new Dictionary<string, Assembly>(StringComparer.Ordinal);
        protected Dictionary<string, Type> m_TypeDic = new Dictionary<string, Type>(StringComparer.Ordinal);


        public abstract void Init();



        public abstract UniTask LoadScriptAsync(string hotfixDllName, string dir = "Hotfix/");

        public abstract void Update();

        /// <summary>
        /// 添加当前域中的特性及类型
        /// </summary>
        /// <param name="p_TypeArray"></param>
        public void AddHotfixTypeAttr(List<Type> p_TypeArray)
        {
            if (p_TypeArray == null || p_TypeArray.Count <= 0)
            {
                return;
            }
            if (m_TypeAttrHotfixListDic == null)
            {
                m_TypeAttrHotfixListDic = new Dictionary<string, List<TypeAttr>>();
            }
            if (m_TypeHotfixList == null)
            {
                m_TypeHotfixList = new List<Type>();
            }

            //将所有带有特性的类进行字典管理
            for (int i = 0; i < p_TypeArray.Count; i++)
            {
                Type item = p_TypeArray[i];
                if (item == null)
                {
                    continue;
                }

                string fullTypeName = item.FullName;
                if (!string.IsNullOrEmpty(fullTypeName))
                {
                    m_TypeDic[fullTypeName] = item;
                }

                string typeName = item.Name;
                if (!string.IsNullOrEmpty(typeName) && !m_TypeDic.ContainsKey(typeName))
                {
                    m_TypeDic[typeName] = item;
                }

                if (!m_TypeHotfixList.Contains(item))
                {
                    m_TypeHotfixList.Add(item);
                }

                if (item.IsClass)
                {
                    object[] attrs = item.GetCustomAttributes(false);
                    for (int j = 0; j < attrs.Length; j++)
                    {
                        object attr = attrs[j];
                        if (attr == null)
                        {
                            continue;
                        }

                        string attrKey = attr.GetType().FullName;
                        if (string.IsNullOrEmpty(attrKey))
                        {
                            continue;
                        }

                        if (!m_TypeAttrHotfixListDic.ContainsKey(attrKey))
                        {
                            m_TypeAttrHotfixListDic[attrKey] = new List<TypeAttr>();
                        }

                        TypeAttr classData = new TypeAttr { attr = (Attribute)attr, type = item };
                        m_TypeAttrHotfixListDic[attrKey].Add(classData);
                    }
                }
            }
        }

        /// <summary>
        /// 根据特性去获取对应的所有type
        /// </summary>
        /// <typeparam name="T">特性</typeparam>
        /// <returns></returns>
        public List<TypeAttr> GetAttrTypeDataList<T>()
        {
            string attrTypeName = typeof(T).FullName;
            if (string.IsNullOrEmpty(attrTypeName))
            {
                return null;
            }

            if (m_TypeAttrHotfixListDic == null || !m_TypeAttrHotfixListDic.ContainsKey(attrTypeName))
            {
                LWDebug.LogWarning("当前域下找不到这个包含这个特性的类" + attrTypeName);
                return null;
            }

            return m_TypeAttrHotfixListDic[attrTypeName];
        }

        /// <summary>
        /// 根据typeName去获取类的特性  反射 IL都可用
        /// </summary>
        /// <typeparam name="T">特性</typeparam>
        /// <returns></returns>
        public T FindAttr<T>(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return default;
            }

            Type targetType = GetTypeByName(typeName);
            if (targetType == null)
            {
                LWDebug.LogWarning($"当前没有找到类型 {typeName} 对应的特性 {typeof(T).FullName}");
                return default;
            }

            object attr = targetType.GetCustomAttributes(typeof(T), true).FirstOrDefault();
            if (attr == null)
            {
                return default;
            }

            return (T)attr;
        }

        public Assembly FindDomainByTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            Type type = GetTypeByName(typeName);
            if (type == null)
            {
                LWDebug.LogError($"在热更域中没有找到这个类型 {typeName}");
                return null;
            }

            Assembly assembly = type.Assembly;
            if (assembly == null)
            {
                LWDebug.LogError($"类型 {typeName} 没有可用程序集");
                return null;
            }

            return assembly;
        }

        protected bool HasLoadedAssembly(string hotfixDllName)
        {
            if (string.IsNullOrEmpty(hotfixDllName))
            {
                return false;
            }

            return m_AssemblyDic != null && m_AssemblyDic.ContainsKey(hotfixDllName);
        }

        protected Assembly GetLoadedAssembly(string hotfixDllName)
        {
            if (string.IsNullOrEmpty(hotfixDllName) || m_AssemblyDic == null)
            {
                return null;
            }

            Assembly assembly;
            if (m_AssemblyDic.TryGetValue(hotfixDllName, out assembly))
            {
                return assembly;
            }

            return null;
        }

        protected bool RegisterAssembly(Assembly assembly)
        {
            if (assembly == null)
            {
                return false;
            }

            string assemblyName = assembly.GetName().Name;
            if (string.IsNullOrEmpty(assemblyName))
            {
                LWDebug.LogError("尝试注册无名称程序集");
                return false;
            }

            if (m_AssemblyDic.ContainsKey(assemblyName))
            {
                return false;
            }

            m_AssemblyDic[assemblyName] = assembly;
            m_HotfixDllNameList.Add(assemblyName);
            m_AssemblyList.Add(assembly);
            AddHotfixTypeAttr(GetAssemblyTypesSafely(assembly));
            m_IsLoaded = true;
            LWDebug.Log("Dll加载完成" + assemblyName);
            return true;
        }

        protected List<Type> GetAssemblyTypesSafely(Assembly assembly)
        {
            if (assembly == null)
            {
                return new List<Type>();
            }

            try
            {
                return assembly.GetTypes().ToList();
            }
            catch (ReflectionTypeLoadException ex)
            {
                List<Type> loadedTypes = new List<Type>();
                Type[] types = ex.Types;
                for (int i = 0; i < types.Length; i++)
                {
                    Type type = types[i];
                    if (type != null)
                    {
                        loadedTypes.Add(type);
                    }
                }

                LWDebug.LogError($"程序集 {assembly.GetName().Name} 存在部分类型加载失败，将继续注册已成功加载的类型。");
                return loadedTypes;
            }
        }

        public virtual T Instantiate<T>(string typeName, object[] args = null)
        {
            Type targetType = GetTypeByName(typeName);
            if (targetType == null)
            {
                return default;
            }

            object ret;
            try
            {
                ret = args != null ? Activator.CreateInstance(targetType, args) : Activator.CreateInstance(targetType);
            }
            catch (Exception ex)
            {
                LWDebug.LogError($"通过反射实例化失败: {typeName}，原因：{ex.Message}");
                return default;
            }

            if (!(ret is T))
            {
                LWDebug.LogError($"反射实例类型不匹配: {typeName}, 期望 {typeof(T).FullName}, 实际 {ret.GetType().FullName}");
                return default;
            }

            return (T)ret;
        }


        public virtual Type GetTypeByName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            if (m_TypeDic != null)
            {
                Type cachedType;
                if (m_TypeDic.TryGetValue(typeName, out cachedType))
                {
                    return cachedType;
                }
            }

            for (int i = 0; i < m_AssemblyList.Count; i++)
            {
                Assembly assembly = m_AssemblyList[i];
                if (assembly == null)
                {
                    continue;
                }

                Type type = assembly.GetType(typeName, false);
                if (type != null)
                {
                    m_TypeDic[typeName] = type;
                    return type;
                }

                List<Type> types = GetAssemblyTypesSafely(assembly);
                for (int j = 0; j < types.Count; j++)
                {
                    Type searchType = types[j];
                    if (searchType == null || searchType.Name != typeName)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(searchType.FullName))
                    {
                        m_TypeDic[searchType.FullName] = searchType;
                    }
                    m_TypeDic[typeName] = searchType;
                    return searchType;
                }
            }

            return null;
        }

        public virtual void Invoke(string type, string method, object instance, params object[] p)
        {
            Type invokeType = GetTypeByName(type);
            if (invokeType == null)
            {
                LWDebug.LogError($"调用失败，找不到类型: {type}");
                return;
            }

            MethodInfo methodInfo = invokeType.GetMethod(method);
            if (methodInfo == null)
            {
                LWDebug.LogError($"调用失败，找不到方法: {type}.{method}");
                return;
            }

            methodInfo.Invoke(instance, p);
        }

        protected void OnHotFixLoaded(Assembly assembly)
        {
            if (assembly == null)
            {
                return;
            }

            if (!RegisterAssembly(assembly))
            {
                LWDebug.LogWarning("内存中已经加载了" + assembly.GetName().Name);
            }
        }

        public virtual void Destroy()
        {
            m_IsLoaded = false;
            m_AssemblyList.Clear();
            m_HotfixDllNameList.Clear();
            m_TypeHotfixList.Clear();
            m_TypeAttrHotfixListDic.Clear();
            m_AssemblyDic.Clear();
            m_TypeDic.Clear();
        }

    }


}
