
using Cysharp.Threading.Tasks;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if HYBRIDCLR
using HybridCLR;
namespace LWFramework.Core
{

    /// <summary>
    /// 热更环境初始化处理
    /// </summary>
    public class HotFixHyBridCLRManager : HotFixBaseManager, IManager
    {
        private bool m_LoadMetadata = false;
        public override void Init()
        {

        }

        public override void Update()
        {

        }
        public override async UniTask LoadScriptAsync(string hotfixName,string dir = "Hotfix/")
        {
            if (m_HotfixDllNameList.Contains(hotfixName))
            {
                Debug.LogWarning("内存中已经加载了" + hotfixName);
                return;
            }
            if (!m_LoadMetadata) {
                await LoadMetadataForAOTAssemblies();
                m_LoadMetadata = true;
            }            
            await LoadHotFixDll(hotfixName, dir);
         
        }
        async UniTask LoadHotFixDll(string hotfixName, string dir = "Hotfix/")
        {

            string dllPath = "";
            byte[] bytes = null;
            if (Application.isEditor)
            {
                //这里情况比较复杂,Mobile上基本认为Persistent才支持File操作,
                 dllPath = $"{Application.dataPath}/@Resources/{dir}{hotfixName}.dll.bytes" ;
                if (FileUtility.ExistsFile(dllPath))
                {
                    bytes = File.ReadAllBytes(dllPath);
                }
                else {
                    bytes = await ManagerUtility.AssetsMgr.LoadByteAsync($"Assets/@Resources/{dir}{hotfixName}.dll.bytes");
                     //解密      
                    if (LWUtility.GlobalConfig.encrypt)
                    {
                         bytes = LWFramework.Asset.EncryptDecryptManager.GetEncryptDecryptServices(LWUtility.GlobalConfig.encryptType).Decrypt(bytes);
                    }
                }
              
            }
            else
            {
                bytes = await ManagerUtility.AssetsMgr.LoadByteAsync($"Assets/@Resources/{dir}{hotfixName}.dll.bytes");
                //解密
                if (LWUtility.GlobalConfig.encrypt)
                {
                    bytes = LWFramework.Asset.EncryptDecryptManager.GetEncryptDecryptServices(LWUtility.GlobalConfig.encryptType).Decrypt(bytes);
                }
            }
          
            LWDebug.Log("HyBridCLR :" + bytes.Length);            
            
            Assembly assembly = Assembly.Load(bytes);
            OnHotFixLoaded(assembly);
        }
        /// <summary>
        /// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
        /// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
        /// </summary>
        private  async UniTask LoadMetadataForAOTAssemblies()
        {
            /// 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
            /// 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误
            /// 
            HomologousImageMode mode = HomologousImageMode.SuperSet;
           

            foreach (var aotDllName in LWUtility.GlobalConfig.aotMetaArray)
            {
                byte[] aotDllBytes;
                if (Application.isEditor)
                {
                    //这里情况比较复杂,Mobile上基本认为Persistent才支持File操作,
                    string dllPath = $"{Application.dataPath}/@Resources/Hotfix/{aotDllName}.dll.bytes";
                    aotDllBytes = File.ReadAllBytes(dllPath);
                }
                else
                {
                    aotDllBytes = await ManagerUtility.AssetsMgr.LoadByteAsync($"Assets/@Resources/Hotfix/{aotDllName}.dll.bytes");
                    //解密
                    if (LWUtility.GlobalConfig.encrypt)
                    {
                        aotDllBytes = LWFramework.Asset.EncryptDecryptManager.GetEncryptDecryptServices(LWUtility.GlobalConfig.encryptType).Decrypt(aotDllBytes);
                    }
                }
                LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(aotDllBytes, mode);
                Debug.Log($"LoadMetadataForAOTAssembly: mode:{mode} ret:{err}");
            }
        }


        //public override T Instantiate<T>(string typeName, object[] args = null)
        //{
        //   // Assembly assembly = FindDomainByTypeName(typeName);
        //  //  object ret = assembly.CreateInstance(typeName, false, BindingFlags.Default, null, args, null, null);           
        //}




        public override  void Destroy()
        {
            base.Destroy();
        }
    }
}
#endif