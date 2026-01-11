Shader "Custom/HoloScan"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.0, 1.0, 1.0, 1.0)
        _RimColor ("Rim Color", Color) = (0.1, 0.6, 1.0, 1.0)
        _RimPower ("Rim Power", Range(0.1, 10.0)) = 3.0
        _ScanColor ("Scan Color", Color) = (1.0, 0.2, 1.0, 1.0)
        _ScanLineDensity ("Scan Line Density", Range(0.0, 50.0)) = 12.0
        _ScanSpeed ("Scan Speed", Range(-10.0, 10.0)) = 2.0
        _BandWidth ("Band Width", Range(0.01, 1.0)) = 0.18
        _BandIntensity ("Band Intensity", Range(0.0, 3.0)) = 1.2
        _NoiseScale ("Noise Scale", Range(0.1, 30.0)) = 7.0
        _NoiseIntensity ("Noise Intensity", Range(0.0, 1.0)) = 0.25
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
            fixed4 _ScanColor;
            float _ScanLineDensity;
            float _ScanSpeed;
            float _BandWidth;
            float _BandIntensity;
            float _NoiseScale;
            float _NoiseIntensity;
            float _Alpha;

            float Hash31(float3 p)
            {
                float n = dot(p, float3(12.9898, 78.233, 37.719));
                return frac(sin(n) * 43758.5453);
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

                float scanPhase = (i.worldPos.y * _ScanLineDensity + _Time.y * _ScanSpeed) * 6.2831853;
                float scanLine = 0.5 + 0.5 * sin(scanPhase);
                scanLine = saturate(scanLine);

                float bandPhase = frac(i.worldPos.y * 0.65 + _Time.y * 0.35);
                float band = 1.0 - abs(bandPhase - 0.5) * 2.0;
                band = saturate(band / max(1e-4, _BandWidth));
                band = band * band;

                float noise = Hash31(floor(i.worldPos * _NoiseScale) + _Time.yyy);

                fixed4 albedo = tex2D(_MainTex, i.uv);
                float3 baseCol = albedo.rgb * _BaseColor.rgb;

                float3 col = baseCol * (0.25 + 0.75 * scanLine);
                col += _ScanColor.rgb * (band * _BandIntensity);
                col += _RimColor.rgb * fresnel;
                col += (noise - 0.5) * _NoiseIntensity;

                float alpha = _Alpha;
                alpha *= saturate(0.2 + fresnel * 1.0 + scanLine * 0.6 + band * _BandIntensity);
                alpha += (noise - 0.5) * _NoiseIntensity * 0.6;
                alpha = saturate(alpha);

                return fixed4(saturate(col), alpha);
            }
            ENDCG
        }
    }
}

