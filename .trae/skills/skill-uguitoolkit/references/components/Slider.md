# Slider（滑动条）

## 作用

Slider 是 Unity 内置的滑动条组件，用于数值调节。

在 uguitoolkit 的 UGUI JSON 中，Slider 的关键在于：

- targetGraphic / fillRect / handleRect 是引用字段
- 引用支持三种写法：
  - "."：指向本节点
  - "A/B/C"：相对路径（从 Slider 所在节点开始）
  - "NodeName"：按名字在子树中查找

## Unity 组件信息

- ComponentType：UnityEngine.UI.Slider
- BaseType：UnityEngine.UI.Selectable

## 在 UGUI JSON 中怎么用

在节点的 components 数组里，把 type 写成 Slider，并把配置写进 data：

```json
{
  "type": "Slider",
  "data": {
    "interactable": true,
    "targetGraphic": "ImgHandle",
    "transition": "ColorTint",
    "fillRect": "ImgFill",
    "handleRect": "ImgHandle",
    "direction": "LeftToRight",
    "minValue": 0.0,
    "maxValue": 1.0,
    "wholeNumbers": false,
    "value": 0.078,
    "navigation": {
      "mode": "Automatic"
    }
  }
}
```

典型层级（建议）：

- SldProgress（挂 Slider）
  - ImgSldBackground（背景）
  - ImgFill（填充条，Image 填充模式）
  - ImgHandle（拖拽手柄）

## 默认 baseui 资源

- ImgSldBackground：assets/arts/baseui/进度条填充.png
- ImgFill：assets/arts/baseui/进度条背景.png
- ImgHandle：assets/arts/baseui/圆.png

## 重要属性（常用）

### 引用字段（最容易写错）

- targetGraphic（string）：目标 Graphic，常用手柄 Image
- fillRect（string）：填充条 RectTransform
- handleRect（string）：手柄 RectTransform

### 数值与方向

- direction（枚举字符串）：LeftToRight / RightToLeft / BottomToTop / TopToBottom
- minValue / maxValue（float）：数值范围
- wholeNumbers（bool）：是否仅整数
- value（float）：初始值

## 常见问题

- 拖拽无效：handleRect 未正确引用
- 填充不动：fillRect 未设置或 Image 不是 Filled
