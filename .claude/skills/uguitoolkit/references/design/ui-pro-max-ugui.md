# UI Pro Max → Unity UGUI（可落地的设计与主题令牌）

本文件把 ui-ux-pro-max 的 UI 理念、深色模式配色与交互原则，转换成 Unity UGUI（Image/Text/Button/InputField/ScrollRect 等）可直接落地的做法与数值建议。

## 核心理念（UGUI 适配）

- 层级优先：用“背景 / 容器 / 内容 / 强调”的 4 层结构组织界面；不要用同一个颜色承担多个层级职责
- 玻璃质感要可见：浅色模式下透明度不能过低，避免“卡片消失”；深色模式下也必须保留边界（Border/描边/分隔）
- 对比度达标：正文文本与背景对比度至少 4.5:1；低优先级信息用颜色与字号双重降级
- 交互有反馈：所有可点区域都要有 Selectable 状态（highlight/pressed/disabled），并且能被键盘/手柄导航
- 动效克制：不使用持续循环装饰动效；需要动效时提供“减少动态”的开关（通过全局配置关闭 Tweens/Animator）
- 图标统一：不用 emoji 当图标；统一使用同一套 Sprite 风格与尺寸

推荐的 UGUI 结构：

- Root
  - ImgBackground（全屏背景）
  - PnlLayout（内容根容器）
    - PnlHeader（标题区，可固定）
    - PnlContent（ScrollRect 或静态内容）
    - PnlFooter（按钮区/提示区，可选）

## 主题令牌（浅色 / 深色）

本节使用 ui-ux-pro-max 的 Portfolio/Personal + Developer Tool/IDE 的倾向配色，并给出 UGUI 可直接填入 JSON 的 RGBA（0-1 浮点）。

浅色（Light）：

| 令牌 | Hex | RGBA |
|---|---|---|
| Background | #FAFAFA | [0.980392, 0.980392, 0.980392, 1.0] |
| Surface | #FFFFFF | [1.0, 1.0, 1.0, 1.0] |
| Text | #09090B | [0.035294, 0.035294, 0.043137, 1.0] |
| Muted | #3F3F46 | [0.247059, 0.247059, 0.274510, 1.0] |
| Border | #E4E4E7 | [0.894118, 0.894118, 0.905882, 1.0] |
| Primary（CTA） | #2563EB | [0.145098, 0.388235, 0.921569, 1.0] |

深色（Dark）：

| 令牌 | Hex | RGBA |
|---|---|---|
| Background | #09090B | [0.035294, 0.035294, 0.043137, 1.0] |
| Surface | #18181B | [0.094118, 0.094118, 0.105882, 1.0] |
| Text | #FAFAFA | [0.980392, 0.980392, 0.980392, 1.0] |
| Muted | #A1A1AA | [0.631373, 0.631373, 0.666667, 1.0] |
| Border | #3F3F46 | [0.247059, 0.247059, 0.274510, 1.0] |
| Primary（CTA） | #60A5FA | [0.376471, 0.647059, 0.980392, 1.0] |

状态色（两套主题都可复用）：

| 令牌 | Hex | RGBA |
|---|---|---|
| Success | #16A34A | [0.086275, 0.639216, 0.290196, 1.0] |
| Warning | #F59E0B | [0.960784, 0.619608, 0.043137, 1.0] |
| Danger | #EF4444 | [0.937255, 0.266667, 0.266667, 1.0] |

## 令牌到 UGUI 组件的映射

- ImgBackground（Image.color）：Background
- 面板/卡片（Image.color）：Surface + alpha
  - 浅色推荐 alpha 0.88～0.96（不要低于 0.75）
  - 深色推荐 alpha 0.55～0.78（但需要有边界）
- 分割线/描边（Image + Sliced 边框图 或 叠一层 Image）：Border（alpha 0.6～1.0）
- 标题（Text.color）：Text；正文（Text.color）：Muted
- 强调元素（按钮/标签/高亮 icon）：Primary 或状态色

## Selectable 交互状态（Button / Toggle / InputField）

UGUI 中“hover/pressed/disabled”优先用 Selectable（Button/Toggle/InputField）自带状态，不依赖脚本也能获得一致反馈（鼠标 + 手柄/键盘导航）。

通用建议：

- transition：ColorTint
- fadeDuration：0.10～0.18
- navigation.mode：Automatic（需要键盘/手柄时更稳）

主要按钮（Primary）建议：

- Image.color：Primary
- Button.colors：
  - normalColor：Primary
  - highlightedColor：Primary 稍亮（RGB + 0.04～0.08，最大不超过 1.0）
  - pressedColor：Primary 稍暗（RGB - 0.08～0.14，最小不低于 0.0）
  - disabledColor：Border（alpha 0.45～0.6）

次要按钮（Surface）建议：

- Image.color：Surface（alpha 0.8～0.95）
- Button.colors：
  - normalColor：Surface（alpha 0.8～0.95）
  - highlightedColor：Surface 更接近不透明（alpha + 0.05～0.10）
  - pressedColor：Surface 略暗（RGB - 0.03～0.08）
  - disabledColor：Border（alpha 0.35～0.55）

InputField 建议：

- 输入文本（InputField.textComponent 对应的 Text.color）：Text
- Placeholder（InputField.placeholder 对应的 Text.color）：Muted（alpha 0.7～0.85）
- 底（Image.color）：Surface（alpha 0.8～0.95），边界用 Border

Toggle 建议：

- targetGraphic（底）：Surface
- graphic（勾）：Primary 或 Success

## 字体与排版（Text）

UGUI（Text）落地时不绑定具体字体文件，只给可执行策略：

- 字体：标题与正文使用同一套无衬线（中文优先），避免混用过多字体
- 字号层级：标题 28～40（移动端可下调），小标题 20～24，正文 16～18，辅助信息 12～14
- 行距：lineSpacing 1.10～1.30；正文不要低于 1.10
- 对齐：标题常用 MiddleLeft；按钮文字常用 MiddleCenter

## Bento 卡片网格在 UGUI 的实现

推荐把 Bento Grid 放在 ScrollRect.content 内，并使用 GridLayoutGroup + ContentSizeFitter（只在 ScrollRect Content 上允许）：

- ScrollRect
  - Viewport（Mask）
    - Content（GridLayoutGroup + ContentSizeFitter）
      - CardItem（Image + Button + 子 Text/Icon）

CardItem 的视觉建议：

- 圆角：使用 9-sliced Sprite（Image.imageType=Sliced）实现稳定圆角
- 阴影：不建议大面积实时阴影；用轻阴影底图或加一层半透明 Image 模拟
- Hover：只做颜色/描边/阴影强度变化，不做 scale 抖动（避免布局跳动）

## 交付自检（UGUI 版本）

- 亮/暗主题都可读：正文 Text 与背景对比度达标，Muted 不“发灰看不清”
- 卡片在浅色不消失：Surface 的 alpha 不低于 0.75，并且有 Border/分隔
- 所有可点区域有状态：Button/Toggle/InputField 都有 ColorTint 状态（含 disabled）
- 导航可用：Selectable.navigation.mode 为 Automatic，重要按钮可被 Tab/手柄聚焦
- 不做布局抖动：Hover 不用缩放，避免 Grid/Layout 重新计算导致跳动

