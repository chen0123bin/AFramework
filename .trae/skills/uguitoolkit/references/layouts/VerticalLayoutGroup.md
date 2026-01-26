# VerticalLayoutGroup（垂直布局）

## 作用

VerticalLayoutGroup 是 Unity 内置的垂直布局组件，用于按垂直方向自动排列子节点。

## Unity 组件信息

- ComponentType：UnityEngine.UI.VerticalLayoutGroup
- BaseType：UnityEngine.UI.LayoutGroup

## 在 UGUI JSON 中怎么用

在节点的 components 数组里，把 type 写成 VerticalLayoutGroup，并把配置写进 data：

```json
{
  "type": "VerticalLayoutGroup",
  "data": {
    "padding": {
      "left": 0,
      "right": 0,
      "top": 0,
      "bottom": 0
    },
    "childAlignment": "UpperLeft",
    "spacing": 0.0,
    "childControlWidth": false,
    "childControlHeight": false,
    "childForceExpandWidth": true,
    "childForceExpandHeight": true,
    "childScaleWidth": false,
    "childScaleHeight": false,
    "reverseArrangement": false
  }
}
```

## 重要属性（常用）

- padding（object）：内边距
- childAlignment（枚举字符串）：如 UpperLeft / MiddleCenter
- spacing（float）：子节点间距
- childControlWidth/childControlHeight（bool）：布局控制子节点尺寸
- childForceExpandWidth/childForceExpandHeight（bool）：强制拉伸
- childScaleWidth/childScaleHeight（bool）：按子节点缩放
- reverseArrangement（bool）：反向排列

## 常见问题

- 子项高度异常：ContentSizeFitter 未配合或 LayoutElement 设置冲突
