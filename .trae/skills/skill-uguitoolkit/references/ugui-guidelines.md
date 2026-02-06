# UGUI JSON 设计规范

本文件整合 UGUI JSON 的结构规则与设计建议，用于生成结构正确且视觉统一的 UGUI JSON。

---

## 第一部分：结构规则（硬规则）

任何字段名、枚举字符串、组件 data 的细节，都以 assets/templates/UGUITempView.json 为唯一依据。

### 1) 节点结构（字段白名单）
- 顶层：Root
- 节点对象允许字段：name / active / rectTransform / components / children
  - active 可省略（默认 true）

### 2) RectTransform（必填字段）
rectTransform 必须包含以下字段：
- anchorMin：[x, y]
- anchorMax：[x, y]
- pivot：[x, y]
- anchoredPosition：[x, y]
- sizeDelta：[w, h]
- rotation：[x, y, z]
- scale：[x, y, z]

### 3) Components（通用结构）
- components 是数组
- 每一项包含：
  - type：组件类型字符串（例如 Image / Text / Button / InputField / ScrollRect 等）
  - data：该组件的序列化字段（字段名与枚举字符串必须跟随模板）

### 4) 命名规范（必须）
- Txt：TxtTitle / TxtDesc / TxtPlaceholder / TxtText
- Img：ImgBg / ImgIcon / ImgBackground
- Btn：BtnClose / BtnSubmit
- Tgl：TglChoose
- Sld：SldProgress
- Ipf：IpfUsername / IpfPassword
- Dpd：DpdOption
- Pnl：PnlLeft / PnlRight

### 5) 引用字段的可解析性（必须）
当组件需要引用子节点（例如 InputField.textComponent、Dropdown.template、ScrollRect.content）：
- 被引用的子节点 name 必须与引用路径一致
- 引用常见写法：
  - "."：指向本节点
  - "A/B/C"：相对路径（从组件所在节点开始）
  - "NodeName"：按名字在子树中查找

具体组件的引用字段与推荐层级，按需查看：references/components/*.md。

### 6) Layout 使用边界（性能优先）
- 默认不使用任何 Layout 组件
- 仅允许场景：ScrollRect 的 Content 节点用于动态列表
- 除 ScrollRect Content 外的所有区域，一律使用 RectTransform 手动排版
- 详细布局说明请参考：references/ugui-layouts.md

---

## 第二部分：主题与交互建议

本部分用于让你生成的 UGUI JSON 在"结构正确"之外，具备统一的颜色语义与一致的交互状态。

### 1) 快速用法
1. 先选 ThemeName（来自 theme.csv）。
2. 把 token 的 `#RRGGBB` + `*_A` 换算为 `[r,g,b,a]`（0~1）。
3. 按"组件语义"把 token 填到 Image/Text/Selectable。

### 2) theme.csv（主题令牌表）
theme.csv 每一行是一个主题：
- ThemeName：主题唯一标识
- ThemeDescription：主题说明
- Bg/Surface/Primary/CTA/TextPrimary/Border/Overlay 等：颜色 Hex（#RRGGBB）
- *_A：对应颜色 alpha（0~1）

### 3) 常用映射（token → 组件字段）
- 背景（可选，谨慎）：ImgBackground.Image.color → Bg
- 面板/卡片：Pnl*/Img*.Image.color → Surface / SurfaceAlt / SurfaceGlass*
- 主按钮（CTA）：Btn*.Image.color 与 Btn*.Button.colors.normalColor → CTA 或 Primary
- 辅助按钮：→ Secondary
- 主文本：Txt*.Text.color → TextPrimary
- 次文本：→ TextSecondary（或用 TextPrimary 降低 alpha）
- 边框：RoundedImage.borderColor / 其它边框色 → Border 或 BorderGlass
- 遮罩：ImgMask.Image.color → Overlay

### 4) 全屏背景层的遮挡与 Overdraw
Unity 的 UI 常与 2D/3D 同屏存在：
- 不透明全屏背景 Image 会遮挡后面的 2D/3D 内容。
- 全屏 Image 还会增加 Overdraw，尤其叠多层时。

建议策略：
- 背景层默认"可选"，优先不生成。
- 必须要背景时：优先半透明（降低 alpha），并设置 raycastTarget=false。
- 需要暗化/聚焦时：用 Overlay 语义的半透明遮罩，而不是实色背景。

### 5) 交互状态（Selectable / Button ColorTint）
建议统一状态策略（参考 status.csv 的 Selectable_ColorTint 组）：
- normal：基准色 alpha 0.85-1
- highlighted：基准色略提亮（或略提高 alpha）
- pressed：基准色略压暗（或略降低 alpha）
- selected：轻微提亮
- disabled：alpha 约 0.55，并降低饱和度或用 Border 近似




