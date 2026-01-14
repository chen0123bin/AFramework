
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
            m_AssemblyList.Add(assembly);
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
            if (m_HotfixDllNameList.Contains(hotfixDllName))
            {
                Debug.LogWarning("内存中已经加载了" + hotfixDllName);
                return;
            }
            await UniTask.DelayFrame(1);

            LWDebug.Log("Code模式");

            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().First(assembly => assembly.GetName().Name == hotfixDllName);
            OnHotFixLoaded(assembly);

        }


        public override void Destroy()
        {
            base.Destroy();
        }


    }
}