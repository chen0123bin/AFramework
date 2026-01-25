# UGUI JSON 结构规则（严格对齐 UGUITempView.json）

本文件只描述“不变的硬规则”。任何字段名、枚举字符串、组件 data 的细节，都以 assets/templates/UGUITempView.json 为唯一依据。

## 1) 节点结构（字段白名单）
- 顶层：Root
- 节点对象允许字段：name / active / rectTransform / components / children
  - active 可省略（默认 true）

## 2) RectTransform（必填字段）
rectTransform 必须包含以下字段：
- anchorMin：[x, y]
- anchorMax：[x, y]
- pivot：[x, y]
- anchoredPosition：[x, y]
- sizeDelta：[w, h]
- rotation：[x, y, z]
- scale：[x, y, z]

## 3) Components（通用结构）
- components 是数组
- 每一项包含：
  - type：组件类型字符串（例如 Image / Text / Button / InputField / ScrollRect 等）
  - data：该组件的序列化字段（字段名与枚举字符串必须跟随模板）

## 4) 命名规范（必须）
- Txt：TxtTitle / TxtDesc / TxtPlaceholder / TxtText
- Img：ImgBg / ImgIcon / ImgBackground
- Btn：BtnClose / BtnSubmit
- Tgl：TglChoose
- Sld：SldProgress
- Ipf：IpfUsername / IpfPassword
- Dpd：DpdOption
- Pnl：PnlLeft / PnlRight

## 5) 引用字段的可解析性（必须）
当组件需要引用子节点（例如 InputField.textComponent、Dropdown.template、ScrollRect.content）：
- 被引用的子节点 name 必须与引用路径一致
- 引用常见写法：
  - "."：指向本节点
  - "A/B/C"：相对路径（从组件所在节点开始）
  - "NodeName"：按名字在子树中查找

具体组件的引用字段与推荐层级，按需查看：references/components/*.md。

## 6) Layout 使用边界（性能优先）
- 默认不使用任何 Layout 组件
- 仅允许场景：ScrollRect 的 Content 节点用于动态列表
  - Content 可挂 VerticalLayoutGroup/HorizontalLayoutGroup/GridLayoutGroup + ContentSizeFitter
- 除 ScrollRect Content 外的所有区域，一律使用 RectTransform 手动排版
