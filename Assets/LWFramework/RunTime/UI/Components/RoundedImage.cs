using System.Collections.Generic;
using LWAssets;
using LWCore;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace LWUI
{
    //[ExecuteAlways]
    [AddComponentMenu("UI/Rounded Image", 11)]
    public class RoundedImage : Image
    {
        private const string ROUNDED_SHADER_NAME = "UI/LWFramework/RoundedImage";
        private const string ROUNDED_SHADER_ASSET_PATH = "Assets/LWFramework/RunTime/UI/Shaders/UIRoundedImage.shader";
        private const int MESH_CORNER_SEGMENTS = 8;

        private static Shader s_RoundedShader;
        private static bool s_HasAttemptedLoadRoundedShader;

        [Header("Rounded")]
        [SerializeField]
        [Tooltip("是否启用四角独立圆角半径。关闭时使用统一圆角半径")]
        private bool m_IsIndependentCorners = true;

        [SerializeField]
        [Tooltip("统一圆角半径（像素）。仅在 Independent Corners 关闭时生效")]
        private float m_CornerRadius = 16.0f;

        [SerializeField]
        [Tooltip("左上角圆角半径（像素）。仅在 Independent Corners 开启时生效")]
        private float m_TopLeftRadius = 16.0f;

        [SerializeField]
        [Tooltip("右上角圆角半径（像素）。仅在 Independent Corners 开启时生效")]
        private float m_TopRightRadius = 16.0f;

        [SerializeField]
        [Tooltip("右下角圆角半径（像素）。仅在 Independent Corners 开启时生效")]
        private float m_BottomRightRadius = 16.0f;

        [SerializeField]
        [Tooltip("左下角圆角半径（像素）。仅在 Independent Corners 开启时生效")]
        private float m_BottomLeftRadius = 16.0f;

        [Header("Border")]
        [SerializeField]
        [Tooltip("是否绘制边框")]
        private bool m_IsBorderEnabled;

        [SerializeField]
        [Tooltip("边框颜色")]
        private Color m_BorderColor = Color.white;

        [SerializeField]
        [Tooltip("边框粗细（像素）")]
        private float m_BorderThickness = 2.0f;

        [SerializeField]
        [Tooltip("是否镂空：开启后只绘制边框，内部透明")]
        private bool m_IsHollow;

        [SerializeField]
        [Tooltip("镂空区域是否接收 Raycast。开启时内部透明区域也会被点击命中")]
        private bool m_IsHollowAreaRaycastEnabled = true;

        [Header("Rendering")]
        [SerializeField]
        [Tooltip("是否启用 Shader 渲染模式。开启后支持所有 Image Type（Simple/Sliced/Tiled/Filled）")]
        private bool m_IsShaderRenderingEnabled = true;

        [SerializeField]
        [Tooltip("自定义圆角 Shader 材质（可选）。为空时自动创建 UI/LWFramework/RoundedImage 材质")]
        private Material m_RoundedShaderMaterial;

        private readonly List<Vector2> m_OuterPolygonPoints = new List<Vector2>(256);
        private readonly List<Vector2> m_InnerPolygonPoints = new List<Vector2>(256);

        private Material m_RuntimeRoundedMaterial;
        private Material m_RuntimePerInstanceMaterial;
        private Material m_LastSourceModifiedMaterial;

        private static readonly int SHADER_ID_ROUNDED_RECT = Shader.PropertyToID("_RoundedRect");
        private static readonly int SHADER_ID_CORNER_RADII = Shader.PropertyToID("_CornerRadii");
        private static readonly int SHADER_ID_BORDER_THICKNESS = Shader.PropertyToID("_BorderThickness");
        private static readonly int SHADER_ID_BORDER_COLOR = Shader.PropertyToID("_BorderColor");
        private static readonly int SHADER_ID_HOLLOW = Shader.PropertyToID("_Hollow");

        private static readonly int SHADER_ID_RECT_CENTER = Shader.PropertyToID("_RectCenter");
        private static readonly int SHADER_ID_RECT_AXIS_RIGHT = Shader.PropertyToID("_RectAxisRight");
        private static readonly int SHADER_ID_RECT_AXIS_UP = Shader.PropertyToID("_RectAxisUp");
        private static readonly int SHADER_ID_RECT_HALF_SIZE = Shader.PropertyToID("_RectHalfSize");

        public bool IsIndependentCorners
        {
            get { return m_IsIndependentCorners; }
        }

        public float CornerRadius
        {
            get { return m_CornerRadius; }
        }

        public float TopLeftRadius
        {
            get { return m_TopLeftRadius; }
        }

        public float TopRightRadius
        {
            get { return m_TopRightRadius; }
        }

        public float BottomRightRadius
        {
            get { return m_BottomRightRadius; }
        }

        public float BottomLeftRadius
        {
            get { return m_BottomLeftRadius; }
        }

        public bool IsBorderEnabled
        {
            get { return m_IsBorderEnabled; }
        }

        public Color BorderColor
        {
            get { return m_BorderColor; }
        }

        public float BorderThickness
        {
            get { return m_BorderThickness; }
        }

        public bool IsHollow
        {
            get { return m_IsHollow; }
        }

        public bool IsHollowAreaRaycastEnabled
        {
            get { return m_IsHollowAreaRaycastEnabled; }
        }

        public bool IsShaderRenderingEnabled
        {
            get { return m_IsShaderRenderingEnabled; }
        }

        public Material RoundedShaderMaterial
        {
            get { return m_RoundedShaderMaterial; }
            set
            {
                if (m_RoundedShaderMaterial == value)
                {
                    return;
                }

                m_RoundedShaderMaterial = value;
                ReleaseRuntimeMaterial();
                MarkGeometryOrMaterialDirty();
            }
        }

        /// <summary>
        /// 设置统一圆角半径。
        /// </summary>
        /// <param name="cornerRadius">统一圆角半径（像素）。</param>
        public void SetCornerRadius(float cornerRadius)
        {
            m_IsIndependentCorners = false;
            m_CornerRadius = Mathf.Max(0.0f, cornerRadius);
            MarkGeometryOrMaterialDirty();
        }

        /// <summary>
        /// 设置四个角的圆角半径。
        /// </summary>
        public void SetCornerRadii(float topLeftRadius, float topRightRadius, float bottomRightRadius, float bottomLeftRadius)
        {
            m_IsIndependentCorners = true;
            m_TopLeftRadius = Mathf.Max(0.0f, topLeftRadius);
            m_TopRightRadius = Mathf.Max(0.0f, topRightRadius);
            m_BottomRightRadius = Mathf.Max(0.0f, bottomRightRadius);
            m_BottomLeftRadius = Mathf.Max(0.0f, bottomLeftRadius);
            MarkGeometryOrMaterialDirty();
        }

        /// <summary>
        /// 设置边框是否启用。
        /// </summary>
        public void SetBorderEnabled(bool isEnabled)
        {
            m_IsBorderEnabled = isEnabled;
            MarkGeometryOrMaterialDirty();
        }

        /// <summary>
        /// 设置边框颜色。
        /// </summary>
        public void SetBorderColor(Color borderColor)
        {
            m_BorderColor = borderColor;
            MarkGeometryOrMaterialDirty();
        }

        /// <summary>
        /// 设置边框粗细（像素）。
        /// </summary>
        public void SetBorderThickness(float borderThickness)
        {
            m_BorderThickness = Mathf.Max(0.0f, borderThickness);
            MarkGeometryOrMaterialDirty();
        }

        /// <summary>
        /// 设置是否启用镂空（只绘制边框，中心透明）。
        /// </summary>
        public void SetHollow(bool isHollow)
        {
            m_IsHollow = isHollow;
            if (m_IsHollow)
            {
                m_IsBorderEnabled = true;
            }
            MarkGeometryOrMaterialDirty();
        }

        /// <summary>
        /// 设置是否启用 Shader 渲染模式（支持所有 Image Type）。
        /// </summary>
        public void SetShaderRenderingEnabled(bool isEnabled)
        {
            m_IsShaderRenderingEnabled = isEnabled;
            MarkGeometryOrMaterialDirty();
        }

        /// <summary>
        /// 设置镂空区域是否接收 Raycast。
        /// </summary>
        public void SetHollowAreaRaycastEnabled(bool isEnabled)
        {
            m_IsHollowAreaRaycastEnabled = isEnabled;
        }

        /// <summary>
        /// 生成圆角矩形网格。
        /// </summary>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (m_IsShaderRenderingEnabled)
            {
                base.OnPopulateMesh(vh);
                return;
            }

            if (type != Type.Simple)
            {
                base.OnPopulateMesh(vh);
                return;
            }

            Rect drawingRect = GetDrawingRect(preserveAspect);
            float width = drawingRect.width;
            float height = drawingRect.height;

            if (width <= 0.0f || height <= 0.0f)
            {
                vh.Clear();
                return;
            }

            int segments = MESH_CORNER_SEGMENTS;

            float topLeftRadius;
            float topRightRadius;
            float bottomRightRadius;
            float bottomLeftRadius;
            GetNormalizedRadii(width, height, out topLeftRadius, out topRightRadius, out bottomRightRadius, out bottomLeftRadius);

            bool isBorderEnabled = m_IsBorderEnabled || m_IsHollow;
            bool isFillEnabled = !m_IsHollow;

            vh.Clear();

            BuildRoundedPolygonFixed(drawingRect, topLeftRadius, topRightRadius, bottomRightRadius, bottomLeftRadius, segments, m_OuterPolygonPoints);

            if (isFillEnabled)
            {
                AppendFillMesh(vh, drawingRect, m_OuterPolygonPoints, (Color32)color);
            }

            if (isBorderEnabled)
            {
                float borderThickness = GetClampedBorderThickness(width, height);
                if (borderThickness > 0.0001f)
                {
                    Rect innerRect = Rect.MinMaxRect(drawingRect.xMin + borderThickness, drawingRect.yMin + borderThickness, drawingRect.xMax - borderThickness, drawingRect.yMax - borderThickness);
                    if (innerRect.width > 0.0f && innerRect.height > 0.0f)
                    {
                        float innerTopLeftRadius = Mathf.Max(0.0f, topLeftRadius - borderThickness);
                        float innerTopRightRadius = Mathf.Max(0.0f, topRightRadius - borderThickness);
                        float innerBottomRightRadius = Mathf.Max(0.0f, bottomRightRadius - borderThickness);
                        float innerBottomLeftRadius = Mathf.Max(0.0f, bottomLeftRadius - borderThickness);
                        NormalizeRadii(innerRect.width, innerRect.height, ref innerTopLeftRadius, ref innerTopRightRadius, ref innerBottomRightRadius, ref innerBottomLeftRadius);

                        BuildRoundedPolygonFixed(innerRect, innerTopLeftRadius, innerTopRightRadius, innerBottomRightRadius, innerBottomLeftRadius, segments, m_InnerPolygonPoints);
                        AppendBorderRingMesh(vh, drawingRect, m_OuterPolygonPoints, m_InnerPolygonPoints, (Color32)m_BorderColor);
                    }
                }
            }
        }

        /// <summary>
        /// 圆角命中检测：用于让按钮等组件的点击区域匹配圆角形状。
        /// </summary>
        public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (type != Type.Simple && !m_IsShaderRenderingEnabled)
            {
                return base.IsRaycastLocationValid(screenPoint, eventCamera);
            }

            Vector2 localPoint;
            bool isInside = RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out localPoint);
            if (!isInside)
            {
                return false;
            }

            Rect drawingRect = GetDrawingRect(preserveAspect);
            float width = drawingRect.width;
            float height = drawingRect.height;
            if (width <= 0.0f || height <= 0.0f)
            {
                return false;
            }

            float x = localPoint.x - drawingRect.xMin;
            float y = localPoint.y - drawingRect.yMin;
            if (x < 0.0f || x > width || y < 0.0f || y > height)
            {
                return false;
            }

            float topLeftRadius;
            float topRightRadius;
            float bottomRightRadius;
            float bottomLeftRadius;
            GetNormalizedRadii(width, height, out topLeftRadius, out topRightRadius, out bottomRightRadius, out bottomLeftRadius);

            if (!IsInsideRoundedRect(x, y, width, height, topLeftRadius, topRightRadius, bottomRightRadius, bottomLeftRadius))
            {
                return false;
            }

            if (!m_IsHollow)
            {
                return true;
            }

            if (m_IsHollowAreaRaycastEnabled)
            {
                return true;
            }

            float borderThickness = GetClampedBorderThickness(width, height);
            if (borderThickness <= 0.0001f)
            {
                return false;
            }

            float innerWidth = width - borderThickness * 2.0f;
            float innerHeight = height - borderThickness * 2.0f;
            if (innerWidth <= 0.0f || innerHeight <= 0.0f)
            {
                return false;
            }

            float innerX = x - borderThickness;
            float innerY = y - borderThickness;
            float innerTopLeftRadius = Mathf.Max(0.0f, topLeftRadius - borderThickness);
            float innerTopRightRadius = Mathf.Max(0.0f, topRightRadius - borderThickness);
            float innerBottomRightRadius = Mathf.Max(0.0f, bottomRightRadius - borderThickness);
            float innerBottomLeftRadius = Mathf.Max(0.0f, bottomLeftRadius - borderThickness);
            NormalizeRadii(innerWidth, innerHeight, ref innerTopLeftRadius, ref innerTopRightRadius, ref innerBottomRightRadius, ref innerBottomLeftRadius);

            if (IsInsideRoundedRect(innerX, innerY, innerWidth, innerHeight, innerTopLeftRadius, innerTopRightRadius, innerBottomRightRadius, innerBottomLeftRadius))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// RectTransform 尺寸变化时刷新顶点。
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            MarkGeometryOrMaterialDirty();
        }

        /// <summary>
        /// 组件启用时确保使用 Simple 类型并刷新网格。
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            MarkGeometryOrMaterialDirty();
        }

        /// <summary>
        /// RectTransform 发生位移/旋转/缩放等变换时刷新材质参数。
        /// </summary>
        private void Update()
        {
            if (!m_IsShaderRenderingEnabled)
            {
                return;
            }

            if (!rectTransform.hasChanged)
            {
                return;
            }

            rectTransform.hasChanged = false;
            MarkGeometryOrMaterialDirty();
        }

        /// <summary>
        /// 组件禁用时释放运行时材质实例。
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            ReleaseRuntimeMaterial();
        }

        /// <summary>
        /// 编辑器参数变化时自动校验。
        /// </summary>
        // protected override void OnValidate()
        // {
        //     base.OnValidate();

        //     m_CornerRadius = Mathf.Max(0.0f, m_CornerRadius);
        //     m_TopLeftRadius = Mathf.Max(0.0f, m_TopLeftRadius);
        //     m_TopRightRadius = Mathf.Max(0.0f, m_TopRightRadius);
        //     m_BottomRightRadius = Mathf.Max(0.0f, m_BottomRightRadius);
        //     m_BottomLeftRadius = Mathf.Max(0.0f, m_BottomLeftRadius);

        //     m_BorderThickness = Mathf.Max(0.0f, m_BorderThickness);
        //     if (m_IsHollow)
        //     {
        //         m_IsBorderEnabled = true;
        //     }

        //     MarkGeometryOrMaterialDirty();
        // }

        /// <summary>
        /// Shader 渲染模式下返回带圆角裁剪的材质。
        /// </summary>
        public override Material materialForRendering
        {
            get
            {
                if (!m_IsShaderRenderingEnabled)
                {
                    return base.materialForRendering;
                }

                Material roundedMaterial = GetOrCreateRoundedMaterial();
                if (roundedMaterial == null)
                {
                    return base.materialForRendering;
                }

                Material sourceModifiedMaterial = GetModifiedMaterial(roundedMaterial);
                Material perInstanceMaterial = GetOrCreatePerInstanceModifiedMaterial(sourceModifiedMaterial);
                UpdateRoundedMaterialProperties(perInstanceMaterial);
                return perInstanceMaterial;
            }
        }

        /// <summary>
        /// 标记需要刷新网格与材质。
        /// </summary>
        private void MarkGeometryOrMaterialDirty()
        {
            SetVerticesDirty();
            SetMaterialDirty();
        }

        /// <summary>
        /// 获取或创建用于圆角渲染的运行时材质实例。
        /// </summary>
        private Material GetOrCreateRoundedMaterial()
        {
            Material sourceMaterial = m_RoundedShaderMaterial;
            if (sourceMaterial == null)
            {
                Shader shader = Shader.Find(ROUNDED_SHADER_NAME);
                if (shader == null)
                {
                    shader = GetOrLoadRoundedShader();
                }
                if (shader == null)
                {
                    return null;
                }

                if (m_RuntimeRoundedMaterial != null && m_RuntimeRoundedMaterial.shader == shader)
                {
                    return m_RuntimeRoundedMaterial;
                }

                ReleaseRuntimeMaterial();
                m_RuntimeRoundedMaterial = new Material(shader);
                m_RuntimeRoundedMaterial.hideFlags = HideFlags.HideAndDontSave;
                return m_RuntimeRoundedMaterial;
            }

            if (m_RuntimeRoundedMaterial == null || m_RuntimeRoundedMaterial.shader != sourceMaterial.shader)
            {
                ReleaseRuntimeMaterial();
                m_RuntimeRoundedMaterial = new Material(sourceMaterial);
                m_RuntimeRoundedMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            return m_RuntimeRoundedMaterial;
        }

        private static Shader GetOrLoadRoundedShader()
        {
            if (s_RoundedShader != null)
            {
                return s_RoundedShader;
            }

            if (s_HasAttemptedLoadRoundedShader)
            {
                return null;
            }

            IAssetsManager assetsManager = ManagerUtility.AssetsMgr;
            if (assetsManager == null || !assetsManager.IsInitialized)
            {
                return null;
            }

            Shader loadedShader = null;
            try
            {
                loadedShader = assetsManager.LoadAsset<Shader>(ROUNDED_SHADER_ASSET_PATH);
            }
            catch
            {
                loadedShader = null;
            }

            s_HasAttemptedLoadRoundedShader = true;
            s_RoundedShader = loadedShader;
            return s_RoundedShader;
        }

        /// <summary>
        /// 释放运行时材质实例，避免泄漏。
        /// </summary>
        private void ReleaseRuntimeMaterial()
        {
            if (m_RuntimeRoundedMaterial == null)
            {
                ReleasePerInstanceMaterial();
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(m_RuntimeRoundedMaterial);
            }
            else
            {
                DestroyImmediate(m_RuntimeRoundedMaterial);
            }

            m_RuntimeRoundedMaterial = null;

            ReleasePerInstanceMaterial();
        }

        /// <summary>
        /// 获取或创建每实例渲染材质，避免修改共享 Mask/Stenci 材质导致串参数。
        /// </summary>
        private Material GetOrCreatePerInstanceModifiedMaterial(Material sourceModifiedMaterial)
        {
            if (sourceModifiedMaterial == null)
            {
                return null;
            }

            if (m_RuntimePerInstanceMaterial != null && m_LastSourceModifiedMaterial == sourceModifiedMaterial && m_RuntimePerInstanceMaterial.shader == sourceModifiedMaterial.shader)
            {
                return m_RuntimePerInstanceMaterial;
            }

            ReleasePerInstanceMaterial();

            m_LastSourceModifiedMaterial = sourceModifiedMaterial;
            m_RuntimePerInstanceMaterial = new Material(sourceModifiedMaterial);
            m_RuntimePerInstanceMaterial.hideFlags = HideFlags.HideAndDontSave;
            return m_RuntimePerInstanceMaterial;
        }

        /// <summary>
        /// 释放每实例渲染材质实例。
        /// </summary>
        private void ReleasePerInstanceMaterial()
        {
            if (m_RuntimePerInstanceMaterial == null)
            {
                m_LastSourceModifiedMaterial = null;
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(m_RuntimePerInstanceMaterial);
            }
            else
            {
                DestroyImmediate(m_RuntimePerInstanceMaterial);
            }

            m_RuntimePerInstanceMaterial = null;
            m_LastSourceModifiedMaterial = null;
        }

        /// <summary>
        /// 更新 Shader 参数（圆角、边框、镂空、矩形范围）。
        /// </summary>
        private void UpdateRoundedMaterialProperties(Material material)
        {
            Rect drawingRect = GetDrawingRect(preserveAspect);

            float localWidth = drawingRect.width;
            float localHeight = drawingRect.height;

            if (localWidth <= 0.0f || localHeight <= 0.0f)
            {
                material.SetVector(SHADER_ID_ROUNDED_RECT, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
                material.SetVector(SHADER_ID_CORNER_RADII, Vector4.zero);
                material.SetFloat(SHADER_ID_BORDER_THICKNESS, 0.0f);
                material.SetColor(SHADER_ID_BORDER_COLOR, m_BorderColor);
                material.SetFloat(SHADER_ID_HOLLOW, 0.0f);
                return;
            }

            Vector3 worldBottomLeft = rectTransform.TransformPoint(new Vector3(drawingRect.xMin, drawingRect.yMin, 0.0f));
            Vector3 worldTopLeft = rectTransform.TransformPoint(new Vector3(drawingRect.xMin, drawingRect.yMax, 0.0f));
            Vector3 worldTopRight = rectTransform.TransformPoint(new Vector3(drawingRect.xMax, drawingRect.yMax, 0.0f));
            Vector3 worldBottomRight = rectTransform.TransformPoint(new Vector3(drawingRect.xMax, drawingRect.yMin, 0.0f));

            Canvas currentCanvas = canvas;
            if (currentCanvas != null && currentCanvas.rootCanvas != null)
            {
                Transform rootTransform = currentCanvas.rootCanvas.transform;
                worldBottomLeft = rootTransform.InverseTransformPoint(worldBottomLeft);
                worldTopLeft = rootTransform.InverseTransformPoint(worldTopLeft);
                worldTopRight = rootTransform.InverseTransformPoint(worldTopRight);
                worldBottomRight = rootTransform.InverseTransformPoint(worldBottomRight);
            }

            Vector2 bottomLeft = new Vector2(worldBottomLeft.x, worldBottomLeft.y);
            Vector2 topLeft = new Vector2(worldTopLeft.x, worldTopLeft.y);
            Vector2 topRight = new Vector2(worldTopRight.x, worldTopRight.y);
            Vector2 bottomRight = new Vector2(worldBottomRight.x, worldBottomRight.y);

            Vector2 rectCenter = (bottomLeft + topRight) * 0.5f;
            Vector2 rectRight = bottomRight - bottomLeft;
            Vector2 rectUp = topLeft - bottomLeft;

            float width = rectRight.magnitude;
            float height = rectUp.magnitude;

            if (width <= 0.0f || height <= 0.0f)
            {
                material.SetVector(SHADER_ID_ROUNDED_RECT, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
                material.SetVector(SHADER_ID_CORNER_RADII, Vector4.zero);
                material.SetFloat(SHADER_ID_BORDER_THICKNESS, 0.0f);
                material.SetColor(SHADER_ID_BORDER_COLOR, m_BorderColor);
                material.SetFloat(SHADER_ID_HOLLOW, 0.0f);
                return;
            }

            Vector2 axisRight = rectRight / width;
            Vector2 axisUp = rectUp / height;

            float scaleX = width / localWidth;
            float scaleY = height / localHeight;
            float radiusScale = Mathf.Min(scaleX, scaleY);

            float topLeftRadius = m_IsIndependentCorners ? m_TopLeftRadius : m_CornerRadius;
            float topRightRadius = m_IsIndependentCorners ? m_TopRightRadius : m_CornerRadius;
            float bottomRightRadius = m_IsIndependentCorners ? m_BottomRightRadius : m_CornerRadius;
            float bottomLeftRadius = m_IsIndependentCorners ? m_BottomLeftRadius : m_CornerRadius;

            topLeftRadius *= radiusScale;
            topRightRadius *= radiusScale;
            bottomRightRadius *= radiusScale;
            bottomLeftRadius *= radiusScale;
            NormalizeRadii(width, height, ref topLeftRadius, ref topRightRadius, ref bottomRightRadius, ref bottomLeftRadius);

            float borderThickness = (m_IsBorderEnabled || m_IsHollow) ? m_BorderThickness * radiusScale : 0.0f;
            borderThickness = Mathf.Max(0.0f, borderThickness);
            borderThickness = Mathf.Min(borderThickness, Mathf.Min(width, height) * 0.5f);

            material.SetVector(SHADER_ID_RECT_CENTER, new Vector4(rectCenter.x, rectCenter.y, 0.0f, 0.0f));
            material.SetVector(SHADER_ID_RECT_AXIS_RIGHT, new Vector4(axisRight.x, axisRight.y, 0.0f, 0.0f));
            material.SetVector(SHADER_ID_RECT_AXIS_UP, new Vector4(axisUp.x, axisUp.y, 0.0f, 0.0f));
            material.SetVector(SHADER_ID_RECT_HALF_SIZE, new Vector4(width * 0.5f, height * 0.5f, 0.0f, 0.0f));

            material.SetVector(SHADER_ID_ROUNDED_RECT, new Vector4(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y));
            material.SetVector(SHADER_ID_CORNER_RADII, new Vector4(topLeftRadius, topRightRadius, bottomRightRadius, bottomLeftRadius));
            material.SetFloat(SHADER_ID_BORDER_THICKNESS, borderThickness);
            material.SetColor(SHADER_ID_BORDER_COLOR, m_BorderColor);
            material.SetFloat(SHADER_ID_HOLLOW, m_IsHollow ? 1.0f : 0.0f);
        }

        /// <summary>
        /// 计算带 Sprite Padding/PreserveAspect 的绘制矩形。
        /// </summary>
        private Rect GetDrawingRect(bool shouldPreserveAspect)
        {
            Rect rect = GetPixelAdjustedRect();
            Sprite sprite = overrideSprite;
            if (sprite == null)
            {
                return rect;
            }

            Vector4 padding = DataUtility.GetPadding(sprite);
            Vector2 spriteSize = sprite.rect.size;

            float spriteWidth = spriteSize.x;
            float spriteHeight = spriteSize.y;
            if (spriteWidth <= 0.0f || spriteHeight <= 0.0f)
            {
                return rect;
            }

            float left = padding.x / spriteWidth;
            float bottom = padding.y / spriteHeight;
            float right = (spriteWidth - padding.z) / spriteWidth;
            float top = (spriteHeight - padding.w) / spriteHeight;

            float xMin = rect.xMin + rect.width * left;
            float yMin = rect.yMin + rect.height * bottom;
            float xMax = rect.xMin + rect.width * right;
            float yMax = rect.yMin + rect.height * top;

            Rect drawingRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);

            if (shouldPreserveAspect)
            {
                float spriteRatio = spriteWidth / spriteHeight;
                float rectRatio = drawingRect.width / drawingRect.height;

                if (spriteRatio > rectRatio)
                {
                    float oldHeight = drawingRect.height;
                    float newHeight = drawingRect.width / spriteRatio;
                    float offset = (oldHeight - newHeight) * 0.5f;
                    drawingRect.yMin += offset;
                    drawingRect.yMax -= offset;
                }
                else
                {
                    float oldWidth = drawingRect.width;
                    float newWidth = drawingRect.height * spriteRatio;
                    float offset = (oldWidth - newWidth) * 0.5f;
                    drawingRect.xMin += offset;
                    drawingRect.xMax -= offset;
                }
            }

            return drawingRect;
        }

        /// <summary>
        /// 获取四角圆角半径，并按矩形尺寸进行约束归一化。
        /// </summary>
        private void GetNormalizedRadii(float width, float height, out float topLeftRadius, out float topRightRadius, out float bottomRightRadius, out float bottomLeftRadius)
        {
            if (m_IsIndependentCorners)
            {
                topLeftRadius = m_TopLeftRadius;
                topRightRadius = m_TopRightRadius;
                bottomRightRadius = m_BottomRightRadius;
                bottomLeftRadius = m_BottomLeftRadius;
            }
            else
            {
                topLeftRadius = m_CornerRadius;
                topRightRadius = m_CornerRadius;
                bottomRightRadius = m_CornerRadius;
                bottomLeftRadius = m_CornerRadius;
            }

            NormalizeRadii(width, height, ref topLeftRadius, ref topRightRadius, ref bottomRightRadius, ref bottomLeftRadius);
        }

        /// <summary>
        /// 按矩形尺寸约束并归一化四角圆角半径。
        /// </summary>
        private void NormalizeRadii(float width, float height, ref float topLeftRadius, ref float topRightRadius, ref float bottomRightRadius, ref float bottomLeftRadius)
        {
            float maxRadius = Mathf.Max(0.0f, Mathf.Min(width, height) * 0.5f);
            topLeftRadius = Mathf.Clamp(topLeftRadius, 0.0f, maxRadius);
            topRightRadius = Mathf.Clamp(topRightRadius, 0.0f, maxRadius);
            bottomRightRadius = Mathf.Clamp(bottomRightRadius, 0.0f, maxRadius);
            bottomLeftRadius = Mathf.Clamp(bottomLeftRadius, 0.0f, maxRadius);

            float scale = 1.0f;
            float sumTop = topLeftRadius + topRightRadius;
            float sumBottom = bottomLeftRadius + bottomRightRadius;
            float sumLeft = topLeftRadius + bottomLeftRadius;
            float sumRight = topRightRadius + bottomRightRadius;

            if (sumTop > width)
            {
                scale = Mathf.Min(scale, width / sumTop);
            }
            if (sumBottom > width)
            {
                scale = Mathf.Min(scale, width / sumBottom);
            }
            if (sumLeft > height)
            {
                scale = Mathf.Min(scale, height / sumLeft);
            }
            if (sumRight > height)
            {
                scale = Mathf.Min(scale, height / sumRight);
            }

            if (scale < 1.0f)
            {
                topLeftRadius *= scale;
                topRightRadius *= scale;
                bottomRightRadius *= scale;
                bottomLeftRadius *= scale;
            }
        }

        /// <summary>
        /// 获取边框粗细的安全值，避免内矩形反转。
        /// </summary>
        private float GetClampedBorderThickness(float width, float height)
        {
            float maxThickness = Mathf.Max(0.0f, Mathf.Min(width, height) * 0.5f - 0.0001f);
            return Mathf.Clamp(m_BorderThickness, 0.0f, maxThickness);
        }

        /// <summary>
        /// 构建固定顶点数量的圆角矩形外轮廓点（用于边框环形网格）。
        /// </summary>
        private void BuildRoundedPolygonFixed(Rect rect, float topLeftRadius, float topRightRadius, float bottomRightRadius, float bottomLeftRadius, int segments, List<Vector2> points)
        {
            points.Clear();

            float xMin = rect.xMin;
            float xMax = rect.xMax;
            float yMin = rect.yMin;
            float yMax = rect.yMax;

            AddArcPoints(new Vector2(xMin + topLeftRadius, yMax - topLeftRadius), topLeftRadius, 180.0f, 90.0f, segments, true, points);
            AddArcPoints(new Vector2(xMax - topRightRadius, yMax - topRightRadius), topRightRadius, 90.0f, 0.0f, segments, false, points);
            AddArcPoints(new Vector2(xMax - bottomRightRadius, yMin + bottomRightRadius), bottomRightRadius, 0.0f, -90.0f, segments, false, points);
            AddArcPoints(new Vector2(xMin + bottomLeftRadius, yMin + bottomLeftRadius), bottomLeftRadius, -90.0f, -180.0f, segments, false, points);
        }

        /// <summary>
        /// 追加圆弧点到列表。
        /// </summary>
        private void AddArcPoints(Vector2 center, float radius, float startDeg, float endDeg, int segments, bool includeStart, List<Vector2> points)
        {
            int i;
            for (i = 0; i <= segments; i++)
            {
                if (i == 0 && !includeStart)
                {
                    continue;
                }

                float t = (float)i / (float)segments;
                float deg = Mathf.Lerp(startDeg, endDeg, t);
                float rad = deg * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);
                Vector2 point = new Vector2(center.x + cos * radius, center.y + sin * radius);
                points.Add(point);
            }
        }

        /// <summary>
        /// 追加填充网格（中心扇形）。
        /// </summary>
        private void AppendFillMesh(VertexHelper vh, Rect uvRect, List<Vector2> points, Color32 vertexColor)
        {
            int pointCount = points.Count;
            if (pointCount < 3)
            {
                return;
            }

            Vector4 outerUv = GetOuterUv();

            Vector2 center = uvRect.center;
            Vector2 centerUv = GetUv(center, uvRect, outerUv);
            vh.AddVert(new Vector3(center.x, center.y, 0.0f), vertexColor, centerUv);
            int baseIndex = 1;

            int i;
            for (i = 0; i < pointCount; i++)
            {
                Vector2 position = points[i];
                Vector2 uv = GetUv(position, uvRect, outerUv);
                vh.AddVert(new Vector3(position.x, position.y, 0.0f), vertexColor, uv);
            }

            for (i = 0; i < pointCount; i++)
            {
                int index0 = 0;
                int index1 = baseIndex + i;
                int index2 = baseIndex + (i + 1) % pointCount;
                vh.AddTriangle(index0, index1, index2);
            }
        }

        /// <summary>
        /// 追加边框环形网格（外轮廓与内轮廓之间）。
        /// </summary>
        private void AppendBorderRingMesh(VertexHelper vh, Rect uvRect, List<Vector2> outerPoints, List<Vector2> innerPoints, Color32 borderColor)
        {
            int pointCount = Mathf.Min(outerPoints.Count, innerPoints.Count);
            if (pointCount < 3)
            {
                return;
            }

            Vector4 outerUv = GetOuterUv();
            int baseIndex = vh.currentVertCount;

            int i;
            for (i = 0; i < pointCount; i++)
            {
                Vector2 outerPos = outerPoints[i];
                Vector2 innerPos = innerPoints[i];
                Vector2 outerUvPos = GetUv(outerPos, uvRect, outerUv);
                Vector2 innerUvPos = GetUv(innerPos, uvRect, outerUv);

                vh.AddVert(new Vector3(outerPos.x, outerPos.y, 0.0f), borderColor, outerUvPos);
                vh.AddVert(new Vector3(innerPos.x, innerPos.y, 0.0f), borderColor, innerUvPos);
            }

            for (i = 0; i < pointCount; i++)
            {
                int next = (i + 1) % pointCount;

                int outer0 = baseIndex + i * 2;
                int inner0 = outer0 + 1;
                int outer1 = baseIndex + next * 2;
                int inner1 = outer1 + 1;

                vh.AddTriangle(outer0, outer1, inner1);
                vh.AddTriangle(outer0, inner1, inner0);
            }
        }

        /// <summary>
        /// 判断点是否在圆角矩形内（坐标原点为矩形左下角）。
        /// </summary>
        private bool IsInsideRoundedRect(float x, float y, float width, float height, float topLeftRadius, float topRightRadius, float bottomRightRadius, float bottomLeftRadius)
        {
            if (x < 0.0f || x > width || y < 0.0f || y > height)
            {
                return false;
            }

            if (x < bottomLeftRadius && y < bottomLeftRadius)
            {
                return IsPointInsideCorner(x, y, bottomLeftRadius, bottomLeftRadius, bottomLeftRadius);
            }

            if (x > width - bottomRightRadius && y < bottomRightRadius)
            {
                return IsPointInsideCorner(x, y, width - bottomRightRadius, bottomRightRadius, bottomRightRadius);
            }

            if (x > width - topRightRadius && y > height - topRightRadius)
            {
                return IsPointInsideCorner(x, y, width - topRightRadius, height - topRightRadius, topRightRadius);
            }

            if (x < topLeftRadius && y > height - topLeftRadius)
            {
                return IsPointInsideCorner(x, y, topLeftRadius, height - topLeftRadius, topLeftRadius);
            }

            return true;
        }

        /// <summary>
        /// 获取 Sprite 的 OuterUV；当 Sprite 为空时使用默认 UV。
        /// </summary>
        private Vector4 GetOuterUv()
        {
            Sprite sprite = overrideSprite;
            if (sprite == null)
            {
                return new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
            }

            return DataUtility.GetOuterUV(sprite);
        }

        /// <summary>
        /// 根据矩形位置线性映射到 OuterUV。
        /// </summary>
        private Vector2 GetUv(Vector2 position, Rect rect, Vector4 outerUv)
        {
            float u = (position.x - rect.xMin) / rect.width;
            float v = (position.y - rect.yMin) / rect.height;
            float uvX = Mathf.Lerp(outerUv.x, outerUv.z, u);
            float uvY = Mathf.Lerp(outerUv.y, outerUv.w, v);
            return new Vector2(uvX, uvY);
        }

        /// <summary>
        /// 判断点是否在某个圆角的四分之一圆内。
        /// </summary>
        private bool IsPointInsideCorner(float x, float y, float centerX, float centerY, float radius)
        {
            if (radius <= 0.0001f)
            {
                return true;
            }

            float dx = x - centerX;
            float dy = y - centerY;
            return dx * dx + dy * dy <= radius * radius;
        }
    }
}
