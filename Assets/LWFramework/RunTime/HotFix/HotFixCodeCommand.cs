using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class HotFixCodeCommand : IHotFixCommand
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

    public HotFixCodeCommand(GameObject go, HotFixMonoBehavior hotFixMonoBehavior) {

        this.className = hotFixMonoBehavior.ToString();
        this.gameObject = go;
        this.hotFixMonoBehavior = hotFixMonoBehavior;

        this.hotFixMonoBehavior.Init(gameObject);
    }
   
  
    public void ExecuteAwake()
    {
        this.hotFixMonoBehavior.Awake();
    }
    public void ExecuteStart()
    {
        this.hotFixMonoBehavior.Start();
    }
    public void ExecuteUpdate()
    {
        this.hotFixMonoBehavior.Update();
    }

    public void ExecuteOnDestroy()
    {
        this.hotFixMonoBehavior.OnDestroy();
    }

    public void ExecuteOnEnable()
    {
        this.hotFixMonoBehavior.OnEnable();
    }

    public void ExecuteOnDisable()
    {
        this.hotFixMonoBehavior.OnDisable();
    }
}
