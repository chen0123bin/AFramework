# References 索引（uguitoolkit）

## 快速入口

- UI 设计与主题令牌（含深色模式）：design/ui-pro-max-ugui.md
- 主题令牌表：design/theme.csv
- 资源路径映射：spec/asset-path-mapping.md

## 关键文件（严格对齐）

- 规范模板：../assets/templates/UGUITempView.json
- 示例：../assets/templates/QuizView.json
- 示例：../assets/templates/LoadingView.json
- 示例：../assets/templates/LoginView.json

## 自定义组件（生成 JSON 时可用）

- 组件描述库：components/*.md（或 *.json）
- 自定义组件：components/RoundedImage.md

## 组件速查（常用）

| 组件 | 关键引用字段 | 常见层级要求 | 说明 |
|---|---|---|---|
| InputField | textComponent / placeholder | 子节点需包含 Text | components/InputField.md |
| ScrollRect | content / viewport / scrollbars | Viewport(Mask) + Content | components/ScrollRect.md |
| Dropdown | template / captionText / itemText / targetGraphic | Template(默认隐藏) | components/Dropdown.md |

## 关键词导航

| 关键词 | 去哪里看 |
|---|---|
| 深色模式 / 主题 / 令牌 / RGBA | design/ui-pro-max-ugui.md |
| Button 颜色状态 / ColorTint / navigation | design/ui-pro-max-ugui.md |
| 资源路径 / Assets/Arts/BaseUI | spec/asset-path-mapping.md |
| theme / 主题令牌表 / Primary / Border | design/theme.csv |

## 怎么搜

在 references/ 下用 IDE 全局搜索关键词（主题/令牌/引用路径/Button 等）。

自定义组件可直接查看：components/*.md（或 *.json）。
