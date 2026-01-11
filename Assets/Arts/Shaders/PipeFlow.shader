Shader "Custom/PipeFlow"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _LiquidColor ("Liquid Color", Color) = (0.1, 0.9, 1.0, 1.0)
        _DeepColor ("Deep Color", Color) = (0.0, 0.25, 0.45, 1.0)
        _RimColor ("Rim Color", Color) = (0.25, 0.9, 1.0, 1.0)
        _RimPower ("Rim Power", Range(0.2, 10.0)) = 3.0

        [Enum(UV,0,UVSwap,1,WorldXZ,2,WorldXY,3,WorldYZ,4)] _FlowUVSource ("Flow Source", Float) = 0
        _FlowDir ("Flow Dir (x,y)", Vector) = (0.0, 1.0, 0.0, 0.0)
        _CoordScale ("Coord Scale", Range(0.01, 20.0)) = 1.0
        _CoordOffset ("Coord Offset", Vector) = (0.0, 0.0, 0.0, 0.0)

        _FlowTiling ("Flow Tiling", Range(0.1, 30.0)) = 6.0
        _FlowSpeed ("Flow Speed", Range(-10.0, 10.0)) = 2.0
        _SwirlStrength ("Swirl Strength", Range(0.0, 3.0)) = 0.6

        _NoiseScale ("Noise Scale", Range(0.1, 30.0)) = 8.0
        _NoiseSpeed ("Noise Speed", Range(-10.0, 10.0)) = 1.2
        _NoiseDistort ("Noise Distort", Range(0.0, 0.5)) = 0.08

        _FoamColor ("Foam Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _FoamIntensity ("Foam Intensity", Range(0.0, 3.0)) = 1.0
        _FoamWidth ("Foam Width", Range(0.0, 0.5)) = 0.08

        _Alpha ("Alpha", Range(0.0, 2.0)) = 0.9
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

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

            fixed4 _LiquidColor;
            fixed4 _DeepColor;
            fixed4 _RimColor;
            float _RimPower;

            float _FlowUVSource;
            float4 _FlowDir;
            float _CoordScale;
            float4 _CoordOffset;

            float _FlowTiling;
            float _FlowSpeed;
            float _SwirlStrength;

            float _NoiseScale;
            float _NoiseSpeed;
            float _NoiseDistort;

            fixed4 _FoamColor;
            float _FoamIntensity;
            float _FoamWidth;

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

                float along0 = dot(baseUV, dir);
                float across0 = dot(baseUV, perp);
                float swirl = sin((across0 + t * 0.6) * 6.2831853) * _SwirlStrength;

                float2 uvLocal;
                uvLocal.x = across0;
                uvLocal.y = along0;
                uvLocal.y = uvLocal.y * _FlowTiling + t * _FlowSpeed + swirl;
                float2 uvFlow = perp * uvLocal.x + dir * uvLocal.y;

                float2 nUV = (baseUV * _NoiseScale) + float2(t * _NoiseSpeed, t * (_NoiseSpeed * 0.73));
                float n0 = Noise2D(nUV);
                float n1 = Noise2D(nUV + 13.37);
                float2 distort = (float2(n0, n1) - 0.5) * _NoiseDistort;
                uvFlow += distort;

                float flowCoord = dot(uvFlow, dir);
                float flow = 0.5 + 0.5 * sin(flowCoord * 6.2831853);
                flow = saturate(flow);

                float foam = smoothstep(1.0 - _FoamWidth, 1.0, flow);

                fixed4 albedo = tex2D(_MainTex, uvFlow);
                float depthLerp = saturate(0.15 + flow * 0.85);
                float3 col = lerp(_DeepColor.rgb, _LiquidColor.rgb, depthLerp);
                col *= (0.65 + 0.35 * albedo.rgb);
                col += _RimColor.rgb * fresnel;
                col += _FoamColor.rgb * (foam * _FoamIntensity);

                float alpha = _Alpha;
                alpha *= saturate(0.25 + fresnel * 0.9 + flow * 0.6);
                alpha += foam * 0.25;
                alpha = saturate(alpha);

                return fixed4(saturate(col), alpha);
            }
            ENDCG
        }
    }
}
