
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
        private bool isLoaded = false;
        public  bool Loaded { get => isLoaded; }

        protected List<Assembly> m_AssemblyList = new List<Assembly>();
        protected List<string> m_HotfixDllNameList = new List<string>();
        //热更DLL中所有的type
        protected List<Type> m_TypeHotfixList = new List<Type> ();
        //管理热更中的所有的Type
        protected Dictionary<string, List<TypeAttr>> m_TypeAttrHotfixListDic = new Dictionary<string, List<TypeAttr>> ();


        public abstract void Init();



        public abstract UniTask LoadScriptAsync(string hotfixDllName, string dir = "Hotfix/");

        public abstract void Update();

        /// <summary>
        /// 添加当前域中的特性及类型
        /// </summary>
        /// <param name="p_TypeArray"></param>
        public void AddHotfixTypeAttr(List<Type> p_TypeArray)
        {
            if (m_TypeAttrHotfixListDic == null)
            {
                m_TypeAttrHotfixListDic = new Dictionary<string, List<TypeAttr>>();
            }
            if (m_TypeHotfixList == null) {
                m_TypeHotfixList = new List<Type>();
            }
            this.m_TypeHotfixList.AddRange(p_TypeArray);
            //将所有带有特性的类进行字典管理
            foreach (var item in p_TypeArray)
            {
                if (item.IsClass)
                {
                    var attrs = item.GetCustomAttributes(false);
                    foreach (var attr in attrs)
                    {
                        if (attr == null)
                        {
                            continue;
                        }
                        if (!m_TypeAttrHotfixListDic.ContainsKey(attr.ToString()))
                        {
                            m_TypeAttrHotfixListDic[attr.ToString()] = new List<TypeAttr>();
                        }
                        TypeAttr classData = new TypeAttr { attr = (Attribute)attr, type = item };
                        m_TypeAttrHotfixListDic[attr.ToString()].Add(classData);
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
            if (m_TypeAttrHotfixListDic == null || !m_TypeAttrHotfixListDic.ContainsKey(typeof(T).FullName))
            {
                LWDebug.LogWarning("当前域下找不到这个包含这个特性的类" + typeof(T).FullName);
                return null;
            }
            else
            {
                return m_TypeAttrHotfixListDic[typeof(T).FullName];
            }
        }
        /// <summary>
        /// 根据typeName去获取类的特性  反射 IL都可用
        /// </summary>
        /// <typeparam name="T">特性</typeparam>
        /// <returns></returns>
        public T FindAttr<T>(string typeName)
        {
            List<TypeAttr> list = GetAttrTypeDataList<T>();
            TypeAttr attributeType = list.Find((_f) =>
            {
                return _f.type.Name == typeName;
            });
            return (T)attributeType.type.GetCustomAttributes(typeof(T), true).FirstOrDefault();
        }
        public Assembly FindDomainByTypeName(string typeName)
        {
            foreach (var item in m_AssemblyList)
            {
                if (item.GetType(typeName, false) != null)
                {
                    return item;
                }
            }
            LWDebug.LogError($"在热更域中没有找到这个类型{typeName}");
            return null;
        }
        public virtual T Instantiate<T>(string typeName, object[] args = null)
        {

            //Type type = GetTypeByName(typeName);
           // object ret;
            Assembly assembly = FindDomainByTypeName(typeName);
            object ret = assembly.CreateInstance(typeName, false, BindingFlags.Default, null, args, null, null);
            return (T)ret;
        }


        public virtual Type GetTypeByName(string typeName)
        {

            return FindDomainByTypeName(typeName).GetType(typeName); ;
        }
        public virtual void Invoke(string type, string method, object instance, params object[] p)
        {
            MethodInfo methodInfo = GetTypeByName(type).GetMethod(method);
            methodInfo.Invoke(instance, p);
        }
        protected void OnHotFixLoaded(Assembly assembly)
        {        
            isLoaded = true;
            if (assembly != null) {
                LWDebug.Log("Dll加载完成" + assembly.GetName().Name);
                m_HotfixDllNameList.Add(assembly.GetName().Name);
                m_AssemblyList.Add(assembly);
                AddHotfixTypeAttr(assembly.GetTypes().ToList());
            }        
        }

        public virtual void Destroy()
        {
            m_TypeHotfixList.Clear();
            m_TypeAttrHotfixListDic.Clear();
            m_TypeHotfixList = null;
            m_TypeAttrHotfixListDic = null;
        }

    }

    
}
