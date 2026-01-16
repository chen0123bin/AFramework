# References 索引（uguitoolkit）

## 快速入口

- 规则与自检：spec/ugui-json-rules.md
- UI 设计与主题令牌（含深色模式）：design/ui-pro-max-ugui.md
- 主题令牌 JSON：design/theme-tokens.json
- 资源路径映射：spec/asset-path-mapping.md

## 关键文件（严格对齐）

- 规范模板：templates/UGUITempView.json
- 示例：templates/QuizView.json
- 示例：templates/LoadingView.json
- 示例：templates/LoginView.json

## 自定义组件（生成 JSON 时可用）

- 组件描述库：components/*.md（或 *.json）
- 示例组件：components/RoundedImage.md

## 关键词导航

| 关键词 | 去哪里看 |
|---|---|
| Root / RectTransform / 白名单 | spec/ugui-json-rules.md |
| 引用路径 / ScrollRect.content / InputField.placeholder | spec/ugui-json-rules.md |
| Layout 组件使用边界 | spec/ugui-json-rules.md |
| 深色模式 / 主题 / 令牌 / RGBA | design/ui-pro-max-ugui.md |
| Button 颜色状态 / ColorTint / navigation | design/ui-pro-max-ugui.md |
| 资源路径 / Assets/Arts/BaseUI | spec/asset-path-mapping.md |
| theme-tokens / 主题令牌 JSON / Primary / Border | design/theme-tokens.json |

## 怎么搜

在 references/ 下搜索关键词：

- python .claude/skills/uguitoolkit/scripts/search_refs.py 主题
- python .claude/skills/uguitoolkit/scripts/search_refs.py 引用路径
- python .claude/skills/uguitoolkit/scripts/search_refs.py Button

列出/查询自定义组件：

- python .claude/skills/uguitoolkit/scripts/search_refs.py --list-components
- python .claude/skills/uguitoolkit/scripts/search_refs.py --show-component RoundedImage
