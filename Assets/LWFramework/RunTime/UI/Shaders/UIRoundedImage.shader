Shader "UI/LWFramework/RoundedImage"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        _RoundedRect("Rounded Rect", Vector) = (0,0,100,100)
        _RectCenter("Rect Center", Vector) = (0,0,0,0)
        _RectAxisRight("Rect Axis Right", Vector) = (1,0,0,0)
        _RectAxisUp("Rect Axis Up", Vector) = (0,1,0,0)
        _RectHalfSize("Rect Half Size", Vector) = (50,50,0,0)
        _CornerRadii("Corner Radii (TL,TR,BR,BL)", Vector) = (16,16,16,16)
        _BorderThickness("Border Thickness", Float) = 0
        _BorderColor("Border Color", Color) = (1,1,1,1)
        _Hollow("Hollow", Float) = 0

        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255

        _ColorMask("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest[unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask[_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;

            float4 _RoundedRect;
            float4 _RectCenter;
            float4 _RectAxisRight;
            float4 _RectAxisUp;
            float4 _RectHalfSize;
            float4 _CornerRadii;
            float _BorderThickness;
            fixed4 _BorderColor;
            float _Hollow;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            float SdRoundRect(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - halfSize + radius;
                float2 maxQ = max(q, 0.0);
                return length(maxQ) + min(max(q.x, q.y), 0.0) - radius;
            }

            float SelectCornerRadius(float2 p, float4 radii)
            {
                float isRight = step(0.0, p.x);
                float isTop = step(0.0, p.y);

                float topRadius = lerp(radii.x, radii.y, isRight);
                float bottomRadius = lerp(radii.w, radii.z, isRight);
                return lerp(bottomRadius, topRadius, isTop);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.texcoord) * i.color;

                float clipFactor = 1.0;

                #ifdef UNITY_UI_CLIP_RECT
                clipFactor = UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                texColor.a *= clipFactor;
                #endif

                float2 pCanvas = i.worldPosition.xy - _RectCenter.xy;
                float2 p = float2(dot(pCanvas, _RectAxisRight.xy), dot(pCanvas, _RectAxisUp.xy));
                float2 halfSize = _RectHalfSize.xy;

                float cornerRadius = SelectCornerRadius(p, _CornerRadii);
                float distOuter = SdRoundRect(p, halfSize, cornerRadius);

                float aaOuter = max(fwidth(distOuter), 0.0001);
                float outerMask = saturate(0.5 - distOuter / aaOuter);

                float borderThickness = max(_BorderThickness, 0.0);
                float borderMask = 0.0;
                if (borderThickness > 0.0001)
                {
                    float2 halfInner = halfSize - borderThickness;
                    if (halfInner.x > 0.0 && halfInner.y > 0.0)
                    {
                        float cornerRadiusInner = max(0.0, cornerRadius - borderThickness);
                        float distInner = SdRoundRect(p, halfInner, cornerRadiusInner);
                        float aaInner = max(fwidth(distInner), 0.0001);
                        float innerMask = saturate(0.5 - distInner / aaInner);
                        borderMask = saturate(outerMask - innerMask);
                    }
                    else
                    {
                        borderMask = outerMask;
                    }
                }

                float fillMask = (_Hollow > 0.5) ? 0.0 : outerMask;
                float insideMask = saturate(fillMask - borderMask);

                fixed4 borderColor = _BorderColor;
                borderColor.a *= clipFactor;

                fixed4 finalColor;
                finalColor.rgb = texColor.rgb * insideMask + borderColor.rgb * borderMask;
                finalColor.a = texColor.a * insideMask + borderColor.a * borderMask;

                #ifdef UNITY_UI_ALPHACLIP
                clip(finalColor.a - 0.001);
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
}
