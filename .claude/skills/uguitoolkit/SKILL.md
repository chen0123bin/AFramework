---
name: uguitoolkit
description: Unity UGUI 结构 JSON 生成工具箱。适用于把界面设计、布局适配、交互反馈落到一个可存储的 UGUI 结构 JSON（严格遵循 references/UGUITempView.json 规则），并在 Unity 中根据该 JSON 自动创建 UGUI 层级与组件（Canvas、RectTransform、Image/Text/Button/InputField/ScrollRect 等）。当用户提出“生成/保存 UGUI 结构 JSON”“按模板自动搭 UI  JSON”“按 UGUITempView 规则自动创建 UGUI JSON”的需求时使用。。
---

# Unity UGUI JSON 模板生成与自动搭建

## 快速导航

- 规则与自检：references/spec/ugui-json-rules.md
- UI 设计与主题令牌（含深色模式）：references/design/ui-pro-max-ugui.md
- 主题令牌查询命令：python .claude/skills/uguitoolkit/scripts/search_refs.py 主题 --list-themes
- 自定义组件查询命令：python .claude/skills/uguitoolkit/scripts/search_refs.py --list-components
- 资源路径映射：references/spec/asset-path-mapping.md
- 参考与索引：references/index.md

## 你要做什么

把“界面结构 + 布局适配 + 交互状态”固化成一个 UGUI 结构 JSON，并确保该 JSON 与 references/UGUITempView.json 完全一致（字段名、枚举字符串、引用路径、层级结构）。

## 输入

- 页面名（Root.name，同时也是文件名）
- 目标平台（PC/移动端/横屏/竖屏）
- 交互清单（按钮/输入框/滚动列表/弹窗等）
- 资源信息（sprite/字体/材质，未知可用 null 或空字符串）
- 主题偏好（影响默认颜色令牌）

## 输出

- 仅输出一个 JSON，默认保存到 `Assets/UIJsonData/`，文件名与 Root.name 一致

## 生成工作流（推荐）

1. 先给层级草图：背景层 / 内容层 / 浮层
2. 把适配策略翻译成 RectTransform：anchorMin/anchorMax/pivot/sizeDelta/anchoredPosition
3. 把交互翻译成组件 data：Button/Toggle/InputField/ScrollRect 的关键字段与引用路径
4. 选主题令牌（可选）：按 references/design/ui-pro-max-ugui.md 给 Image/Text/Selectable 套默认色与状态色
5. 若用到自定义组件：先查 references/components/*.md（或 *.json），按组件描述补齐 type/data
6. 做自检：按 references/spec/ugui-json-rules.md 的清单检查字段、引用路径、白名单

## 默认约束（性能与稳定性）

- 除 ScrollRect 的 Content 节点外，默认不使用任何 Layout 组件
- 不引入持续循环的装饰动效；交互反馈优先用 Selectable（ColorTint）状态
- 所有引用路径必须能在 children 层级中解析到目标节点

## 资源目录

- references/：存放规则、索引、示例与设计令牌
- assets/：存放 UI 示例图片


