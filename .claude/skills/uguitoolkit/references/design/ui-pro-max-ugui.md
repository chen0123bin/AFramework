
# UI Pro Max → Unity UGUI 迁移规范（设计 / 风格 / 颜色）

本文件用于把 ui-ux-pro-max 的“设计系统输出”（Pattern / Style / Colors / Typography / Effects / Anti-patterns）迁移到 uguitoolkit 的 UGUI JSON 生产流程中。

目标：让你在生成 UGUI JSON 时，不只是“结构正确”，还能具备统一的视觉语言、颜色语义与交互反馈。

---

## 1. 主题令牌（Theme Tokens）

主题令牌统一存放在 [theme-tokens.json](theme-tokens.json)。

### 1.1 选择主题

根据需要生成的内容自动推荐使用的主题（对应 ui-ux-pro-max 常见风格落地）
或者使用指定的主题（如Light_Wellness_SoftUI）。



### 1.2 Token → UGUI JSON 映射

UGUI JSON 中颜色字段都是 `[r,g,b,a]`（0~1）。使用 token 时按组件类型映射：

- 背景大图（ImgBackground.Image.color）→ `Bg`
- 卡片/面板（Pnl*/Img*.Image.color）→ `Surface` / `SurfaceAlt` / `SurfaceGlass*`
- 主要按钮底（Btn*.Image.color + Btn*.Button.colors.normalColor）→ `Primary` 或 `CTA`
- 辅助按钮底 → `Secondary`
- 主文本（Txt*.Text.color）→ `TextPrimary`
- 次文本/说明（Txt*.Text.color）→ `TextSecondary` 或在 `TextPrimary` 基础上降低 alpha
- 边框（Image.borderColor）→ `Border` 或 `BorderGlass`
- 弹窗遮罩（ImgMask.Image.color）→ `Overlay`

---

## 2. UGUI 设计规范（结构、适配、性能）

### 2.1 层级结构（建议三层）

- 背景层：ImgBackground（全屏 Image）
- 内容层：PnlLayout（承载主要布局与内容）
- 浮层：PnlPopup / PnlToast / PnlGuide（弹窗、提示、引导）

### 2.2 分辨率与适配

UGUI JSON 只描述 RectTransform，但你在设计时要遵守一致的“锚点策略”：

- 全屏背景：anchorMin=(0,0) anchorMax=(1,1) sizeDelta=(0,0)
- 顶部条：anchorMin=(0,1) anchorMax=(1,1) pivot=(0.5,1)
- 底部条：anchorMin=(0,0) anchorMax=(1,0) pivot=(0.5,0)
- 居中卡片/弹窗：anchorMin=anchorMax=(0.5,0.5)

### 2.3 Layout 组件使用边界

除 ScrollRect 的 Content 外，默认不使用 Layout 组件。具体规则以 [ugui-json-rules.md](../spec/ugui-json-rules.md) 为准。

---

## 3. 风格落地到 UGUI 组件（最常用的两套）

### 3.1 Soft UI Evolution（推荐用于：健康/美容/服务型界面）

核心要点：柔和底色 + 可读性优先 + 有层次但不过度拟物。

组件建议：

- 面板/卡片
  - Image.color：`Surface` 或 `SurfaceAlt`
  - Image.imageType：Sliced（配合圆角九宫格）
  - Image.cornerRadius：12~16（与模板字段一致）
  - Image.borderEnabled：true
  - Image.borderColor：`Border`
  - Image.borderThickness：1~2

- 主按钮（CTA）
  - Button.transition：ColorTint
  - Button.colors.normalColor：`CTA`
  - highlightedColor：在 normalColor 基础上提高亮度或提高 alpha（参考 theme-tokens.json 的状态倍率）
  - pressedColor：在 normalColor 基础上降低亮度
  - disabledColor：使用 `Border` 并降低 alpha

- 文本
  - 标题：TxtTitle（字体更粗/更大）
  - 正文：TxtDesc（16px 起步的“可读字号思路”迁移到 UGUI：在 1080p 画布建议 20~26）

示例主题（与 ui-ux-pro-max 的 Serenity Spa 设计系统一致）：

- Primary：#10B981
- CTA：#8B5CF6
- Background：#ECFDF5
- Text：#064E3B

### 3.2 Glassmorphism（推荐用于：金融/科技暗色界面）

核心要点：暗底 + 半透明卡片 + 明确的文字对比度。

UGUI 约束：纯 UGUI 无真实背景模糊时，用“半透明 Surface + 轻边框 + 亮点缀色”模拟玻璃感。

组件建议：

- 玻璃卡片
  - Image.color：`SurfaceGlass` 或 `SurfaceGlassStrong`
  - Image.borderEnabled：true
  - Image.borderColor：`BorderGlass`
  - Image.borderThickness：1
  - Image.cornerRadius：14~18

- 主文本与次文本
  - 主文本：`TextPrimary`（必须保证可读）
  - 次文本：`TextSecondary` 或降低 alpha

示例主题（与 ui-ux-pro-max 的 VaultX 设计系统一致）：

- Background：#0F172A
- Primary：#F59E0B
- CTA：#8B5CF6
- Text：#F8FAFC

---

## 4. 交互状态规范（Button / Toggle / InputField）

UGUI JSON 的 Button 使用 `ColorTint` 时，建议统一采用同一套状态策略（在 [theme-tokens.json](theme-tokens.json) 的 `status.Selectable_ColorTint`）：

- normal：基准色
- highlighted：基准色 * 1.08（或轻微提高 alpha）
- pressed：基准色 * 0.92（或轻微降低 alpha）
- selected：基准色 * 1.02
- disabled：alpha = 0.55（并降低饱和度/使用 Border 近似）

InputField 建议：

- 输入框底（ImgInputBg）：Surface/SurfaceAlt
- Placeholder：TextPrimary + mutedAlpha
- Text：TextPrimary

---

## 5. 自检要点（迁移 ui-ux-pro-max 规则到 UGUI）

- 浅色模式不要过透明：卡片/面板 alpha 太低会“看不见”
- 文本对比度优先：主文本与背景必须有明显区分
- hover/press 不要改变布局：只改色/透明度，不改 sizeDelta
- 交互元素都有反馈：Button/Toggle/InputField 需要高亮/按压/禁用态
