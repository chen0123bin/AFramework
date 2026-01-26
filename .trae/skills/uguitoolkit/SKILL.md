---
name: uguitoolkit
description: Unity UGUI 结构 JSON 生成工具箱。用于把界面层级、RectTransform 适配、交互组件配置固化为可存储的 UGUI 结构 JSON（严格遵循 assets/templates/UGUITempView.json 字段与枚举）。当用户提出“生成/保存 UGUI 结构 JSON”“按模板自动搭 UI JSON”“按规则生成 UI JSON”“导出 UI JSON”时使用。
---

# Unity UGUI JSON 生成（仅生成 JSON）

仅生成 UGUI 结构 JSON，不生成 Unity 内自动创建 UI 的脚本。

## 五步法工作流（严格执行）
1. 确认主题色彩：确定 ThemeName 与颜色语义映射；需要时把颜色转换为 RGBA。
2. 确认界面布局：先定层级（内容层/浮层/背景层），再把适配策略翻译成 RectTransform。
3. 确认组件清单：逐个组件读取描述与结构要求，并列出必备素材与引用字段。
4. 确认重要事项与约束：性能边界、命名规范、引用可解析性、背景遮挡风险。
5. 生成 JSON：严格对齐 assets/templates/UGUITempView.json，并输出到用户指定文件夹。

## 输入
- 提供页面名：Root.name（同时也是文件名）
- 提供目标平台：PC / 移动端 / 横屏 / 竖屏
- 提供主题偏好：ThemeName 或色板基准色（缺省则不启用主题映射）
- 提供交互清单：Button / Toggle / InputField / ScrollRect / Dropdown 等
- 提供资源信息：sprite / 字体 / 材质（未知可用 null 或空字符串）
- 指定输出路径：用户指定目录（若未给出，默认 Assets/UIJsonData/）

## 输出
- 输出一个 JSON，文件名与 Root.name 一致，json文件中需记录"themeName": "记录使用的主题"

## 重要事项与约束
- 严格遵循模板字段与枚举：assets/templates/UGUITempView.json
- 控制背景遮挡风险：非纯 UI 场景默认不生成不透明全屏背景
- 按需使用 Layout 组件：仅在需要自动排版时启用，并先读取 references/layouts/*.md
- 保证引用可解析：所有引用路径必须能在 children 层级中解析到目标节点
- 交互反馈优先使用 Selectable（ColorTint），不使用持续循环装饰动效
- 使用 BaseUI 资源时按映射规则输出路径：见下方资源路径映射


## 组件与布局（渐进式读取）
- 仅在需要使用某个组件或布局时，读取对应描述文件
- 组件描述：references/components/*.md
- 布局描述：references/layouts/*.md

## 参考文件导航（按五步法）
- 第一步 主题色彩：references/design/ui-pro-max-ugui.md、references/design/theme.csv、references/design/status.csv
- 第二步 布局与命名：references/spec/ugui-json-rules.md（需要布局时再读 references/layouts/*.md）
- 第三步 组件清单与结构：需要组件时再读 references/components/*.md
- 第四步 重要事项与资源映射：SKILL.md 内的资源路径映射
- 第五步 规范模板：assets/templates/UGUITempView.json

### BaseUI 资源映射规则
- 示例资源路径：assets/arts/baseui/...
- 工程建议路径：Assets/Arts/BaseUI/...
- 映射规则：
  - 输入或示例出现以 assets/arts/baseui/ 开头的路径时，输出到 JSON 中一律替换为 Assets/Arts/BaseUI/
  - 输入或示例使用 Windows 分隔符（\）时，输出到 JSON 中一律转换为 /
- 示例：
  - assets/arts/baseui/白色图片.png → Assets/Arts/BaseUI/白色图片.png
  - assets\arts\baseui\下拉.png → Assets/Arts/BaseUI/下拉.png

