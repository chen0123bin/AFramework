
using Cysharp.Threading.Tasks;
using LWCore;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace LWHotfix
{

    /// <summary>
    /// 热更环境初始化处理
    /// </summary>
    public class HotFixRefManager : HotFixBaseManager, IManager
    {
        // public Assembly Assembly { get; private set; }

        public override void Init()
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
            await LoadHotFixDll(hotfixDllName, dir);
           
        }
        /**
        async UniTask LoadHotFixDll2()
        {
           
            string dllPath = "";
            if (Application.isEditor)
            {
                //这里情况比较复杂,Mobile上基本认为Persistent才支持File操作,
                dllPath = Application.dataPath + "/@Resources/Hotfix/" + LWUtility.HotfixByteFileName;
            }
            else
            {
                dllPath = Application.persistentDataPath + "/Bundles/@Resources/Hotfix/" + LWUtility.HotfixByteFileName;
                //热更文件存放在persistentDataPath，判断如果不存在的话，则从streamingAssetsPath从复制过来
                if (!File.Exists(dllPath))
                {
                    var secondPath = Application.streamingAssetsPath + "/Bundles/@Resources/Hotfix/" + LWUtility.HotfixByteFileName; ;// Application.streamingAssetsPath + "/" + LWUtility.AssetBundles + "/" + LWUtility.GetPlatform() + "/" + LWUtility.HotfixFileName;
                    var request = UnityWebRequest.Get(secondPath);
                    LWDebug.Log("firstPath:" + dllPath);
                    LWDebug.Log("secondPath:" + secondPath);
                    await request.SendWebRequest();
                    if (request.isDone && request.error == null)
                    {
                        LWDebug.Log("拷贝dll成功:" + dllPath + request.downloadHandler.data.Length);
                        byte[] results = request.downloadHandler.data;
                        FileTool.WriteByteToFile(LWUtility.HotfixByteFileName, results, Application.persistentDataPath + "/Bundles/@Resources/Hotfix/");
                    }

                }
            }
            LWDebug.Log("反射模式 - Dll路径:" + dllPath);
            //反射执行
            byte[] bytes = File.ReadAllBytes(dllPath);
            if (LWUtility.GlobalConfig.encrypt)
            {
                bytes = AESUtility.AesDecrypt(bytes, KeyHelp.Instance.AesKey);
            }
            var pdb = dllPath.Replace(".byte", "") + ".pdb";
            if (File.Exists(pdb))
            {
                var bytes2 = File.ReadAllBytes(pdb);
                Assembly = Assembly.Load(bytes, bytes2);
            }
            else
            {
                Assembly = Assembly.Load(bytes);
            }
        }
        */
        async UniTask LoadHotFixDll(string hotfixName, string dir = "Hotfix/")
        {
            //string dllPath = "";
            //byte[] bytes = null;
            //byte[] bytes2 = null;
            // if (Application.isEditor)
            // {
            //     //这里情况比较复杂,Mobile上基本认为Persistent才支持File操作,
            //     dllPath = $"{Application.dataPath}/@Resources/{dir}{hotfixName}.dll.bytes" ;
            //     if (FileUtility.ExistsFile(dllPath))
            //     {
            //         bytes = File.ReadAllBytes(dllPath);
            //     }
            //     else {
            //         bytes = await ManagerUtility.AssetsMgr.LoadByteAsync($"Assets/@Resources/{dir}{hotfixName}.dll.bytes");
            //         if (LWUtility.GlobalConfig.encrypt)
            //         {                      
            //             bytes = LWFramework.Asset.EncryptDecryptManager.GetEncryptDecryptServices(LWUtility.GlobalConfig.encryptType).Decrypt(bytes);
            //         }
            //     }
              
            // }
            // else
            // {              
            //     bytes = await ManagerUtility.AssetsMgr.LoadByteAsync($"Assets/@Resources/{dir}{hotfixName}.dll.bytes");
            //     if (LWUtility.GlobalConfig.encrypt)
            //     {
            //         bytes = LWFramework.Asset.EncryptDecryptManager.GetEncryptDecryptServices(LWUtility.GlobalConfig.encryptType).Decrypt(bytes);
            //     }
            // }
          
            //LWDebug.Log("反射模式:"  + bytes.Length);
            //反射执行          
          
            //Assembly assembly = Assembly.Load(bytes);
            

            //OnHotFixLoaded(assembly);
        }



        

        public override  void Destroy()
        {
            base.Destroy();
        }
    }
}