
# Unity UGUI 主题与界面结构规范（uguitoolkit）

本文件用于规范 uguitoolkit 生成 UGUI JSON 时的：主题使用（theme.csv）、颜色令牌映射、界面结构分层、适配策略与交互状态。

目标：让生成的 UGUI JSON 不只是“结构正确”，还具备统一的颜色语义、清晰的信息层级与一致的交互反馈。

---

## 1. 主题与令牌

主题令牌统一存放在 [theme.csv](theme.csv)。每一行代表一个主题：

- ThemeName：主题唯一标识（建议在需求中直接指定）
- ThemeDescription：主题说明（用于理解配色意图）
- Bg/Surface/Primary/CTA/TextPrimary/Border/Overlay 等：颜色 Hex（#RRGGBB）
- *_A：对应颜色的 alpha（0~1）

### 1.1 选择主题

优先使用指定 ThemeName（避免“看起来差不多”的主题被混用）。

常见选择建议：

- 业务/工具/看板（浅色）：Light_Default_SaaS / Light_B2B_Service
- 健康/服务/养成（浅色）：Light_Beauty_Spa_Wellness_Service
- 游戏/娱乐（深色）：Dark_Gaming
- 科技/玻璃暗色：Dark_Tech_Glass / Dark_Fintech_Crypto



### 1.2 令牌 → UGUI JSON 映射

UGUI JSON 中颜色字段统一为 `[r,g,b,a]`（0~1）。从 theme.csv 读取 `#RRGGBB` 与对应 `*_A` 后，分别换算为 r/g/b 与 a。

按组件类型建议映射：

- 背景大图（ImgBackground.Image.color）→ `Bg`
- 卡片/面板（Pnl*/Img*.Image.color）→ `Surface` / `SurfaceAlt` / `SurfaceGlass*`
- 主要按钮底（Btn*.Image.color + Btn*.Button.colors.normalColor）→ `Primary` 或 `CTA`
- 辅助按钮底 → `Secondary`
- 主文本（Txt*.Text.color）→ `TextPrimary`
- 次文本/说明（Txt*.Text.color）→ `TextSecondary` 或在 `TextPrimary` 基础上降低 alpha
- 边框（Image.borderColor）→ `Border` 或 `BorderGlass`
- 弹窗遮罩（ImgMask.Image.color）→ `Overlay`

### 1.3 维护 theme.csv

- 新增主题时：复制一行现有主题，修改 ThemeName/ThemeDescription，再替换各 token 的 Hex 与 *_A
- 允许留空：某些 token 为空时，代表该主题不提供该 token 的建议值
- 避免复用：ThemeName 作为唯一键，建议保持稳定，不随意改名

---

## 2. 界面结构与适配规范

### 2.1 层级结构（建议三层）

- 背景层：ImgBackground（全屏 Image）
- 内容层：PnlLayout（承载主要布局与内容）
- 浮层：PnlPopup / PnlToast / PnlGuide（弹窗、提示、引导）

命名建议（与 uguitoolkit 模板保持一致）：

- 文字：TxtTitle / TxtDesc / TxtTip
- 图片：ImgBackground / ImgMask / ImgIcon
- 按钮：BtnConfirm / BtnCancel
- 面板：PnlLayout / PnlPopup

### 2.2 分辨率与适配

UGUI JSON 只描述 RectTransform，但你在设计时要遵守一致的“锚点策略”：

- 全屏背景：anchorMin=(0,0) anchorMax=(1,1) sizeDelta=(0,0)
- 顶部条：anchorMin=(0,1) anchorMax=(1,1) pivot=(0.5,1)
- 底部条：anchorMin=(0,0) anchorMax=(1,0) pivot=(0.5,0)
- 居中卡片/弹窗：anchorMin=anchorMax=(0.5,0.5)

### 2.3 Layout 组件使用边界

除 ScrollRect 的 Content 外，默认不使用 Layout 组件。

---

## 3. 组件落地建议（按主题令牌）

### 3.1 浅色服务型主题（示例：Light_Beauty_Spa_Wellness_Service）

核心要点：柔和底色 + 文字可读优先 + 层次分明但不过度装饰。

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
  - highlightedColor：在 normalColor 基础上提高亮度或提高 alpha（参考 status.csv 的状态倍率）
  - pressedColor：在 normalColor 基础上降低亮度
  - disabledColor：使用 `Border` 并降低 alpha

- 文本
  - 标题：TxtTitle（更粗/更大）
  - 正文：TxtDesc（以可读为准；1080p 画布建议 20~26 起）

### 3.2 深色玻璃/科技主题（示例：Dark_Tech_Glass）

核心要点：暗底 + 半透明 SurfaceGlass + 明确的文字对比度。

UGUI 约束：纯 UGUI 无真实背景模糊时，用“半透明 SurfaceGlass + BorderGlass + 高对比文字”模拟玻璃感。

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

---

## 4. 交互状态规范（Button / Toggle / InputField）

UGUI JSON 的 Button 使用 `ColorTint` 时，建议统一采用同一套状态策略（参考 [status.csv](status.csv) 的 `Selectable_ColorTint` 组）：

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

