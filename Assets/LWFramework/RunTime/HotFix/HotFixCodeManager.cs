
using Cysharp.Threading.Tasks;
using LWCore;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LWHotfix
{

    /// <summary>
    /// 热更环境初始化处理
    /// </summary>
    public class HotFixCodeManager : HotFixBaseManager, IManager
    {
        public override void Init()
        {
            Assembly assembly = Assembly.Load("Assembly-CSharp");
            OnHotFixLoaded(assembly);
        }

        public void LateUpdate()
        {

        }

        public override void Update()
        {

        }
        public override async UniTask LoadScriptAsync(string hotfixDllName, string dir = "Hotfix/")
        {
            if (string.IsNullOrEmpty(hotfixDllName))
            {
                LWDebug.LogError("热更程序集名称为空，无法加载。");
                return;
            }

            if (HasLoadedAssembly(hotfixDllName))
            {
                Debug.LogWarning("内存中已经加载了" + hotfixDllName);
                return;
            }

            await UniTask.DelayFrame(1);

            LWDebug.Log("Code模式");

            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(findAssembly => findAssembly != null && findAssembly.GetName().Name == hotfixDllName);
            if (assembly == null)
            {
                LWDebug.LogError("当前域中没有找到热更程序集 " + hotfixDllName);
                return;
            }

            OnHotFixLoaded(assembly);
        }

        public override void Destroy()
        {
            base.Destroy();
        }
    }
}
