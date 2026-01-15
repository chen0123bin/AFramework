# 资源路径映射（BaseUI）

为了避免生成的组件找不到资源文件，输出到 JSON 的 sprite 路径建议统一使用 Unity 的 AssetDatabase 路径格式：

- 必须以 Assets/ 开头
- 使用 / 作为分隔符（不使用 Windows 的 \）

## BaseUI 资源映射规则

本技能内示例资源路径：

- assets/arts/baseui/...

你的 Unity 工程建议放置路径：

- Assets/Arts/BaseUI/...

映射规则：

- 当输入或示例出现以 assets/arts/baseui/ 开头的路径时，输出到 JSON 中一律替换为 Assets/Arts/BaseUI/
- 当输入或示例使用 Windows 分隔符（\）时，输出到 JSON 中一律转换为 /

示例：

- assets/arts/baseui/按钮.png → Assets/Arts/BaseUI/按钮.png
- assets\arts\baseui\边框.png → Assets/Arts/BaseUI/边框.png

## Sprite 与 Image 的落地建议（UGUI）

当你使用 BaseUI 下的“按钮/边框/面板底图”等资源时：

- Image.imageType 推荐使用 Sliced（9-sliced），避免缩放导致圆角或边框变形
- 图集或 SpriteBorder 的设置在 Unity 导入设置中完成；JSON 只负责引用 sprite 与设置 imageType

