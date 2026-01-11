Shader "Custom/ElectricCurrentFlow"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _LineColor ("Line Color", Color) = (0.1, 0.9, 1.0, 1.0)
        _GlowColor ("Glow Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _RimColor ("Rim Color", Color) = (0.25, 0.9, 1.0, 1.0)
        _RimPower ("Rim Power", Range(0.2, 10.0)) = 3.0

        [Enum(UV,0,UVSwap,1,WorldXZ,2,WorldXY,3,WorldYZ,4)] _FlowUVSource ("Flow Source", Float) = 0
        _FlowDir ("Flow Dir (x,y)", Vector) = (0.0, 1.0, 0.0, 0.0)
        _CoordScale ("Coord Scale", Range(0.01, 20.0)) = 1.0
        _CoordOffset ("Coord Offset", Vector) = (0.0, 0.0, 0.0, 0.0)

        _LineDensity ("Line Density", Range(0.1, 60.0)) = 10.0
        _LineWidth ("Line Width", Range(0.0, 0.5)) = 0.12

        _Speed ("Speed", Range(-10.0, 10.0)) = 2.5
        _ArrowSpacing ("Arrow Spacing", Range(0.1, 30.0)) = 6.0
        _ArrowLength ("Arrow Length", Range(0.05, 1.0)) = 0.45
        _ArrowWidth ("Arrow Width", Range(0.0, 0.5)) = 0.18
        _ArrowSharpness ("Arrow Sharpness", Range(0.1, 10.0)) = 3.0

        _PulseStrength ("Pulse Strength", Range(0.0, 3.0)) = 1.0
        _PulseWidth ("Pulse Width", Range(0.01, 1.0)) = 0.22

        _NoiseScale ("Noise Scale", Range(0.1, 40.0)) = 12.0
        _NoiseSpeed ("Noise Speed", Range(-10.0, 10.0)) = 1.4
        _NoiseIntensity ("Noise Intensity", Range(0.0, 1.0)) = 0.18

        _Alpha ("Alpha", Range(0.0, 2.0)) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            ZWrite Off
            Cull Back
            Blend SrcAlpha One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct a2v
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed4 _LineColor;
            fixed4 _GlowColor;
            fixed4 _RimColor;
            float _RimPower;

            float _FlowUVSource;
            float4 _FlowDir;
            float _CoordScale;
            float4 _CoordOffset;

            float _LineDensity;
            float _LineWidth;

            float _Speed;
            float _ArrowSpacing;
            float _ArrowLength;
            float _ArrowWidth;
            float _ArrowSharpness;

            float _PulseStrength;
            float _PulseWidth;

            float _NoiseScale;
            float _NoiseSpeed;
            float _NoiseIntensity;

            float _Alpha;

            float Hash21(float2 p)
            {
                float n = dot(p, float2(12.9898, 78.233));
                return frac(sin(n) * 43758.5453);
            }

            float Noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Tri01(float x)
            {
                float f = frac(x);
                return 1.0 - abs(f * 2.0 - 1.0);
            }

            v2f vert(a2v v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_TARGET
            {
                float3 n = normalize(i.worldNormal);
                float3 v = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                float fresnel = pow(1.0 - saturate(dot(n, v)), _RimPower);

                float t = _Time.y;

                float2 baseUV;
                if (_FlowUVSource < 0.5)
                {
                    baseUV = i.uv;
                }
                else if (_FlowUVSource < 1.5)
                {
                    baseUV = i.uv.yx;
                }
                else if (_FlowUVSource < 2.5)
                {
                    baseUV = i.worldPos.xz;
                }
                else if (_FlowUVSource < 3.5)
                {
                    baseUV = i.worldPos.xy;
                }
                else
                {
                    baseUV = i.worldPos.yz;
                }

                baseUV = baseUV * _CoordScale + _CoordOffset.xy;

                float2 dir = _FlowDir.xy;
                float dirLenSq = dot(dir, dir);
                dir = (dirLenSq > 1e-6) ? (dir * rsqrt(dirLenSq)) : float2(0.0, 1.0);
                float2 perp = float2(-dir.y, dir.x);

                float along = dot(baseUV, dir);
                float across = dot(baseUV, perp);

                float lineCell = frac(across * _LineDensity) - 0.5;
                float lineDist = abs(lineCell);
                float lineMask = smoothstep(_LineWidth, max(1e-5, _LineWidth * 0.65), lineDist);
                lineMask = 1.0 - lineMask;

                float arrowPhase = along * _ArrowSpacing - t * _Speed;
                float arrow01 = frac(arrowPhase);
                float arrowGate = 1.0 - smoothstep(_ArrowLength, _ArrowLength + 0.02, arrow01);

                float w = lerp(_ArrowWidth, 0.0, saturate(arrow01 / max(1e-5, _ArrowLength)));
                float arrowBody = smoothstep(w, max(1e-5, w * 0.6), lineDist);
                arrowBody = 1.0 - arrowBody;
                float arrowMask = pow(saturate(arrowBody * arrowGate), _ArrowSharpness);

                float pulse = Tri01(arrowPhase);
                pulse = pow(saturate(pulse / max(1e-5, _PulseWidth)), 2.0) * _PulseStrength;

                float2 nUV = baseUV * _NoiseScale + float2(t * _NoiseSpeed, t * (_NoiseSpeed * 0.73));
                float noise = Noise2D(nUV);
                float flicker = 1.0 + (noise - 0.5) * 2.0 * _NoiseIntensity;

                fixed4 texCol = tex2D(_MainTex, i.uv);
                float texLerp = saturate(dot(texCol.rgb, float3(0.3333, 0.3333, 0.3333)));

                float core = lineMask * (0.35 + 0.65 * texLerp);
                float glow = arrowMask * (1.0 + pulse);

                float3 col = _LineColor.rgb * core;
                col += _GlowColor.rgb * glow;
                col += _RimColor.rgb * fresnel;
                col *= flicker;

                float alpha = _Alpha;
                alpha *= saturate(core * 0.8 + glow * 0.9 + fresnel * 0.35);
                alpha = saturate(alpha);

                return fixed4(saturate(col), alpha);
            }
            ENDCG
        }
    }
}

