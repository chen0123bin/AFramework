# Button（按钮）

## 作用

Button 是 Unity 内置的点击组件，用于触发交互行为。

## Unity 组件信息

- ComponentType：UnityEngine.UI.Button
- BaseType：UnityEngine.UI.Selectable

## 在 UGUI JSON 中怎么用

在节点的 components 数组里，把 type 写成 Button，并把配置写进 data：

```json
{
  "type": "Button",
  "data": {
    "interactable": true,
    "transition": "ColorTint",
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

## 重要属性（常用）

- interactable（bool）：是否可交互
- transition（枚举字符串）：ColorTint
- colors（object）：颜色状态集合
- navigation（object）：导航模式

## 常见问题

- 点击无反馈：节点缺少 Image 或 Image.raycastTarget=false
- 状态颜色不生效：transition 未设置为 ColorTint
