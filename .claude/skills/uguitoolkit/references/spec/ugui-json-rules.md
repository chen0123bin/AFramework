# UGUI JSON 规则与自检（严格对齐 UGUITempView.json）

本规则用于确保你生成的 JSON 可以在 Unity 中被“按模板反序列化并创建 UGUI 层级”。所有字段名、枚举字符串、层级结构、引用路径，必须与 references/UGUITempView.json 完全一致。

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

## 自检清单（生成 JSON 时逐项对照）

本技能不执行命令行校验；只做“结构 + 字段 + 引用路径”层面的自检。

1. Root 结构
   - 顶层必须包含 Root
   - Root.name 必须存在且与文件名一致
   - Root.rectTransform 必须存在且包含所有 RectTransform 字段
   - Root.children 建议始终存在（可为空数组）

2. RectTransform 字段长度
   - anchorMin/anchorMax/pivot/anchoredPosition/sizeDelta：必须是长度为 2 的数组
   - rotation/scale：必须是长度为 3 的数组

3. 节点字段白名单
   - 每个节点只允许出现：name、active、rectTransform、components、children
   - active 可省略（默认 true）

4. 组件字段
   - components 中每一项必须包含 type 与 data
   - type 必须是模板中出现过的组件类型
   - data 字段名与枚举字符串必须与 UGUITempView.json 对齐：不新增字段、不漏字段、不随意改枚举字符串

5. 引用路径一致性（最容易出错）
   - ScrollRect.data.content / viewport / horizontalScrollbar / verticalScrollbar：引用路径必须能在 children 中找到
   - Toggle.data.targetGraphic / graphic / group：引用的节点/组件必须存在
   - InputField.data.textComponent / placeholder：引用的子节点必须存在
   - Dropdown.data.template / captionText / itemText：引用的子节点必须存在
   - 引用值若使用 A/B/C 形式，必须能按层级逐级找到

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

