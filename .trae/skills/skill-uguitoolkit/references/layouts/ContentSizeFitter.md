# ContentSizeFitter（内容自适应）

## 作用

ContentSizeFitter 是 Unity 内置的尺寸自适应组件，用于根据子内容调整自身大小。

## Unity 组件信息

- ComponentType：UnityEngine.UI.ContentSizeFitter
- BaseType：UnityEngine.EventSystems.UIBehaviour

## 在 UGUI JSON 中怎么用

在节点的 components 数组里，把 type 写成 ContentSizeFitter，并把配置写进 data：

```json
{
  "type": "ContentSizeFitter",
  "data": {
    "horizontalFit": "Unconstrained",
    "verticalFit": "PreferredSize"
  }
}
```

## 重要属性（常用）

- horizontalFit（枚举字符串）：Unconstrained / MinSize / PreferredSize
- verticalFit（枚举字符串）：Unconstrained / MinSize / PreferredSize

## 常见问题

- 尺寸抖动：与 LayoutGroup 同时控制尺寸导致循环
