# AspectRatioFitter（宽高比适配）

## 作用

AspectRatioFitter 是 Unity 内置的宽高比适配组件，用于保持子节点或自身的固定比例。

## Unity 组件信息

- ComponentType：UnityEngine.UI.AspectRatioFitter
- BaseType：UnityEngine.EventSystems.UIBehaviour

## 在 UGUI JSON 中怎么用

在节点的 components 数组里，把 type 写成 AspectRatioFitter，并把配置写进 data：

```json
{
  "type": "AspectRatioFitter",
  "data": {
    "aspectMode": "FitInParent",
    "aspectRatio": 1.7777778
  }
}
```

## 重要属性（常用）

- aspectMode（枚举字符串）：None / WidthControlsHeight / HeightControlsWidth / FitInParent / EnvelopeParent
- aspectRatio（float）：宽高比（宽/高）

## 常见问题

- 适配异常：RectTransform 锚点不合理导致显示区域被裁切
