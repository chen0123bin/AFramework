using LWCore;
using LWAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LWUI
{
    public class UIUtility : IUIUtility
    {

        /// <summary>
        /// 所有UI的父节点缓存，每次使用的都记录一次避免多次查找
        /// </summary>
        private Dictionary<string, Transform> m_UIParentDicCache = new Dictionary<string, Transform>();

        private Dictionary<string, GameObject> m_PreloadUIPrefabCache = new Dictionary<string, GameObject>();

        private string m_Style;

        /// <summary>
        /// 创建一个VIEW
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public BaseUIView CreateView<T>(GameObject uiGameObject = null, string loadPath = null)
        {
            //BaseUIView uiView = ManagerUtility.HotfixMgr.Instantiate<BaseUIView>(typeof(T).Name);//Activator.CreateInstance(typeof(T)) as BaseUIView;
            //获取UIViewDataAttribute特性
            //var attr = (UIViewDataAttribute)typeof(T).GetCustomAttributes(typeof(UIViewDataAttribute), true).FirstOrDefault();
            return CreateView_Internal(typeof(T).Name, uiGameObject, loadPath);
        }
        /// <summary>
        /// 创建一个VIEW
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public BaseUIView CreateView(string viewName, GameObject uiGameObject = null, string loadPath = null)
        {

            return CreateView_Internal(viewName, uiGameObject, loadPath);
        }

        private BaseUIView CreateView_Internal(string viewName, GameObject uiGameObject = null, string loadPath = null)
        {

            //获取UIViewDataAttribute特性
            // var attr = (UIViewDataAttribute)ManagerUtility.HotfixMgr.GetTypeByName(viewName).GetCustomAttributes(typeof(UIViewDataAttribute), true).FirstOrDefault();
            var attr = ManagerUtility.HotfixMgr.FindAttr<UIViewDataAttribute>(viewName);
            if (attr == null)
            {
                LWDebug.LogError("没有找到UIViewDataAttribute这个特性");
                return null;
            }
            BaseUIView uiView = ManagerUtility.HotfixMgr.Instantiate<BaseUIView>(viewName);//Activator.CreateInstance(typeof(T)) as BaseUIView;


            if (uiGameObject == null)
            {
                string path = attr.m_LoadPath;
                if (path.Contains("{0}"))
                {
                    m_Style = ManagerUtility.UIMgr.GetStyle();
                    path = string.Format(path, m_Style);
                }
                //创建UI对象
                if (loadPath != null)
                {
                    path = loadPath;
                }
                if (!m_PreloadUIPrefabCache.TryGetValue(path, out uiGameObject))
                {

                    uiGameObject = ManagerUtility.AssetsMgr.Instantiate(path);//InstanceUIGameObject(loadPath);
                }

            }

            SetParent(uiGameObject, attr.m_FindType, attr.m_Param);
            //初始化UI
            //view上的组件
            uiView.CreateView(uiGameObject);
            uiView.ShowHideType = attr.m_ShowHideType;
            //LWDebug.Log("UIManager：" + typeof(T).ToString());
            return uiView;
        }

        /// <summary>
        /// 异步创建一个VIEW
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async UniTask<BaseUIView> CreateViewAsync<T>(GameObject uiGameObject = null, string loadPath = null)
        {
            //BaseUIView uiView = Activator.CreateInstance(typeof(T)) as BaseUIView;
            //获取UIViewDataAttribute特性
            //var attr = (UIViewDataAttribute)typeof(T).GetCustomAttributes(typeof(UIViewDataAttribute), true).FirstOrDefault();
            var attr = ManagerUtility.HotfixMgr.FindAttr<UIViewDataAttribute>(typeof(T).Name);
            BaseUIView uiView = ManagerUtility.HotfixMgr.Instantiate<BaseUIView>(typeof(T).Name);//Activator.CreateInstance(typeof(T)) as BaseUIView;
            if (attr == null)
            {
                LWDebug.LogError("没有找到UIViewDataAttribute这个特性");
                return null;
            }
            if (uiGameObject == null)
            {
                string path = attr.m_LoadPath;
                if (path.Contains("{0}"))
                {
                    m_Style = ManagerUtility.UIMgr.GetStyle();
                    path = string.Format(path, m_Style);
                }
                //创建UI对象
                if (loadPath != null)
                {
                    path = loadPath;
                }
                //LWViewHelp.OpenLoadingView();
                uiGameObject = await ManagerUtility.AssetsMgr.InstantiateAsync(path);
                //LWViewHelp.CloseLoadingView();
            }
            SetParent(uiGameObject, attr.m_FindType, attr.m_Param);
            //初始化UI
            uiView.CreateView(uiGameObject);
            LWDebug.Log("UIManager：" + typeof(T).ToString());
            return uiView;
        }

        /// <summary>
        /// 异步预加载一个对象
        /// </summary>
        /// <param name="loadPath">资源路径</param>
        /// <returns></returns>
        public async UniTask PreloadViewAsync(string loadPath = null)
        {
            if (!m_PreloadUIPrefabCache.ContainsKey(loadPath))
            {
                if (loadPath.Contains("{0}"))
                {
                    m_Style = ManagerUtility.UIMgr.GetStyle();
                    loadPath = string.Format(loadPath, m_Style);
                }
                GameObject uiGameObject = await ManagerUtility.AssetsMgr.InstantiateAsync(loadPath);
                SetParent(uiGameObject, FindType.Name, "LWFramework/Canvas/Edit");
                m_PreloadUIPrefabCache.Add(loadPath, uiGameObject);
            }
        }
        /// <summary>
        /// 异步预加载一个对象
        /// </summary>
        /// <typeparam name="T">View</typeparam>
        /// <returns></returns>
        public async UniTask PreloadViewAsync<T>()
        {
            string viewName = typeof(T).Name;
            var attr = ManagerUtility.HotfixMgr.FindAttr<UIViewDataAttribute>(viewName);
            if (attr == null)
            {
                LWDebug.LogError("没有找到FGUIViewDataAttribute这个特性");
                return;
            }
            await PreloadViewAsync(attr.m_LoadPath);
        }
        /// <summary>
        /// 根据特性 获取父级
        /// </summary>
        /// <param name="findType">查找的类型</param>
        /// <param name="param">参数</param>
        /// <returns></returns>
        public Transform GetParent(FindType findType, string param)
        {
            Transform ret = null;
            if (m_UIParentDicCache.ContainsKey(param))
            {
                ret = m_UIParentDicCache[param];
            }
            else if (findType == FindType.Name)
            {
                GameObject gameObject = GameObject.Find(param);
                if (gameObject == null)
                {
                    LWDebug.LogError(string.Format("当前没有找到{0}这个GameObject对象", param));
                }
                ret = gameObject.transform;
                m_UIParentDicCache.Add(param, ret);
            }
            else if (findType == FindType.Tag)
            {
                GameObject gameObject = GameObject.FindGameObjectWithTag(param);
                if (gameObject == null)
                {
                    LWDebug.LogError(string.Format("当前没有找到{0}这个Tag GameObject对象", param));
                }
                ret = gameObject.transform;
                m_UIParentDicCache.Add(param, ret);
            }
            return ret;
        }


        /// <summary>
        /// 根据特性设置父节点
        /// </summary>
        /// <param name="go"></param>
        /// <param name="findType"></param>
        /// <param name="rootPath"></param>
        private void SetParent(GameObject go, FindType findType, string rootPath)
        {
            Transform parent = ManagerUtility.UIMgr.IUIUtility.GetParent(findType, rootPath);
            if (parent == null)
            {
                LWDebug.LogError($"没有找到这个{rootPath}路径的对象节点");
            }
            else
            {
                go.transform.SetParent(parent, false);
            }

        }

        /// <summary>
        /// 根据ab路径获取精灵图片
        /// </summary>
        /// <param name="abPath">ab的路径</param>
        /// <returns></returns>
        Sprite GetSprite(string abPath)
        {
            return ManagerUtility.AssetsMgr.LoadAsset<UnityEngine.Sprite>(abPath);
        }

        /// <summary>
        /// 根据特性获取UI对象
        /// </summary>
        /// <param name="entity"></param>
        public void SetViewElement(object entity, GameObject uiGameObject)
        {
            var attr = ManagerUtility.HotfixMgr.FindAttr<UIViewDataAttribute>(entity.ToString());
            Type type = ManagerUtility.HotfixMgr.GetTypeByName(entity.ToString());//entity.GetType();          
            //获取字段属性
            FieldInfo[] infos = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            //遍历字段属性
            for (int i = 0; i < infos.Length; i++)
            {
                //获取属性上的特性
                object[] attributes = infos[i].GetCustomAttributes(true);
                for (int j = 0; j < attributes.Length; j++)
                {
                    var attribute = attributes[j];
                    if (attribute is UIElementAttribute)
                    {
                        SetUIElementAttribute(entity, infos[i], uiGameObject, attribute as UIElementAttribute);
                    }

                }

            }
        }
        /// <summary>
        /// 根据特性获取UI对象
        /// </summary>
        /// <param name="entity"></param>
        public void SetViewElement(object entity, Type type, BaseUIView uiView)
        {
            SetViewElement(entity, type, uiView.Entity);
        }
        public void SetViewElement(object entity, Type type, GameObject uiGameObject)
        {
            //获取字段属性
            FieldInfo[] infos = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            //遍历字段属性
            for (int i = 0; i < infos.Length; i++)
            {
                //获取属性上的特性
                object[] attributes = infos[i].GetCustomAttributes(true);
                for (int j = 0; j < attributes.Length; j++)
                {
                    var attribute = attributes[j];
                    if (attribute is UIElementAttribute)
                    {
                        SetUIElementAttribute(entity, infos[i], uiGameObject, attribute as UIElementAttribute);
                    }

                }

            }
        }


        /// <summary>
        /// 设置UIElementAttribute 特性
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="objectField"></param>
        /// <param name="uiGameObject"></param>
        /// <param name="uiElement"></param>
        void SetUIElementAttribute(object entity, FieldInfo objectField, GameObject uiGameObject, UIElementAttribute uiElement)
        {
            try
            {
                if (uiElement.m_Type == UIElementType.Style && !this.m_Style.Contains(uiElement.m_Style))
                {
                    return;
                }
                UnityEngine.Object obj = GetUIComponent(uiGameObject.transform.Find(uiElement.m_RootPath), objectField.FieldType);
                //给当前的字段赋值
                objectField.SetValue(entity, obj);
                //处理初始化动态图片
                if (uiElement.m_ResPath != "")
                {
                    if (objectField.FieldType == typeof(UnityEngine.UI.Image) && !uiElement.m_ResPath.IsEmpty())
                    {
                        ((UnityEngine.UI.Image)obj).sprite = GetSprite(uiElement.m_ResPath);
                    }
                    else if (objectField.FieldType == typeof(UnityEngine.UI.Button) && !uiElement.m_ResPath.IsEmpty())
                    {
                        ((UnityEngine.UI.Button)obj).GetComponent<UnityEngine.UI.Image>().sprite = GetSprite(uiElement.m_ResPath);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("当前: {0} 路径上没有找到对应的物体   {1} {2}", uiElement.m_RootPath, e.ToString(), e.StackTrace));

            }
        }
        UnityEngine.Object GetUIComponent(Transform go, Type type)
        {
            UnityEngine.Object ret = null;
            if (type.Name == "Button")
            {
                ret = go.GetComponent<Button>();
            }
            else if (type.Name == "Image")
            {
                ret = go.GetComponent<Image>();
            }
            else if (type.Name == "Slider")
            {
                ret = go.GetComponent<Slider>();
            }
            else if (type.Name == "RawImage")
            {
                ret = go.GetComponent<RawImage>();
            }
            else if (type.Name == "Text")
            {
                ret = go.GetComponent<Text>();
            }
            else if (type.Name == "Toggle")
            {
                ret = go.GetComponent<Toggle>();
            }
            else if (type.Name == "Dropdown")
            {
                ret = go.GetComponent<Dropdown>();
            }
            else if (type.Name == "CanvasGroup")
            {
                ret = go.GetComponent<CanvasGroup>();
            }
            else if (type.Name == "ScrollRect")
            {
                ret = go.GetComponent<ScrollRect>();
            }
            else if (type.Name == "Scrollbar")
            {
                ret = go.GetComponent<Scrollbar>();
            }
            else if (type.Name == "InputField")
            {
                ret = go.GetComponent<InputField>();
            }
            else if (type.Name == "RectTransform")
            {
                ret = go.GetComponent<RectTransform>();
            }
#if TMPRO
            else if (type.Name.Contains( "TMP_Text"))
            {
                ret = go.GetComponent<TMPro.TMP_Text>();
            }
            else if (type.Name.Contains("TMP_InputField"))
            {
                ret = go.GetComponent<TMPro.TMP_InputField>();
            }
            else if (type.Name.Contains("TMP_Dropdown"))
            {
                ret = go.GetComponent<TMPro.TMP_Dropdown>();
            }
           
# endif
            else
            {
                ret = go.GetComponent<Transform>();//ObjectGroupToggle
            }
            return ret;
        }





    }

}

