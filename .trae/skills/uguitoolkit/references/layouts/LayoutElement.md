# LayoutElement（布局元素）

## 作用

LayoutElement 是 Unity 内置的布局元素组件，用于为 LayoutGroup 提供尺寸与优先级信息。

## Unity 组件信息

- ComponentType：UnityEngine.UI.LayoutElement
- BaseType：UnityEngine.EventSystems.UIBehaviour

## 在 UGUI JSON 中怎么用

在节点的 components 数组里，把 type 写成 LayoutElement，并把配置写进 data：

```json
{
  "type": "LayoutElement",
  "data": {
    "ignoreLayout": false,
    "minWidth": -1.0,
    "minHeight": -1.0,
    "preferredWidth": 108.0,
    "preferredHeight": 60.0,
    "flexibleWidth": -1.0,
    "flexibleHeight": -1.0,
    "layoutPriority": 1
  }
}
```

## 重要属性（常用）

- ignoreLayout（bool）：是否参与布局
- minWidth/minHeight（float）：最小尺寸
- preferredWidth/preferredHeight（float）：优选尺寸
- flexibleWidth/flexibleHeight（float）：弹性尺寸
- layoutPriority（int）：优先级

## 常见问题

- 尺寸不符合预期：preferred 与 flexible 组合冲突
