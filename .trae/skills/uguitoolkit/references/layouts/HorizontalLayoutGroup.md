# HorizontalLayoutGroup（水平布局）

## 作用

HorizontalLayoutGroup 是 Unity 内置的水平布局组件，用于按水平方向自动排列子节点。

## Unity 组件信息

- ComponentType：UnityEngine.UI.HorizontalLayoutGroup
- BaseType：UnityEngine.UI.LayoutGroup

## 在 UGUI JSON 中怎么用

在节点的 components 数组里，把 type 写成 HorizontalLayoutGroup，并把配置写进 data：

```json
{
  "type": "HorizontalLayoutGroup",
  "data": {
    "padding": {
      "left": 16,
      "right": 16,
      "top": 16,
      "bottom": 16
    },
    "childAlignment": "MiddleCenter",
    "spacing": 12.0,
    "childControlWidth": true,
    "childControlHeight": true,
    "childForceExpandWidth": false,
    "childForceExpandHeight": false,
    "childScaleWidth": false,
    "childScaleHeight": false,
    "reverseArrangement": false
  }
}
```

## 重要属性（常用）

- padding（object）：内边距
- childAlignment（枚举字符串）：如 MiddleCenter / UpperLeft
- spacing（float）：子节点间距
- childControlWidth/childControlHeight（bool）：布局控制子节点尺寸
- childForceExpandWidth/childForceExpandHeight（bool）：强制拉伸
- childScaleWidth/childScaleHeight（bool）：按子节点缩放
- reverseArrangement（bool）：反向排列

## 常见问题

- 排列不生效：节点未包含可布局子节点或 LayoutElement 被忽略
