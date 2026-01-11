Shader "Custom/MCP_CoolNeon"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.08, 0.95, 1.0, 1.0)
        _RimColor ("Rim Color", Color) = (0.9, 0.2, 1.0, 1.0)
        _RimPower ("Rim Power", Range(0.1, 10.0)) = 3.5
        _GlowColor ("Glow Color", Color) = (0.25, 1.2, 0.9, 1.0)
        _GlowIntensity ("Glow Intensity", Range(0.0, 6.0)) = 2.2

        _ScrollSpeed ("Scroll Speed", Range(-10.0, 10.0)) = 1.0
        _WarpScale ("Warp Scale", Range(0.1, 20.0)) = 6.0
        _WarpStrength ("Warp Strength", Range(0.0, 2.0)) = 0.55

        _StripeDensity ("Stripe Density", Range(0.0, 80.0)) = 24.0
        _StripeSpeed ("Stripe Speed", Range(-10.0, 10.0)) = 2.6
        _StripeWidth ("Stripe Width", Range(0.01, 1.0)) = 0.25

        _Alpha ("Alpha", Range(0.0, 2.0)) = 0.9
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            ZWrite Off
            Cull Off
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

            fixed4 _BaseColor;
            fixed4 _RimColor;
            float _RimPower;
            fixed4 _GlowColor;
            float _GlowIntensity;

            float _ScrollSpeed;
            float _WarpScale;
            float _WarpStrength;

            float _StripeDensity;
            float _StripeSpeed;
            float _StripeWidth;

            float _Alpha;

            float Hash21(float2 p)
            {
                float n = dot(p, float2(127.1, 311.7));
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

                float2 uv = i.uv;
                uv.y += t * _ScrollSpeed;

                float2 p = (uv - 0.5) * _WarpScale;
                float ang = atan2(p.y, p.x);
                float rad = length(p);

                float warp = Noise2D(p + float2(t * 0.35, t * 0.27));
                ang += (warp - 0.5) * _WarpStrength * 3.14159;

                float2 warpedUv = float2(cos(ang), sin(ang)) * rad;
                warpedUv = warpedUv / _WarpScale + 0.5;

                fixed4 albedo = tex2D(_MainTex, warpedUv);

                float stripePhase = (i.worldPos.y * _StripeDensity + t * _StripeSpeed) * 6.2831853;
                float stripe = 0.5 + 0.5 * sin(stripePhase);
                stripe = saturate((stripe - (1.0 - _StripeWidth)) / max(1e-4, _StripeWidth));
                stripe = stripe * stripe;

                float glow = (_GlowIntensity * (0.25 + 0.75 * stripe)) * (0.35 + 0.65 * fresnel);

                float3 col = albedo.rgb * _BaseColor.rgb;
                col += _RimColor.rgb * fresnel;
                col += _GlowColor.rgb * glow;

                float alpha = _Alpha;
                alpha *= saturate(0.25 + fresnel * 1.0 + stripe * 0.85);
                alpha = saturate(alpha);

                return fixed4(saturate(col), alpha);
            }
            ENDCG
        }
    }
}
