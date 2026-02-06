# 布局组件清单

本文件提供 UGUI 布局组件的简要说明，详细配置请参考 references/layouts/*.md。

---

## 布局组件分类

- **自动布局组**：VerticalLayoutGroup, HorizontalLayoutGroup, GridLayoutGroup
- **自适应组件**：ContentSizeFitter, AspectRatioFitter
- **布局元素**：LayoutElement

---

## 布局组件速查

### VerticalLayoutGroup（垂直布局）
- **作用**：按垂直方向自动排列子节点
- **关键属性**：padding, spacing, childAlignment, childControlWidth/Height, childForceExpandWidth/Height
- **使用场景**：垂直列表、表单布局
- **详细文档**：references/layouts/VerticalLayoutGroup.md

### HorizontalLayoutGroup（水平布局）
- **作用**：按水平方向自动排列子节点
- **关键属性**：padding, spacing, childAlignment, childControlWidth/Height, childForceExpandWidth/Height
- **使用场景**：水平按钮组、标签栏
- **详细文档**：references/layouts/HorizontalLayoutGroup.md

### GridLayoutGroup（网格布局）
- **作用**：按行列规则排列子节点
- **关键属性**：cellSize, spacing, startCorner, startAxis, constraint, constraintCount
- **使用场景**：网格列表、图标矩阵、物品栏
- **详细文档**：references/layouts/GridLayoutGroup.md

### ContentSizeFitter（内容自适应）
- **作用**：根据子内容调整自身大小
- **关键属性**：horizontalFit, verticalFit（Unconstrained/MinSize/PreferredSize）
- **使用场景**：ScrollRect 的 Content 节点、动态高度面板
- **详细文档**：references/layouts/ContentSizeFitter.md

### AspectRatioFitter（宽高比适配）
- **作用**：保持固定宽高比
- **关键属性**：aspectMode, aspectRatio
- **使用场景**：图片比例保持、视频播放器、头像框
- **详细文档**：references/layouts/AspectRatioFitter.md

### LayoutElement（布局元素）
- **作用**：为 LayoutGroup 提供尺寸与优先级信息
- **关键属性**：minWidth/Height, preferredWidth/Height, flexibleWidth/Height, layoutPriority
- **使用场景**：控制子项在布局中的尺寸行为
- **详细文档**：references/layouts/LayoutElement.md

---

## 使用边界与性能建议

### 性能优先原则
- **默认不使用任何 Layout 组件**，优先使用 RectTransform 手动排版
- 仅允许场景：**ScrollRect 的 Content 节点用于动态列表**
- 除 ScrollRect Content 外的所有区域，一律使用 RectTransform 手动排版

### 典型组合
- **动态列表**：ScrollRect + VerticalLayoutGroup + ContentSizeFitter
- **网格列表**：ScrollRect + GridLayoutGroup + ContentSizeFitter
- **弹性布局**：LayoutGroup + LayoutElement（控制子项尺寸优先级）

### 常见问题
- **尺寸抖动**：ContentSizeFitter 与 LayoutGroup 同时控制尺寸导致循环
- **布局不生效**：节点未包含可布局子节点或 LayoutElement 被忽略
- **布局溢出**：cellSize 与 spacing 超出容器可用空间
