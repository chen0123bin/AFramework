//// ***********************************************************************
//// Assembly         : LWIL.Runtime
//// Author           : chenbin
//// Created          : 02-22-2022
////
//// Last Modified By : chenbin
//// Last Modified On : 02-24-2022
//// ***********************************************************************
//// <copyright file="HotFixMonoBehaviorAdapter.cs" company="">
////     Copyright (c) . All rights reserved.
//// </copyright>
//// <summary></summary>
//// ***********************************************************************
//using Cysharp.Threading.Tasks;
//using LWFramework.Core;
//using Sirenix.OdinInspector;
//using Sirenix.Serialization;
//using System;
//using System.Reflection;
//using UnityEngine;

///// <summary>
///// 热更mono适配器，在GameObject上挂载，然后通过Odin选择需要序列化的类
///// Implements the <see cref="Sirenix.OdinInspector.SerializedMonoBehaviour" />
///// </summary>
///// <seealso cref="Sirenix.OdinInspector.SerializedMonoBehaviour" />
//public class HotFixMonoBehaviorAdapter : SerializedMonoBehaviour
//{
//    [InfoBox("不能在启动场景中挂载")]
//    /// <summary>
//    ///开启Update选项
//    /// </summary>
//    [LabelText("开启Update")]
//    public bool EnableUpdate;
//    /// <summary>
//    /// The hot fix mono behavior
//    /// </summary>
//    [NonSerialized, OdinSerialize, LabelText("热更Mono")]
//    public HotFixMonoBehavior hotFixMonoBehavior;

//    /// <summary>
//    /// The hot fix command
//    /// </summary>
//    private IHotFixCommand hotFixCommand;
//    /// <summary>
//    /// Awakes this instance.
//    /// </summary>
//    //    async void Awake()
//    //    {
//    //        await UniTask.WaitUntil(() => ManagerUtility.HotfixMgr.Loaded);
//    //        switch ((HotfixCodeRunMode)LWUtility.GlobalConfig.hotfixCodeRunMode)
//    //        {
//    //            case HotfixCodeRunMode.ByILRuntime:
//    //#if IL
//    //                hotFixCommand = new HotFixILCommand(gameObject, hotFixMonoBehavior,hotfixName);
//    //#else
//    //    Debug.LogError("请开始IL宏");
//    //#endif
//    //                break;
//    //            case HotfixCodeRunMode.ByReflection:
//    //                hotFixCommand = new HotFixRefCommand(gameObject, hotFixMonoBehavior, hotfixName);
//    //                break;
//    //            case HotfixCodeRunMode.ByCode:
//    //                hotFixCommand = new HotFixCodeCommand( gameObject, hotFixMonoBehavior, hotfixName);
//    //                break;
//    //            default:
//    //                break;
//    //        }

    
//    //        hotFixCommand.ExecuteAwake();
//    //    }
//    //    /// <summary>
//    //    /// Starts this instance.
//    //    /// </summary>
//    //    async void Start()
//    //    {
//    //        await UniTask.WaitUntil(() => ManagerUtility.HotfixMgr.Loaded);
//    //        await UniTask.WaitForFixedUpdate();
//    //        if ( hotFixCommand != null){
//    //            hotFixCommand.ExecuteStart();
//    //        }

//    //    }


//    void Awake()
//    {
//        switch ((HotfixCodeRunMode)LWUtility.GlobalConfig.hotfixCodeRunMode)
//        {
//            case HotfixCodeRunMode.ByHyBridCLR:
//#if HYBRIDCLR
//                hotFixCommand = new HotFixRefCommand(gameObject, hotFixMonoBehavior);
//#else
//        Debug.LogError("请开始IL宏");
//#endif
//                break;
//            case HotfixCodeRunMode.ByReflection:
//                hotFixCommand = new HotFixRefCommand(gameObject, hotFixMonoBehavior);
//                break;
//            case HotfixCodeRunMode.ByCode:
//                hotFixCommand = new HotFixCodeCommand(gameObject, hotFixMonoBehavior);
//                break;
//            default:
//                break;
//        }

//        hotFixCommand.ExecuteAwake();
//    }
//    /// <summary>
//    /// Starts this instance.
//    /// </summary>
//    void Start()
//    {
//        if (hotFixCommand != null)
//        {
//            hotFixCommand.ExecuteStart();
//        }
//    }
//    /// <summary>
//    /// Updates this instance.
//    /// </summary>
//    private void Update()
//    {
//        if (hotFixCommand != null&& EnableUpdate)
//        {
//            hotFixCommand.ExecuteUpdate();
//        }
       
//    }

//    /// <summary>
//    /// Called when [enable].
//    /// </summary>
//    private void OnEnable()
//    {
//        if (hotFixCommand != null)
//        {
//            hotFixCommand.ExecuteOnEnable();
//        }
      
//    }
//    /// <summary>
//    /// Called when [disable].
//    /// </summary>
//    private void OnDisable()
//    {
//        if (hotFixCommand != null)
//        {
//            hotFixCommand.ExecuteOnDisable();
//        }
       
//    }
//    /// <summary>
//    /// Called when [destroy].
//    /// </summary>
//    private void OnDestroy()
//    {
//        if (hotFixCommand != null)
//        {
//            hotFixCommand.ExecuteOnDestroy();
//            hotFixCommand = null;
//        }

//    }
//}
