# RoundedImage（圆角图片）

## 作用

RoundedImage 是 LWFramework 提供的自定义 UGUI 组件，继承自 UnityEngine.UI.Image，在保留 Image 常规能力的基础上，额外支持：

- 圆角（统一圆角 / 四角独立圆角）
- 边框（颜色、粗细）
- 镂空（仅描边、内部透明，可控制透明区域是否参与点击）
- Shader 渲染模式（支持所有 ImageType：Simple/Sliced/Tiled/Filled）

它适合用来做：卡片/弹窗底板、输入框外框、玻璃拟态面板、带圆角的按钮底图、头像框等。

## Unity 组件信息

- ComponentType：LWUI.RoundedImage
- BaseType：UnityEngine.UI.Image
- AddComponentMenu：UI/Rounded Image

## 在 UGUI JSON 中怎么用

在节点的 components 数组里，把 type 写成 RoundedImage，并把配置写进 data：

```json
{
  "type": "RoundedImage",
  "data": {
    "sprite": "Assets/Arts/BaseUI/白色背景.png",
    "color": [1.0, 1.0, 1.0, 0.12]
  }
}
```

运行时代码：Assets/LWFramework/RunTime/UI/Components/RoundedImage.cs

## 重要属性（常用）

### 基础（Image 通用）

- sprite（string|null）：Sprite 资源路径，null/缺省表示不设置
- color（float[4]）：RGBA（0~1）
- material（string）：通常用 Default UI Material
- raycastTarget（bool）：纯装饰建议 false；需要拦截点击/作为按钮底图时 true
- imageType（Simple/Sliced/Tiled/Filled）：一般卡片用 Sliced
- preserveAspect（bool）：头像/比例敏感图可开

### 圆角

- independentCorners（bool）
  - false：使用统一圆角 cornerRadius
  - true：使用四角半径 topLeftRadius/topRightRadius/bottomRightRadius/bottomLeftRadius
- cornerRadius（float）：统一圆角半径（像素，仅 independentCorners=false 生效）
- topLeftRadius/topRightRadius/bottomRightRadius/bottomLeftRadius（float）：四角独立半径（像素，仅 independentCorners=true 生效）

推荐：

- 卡片/弹窗底板：independentCorners=false + cornerRadius 12~20
- 复杂异形：independentCorners=true，仅设置需要变化的角

### 边框

- borderEnabled（bool）：是否绘制边框
- borderColor（float[4]）：边框颜色 RGBA
- borderThickness（float）：边框粗细（像素）

### 镂空（只描边）

- hollow（bool）：开启后只绘制边框，内部透明（会强制开启边框）
- hollowAreaRaycastEnabled（bool）：内部透明区域是否也接收点击

典型用法：输入框外框、选中态描边、卡片描边层。

### 渲染模式

- shaderRenderingEnabled（bool）：是否启用 Shader 渲染模式（建议保持 true）
- roundedShaderMaterial（string）：可选，自定义圆角 Shader 材质路径
- shaderMaterial（string）：roundedShaderMaterial 的别名字段（二选一即可）

## 示例

### 示例 1：玻璃拟态卡片（推荐）

```json
{
  "type": "RoundedImage",
  "data": {
    "sprite": "Assets/Arts/BaseUI/白色背景.png",
    "color": [1.0, 1.0, 1.0, 0.12],
    "raycastTarget": true,
    "imageType": "Sliced",
    "independentCorners": false,
    "cornerRadius": 18.0,
    "borderEnabled": true,
    "borderColor": [1.0, 1.0, 1.0, 0.2],
    "borderThickness": 1.5,
    "shaderRenderingEnabled": true
  }
}
```

### 示例 2：输入框外框（镂空描边）

```json
{
  "type": "RoundedImage",
  "data": {
    "sprite": "Assets/Arts/BaseUI/白色背景.png",
    "color": [1.0, 1.0, 1.0, 0.0],
    "raycastTarget": false,
    "imageType": "Sliced",
    "independentCorners": false,
    "cornerRadius": 12.0,
    "hollow": true,
    "hollowAreaRaycastEnabled": false,
    "borderEnabled": true,
    "borderColor": [1.0, 1.0, 1.0, 0.25],
    "borderThickness": 2.0
  }
}
```

### 示例 3：最小可用（快速验证）

```json
{
  "type": "RoundedImage",
  "data": {
    "sprite": "Assets/Arts/BaseUI/白色背景.png",
    "color": [1.0, 1.0, 1.0, 0.2],
    "imageType": "Sliced",
    "independentCorners": false,
    "cornerRadius": 16.0
  }
}
```

## 使用建议

- 圆角卡片优先用 Sliced + 合理的 cornerRadius，避免过大半径导致边缘锯齿感。
- 需要点击穿透时，把 RoundedImage 的 raycastTarget 设为 false。
- 镂空描边建议单独一层覆盖在控件上方，hollowAreaRaycastEnabled 通常设为 false，避免透明区拦截点击。

