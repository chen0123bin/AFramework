# UGUI JSON 结构参考（对齐 UGUITempView.json）

本参考用于帮助你在生成 UGUI JSON 时保持结构与字段名统一。

## JSON 结构摘要

顶层：

- Root：根节点对象

节点字段（白名单）：

- name：节点名
- active：可选，默认 true
- rectTransform：RectTransform 数据
- components：可选，组件数组
- children：可选，子节点数组

rectTransform 字段：

- anchorMin：[x, y]
- anchorMax：[x, y]
- pivot：[x, y]
- anchoredPosition：[x, y]
- sizeDelta：[w, h]
- rotation：[x, y, z]
- scale：[x, y, z]

components 结构：

- type：组件类型字符串（例如 Image/Button/Text/InputField/ScrollRect/...）
- data：该组件的序列化字段（字段名与枚举字符串严格跟随模板）

## 命名规范（必须）

- Txt：TxtTitle / TxtDesc / TxtPlaceholder / TxtText
- Img：ImgBg / ImgIcon / ImgBackground
- Btn：BtnClose / BtnSubmit
- Tgl：TglChoose
- Sld：SldProgress
- Ipf：IpfUsername / IpfPassword
- Dpd：DpdOption
- Pnl：PnlLeft / PnlRight

当组件需要引用子节点（例如 InputField.textComponent、Dropdown.template、ScrollRect.content），子节点 name 必须与引用路径一致。

## 组件支持范围（以模板为准）

常用：

- Image、RawImage、Text、Button、Toggle、Slider、Scrollbar、InputField、Dropdown、ScrollRect、Mask

布局：

- VerticalLayoutGroup、HorizontalLayoutGroup、GridLayoutGroup、ContentSizeFitter、AspectRatioFitter、LayoutElement

交互/分组：

- CanvasGroup

## Layout 使用原则（性能优先）

默认以“性能可控、结构清晰”为第一优先级，Layout 组件使用越少越好。

推荐规则：

- 默认不使用任何 Layout 组件（Vertical/Horizontal/Grid/ContentSizeFitter/AspectRatioFitter/LayoutElement）
- 唯一允许场景：ScrollRect 的 Content 节点用于动态列表
  - Content 可挂 VerticalLayoutGroup + ContentSizeFitter（verticalFit=PreferredSize）
  - 或 HorizontalLayoutGroup + ContentSizeFitter（horizontalFit=PreferredSize）
  - 或 GridLayoutGroup + ContentSizeFitter（verticalFit=PreferredSize, horizontalFit=PreferredSize）
- 除 ScrollRect Content 外的所有区域（查询条/表头/分页/弹窗/按钮组等）一律使用 RectTransform 手动排版
- 需要“间距/留白”时，通过 sizeDelta/anchoredPosition 或增加空节点占位实现，不使用 Layout 组件

## 新增组件类型的处理

当用户要求新增组件类型时：

1. 先在 JSON 中定义 type 与 data 字段（字段名尽量与 Unity Inspector 属性一致）
2. 再在 Unity 自动创建脚本里补齐该 type 的映射

