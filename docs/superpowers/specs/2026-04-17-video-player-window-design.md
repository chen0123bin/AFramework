# 设计文档：横屏视频播放器窗口 JSON 与独立控制脚本

> 版本: v1.0-design | 创建时间: 2026-04-17 15:55:54 | 状态: pending-review | 时间格式: `yyyy-MM-dd HH:mm:ss`

---

## 1. 项目概述

### 1.1 背景

当前项目已经具备基于 UGUI JSON 构建界面的能力，也存在 `BaseUIView`、`UIElement` 等标准 UI 绑定方式。用户本次希望基于 [`$skill-uguitoolkit`](D:/UnityProject/AFramework/.codex/skills/skill-uguitoolkit/SKILL.md) 生成一个可在 Unity 中直接使用的横屏视频播放器窗口 JSON，并额外提供一个单独的独立控制脚本。

该窗口需要覆盖以下核心交互：

- 播放
- 暂停
- 当前时间显示
- 总时长显示
- 进度条显示与拖动控制进度
- 放大
- 缩小
- Canvas 内全屏化
- 关闭窗口

同时，控制脚本需要直接驱动 `UnityEngine.Video.VideoPlayer`，并同时支持 `VideoClip` 与 `URL` 两类视频源，还要向外暴露可直接调用的公开函数。

### 1.2 目标

- 生成一个符合现有 UGUI JSON 模板规范的横屏视频播放器窗口 JSON。
- 窗口采用“横屏标准弹窗”方案，适合在 Unity UGUI 的 `Canvas` 中使用。
- 提供一个单独的独立控制脚本，负责驱动 `VideoPlayer` 与窗口内所有交互控件。
- 支持 `VideoClip` 与 `URL` 两种视频源设置方式。
- 支持外部代码直接调用公开函数控制播放器。
- 支持通过进度条拖动来控制视频跳转。
- 时间显示统一使用 `hh:mm:ss`。

### 1.3 非目标

- 不实现系统层面的真正全屏切换。
- 不实现播放列表、上一集、下一集、循环列表、倍速、音量控制、静音控制。
- 不额外引入事件转发层、服务层或第二个辅助脚本。
- 不在 JSON 中直接序列化 `VideoPlayer` 组件。
- 不实现与业务系统强耦合的事件协议。

---

## 2. 用户确认结论

### 2.1 已确认需求

| 轮次 | 维度 | 用户选择 | 结论 |
|------|------|---------|------|
| R1 | 放大/缩小含义 | 播放器窗口尺寸切换 / 全屏化 | 不做视频画面倍率缩放 |
| R2 | 控制脚本职责 | 方案 A | 脚本直接驱动 `UnityEngine.Video.VideoPlayer` |
| R3 | 窗口方向 | 横屏 | 采用横屏 16:9 取向 |
| R4 | 窗口方案 | A | 采用横屏标准弹窗 |
| R5 | 全屏定义 | Canvas 内全屏 | 通过 UGUI 节点尺寸与锚点切换实现 |
| R6 | 视频源 | 两种都支持 | 同时支持 `VideoClip` 与 `URL` |
| R7 | 外部接口 | 需要 | 控制脚本必须暴露公开调用函数 |
| R8 | 进度条行为 | 需要可控制进度 | 支持拖动跳转 |
| R9 | 时间格式 | `hh:mm:ss` | 所有时间统一使用 `hh:mm:ss` |

### 2.2 方案比较结论

| 方案 | 描述 | 结果 | 说明 |
|------|------|------|------|
| 方案 1 | 单独控制脚本直接绑定并驱动窗口内全部控件与 `VideoPlayer` | 采纳 | 最小实现、最贴合本次需求 |
| 方案 2 | `View` 与播放器控制器拆分 | 否决 | 会增加额外层级和复杂度 |
| 方案 3 | 继承 `BaseUIView` 并混合框架耦合逻辑 | 否决 | 与“独立控制脚本”目标不完全一致 |

---

## 3. 输出物范围

本次设计的最终输出物限定为以下两项：

1. `Assets/UIJsonData/VideoPlayerWindowView.json`
   - 用于生成横屏标准弹窗形式的视频播放器 UGUI 结构。

2. `Assets/Scripts/UI/VideoPlayerWindowController.cs`
   - 用于驱动 `VideoPlayer`、`RenderTexture`、`RawImage`、按钮、文本与进度条。
   - 生成代码时必须添加函数级中文注释。

---

## 4. UI JSON 结构设计

### 4.1 页面命名

- Root.name: `VideoPlayerWindowView`
- 文件名: `VideoPlayerWindowView.json`
- themeName: `Dark_Developer_Tool_IDE`

### 4.2 布局原则

- 根节点拉伸铺满全屏，仅作为容器。
- 默认不生成不透明全屏背景，避免遮挡非 UI 场景内容。
- 主窗口为居中的横屏标准弹窗。
- 视频显示区域采用标准横屏区域，优先保证视频画面的宽度。
- 不使用 Layout 组件，全部使用手动 `RectTransform` 排布。

### 4.3 主要层级

建议层级如下：

```text
Root
└─ PnlWindow
   ├─ ImgWindowBg
   ├─ PnlTop
   │  ├─ TxtTitle
   │  ├─ BtnZoom
   │  │  └─ TxtZoom
   │  ├─ BtnFullscreen
   │  │  └─ TxtFullscreen
   │  └─ BtnClose
   │     └─ TxtClose
   ├─ PnlVideo
   │  ├─ RImgVideo
   │  └─ TxtEmptyHint
   └─ PnlBottom
      ├─ BtnPlay
      │  └─ TxtPlay
      ├─ BtnPause
      │  └─ TxtPause
      ├─ TxtCurrentTime
      ├─ SldProgress
      │  ├─ ImgBackground
      │  ├─ ImgFill
      │  └─ ImgHandle
      ├─ TxtDuration
      └─ BtnShrink
         └─ TxtShrink
```

### 4.4 节点职责

#### Root

- 满屏容器节点。
- 不承担播放器逻辑。

#### PnlWindow

- 播放器主窗口容器。
- 默认状态下为固定尺寸横屏弹窗。
- 脚本负责切换其普通态、放大态、Canvas 全屏态。

#### PnlTop

- 顶部标题栏。
- 放置标题、放大、Canvas 全屏、关闭按钮。

#### PnlVideo

- 视频显示区域。
- `RImgVideo` 为 `RawImage`，显示 `RenderTexture` 输出。
- `TxtEmptyHint` 用于未设置片源时提示，准备播放后可隐藏。

#### PnlBottom

- 底部控制栏。
- 放置播放、暂停、当前时间、进度条、总时长、缩小按钮。

#### SldProgress

- 使用标准 `Slider` 结构。
- 既用于展示播放进度，也用于响应用户拖动跳转。

### 4.5 关键控件命名

| 控件 | 类型 | 作用 |
|------|------|------|
| `TxtTitle` | Text | 显示窗口标题或视频标题 |
| `BtnPlay` | Button | 开始播放 |
| `BtnPause` | Button | 暂停播放 |
| `TxtCurrentTime` | Text | 显示当前播放时间 |
| `TxtDuration` | Text | 显示总时长 |
| `SldProgress` | Slider | 显示并控制播放进度 |
| `BtnZoom` | Button | 进入放大态 |
| `BtnShrink` | Button | 从放大态或全屏态恢复 |
| `BtnFullscreen` | Button | 进入 Canvas 内全屏 |
| `BtnClose` | Button | 关闭窗口 |
| `RImgVideo` | RawImage | 显示视频画面 |
| `TxtEmptyHint` | Text | 无片源提示 |

---

## 5. 控制脚本设计

### 5.1 脚本定位

脚本名：`VideoPlayerWindowController`

设计定位：

- 单脚本独立控制器。
- 直接负责播放器逻辑与窗口交互。
- 不再拆分辅助控制脚本。
- 可被外部代码直接持有与调用。

### 5.2 核心依赖

脚本运行时依赖以下组件或对象：

- `UnityEngine.Video.VideoPlayer`
- `RenderTexture`
- `RawImage`
- `Button`
- `Slider`
- `Text`
- `RectTransform`
- `Canvas`

### 5.3 控件绑定方式

脚本不依赖现有 `UIElementAttribute` 框架做绑定，优先使用序列化字段直接拖拽绑定，保持“独立控制脚本”定位清晰。

建议序列化字段包括：

- `RectTransform m_WindowRect`
- `Text m_TxtTitle`
- `RawImage m_RImgVideo`
- `Text m_TxtEmptyHint`
- `Button m_BtnPlay`
- `Button m_BtnPause`
- `Button m_BtnZoom`
- `Button m_BtnShrink`
- `Button m_BtnFullscreen`
- `Button m_BtnClose`
- `Slider m_SldProgress`
- `Text m_TxtCurrentTime`
- `Text m_TxtDuration`
- `VideoPlayer m_VideoPlayer`

### 5.4 运行时补齐

若 `m_VideoPlayer` 未手动挂载，则脚本可在自身节点上尝试自动获取或添加 `VideoPlayer`，以降低使用门槛。

`RenderTexture` 由脚本在运行时创建并赋给：

- `VideoPlayer.targetTexture`
- `RawImage.texture`

---

## 6. 公开接口设计

### 6.1 视频源设置接口

- `SetVideoClip(VideoClip clip, bool autoPlay = false)`
  - 设置 `VideoClip` 片源。
  - 可选是否自动播放。

- `SetVideoUrl(string url, bool autoPlay = false)`
  - 设置 URL 片源。
  - 可选是否自动播放。

### 6.2 播放控制接口

- `Play()`
- `Pause()`
- `Stop()`

### 6.3 进度控制接口

- `SeekToSeconds(float seconds)`
- `SeekToNormalized(float normalizedValue)`

### 6.4 窗口状态接口

- `EnterZoomMode()`
- `ExitZoomMode()`
- `EnterCanvasFullscreen()`
- `ExitCanvasFullscreen()`

### 6.5 文本与窗口接口

- `SetTitle(string title)`
- `CloseWindow()`

### 6.6 接口设计原则

- 所有公开函数均直接可由外部调用。
- 不强制要求调用者了解内部 UI 控件状态。
- 不要求外部传入复杂上下文对象。

---

## 7. 播放状态与窗口状态设计

### 7.1 播放状态

脚本内部最少维护以下播放相关状态：

- 当前视频源类型：`VideoClip` 或 `URL`
- 是否已准备完成
- 是否正在拖动进度条
- 播放前状态缓存，用于拖动后恢复播放或保持暂停

### 7.2 窗口状态

脚本内部最少维护以下窗口状态：

- 普通态
- 放大态
- Canvas 全屏态

### 7.3 窗口切换原则

#### 普通态

- 窗口保持固定横屏弹窗尺寸。
- 锚点与偏移按普通弹窗布局。

#### 放大态

- 窗口尺寸变大，但仍保留明显的窗口边界与四周留白。
- 仍属于“弹窗”，不是铺满整个屏幕。

#### Canvas 全屏态

- 在当前 `Canvas` 中拉伸铺满可视区域。
- 不调用系统级全屏。
- 保持顶部栏与底部控制栏存在。

### 7.4 状态恢复

进入放大态或全屏态前，脚本需要记录普通态布局数据，用于恢复。

建议缓存：

- `anchorMin`
- `anchorMax`
- `pivot`
- `anchoredPosition`
- `sizeDelta`

---

## 8. 进度条交互设计

### 8.1 基本行为

- 视频正常播放时，`SldProgress` 根据当前时间自动刷新。
- 用户拖动 `SldProgress` 时，可跳转到指定播放进度。

### 8.2 拖动控制策略

由于本次只保留一个独立脚本，不再增加辅助拖动脚本，因此进度条拖动控制采用“单脚本监听 + 拖动状态缓存”方式实现。

建议行为：

1. 用户开始拖动时：
   - 记录当前是否正在播放。
   - 标记 `m_IsDraggingProgress = true`。

2. 用户拖动过程中：
   - 根据当前滑块值实时刷新 `TxtCurrentTime` 预览文本。
   - 暂停自动用播放时间写回 `Slider`。

3. 用户结束拖动时：
   - 调用 `SeekToNormalized`。
   - 如果拖动前处于播放中，则跳转后继续播放。
   - 如果拖动前处于暂停，则跳转后保持暂停。
   - 清除拖动标记。

### 8.3 可行性约束

当前工程已具备 `IBeginDragHandler / IEndDragHandler / IPointerDownHandler` 相关运行时能力，因此本设计允许在单脚本内完成进度条拖动状态处理，不必额外增加第二个辅助脚本。

---

## 9. 时间显示规则

### 9.1 统一格式

所有时间显示统一为：

- `hh:mm:ss`

### 9.2 应用范围

- `TxtCurrentTime`
- `TxtDuration`
- 进度条拖动过程中的预览时间

### 9.3 特殊状态显示

- 未设置片源：`00:00:00`
- 准备中：`00:00:00`
- 停止后：`00:00:00`
- 加载失败：`00:00:00`

---

## 10. 异常处理与兼容边界

### 10.1 异常处理

- 未设置片源时调用 `Play()`
  - 不报错，不播放，输出警告日志。

- 切换片源
  - 先停止旧播放状态。
  - 清理旧进度与显示状态。
  - 再开始准备新片源。

- 准备未完成
  - 时长不进行有效计算。
  - 时间文本保持 `00:00:00`。

- URL 无效或播放失败
  - 通过 `VideoPlayer.errorReceived` 记录错误。
  - 维持可再次设置片源的状态。

- 视频时长无效
  - 若 `length <= 0`，不执行有效进度比计算。

- 脚本销毁或窗口关闭
  - 停止播放。
  - 释放 `RenderTexture`。
  - 解绑 UI 事件。

### 10.2 兼容边界

- 只支持单视频源播放。
- 不支持系统全屏。
- 不支持额外控制条功能扩展。
- 不增加第二个辅助脚本。

---

## 11. 验证标准

### 11.1 功能验收

1. `SetVideoClip(..., true)` 能成功加载并播放 `VideoClip`
   - 验证：`RawImage` 正常显示视频画面，进度条和时间持续更新。

2. `SetVideoUrl(..., true)` 能成功加载并播放 URL 视频
   - 验证：播放逻辑与 `VideoClip` 一致。

3. 播放、暂停、停止按钮行为正确
   - 验证：视频状态与时间刷新符合预期。

4. 拖动 `SldProgress` 能控制视频进度
   - 验证：松手后视频时间、当前时间、进度条位置一致。

5. 普通态、放大态、Canvas 全屏态切换正确
   - 验证：切换后窗口尺寸变化正确，恢复后回到原布局。

6. 关闭窗口后无残留播放与明显资源泄漏
   - 验证：视频停止、纹理释放、重复使用不报错。

### 11.2 结构验收

- JSON 文件字段与枚举严格符合 `UGUITempView.json` 模板约束。
- 节点命名遵循 `skill-uguitoolkit` 命名规范。
- 不额外引入未被用户要求的功能节点。

---

## 12. 实施落点

### 12.1 JSON 文件落点

- `Assets/UIJsonData/VideoPlayerWindowView.json`

### 12.2 控制脚本落点

- `Assets/Scripts/UI/VideoPlayerWindowController.cs`

### 12.3 实施要求

- 生成代码时必须添加函数级中文注释。
- 修改范围只限于本次需求直接相关文件。
- 尽量保持最小实现，不做额外抽象。

---

## 13. 自检结论

本设计已完成以下自检：

- 已去除与本次需求无关的倍速、音量、播放列表等扩展能力。
- 已明确“全屏”定义为 Canvas 内铺满，而非系统全屏。
- 已统一时间格式为 `hh:mm:ss`，不存在多种显示规则并存。
- 已将进度条拖动控制限定为单脚本实现，避免与“独立脚本”要求冲突。
- 已明确输出物、文件路径、节点命名与验证标准，不存在占位项或 `TODO`。

---

## 14. 确认记录

- 当前状态：`pending-review`
- 下一步：用户审阅本设计文档，确认无误后进入实现计划与代码生成阶段。
