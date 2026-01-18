# Dropdown（下拉选择）

## 作用

Dropdown 是 Unity 内置的下拉选择组件，用于从多个选项中选择一个。

在 uguitoolkit 的 UGUI JSON 中，Dropdown 的关键在于：

- template / captionText / itemText / targetGraphic 都是引用字段
- 引用支持三种写法：
  - "."：指向本节点
  - "A/B/C"：相对路径（从 Dropdown 所在节点开始）
  - "NodeName"：按名字在子树中查找

## Unity 组件信息

- ComponentType：UnityEngine.UI.Dropdown
- BaseType：UnityEngine.UI.Selectable
- AddComponentMenu：UI/Dropdown

## 在 UGUI JSON 中怎么用

在节点的 components 数组里，把 type 写成 Dropdown，并把配置写进 data：

```json
{
  "type": "Dropdown",
  "data": {
    "interactable": true,
    "targetGraphic": ".",
    "template": "Template",
    "captionText": "TxtCaption",
    "itemText": "Template/Viewport/Content/Item/TxtItemLabel",
    "value": 0,
    "options": [
      {"text": "Option A", "image": null},
      {"text": "Option B", "image": null}
    ],
    "navigation": {
      "mode": "Automatic"
    }
  }
}
```

典型层级（建议）：

- DpdOption（挂 Image + Dropdown）
  - TxtCaption（显示当前选择）
  - ImgArrow（下拉箭头）
  - Template（默认 inactive；承载下拉列表）
    - Viewport（Mask/RectMask2D）
      - Content（ScrollRect 内容）
        - Item（Toggle）
          - TxtItemLabel（选项文字）

## 重要属性（常用）

### 引用字段（最容易写错）

- targetGraphic（string）：Dropdown 的目标 Graphic，常用 "." 绑定到本节点的 Image
- template（string）：下拉列表根节点 RectTransform 引用，通常为 "Template"
- captionText（string）：显示当前选项的 Text 引用
- itemText（string）：下拉列表 Item 的 Text 引用，通常是 Template 下某个固定路径

### 数据

- value（int）：初始选中索引
- options（array）：选项数组
  - text（string）：选项文本
  - image（string|null）：选项图片 Sprite 路径（可空）

## 常见问题

- 点击后列表不弹出：template 引用写错，或 Template 节点未包含完整的 Viewport/Content/Item 结构。
- 列表弹出但没有文字：itemText 路径写错，或 Item 下没有 Text。
- 当前显示为空：captionText 没有绑定到 Text。
