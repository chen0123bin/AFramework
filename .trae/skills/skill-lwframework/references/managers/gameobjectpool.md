# GameObjectPool 对象池

## 概述

GameObjectPool 是 LWFramework 的对象池系统，用于高效管理 GameObject 的创建、借出、归还和清理，减少频繁的实例化和销毁带来的性能开销。

- **核心类**: `GameObjectPool<T>` 位于 `Assets/LWFramework/RunTime/Core/ObjectPool/`
- **接口**: `IPoolGameObject` 定义了池对象的生命周期回调
- **基类**: `PoolGameObject` 提供了默认实现

---

## 核心特性

- **泛型设计**: 支持自定义池对象类型
- **容量控制**: 可设置最大池容量，超出部分直接释放
- **生命周期回调**: 提供 `OnSpawn` 和 `OnUnSpawn` 回调
- **模板管理**: 支持指定模板对象和父节点

---

## API 参考

### 构造函数

```csharp
/// <summary>
/// 创建对象池
/// </summary>
/// <param name="poolMaxSize">最大池容量</param>
/// <param name="template">模板对象（会被 SetActive(false) 作为克隆源）</param>
/// <param name="parent">池对象父节点</param>
/// <param name="ownsTemplate">是否拥有模板（销毁时是否销毁模板）</param>
GameObjectPool(int poolMaxSize, GameObject template, Transform parent = null, bool ownsTemplate = false);
```

### 借出与归还

```csharp
/// <summary>
/// 从池中借出对象
/// </summary>
T Spawn();

/// <summary>
/// 归还对象到池中
/// </summary>
void Unspawn(T item);

/// <summary>
/// 检查对象是否在池中
/// </summary>
bool IsInPool(T item);
```

### 清理

```csharp
/// <summary>
/// 清空对象池
/// </summary>
/// <param name="includingSpawned">是否同时释放已借出的对象</param>
void Clear(bool includingSpawned = false);
```

---

## IPoolGameObject 接口

```csharp
public interface IPoolGameObject
{
    /// <summary>
    /// 关联的 GameObject
    /// </summary>
    GameObject Entity { get; }

    /// <summary>
    /// 从池中借出时调用
    /// </summary>
    void OnSpawn();

    /// <summary>
    /// 归还到池中时调用
    /// </summary>
    void OnUnSpawn();

    /// <summary>
    /// 释放时调用（池销毁或超出容量）
    /// </summary>
    void OnRelease();
}
```

---

## PoolGameObject 基类

```csharp
public abstract class PoolGameObject : IPoolGameObject
{
    protected GameObject m_Entity;
    
    public GameObject Entity => m_Entity;
    
    public virtual void OnSpawn()
    {
        if (m_Entity != null)
            m_Entity.SetActive(true);
    }
    
    public virtual void OnUnSpawn()
    {
        if (m_Entity != null)
            m_Entity.SetActive(false);
    }
    
    public virtual void OnRelease()
    {
        if (m_Entity != null)
            Object.Destroy(m_Entity);
    }
}
```

---

## 使用示例

### 基础对象池使用

```csharp
using LWCore;
using UnityEngine;

/// <summary>
/// 子弹池对象
/// </summary>
public sealed class BulletPoolItem : PoolGameObject
{
    /// <summary>
    /// 对象从池中借出时回调：做初始化/重置
    /// </summary>
    public override void OnSpawn()
    {
        base.OnSpawn();
        
        if (m_Entity == null)
            return;

        // 重置位置和旋转
        m_Entity.transform.localPosition = Vector3.zero;
        m_Entity.transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// 对象归还进池时回调：做清理/取消绑定
    /// </summary>
    public override void OnUnSpawn()
    {
        base.OnUnSpawn();
        // 清理逻辑
    }
}

/// <summary>
/// 对象池使用示例
/// </summary>
public class ObjectPoolExamples : MonoBehaviour
{
    [SerializeField] private GameObject m_BulletTemplate;
    [SerializeField] private Transform m_PoolRoot;

    private GameObjectPool<BulletPoolItem> m_BulletPool;

    /// <summary>
    /// 初始化对象池：template 会被 SetActive(false) 作为克隆源
    /// </summary>
    public void Initialize()
    {
        if (m_BulletTemplate == null)
        {
            return;
        }

        m_BulletPool = new GameObjectPool<BulletPoolItem>(
            poolMaxSize: 64, 
            template: m_BulletTemplate, 
            parent: m_PoolRoot, 
            ownsTemplate: false);
    }

    /// <summary>
    /// 借出对象：必要时会自动克隆 template
    /// </summary>
    public BulletPoolItem SpawnBullet()
    {
        if (m_BulletPool == null)
        {
            return null;
        }

        BulletPoolItem bullet = m_BulletPool.Spawn();
        return bullet;
    }

    /// <summary>
    /// 归还对象：超出容量会直接释放
    /// </summary>
    public void UnspawnBullet(BulletPoolItem bullet)
    {
        if (m_BulletPool == null || bullet == null)
        {
            return;
        }

        m_BulletPool.Unspawn(bullet);
    }

    /// <summary>
    /// 清空对象池：可选择是否释放"已借出"的对象
    /// </summary>
    public void ClearPool()
    {
        if (m_BulletPool == null)
        {
            return;
        }

        // 只清空池中未使用的对象
        m_BulletPool.Clear(includingSpawned: false);
        
        // 清空所有对象（包括已借出的）
        // m_BulletPool.Clear(includingSpawned: true);
    }
}
```

### 在 UI 中使用对象池

```csharp
using LWUI;
using LWCore;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

/// <summary>
/// 视图：通过 OpenView(data) 接收列表，使用对象池批量生成条目
/// </summary>
[UIViewData("Assets/0Res/Prefabs/UI/StepView.prefab", (int)FindType.Name, "LWFramework/Canvas/Normal")]
public class StepView : BaseUIView
{
    internal static readonly string EVENT_CLOSE = "StepView_Close";

    [UIElement("PnlLeft/PnlStepList/Viewport/Content/PnlStepItem")]
    private Transform m_PnlStepItem;
    [UIElement("BtnPrev")]
    private Button m_BtnPrev;
    [UIElement("BtnNext")]
    private Button m_BtnNext;
    [UIElement("BtnBack")]
    private Button m_BtnBack;

    private GameObjectPool<PnlStepItem> m_PnlStepItemPool;
    private List<PnlStepItem> m_ActiveItems = new List<PnlStepItem>();

    /// <summary>
    /// 创建视图：绑定按钮事件并初始化对象池
    /// </summary>
    public override void CreateView(GameObject gameObject)
    {
        base.CreateView(gameObject);

        m_BtnPrev.onClick.AddListener(() => { });
        m_BtnNext.onClick.AddListener(() => { });
        m_BtnBack.onClick.AddListener(() => { ManagerUtility.EventMgr.DispatchEvent(EVENT_CLOSE); });

        // 初始化对象池
        m_PnlStepItemPool = new GameObjectPool<PnlStepItem>(
            poolMaxSize: 5, 
            template: m_PnlStepItem.gameObject);
    }

    /// <summary>
    /// 打开视图：用 List<string> 作为数据源，批量借出条目并绑定回调
    /// </summary>
    public override void OpenView(object data = null)
    {
        base.OpenView(data);

        // 先归还之前的条目
        ClearItems();

        List<string> stepList = data as List<string>;
        if (stepList == null || stepList.Count == 0)
        {
            return;
        }

        for (int i = 0; i < stepList.Count; i++)
        {
            int index = i;
            PnlStepItem pnlStepItem = m_PnlStepItemPool.Spawn();
            pnlStepItem.StepIndex = (i + 1).ToString();
            pnlStepItem.StepTitle = stepList[i];
            pnlStepItem.OnClickStep = () => { Debug.Log($"点击了第{index + 1}个步骤"); };
            
            m_ActiveItems.Add(pnlStepItem);
        }
    }

    /// <summary>
    /// 关闭视图：归还所有条目
    /// </summary>
    public override void CloseView()
    {
        ClearItems();
        base.CloseView();
    }

    /// <summary>
    /// 归还所有激活的条目
    /// </summary>
    private void ClearItems()
    {
        foreach (var item in m_ActiveItems)
        {
            m_PnlStepItemPool.Unspawn(item);
        }
        m_ActiveItems.Clear();
    }
}

/// <summary>
/// 条目：继承 BaseUIItem，暴露 Action 给 View 绑定点击
/// </summary>
public class PnlStepItem : BaseUIItem
{
    [UIElement("TxtStepIndex")]
    private Text m_TxtStepIndex;
    [UIElement("TxtStepTitle")]
    private Text m_TxtStepTitle;

    private Button m_BtnStep;

    public string StepIndex
    {
        get { return m_TxtStepIndex.text; }
        set { m_TxtStepIndex.text = value; }
    }

    public string StepTitle
    {
        get { return m_TxtStepTitle.text; }
        set { m_TxtStepTitle.text = value; }
    }

    public Action OnClickStep;

    /// <summary>
    /// 创建条目：绑定点击回调
    /// </summary>
    public override void Create(GameObject gameObject)
    {
        base.Create(gameObject);
        m_BtnStep = gameObject.GetComponent<Button>();
        m_BtnStep.onClick.AddListener(() => { OnClickStep?.Invoke(); });
    }

    /// <summary>
    /// 归还到池：按需做清理
    /// </summary>
    public override void OnUnSpawn()
    {
        base.OnUnSpawn();
        OnClickStep = null;
    }

    /// <summary>
    /// 释放条目：按需做资源释放
    /// </summary>
    public override void OnRelease()
    {
        base.OnRelease();
    }
}
```

### 自定义池对象

```csharp
using LWCore;
using UnityEngine;

/// <summary>
/// 自定义池对象示例：带数据的池对象
/// </summary>
public class EnemyPoolItem : PoolGameObject
{
    public int EnemyId { get; set; }
    public int HP { get; set; }
    public int MaxHP { get; set; }

    public override void OnSpawn()
    {
        base.OnSpawn();
        
        // 重置敌人状态
        HP = MaxHP;
        
        // 重置位置
        if (Entity != null)
        {
            Entity.transform.localPosition = Vector3.zero;
            Entity.transform.localRotation = Quaternion.identity;
        }
    }

    public override void OnUnSpawn()
    {
        base.OnUnSpawn();
        
        // 清理敌人状态
        EnemyId = 0;
    }

    public void TakeDamage(int damage)
    {
        HP -= damage;
        if (HP <= 0)
        {
            // 敌人死亡，归还到池
        }
    }
}

/// <summary>
/// 敌人管理器
/// </summary>
public class EnemyManager : MonoBehaviour
{
    [SerializeField] private GameObject m_EnemyTemplate;
    [SerializeField] private Transform m_EnemyRoot;

    private GameObjectPool<EnemyPoolItem> m_EnemyPool;

    private void Start()
    {
        m_EnemyPool = new GameObjectPool<EnemyPoolItem>(
            poolMaxSize: 32,
            template: m_EnemyTemplate,
            parent: m_EnemyRoot,
            ownsTemplate: false);
    }

    public EnemyPoolItem SpawnEnemy(int enemyId, int maxHP)
    {
        EnemyPoolItem enemy = m_EnemyPool.Spawn();
        enemy.EnemyId = enemyId;
        enemy.MaxHP = maxHP;
        enemy.HP = maxHP;
        return enemy;
    }

    public void DespawnEnemy(EnemyPoolItem enemy)
    {
        m_EnemyPool.Unspawn(enemy);
    }
}
```

---

## 最佳实践

1. **合理设置容量**: 根据实际需求设置 `poolMaxSize`，避免内存浪费
2. **正确实现回调**: 在 `OnSpawn` 中重置对象状态，在 `OnUnSpawn` 中清理
3. **管理借出对象**: 跟踪已借出的对象，确保正确归还
4. **选择 ownsTemplate**: 如果模板对象由其他系统管理，设置 `ownsTemplate: false`
5. **及时清空**: 场景切换时及时清空对象池，避免内存泄漏
