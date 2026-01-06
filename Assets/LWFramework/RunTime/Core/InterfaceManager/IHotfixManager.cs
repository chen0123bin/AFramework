using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace LWCore
{
    public enum HotfixCodeRunMode
    {
        /// <summary>
        /// hybridclr 使用插件必须用il2cpp模式打包遵循插件的使用规范，并开启HYBRIDCLR宏
        /// </summary>
        ByHyBridCLR = 0,
        /// <summary>
        /// 必须使用mono模式打包，目前增加自动过滤。。。
        /// 
        /// 后面可以手动备用操作发布程序时热更域必须勾选为其他平台，用于打包时剔除内置的代码，开发时勾选所有平台。(建议勾选一个Editor 和基本不打包的平台),
        /// </summary>
        ByReflection = 1,
        /// <summary>
        /// 必须使用mono模式打包，域不能为Eidtor
        /// </summary>
        ByCode = 2,
    }
    public interface IHotfixManager
    {
        /// <summary>
        /// 是否加载完成
        /// </summary>
        bool Loaded { get; }
        /// <summary>
        /// 使用异步加载新版本热更脚本
        /// </summary>
        /// <param name="hotfixName">dll的名称，包含.dll</param>
        /// <param name="dir">目录 Hotfix/|Hotfix/Test2/ 以/为结尾</param>
        UniTask LoadScriptAsync(string hotfixDllName,string dir = "Hotfix/");



        /// <summary>
        ///  获取反射的type
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="hotfixName">dll的名称</param>
        /// <returns></returns>
        Type GetTypeByName(string typeName);
      

        /// <summary>
        /// 通过反射实例化对象
        /// </summary>
        /// <param name="type">类型名称</param>
        /// <param name="hotfixName">dll的名称</param>
        /// <param name="args">构造实例参数</param>
        /// <returns></returns>
        T Instantiate<T>(string typeName, object[] args = null);

        /// <summary>
        /// 直接通过类型调用方法
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="instance"></param>
        /// <param name="hotfixName">dll的名称</param>
        /// <param name="args"></param>
        void Invoke(string type, string method, object instance, params object[] args);
        void Destroy();
        /// <summary>
        /// 添加当前域中的特性及类型
        /// </summary>
        /// <param name="p_TypeArray"></param>
        void AddHotfixTypeAttr(List<Type> p_TypeArray);
        /// <summary>
        /// 根据特性去获取对应的所有type
        /// </summary>
        /// <typeparam name="T">特性</typeparam>
        /// <returns></returns>
        List<TypeAttr> GetAttrTypeDataList<T>();
        /// 根据typeName去获取类的特性  反射 IL都可用
        /// </summary>
        /// <typeparam name="T">特性</typeparam>
        /// <returns></returns>
        T FindAttr<T>(string typeName);
    }
}