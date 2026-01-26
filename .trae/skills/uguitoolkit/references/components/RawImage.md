# RawImage（原始图片）

## 作用

RawImage 是 Unity 内置的原始图片组件，用于显示 Texture。

## Unity 组件信息

- ComponentType：UnityEngine.UI.RawImage
- BaseType：UnityEngine.UI.MaskableGraphic

## 在 UGUI JSON 中怎么用

在节点的 components 数组里，把 type 写成 RawImage，并把配置写进 data：

```json
{
  "type": "RawImage",
  "data": {
    "texture": null,
    "uvRect": [0.0, 0.0, 1.0, 1.0],
    "color": [1.0, 1.0, 1.0, 1.0],
    "material": "Default UI Material",
    "raycastTarget": true
  }
}
```

## 重要属性（常用）

- texture（string|null）：Texture 资源路径，可空
- uvRect（float[4]）：UV 裁剪矩形 [x, y, w, h]
- color（float[4]）：RGBA（0~1）
- material（string）：材质路径或名称
- raycastTarget（bool）：是否接收点击

## 常见问题

- 贴图不显示：texture 未设置或路径无效
- 裁剪异常：uvRect 数值超出 0~1
