# Toggle（开关）

## 作用

Toggle 是 Unity 内置的选择开关组件，用于二选一或多选。

在 uguitoolkit 的 UGUI JSON 中，Toggle 的关键在于：

- targetGraphic / graphic / group 是引用字段
- 引用支持三种写法：
  - "."：指向本节点
  - "A/B/C"：相对路径（从 Toggle 所在节点开始）
  - "NodeName"：按名字在子树中查找

## Unity 组件信息

- ComponentType：UnityEngine.UI.Toggle
- BaseType：UnityEngine.UI.Selectable

## 在 UGUI JSON 中怎么用

在节点的 components 数组里，把 type 写成 Toggle，并把配置写进 data：

```json
{
  "type": "Toggle",
  "data": {
    "interactable": true,
    "targetGraphic": "ImgBackground",
    "transition": "ColorTint",
    "isOn": false,
    "toggleTransition": "Fade",
    "graphic": "ImgCheckmark",
    "group": null,
    "colors": {
      "normalColor": [1.0, 1.0, 1.0, 1.0],
      "highlightedColor": [0.961, 0.961, 0.961, 1.0],
      "pressedColor": [0.784, 0.784, 0.784, 1.0],
      "selectedColor": [0.961, 0.961, 0.961, 1.0],
      "disabledColor": [0.784, 0.784, 0.784, 0.502],
      "colorMultiplier": 1.0,
      "fadeDuration": 0.1
    },
    "navigation": {
      "mode": "Automatic"
    }
  }
}
```

典型层级（建议）：

- TglAccept（挂 Image + Toggle）
  - ImgBackground（背景）
  - ImgCheckmark（勾选图标）
  - TxtLabel（可选文字）

## 默认 baseui 资源

- ImgCheckmark：assets/arts/baseui/勾选.png

## 重要属性（常用）

### 引用字段（最容易写错）

- targetGraphic（string）：Toggle 的目标 Graphic
- graphic（string）：勾选图标 Graphic
- group（string|null）：ToggleGroup 组件引用

### 状态与动画

- isOn（bool）：默认勾选状态
- toggleTransition（枚举字符串）：Fade

## 常见问题

- 点击无反应：targetGraphic/graphic 引用路径写错
- 勾选图标不显示：graphic 未指向有 Image 的节点
