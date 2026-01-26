# ScrollRect（滚动容器）

## 作用

ScrollRect 是 Unity 内置的滚动容器组件，用于显示可滚动内容（列表、长文本、选项面板等）。

在 uguitoolkit 的 UGUI JSON 中，ScrollRect 的关键在于：

- content / viewport / scrollbar 都是引用字段，必须能在子层级里解析到对应节点
- 引用支持三种写法：
  - "."：指向本节点
  - "A/B/C"：相对路径（从 ScrollRect 所在节点开始）
  - "NodeName"：按名字在子树中查找

## Unity 组件信息

- ComponentType：UnityEngine.UI.ScrollRect
- BaseType：UnityEngine.EventSystems.UIBehaviour

## 在 UGUI JSON 中怎么用

在节点的 components 数组里，把 type 写成 ScrollRect，并把配置写进 data：

```json
{
  "type": "ScrollRect",
  "data": {
    "content": "Viewport/Content",
    "viewport": "Viewport",
    "horizontal": false,
    "vertical": true,
    "movementType": "Elastic",
    "elasticity": 0.1,
    "inertia": true,
    "decelerationRate": 0.135,
    "scrollSensitivity": 1.0,
    "horizontalScrollbar": null,
    "verticalScrollbar": "Scrollbar Vertical"
  }
}
```

典型层级（建议）：

- SrList（挂 Image + ScrollRect）
  - Viewport（挂 Image + Mask 或 RectMask2D）
    - Content（承载列表项；允许 LayoutGroup + ContentSizeFitter）
  - Scrollbar Vertical（挂 Scrollbar，可选）

## 重要属性（常用）

### 引用字段（最容易写错）

- content（string）：内容根节点 RectTransform 引用，通常为 "Viewport/Content"
- viewport（string）：视口 RectTransform 引用，通常为 "Viewport"
- horizontalScrollbar / verticalScrollbar（string|null）：Scrollbar 组件引用，可不填

### 滚动方向

- horizontal（bool）：水平滚动
- vertical（bool）：垂直滚动

### 滚动手感

- movementType（枚举字符串）：Unrestricted / Elastic / Clamped
- elasticity（float）：弹性回弹强度（Elastic 时有效）
- inertia（bool）：是否惯性
- decelerationRate（float）：惯性衰减
- scrollSensitivity（float）：滚动灵敏度

## 使用建议

- Viewport 建议开启 Mask（showMaskGraphic=false）或使用 RectMask2D，避免内容溢出显示。
- Viewport 的上Image透明度一定不能设为0，设置为0的话Content中的内容会不显示。
- Content 里如需自动布局，仅建议用于 ScrollRect.Content，其他区域尽量不用 Layout 组件。
- Content 的锚点通常设为顶部拉伸（anchorMin.y=1, anchorMax.y=1），配合 ContentSizeFitter 垂直 PreferredSize。
