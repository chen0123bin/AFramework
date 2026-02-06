# Image（图片）

## 作用

Image 是 Unity 内置的图片组件，用于显示 Sprite，支持 Sliced、Filled 等常见图像渲染模式。

## Unity 组件信息

- ComponentType：UnityEngine.UI.Image
- BaseType：UnityEngine.UI.Graphic


## 在 UGUI JSON 中怎么用

在节点的 components 数组里，把 type 写成 Image，并把配置写进 data：

```json
{
  "type": "Image",
  "data": {
    "sprite": null,
    "color": [1.0, 1.0, 1.0, 1.0],
    "material": "Default UI Material",
    "raycastTarget": true,
    "imageType": "Sliced",
    "fillCenter": true,
    "pixelsPerUnitMultiplier": 1.0,
    "preserveAspect": false,
    "fillMethod": "Radial360",
    "fillAmount": 1.0,
    "fillClockwise": true,
    "fillOrigin": 0
  }
}
```

## 重要属性（常用）

- sprite（string|null）：Sprite 资源路径，可空
- color（float[4]）：RGBA（0~1）
- material（string）：材质路径或名称
- raycastTarget（bool）：是否接收点击
- imageType（枚举字符串）：Simple / Sliced / Tiled / Filled
- preserveAspect（bool）：保持原图比例

### Filled 相关

- fillMethod（枚举字符串）：Horizontal / Vertical / Radial90 / Radial180 / Radial360
- fillAmount（float）：填充比例（0~1）
- fillClockwise（bool）：顺时针填充
- fillOrigin（int）：起始边/角索引

## 常见问题

- 图片不显示：sprite 未设置或路径无效
- 填充无效：imageType 未设为 Filled
- 点击被穿透：raycastTarget 为 false
