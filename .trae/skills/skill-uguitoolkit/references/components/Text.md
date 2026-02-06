# Text（文字）

## 作用

Text 是 Unity 内置的文本组件，用于显示静态或动态文字。

## Unity 组件信息

- ComponentType：UnityEngine.UI.Text
- BaseType：UnityEngine.UI.Graphic

## 在 UGUI JSON 中怎么用

在节点的 components 数组里，把 type 写成 Text，并把配置写进 data：

```json
{
  "type": "Text",
  "data": {
    "content": "Item A",
    "font": "Library/unity default resources",
    "fontSize": 20,
    "fontStyle": "Normal",
    "color": [0.196, 0.196, 0.196, 1.0],
    "alignment": "MiddleCenter",
    "horizontalOverflow": "Wrap",
    "verticalOverflow": "Truncate",
    "raycastTarget": false,
    "supportRichText": true,
    "lineSpacing": 1.0
  }
}
```

## 重要属性（常用）

- content（string）：文本内容
- font（string）：字体资源路径或名称
- fontSize（int）：字号
- fontStyle（枚举字符串）：Normal / Bold / Italic / BoldAndItalic
- color（float[4]）：RGBA（0~1）
- alignment（枚举字符串）：如 MiddleCenter / MiddleLeft 等
- horizontalOverflow（枚举字符串）：Wrap / Overflow
- verticalOverflow（枚举字符串）：Truncate / Overflow
- raycastTarget（bool）：是否接收点击
- supportRichText（bool）：是否支持富文本
- lineSpacing（float）：行距倍数

## 常见问题

- 文字不显示：font 路径无效或颜色透明
- 文本被截断：verticalOverflow 为 Truncate
- 点击被挡：raycastTarget 为 true 且覆盖交互控件
