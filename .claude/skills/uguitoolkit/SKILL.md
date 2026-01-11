---
name: uguitoolkit
description: Unity UGUI 结构 JSON 生成与自动搭建工具箱。适用于把界面设计、布局适配、交互反馈落到一个可存储的 UGUI 结构 JSON（严格遵循 references/UGUITempView.json 规则），并在 Unity 中根据该 JSON 自动创建 UGUI 层级与组件（Canvas、RectTransform、Image/Text/Button/InputField/ScrollRect 等）。当用户提出“生成/保存 UGUI 结构 JSON”“按模板自动搭 UI”“按 UGUITempView 规则自动创建 UGUI”的需求时使用。
---

# Unity UGUI JSON 模板生成与自动搭建

## 目标

把“界面设计 + 布局适配 + 交互反馈”落到一个标准 JSON 文件中，并能据此自动创建 UGUI 层级。

本技能最终产出通常是：

- 一个符合规则的 JSON（用于存储/版本化 UGUI 结构）
- 以及在 Unity 中从 JSON 自动生成界面的方法（脚本/流程）

## 参考规则

- 规范模板：references/UGUITempView.json
- 示例页面：references/LoadingView.json

生成 JSON 时，字段、层级、组件字段命名、枚举字符串都必须与参考文件保持一致。

## 输入与输出

输入（用户通常会给出其中一部分）：

- 页面名（例如 LoadingView、LoginView）
- 目标平台（PC/移动端/横屏/竖屏）
- 交互清单（按钮、输入框、滚动列表等）
- 美术资源路径（sprite/字体/材质，可先留空或 null）

输出：

- 仅输出一个 JSON（建议文件名与 Root.name 一致，例如 LoadingView.json）

## JSON 结构（UGUITempView 规则摘要）

顶层：

- Root：根节点对象

节点字段：

- name：节点名（同时体现组件语义与命名规范）
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
- data：该组件的序列化字段（字段名与枚举字符串遵循参考 JSON）

## 命名规范（必须）

- Txt：TxtTitle / TxtDesc / TxtPlaceholder / TxtText
- Img：ImgBg / ImgIcon / ImgBackground
- Btn：BtnClose / BtnSubmit
- Tgl：TglChoose
- Sld：SldProgress
- Ipf：IpfUsername / IpfPassword
- Dpd：DpdOption
- Pnl：PnlLeft / PnlRight

当某个组件需要引用子节点（例如 InputField.textComponent、Dropdown.template、ScrollRect.content），子节点名称必须与引用路径一致。

## 生成 JSON 的工作流

1. 拆解界面：先给出层级结构草图（背景层/内容层/浮层），再确定每个节点的 components.type。
2. 落地适配：把适配策略翻译成 rectTransform（锚点、pivot、sizeDelta、anchoredPosition）。
3. 落地交互：把交互需求翻译成组件 data（Button 颜色、InputField placeholder、ScrollRect 方向与引用路径等）。
4. 输出 JSON：严格按模板字段输出；资源路径不确定时用 null 或空字符串。
5. 自检：
   - 所有引用路径都能在 children 中找到
   - 所有 type 的 data 字段名与参考 JSON 完全一致
   - Root.name 与文件名一致


## 组件支持范围（与参考 JSON 对齐）

常用：Image、RawImage、Text、Button、Toggle、Slider、Scrollbar、InputField、Dropdown、ScrollRect、Mask

布局：VerticalLayoutGroup、HorizontalLayoutGroup、GridLayoutGroup、ContentSizeFitter、AspectRatioFitter、LayoutElement

约束：除 ScrollRect 的 Content 节点外，生成时不使用任何 Layout 组件。

交互/分组：CanvasGroup

## Layout 使用原则（性能优先）

本技能生成 UGUI JSON 时，默认以“性能可控、结构清晰”为第一优先级，Layout 组件使用越少越好。

references/UGUITempView.json 中出现的 Layout 组件字段仅用于字段名/枚举对齐，不代表推荐用法。

推荐规则：

- 默认不使用任何 Layout 组件（Vertical/Horizontal/Grid/ContentSizeFitter/AspectRatioFitter/LayoutElement）
- 唯一允许场景：ScrollRect 的 Content 节点用于动态列表，Content 可挂 VerticalLayoutGroup + ContentSizeFitter（verticalFit=PreferredSize）
- 除 ScrollRect Content 外的所有区域（查询条/表头/分页/弹窗/按钮组等）一律使用 RectTransform（锚点/sizeDelta/anchoredPosition）手动排版
- 需要“间距/留白”时，通过 sizeDelta/anchoredPosition 或增加空节点占位实现，不使用 Layout 组件

在输出 JSON 时的默认约束：

- 除 ScrollRect Content 外，不输出任何 Layout 组件

当用户要求新增组件类型时：

1. 先在 JSON 中定义 type 与 data 字段（字段名尽量与 Unity Inspector 属性一致）
2. 再在自动创建脚本里补齐该 type 的映射

## 资源目录

- references/：存放 JSON 规则与示例
- assets/：存放 UI 示例图片

## 资源路径映射（BaseUI）

为避免生成的组件找不到资源文件，本技能在输出 JSON 的 sprite 路径时，按以下规则做路径映射（统一使用 Unity 的 AssetDatabase 路径格式：以 Assets/ 开头，使用 / 作为分隔符）：

- 本技能内：assets/arts/baseui/...
- 你的 Unity 工程：Assets/Arts/BaseUI/...

映射规则：

- 当输入或示例出现以 assets/arts/baseui/ 开头的资源路径时，输出到 JSON 中一律替换为 Assets/Arts/BaseUI/
- 当输入或示例使用 Windows 分隔符（\）时，输出到 JSON 中一律转换为 /

- 示例：

- assets/arts/baseui/按钮.png → Assets/Arts/BaseUI/按钮.png
- assets\arts\baseui\边框.png → Assets/Arts/BaseUI/边框.png

UI 图片设计（canvas-design）：

- 当需要“面板背景/弹窗底图/按钮底图/列表条目底图”等静态图时，先用 canvas-design 生成一张 PNG，再落到 Image.sprite
- 资源自检：只要 JSON 中出现了非空的 sprite 路径，就必须能在本技能的 assets/arts/baseui/ 文件夹中找到同名图片；如果找不到，先用 canvas-design 生成该 PNG 并放入 assets/arts/baseui/，再继续输出/更新 JSON
- 本技能的 UI 示例图片在 assets/arts/baseui/ 目录下，可直接复用或作为风格参考
