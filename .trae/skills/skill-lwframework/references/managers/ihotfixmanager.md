# IHotfixManager 热更管理器

## 概述

IHotfixManager 是 LWFramework 的热更新系统核心接口，提供热更 DLL 加载、反射获取类型和特性、实例化对象和方法调用等功能。

- **接口位置**: `Assets/LWFramework/RunTime/Core/InterfaceManager/IHotfixManager.cs`
- **默认实现**: `Assets/LWFramework/RunTime/HotFix/HotFixBaseManager.cs`（抽象基类）
- **访问入口**: `ManagerUtility.HotfixMgr`

---

## 核心属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Loaded` | `bool` | 热更 DLL 是否已加载 |

---

## API 参考

### 加载与销毁

```csharp
/// <summary>
/// 异步加载热更 DLL
/// </summary>
/// <param name="hotfixDllName">热更 DLL 文件名</param>
/// <param name="dir">DLL 所在目录</param>
UniTask LoadScriptAsync(string hotfixDllName, string dir);

/// <summary>
/// 销毁热更域
/// </summary>
void Destroy();
```

### 类型操作

```csharp
/// <summary>
/// 通过类型名获取 Type
/// </summary>
Type GetTypeByName(string typeName);

/// <summary>
/// 通过类型全名实例化对象
/// </summary>
T Instantiate<T>(string typeFullName) where T : class;
```

### 方法调用

```csharp
/// <summary>
/// 调用热更对象方法
/// </summary>
/// <param name="type">类型名</param>
/// <param name="method">方法名</param>
/// <param name="instance">对象实例</param>
void Invoke(string type, string method, object instance);
```

### 特性系统

```csharp
/// <summary>
/// 添加热更类型特性数据
/// </summary>
void AddHotfixTypeAttr(string typeName, object[] attrs);

/// <summary>
/// 获取带有指定特性的类型列表
/// </summary>
List<TypeAttr> GetAttrTypeDataList<T>() where T : Attribute;

/// <summary>
/// 查找指定类型的特性实例
/// </summary>
T FindAttr<T>(string typeName) where T : Attribute;
```

---

## 使用示例

### 加载热更 DLL

```csharp
using Cysharp.Threading.Tasks;
using LWCore;

public class HotfixExamples
{
    /// <summary>
    /// 加载热更 DLL（热更入口文件名与目录按项目规范传入）
    /// </summary>
    public async UniTask LoadHotfixAsync(string hotfixDllName, string dir)
    {
        await ManagerUtility.HotfixMgr.LoadScriptAsync(hotfixDllName, dir);
    }
}
```

### 通过类型名获取 Type 并实例化

```csharp
using LWCore;
using LWUI;

public class HotfixExamples
{
    /// <summary>
    /// 通过类型名获取 Type，并实例化对象
    /// </summary>
    public BaseUIView CreateViewInstance(string viewTypeName)
    {
        Type type = ManagerUtility.HotfixMgr.GetTypeByName(viewTypeName);
        BaseUIView view = ManagerUtility.HotfixMgr.Instantiate<BaseUIView>(type.FullName);
        return view;
    }
}
```

### 调用热更对象方法

```csharp
/// <summary>
/// 调用热更对象方法（type 为类型名，method 为方法名）
/// </summary>
public void InvokeMethod(string typeName, string methodName, object instance)
{
    ManagerUtility.HotfixMgr.Invoke(typeName, methodName, instance);
}
```

### 从热更域按特性取类型列表

```csharp
using LWCore;
using LWUI;
using System;
using System.Collections.Generic;

public class HotfixExamples
{
    /// <summary>
    /// 从热更域按特性取类型列表（示例：UIViewDataAttribute）
    /// </summary>
    public List<TypeAttr> GetUIViewTypes()
    {
        List<TypeAttr> list = ManagerUtility.HotfixMgr.GetAttrTypeDataList<UIViewDataAttribute>();
        return list;
    }

    /// <summary>
    /// 从类型名获取其特性实例（示例：UIViewDataAttribute）
    /// </summary>
    public UIViewDataAttribute GetUIViewData(string typeName)
    {
        UIViewDataAttribute attr = ManagerUtility.HotfixMgr.FindAttr<UIViewDataAttribute>(typeName);
        return attr;
    }
}
```

### 在启动时加载热更

```csharp
using Cysharp.Threading.Tasks;
using LWCore;
using UnityEngine;

public class GameStartup : MonoBehaviour
{
    private async void Start()
    {
        // 先加载热更 DLL
        await ManagerUtility.HotfixMgr.LoadScriptAsync("Hotfix.dll", "Assets/Hotfix/");
        
        // 然后启动流程
        ManagerUtility.MainMgr.StartProcedure();
    }
}
```

### 动态创建热更视图

```csharp
using LWCore;
using LWUI;

public class DynamicViewCreator
{
    /// <summary>
    /// 从热更域动态创建视图
    /// </summary>
    public BaseUIView CreateHotfixView(string viewTypeName)
    {
        // 检查热更是否已加载
        if (!ManagerUtility.HotfixMgr.Loaded)
        {
            Debug.LogError("热更 DLL 未加载");
            return null;
        }

        // 从热更域获取类型并实例化
        Type viewType = ManagerUtility.HotfixMgr.GetTypeByName(viewTypeName);
        if (viewType == null)
        {
            Debug.LogError($"找不到热更视图类型: {viewTypeName}");
            return null;
        }

        BaseUIView view = ManagerUtility.HotfixMgr.Instantiate<BaseUIView>(viewType.FullName);
        return view;
    }
}
```

---

## 最佳实践

1. **检查加载状态**: 使用 `Loaded` 属性检查热更 DLL 是否已加载
2. **异常处理**: 热更操作可能失败，做好 null 检查和异常处理
3. **类型名格式**: 使用完整类型名（包含命名空间）
4. **生命周期管理**: 热更域销毁后，之前创建的对象可能无法使用
5. **版本兼容**: 确保热更 DLL 与主工程版本兼容
