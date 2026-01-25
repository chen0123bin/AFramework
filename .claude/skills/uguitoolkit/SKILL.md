---
name: uguitoolkit
description: Unity UGUI 结构 JSON 生成工具箱。用于把界面层级、RectTransform 适配、交互组件配置固化为一个可存储的 UGUI 结构 JSON（严格遵循 assets/templates/UGUITempView.json 字段与枚举）。当用户提出“生成/保存 UGUI 结构 JSON”“按模板自动搭 UI JSON”“按 UGUITempView 规则生成 UI JSON”时使用。
---

# Unity UGUI JSON 生成（仅生成 JSON）

本技能只负责生成 UGUI 结构 JSON，不包含在 Unity 内自动创建 UI 的脚本。

## 快速开始
1. 给出 Root.name（也是文件名）与目标平台（PC/移动端/横屏/竖屏）。
2. 提供交互清单（按钮/输入框/滚动列表/弹窗等）与资源路径（未知可填 null 或空字符串）。
3. 输出一个 JSON：字段与枚举严格对齐 assets/templates/UGUITempView.json。

## 输入
- 页面名：Root.name（同时也是文件名）
- 目标平台：PC / 移动端 / 横屏 / 竖屏
- 交互清单：Button / Toggle / InputField / ScrollRect / Dropdown 等
- 资源信息：sprite / 字体 / 材质（未知可用 null 或空字符串）
- 主题偏好（可选）：用于默认颜色令牌映射

## 输出
- 仅输出一个 JSON，建议保存到 `Assets/UIJsonData/`，文件名与 Root.name 一致

## 生成工作流（推荐）
1. 先定层级：内容层（必选）/ 浮层（可选）/ 背景层（可选，谨慎）。
2. 把适配策略翻译成 RectTransform：anchorMin/anchorMax/pivot/sizeDelta/anchoredPosition。
3. 把交互翻译成组件 data：重点写“引用字段”与其子节点命名一致。
4. 套主题令牌（可选）：给 Image/Text/Selectable 填默认色与状态色。
5. 若用到自定义组件：先查 references/components/ 的组件说明，再写 type/data。

## 重要注意：全屏背景层的遮挡风险
Unity 应用/游戏除了 UI 往往还有 2D/3D 内容。如果在 UI 里放了不透明的全屏背景 Image，会直接遮挡后面的 2D/3D 画面。

建议策略：
- 默认不生成不透明全屏背景层；仅在“纯 UI 场景/登录页/独立界面”才使用。
- 必须使用全屏背景时：优先使用半透明（降低 alpha）。
- 需要遮罩效果时：使用 Overlay 语义的半透明遮罩，而不是实色背景。

## 默认约束（性能与稳定性）
- 除 ScrollRect 的 Content 节点外，默认不使用任何 Layout 组件
- 不引入持续循环装饰动效；交互反馈优先用 Selectable（ColorTint）状态
- 所有引用路径必须能在 children 层级中解析到目标节点

## 你该看哪些文件
- 规范模板（字段与枚举唯一依据）：assets/templates/UGUITempView.json
- JSON 结构与命名规范：references/spec/ugui-json-rules.md
- UI 主题令牌与交互状态建议：references/design/ui-pro-max-ugui.md
- 资源路径映射：references/spec/asset-path-mapping.md
- 组件引用字段（InputField/Dropdown/ScrollRect 等）：references/components/*.md

