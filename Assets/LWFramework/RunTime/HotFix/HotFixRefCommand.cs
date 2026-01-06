
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class HotFixRefCommand : IHotFixCommand
{
    protected HotFixMonoBehavior hotFixMonoBehavior;
    protected GameObject gameObject;
    protected string className;
    protected Type classType;
    protected object instance;
    protected MethodInfo init_method;
    protected MethodInfo awake_method;
    protected MethodInfo update_method;
    protected MethodInfo start_method;
    protected MethodInfo onDestroy_method;
    protected MethodInfo onEnable_method;
    protected MethodInfo onDisable_method;

    public HotFixRefCommand(GameObject go, HotFixMonoBehavior hotFixMonoBehavior) {

        this.className = hotFixMonoBehavior.ToString();
        this.gameObject = go;
        this.hotFixMonoBehavior = hotFixMonoBehavior;
        //需要创建的类型
        classType = ManagerUtility.HotfixMgr.GetTypeByName(this.className);//HotFixMgr.Instance.AppDomain.LoadedTypes[bindClass];                                                                                   // classType = HotFixMgr.Instance.AppDomain.LoadedTypes[bindClass];                                                                                   //创建实例
        instance = ManagerUtility.HotfixMgr.Instantiate<object>(this.className);
        //通过反射的方式初始化数据
        InitData();
        //通过ILRunTime反射获取方法
        init_method = classType.GetMethod("Init");
        awake_method = classType.GetMethod("Awake");
        update_method = classType.GetMethod("Update");
        start_method = classType.GetMethod("Start");
        onDestroy_method = classType.GetMethod("OnDestroy");
        onEnable_method = classType.GetMethod("OnEnable");
        onDisable_method = classType.GetMethod("OnDisable");

        init_method.Invoke(instance, new object[] { gameObject });
    }
    void InitData()
    {
        Type type = hotFixMonoBehavior.GetType();
        var fields = type.GetFields();
        foreach (FieldInfo item in fields)
        {
            var typeName = item.Name;
            var typeValue = item.GetValue(hotFixMonoBehavior);

            FieldInfo fieldInfo = classType.GetField(typeName);
            fieldInfo.SetValue(instance, typeValue);
        }

    }
  
    public void ExecuteAwake()
    {
        if(awake_method!=null)
            awake_method.Invoke(instance,null);
        else
            Debug.Log($"没有awake_method这个函数");
    }
    public void ExecuteStart()
    {
        if (start_method != null)
            start_method.Invoke(instance, null);
        else
            Debug.Log($"没有start_method这个函数");
    }
    public void ExecuteUpdate()
    {
        if (update_method != null)
            update_method.Invoke(instance, null);
        else
            Debug.Log($"没有update_method这个函数");
    }

    public void ExecuteOnDestroy()
    {
        if (onDestroy_method != null)
            onDestroy_method.Invoke(instance, null);
        else
            Debug.Log($"没有onDestroy_method这个函数");
    }

    public void ExecuteOnEnable()
    {
        if (onEnable_method != null)
            onEnable_method.Invoke(instance, null);
        else
            Debug.Log($"没有onEnable_method这个函数");
    }

    public void ExecuteOnDisable()
    {
        if (onDisable_method != null)
            onDisable_method.Invoke(instance, null);
        else
            Debug.Log($"没有onDisable_method这个函数");
    }
}
