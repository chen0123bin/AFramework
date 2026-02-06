# GridLayoutGroup（网格布局）

## 作用

GridLayoutGroup 是 Unity 内置的网格布局组件，用于按行列规则排列子节点。

## Unity 组件信息

- ComponentType：UnityEngine.UI.GridLayoutGroup
- BaseType：UnityEngine.UI.LayoutGroup

## 在 UGUI JSON 中怎么用

在节点的 components 数组里，把 type 写成 GridLayoutGroup，并把配置写进 data：

```json
{
  "type": "GridLayoutGroup",
  "data": {
    "padding": {
      "left": 16,
      "right": 16,
      "top": 16,
      "bottom": 16
    },
    "childAlignment": "UpperLeft",
    "cellSize": [108.0, 60.0],
    "spacing": [10.0, 10.0],
    "startCorner": "UpperLeft",
    "startAxis": "Horizontal",
    "constraint": "FixedColumnCount",
    "constraintCount": 3
  }
}
```

## 重要属性（常用）

- padding（object）：内边距
- childAlignment（枚举字符串）：如 UpperLeft / MiddleCenter
- cellSize（float[2]）：单元格尺寸
- spacing（float[2]）：单元格间距
- startCorner（枚举字符串）：UpperLeft / UpperRight / LowerLeft / LowerRight
- startAxis（枚举字符串）：Horizontal / Vertical
- constraint（枚举字符串）：Flexible / FixedColumnCount / FixedRowCount
- constraintCount（int）：约束数量

## 常见问题

- 布局溢出：cellSize 与 spacing 超出容器可用空间
