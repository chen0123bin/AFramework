---
name: skill-uguitoolkit
description: Unity UGUI 结构 JSON 生成工具箱。用于把界面层级、RectTransform 适配、交互组件配置固化为可存储的 UGUI 结构 JSON（严格遵循 assets/templates/UGUITempView.json 字段与枚举）。当用户提出"生成/保存 UGUI 结构 JSON""按模板自动搭 UI JSON""修改 UI JSON""按规则生成 UI JSON""导出 UI JSON"时使用。
---

# Unity UGUI JSON 生成工具箱

本 Skill 用于生成修改符合 UGUITempView.json 模板规范的 UGUI 结构 JSON 文件。

---

## 五步法工作流

### 第一步：确认界面布局与命名
**读取**：references/ugui-guidelines.md（第一部分）

- 确定层级结构：内容层 / 浮层 / 背景层
- 配置 RectTransform：anchorMin/Max, pivot, anchoredPosition, sizeDelta
- 遵循命名规范：Txt/Img/Btn/Tgl/Sld/Ipf/Dpd/Pnl 前缀

**需要布局时**：读取 references/ugui-layouts.md

---

### 第二步：确认主题色彩
**读取**：references/ugui-guidelines.md（第二部分）、references/design/theme.csv、references/design/status.csv

- 选择 ThemeName
- 颜色换算：#RRGGBB → [r,g,b,a]（0~1）
- 组件映射：Bg/Surface/Primary/CTA/TextPrimary → 对应组件字段

---

### 第三步：确认组件清单
**读取**：references/ugui-components.md

- 列出所需组件：Image, Text, Button, InputField, Toggle, Slider, ScrollRect, Dropdown 等
- 确认引用关系：textComponent, placeholder, template 等
- **需要详细配置时**：读取 references/components/*.md

---

### 第四步：确认约束与资源
**参考**：本文件"重要事项与约束"章节

- 性能边界：Layout 使用限制、背景遮挡风险
- 引用可解析性：所有引用路径必须能在 children 中解析
- 资源路径映射：BaseUI 资源路径转换规则

---

### 第五步：生成 JSON
**参考**：assets/templates/UGUITempView.json

- 严格对齐模板字段与枚举
- 输出到 Assets/UIJsonData/ 目录
- 文件名与 Root.name 一致
- 记录 themeName 字段

---

## 输入要求

| 输入项 | 说明 | 示例 |
|--------|------|------|
| 页面名 | Root.name，同时也是文件名 | LoginView |
| 目标平台 | PC / 移动端 / 横屏 / 竖屏 | 移动端竖屏 |
| 主题偏好 | ThemeName 或色板基准色 | Glassmorphism |
| 交互清单 | 所需组件列表 | Button, InputField |
| 资源信息 | sprite / 字体 / 材质 | 未知可用 null |
| 输出路径 | 默认 Assets/UIJsonData/ | 用户可指定 |

---

## 输出规范

- 输出单个 JSON 文件
- 文件名与 Root.name 一致
- 必须包含字段：`"themeName": "使用的主题名"`
- 严格遵循 UGUITempView.json 字段与枚举

---

## 重要事项与约束

### 1. 模板遵循
- 严格遵循：assets/templates/UGUITempView.json
- 字段名、枚举字符串必须与模板一致

### 2. 背景遮挡控制
- 非纯 UI 场景默认不生成不透明全屏背景
- 必须背景时：优先半透明 + raycastTarget=false
- 暗化/聚焦：使用 Overlay 语义半透明遮罩

### 3. Layout 使用边界
- **默认不使用任何 Layout 组件**
- 仅允许：ScrollRect 的 Content 节点用于动态列表
- 其他区域：一律使用 RectTransform 手动排版

### 4. 引用可解析性
- 所有引用路径必须能在 children 层级中解析到目标节点
- 支持写法：`.` / `A/B/C` / `NodeName`

### 5. 交互反馈
- 优先使用 Selectable（ColorTint）
- 不使用持续循环装饰动效

---

## 资源路径映射

### BaseUI 资源规则

| 输入路径 | 输出路径 |
|----------|----------|
| assets/arts/baseui/... | Assets/Arts/BaseUI/... |
| assets\arts\baseui\... | Assets/Arts/BaseUI/... |

**示例**：
- `assets/arts/baseui/白色图片.png` → `Assets/Arts/BaseUI/白色图片.png`
- `assets\arts\baseui\下拉.png` → `Assets/Arts/BaseUI/下拉.png`

---

## 参考文件导航

| 步骤 | 主要参考 | 详细参考 |
|------|----------|----------|
| 第一步 布局与命名 | ugui-guidelines.md（第一部分） | ugui-layouts.md |
| 第二步 主题色彩 | ugui-guidelines.md（第二部分） | theme.csv, status.csv |
| 第三步 组件清单 | ugui-components.md | components/*.md |
| 第四步 约束与资源 | SKILL.md（本章节） | - |
| 第五步 生成 JSON | UGUITempView.json | - |

---

## 渐进式读取说明

- **优先读取清单文件**：ugui-guidelines.md / ugui-components.md / ugui-layouts.md
- **按需读取详细文档**：components/*.md / layouts/*.md
- **避免一次性加载所有参考文件**
