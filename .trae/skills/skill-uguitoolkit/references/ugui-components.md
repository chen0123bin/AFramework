## 组件清单与结构

### 组件分类
- **基础组件**：Image, Text, Button, InputField, Toggle, Slider, ScrollRect, Dropdown, RawImage, RoundedImage
- **交互组件**：EventTrigger（如 PointerEnter, PointerExit, PointerClick）, Selectable（如 Button, InputField）

### 组件描述
本部分提供常用组件的简要说明，详细配置请参考 references/components/*.md。

### Image（图片）
- **作用**：显示 Sprite，支持 Simple/Sliced/Tiled/Filled 模式
- **关键属性**：sprite, color, imageType, raycastTarget
- **详细文档**：references/components/Image.md

### Text（文字）
- **作用**：显示静态或动态文字
- **关键属性**：content, font, fontSize, color, alignment
- **详细文档**：references/components/Text.md

### Button（按钮）
- **作用**：触发点击交互行为
- **关键属性**：interactable, transition, colors, navigation
- **注意**：节点必须挂 Image 且 raycastTarget=true 才能接收点击
- **详细文档**：references/components/Button.md

### InputField（输入框）
- **作用**：接收用户文本输入
- **关键属性**：textComponent, placeholder, contentType, characterLimit
- **典型层级**：IpfXXX（挂 Image + InputField）→ TxtPlaceholder + TxtText
- **详细文档**：references/components/InputField.md

### Toggle（开关）
- **作用**：二选一或多选开关
- **关键属性**：isOn, targetGraphic, graphic, group
- **详细文档**：references/components/Toggle.md

### Slider（滑动条）
- **作用**：数值调节滑动条
- **关键属性**：fillRect, handleRect, direction, minValue, maxValue
- **典型层级**：SldXXX（挂 Slider）→ ImgBackground + ImgFill + ImgHandle
- **详细文档**：references/components/Slider.md

### ScrollRect（滚动容器）
- **作用**：可滚动内容容器（列表、长文本等）
- **关键属性**：content, viewport, horizontal, vertical, movementType
- **典型层级**：SrXXX（挂 Image + ScrollRect）→ Viewport → Content
- **详细文档**：references/components/ScrollRect.md

### Dropdown（下拉选择）
- **作用**：从多个选项中选择一项
- **关键属性**：template, captionText, itemText, options
- **详细文档**：references/components/Dropdown.md

### RawImage（原始图片）
- **作用**：显示 Texture（非 Sprite）
- **关键属性**：texture, uvRect, color
- **详细文档**：references/components/RawImage.md

### RoundedImage（圆角图片）
- **作用**：自定义圆角图片组件（LWFramework 扩展）
- **特点**：支持圆角、边框、镂空效果
- **关键属性**：cornerRadius, borderColor, borderWidth, hollow
- **详细文档**：references/components/RoundedImage.md
- selected：轻微提亮
- disabled：alpha 约 0.55，并降低饱和度或用 Border 近似


