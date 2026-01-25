
# Unity UGUI 主题与交互建议（uguitoolkit）

本文件用于让你生成的 UGUI JSON 在“结构正确”之外，具备统一的颜色语义与一致的交互状态。

## 1) 快速用法
1. 先选 ThemeName（来自 theme.csv）。
2. 把 token 的 `#RRGGBB` + `*_A` 换算为 `[r,g,b,a]`（0~1）。
3. 按“组件语义”把 token 填到 Image/Text/Selectable。

## 2) theme.csv（主题令牌表）
theme.csv 每一行是一个主题：
- ThemeName：主题唯一标识
- ThemeDescription：主题说明
- Bg/Surface/Primary/CTA/TextPrimary/Border/Overlay 等：颜色 Hex（#RRGGBB）
- *_A：对应颜色 alpha（0~1）

## 3) 常用映射（token → 组件字段）
- 背景（可选，谨慎）：ImgBackground.Image.color → Bg
- 面板/卡片：Pnl*/Img*.Image.color → Surface / SurfaceAlt / SurfaceGlass*
- 主按钮（CTA）：Btn*.Image.color 与 Btn*.Button.colors.normalColor → CTA 或 Primary
- 辅助按钮：→ Secondary
- 主文本：Txt*.Text.color → TextPrimary
- 次文本：→ TextSecondary（或用 TextPrimary 降低 alpha）
- 边框：RoundedImage.borderColor / 其它边框色 → Border 或 BorderGlass
- 遮罩：ImgMask.Image.color → Overlay

## 4) 全屏背景层的遮挡与 Overdraw
Unity 的 UI 常与 2D/3D 同屏存在：
- 不透明全屏背景 Image 会遮挡后面的 2D/3D 内容。
- 全屏 Image 还会增加 Overdraw，尤其叠多层时。

建议策略：
- 背景层默认“可选”，优先不生成。
- 必须要背景时：优先半透明（降低 alpha），并设置 raycastTarget=false。
- 需要暗化/聚焦时：用 Overlay 语义的半透明遮罩，而不是实色背景。

## 5) 交互状态（Selectable / Button ColorTint）
建议统一状态策略（参考 status.csv 的 Selectable_ColorTint 组）：
- normal：基准色 alpha 0.85-1
- highlighted：基准色略提亮（或略提高 alpha）
- pressed：基准色略压暗（或略降低 alpha）
- selected：轻微提亮
- disabled：alpha 约 0.55，并降低饱和度或用 Border 近似

InputField 建议：
- 输入框底：Surface / SurfaceAlt
- Placeholder：TextPrimary + 降低 alpha
- Text：TextPrimary

