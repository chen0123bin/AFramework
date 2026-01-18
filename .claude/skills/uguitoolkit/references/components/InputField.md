# InputField（输入框）

## 作用

InputField 是 Unity 内置的输入框组件，用于接收用户文本输入。

在 uguitoolkit 的 UGUI JSON 中，InputField 的关键在于：

- 必须通过引用字段绑定文本节点（textComponent）与占位节点（placeholder）
- 引用支持三种写法：
  - "."：指向本节点
  - "A/B/C"：相对路径（从 InputField 所在节点开始）
  - "NodeName"：按名字在子树中查找

## Unity 组件信息

- ComponentType：UnityEngine.UI.InputField
- BaseType：UnityEngine.UI.Selectable
- AddComponentMenu：UI/Input Field

## 在 UGUI JSON 中怎么用

在节点的 components 数组里，把 type 写成 InputField，并把配置写进 data：

```json
{
  "type": "InputField",
  "data": {
    "interactable": true,
    "textComponent": "TxtText",
    "placeholder": "TxtPlaceholder",
    "text": "",
    "characterLimit": 20,
    "contentType": "Standard",
    "lineType": "SingleLine",
    "caretBlinkRate": 0.85,
    "caretWidth": 1,
    "selectionColor": [0.659, 0.808, 1.0, 0.502],
    "readOnly": false,
    "navigation": {
      "mode": "Automatic"
    }
  }
}
```

典型层级（建议）：

- IpfUserName（挂 Image + InputField）
  - TxtPlaceholder（挂 Text，raycastTarget=false）
  - TxtText（挂 Text，raycastTarget=false）

## 重要属性（常用）

### 引用字段（最容易写错）

- textComponent（string）：输入文字使用的 Text 节点引用，必须能解析到 Text 组件
- placeholder（string）：占位内容引用，必须能解析到 Graphic（常用 Text 或 Image）

### 输入行为

- text（string）：初始文本
- characterLimit（int）：最大长度
- contentType（枚举字符串）：Standard / IntegerNumber / DecimalNumber / Alphanumeric / Name / EmailAddress / Password / Pin / Custom
- lineType（枚举字符串）：SingleLine / MultiLineSubmit / MultiLineNewline
- readOnly（bool）：只读

### 光标与选择

- caretBlinkRate（float）：光标闪烁频率
- caretWidth（int）：光标宽度
- selectionColor（float[4]）：选中范围颜色

## 常见问题

- 输入框能显示占位，但输入文字不显示：textComponent 没有指向带 Text 的节点（或节点名/路径写错）。
- 点击输入框没反应：背景 Image 的 raycastTarget=false，或被上层控件拦截。
- Placeholder 变成普通文字不消失：placeholder 未正确绑定到占位节点。
