Shader "UI/BackdropBlur_GaussianMip"
{
    Properties
    {
        _MainTex ("Background Texture", 2D) = "white" {}
        _BlurStrength ("Blur Strength", Range(0.5, 2.0)) = 1.0
        _Tint ("Glass Tint", Color) = (1,1,1,0.85)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_TexelSize;
            float _BlurStrength;
            float4 _Tint;

            // --- Tunables (from IQ shader) ---
            #define SAMPLES 32
            #define LOD 2
            #define SIGMA (SAMPLES * 0.25)

            float gaussian(float2 i)
            {
                i /= SIGMA;
                return exp(-0.5 * dot(i, i)) / (6.2831853 * SIGMA * SIGMA);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 col = 0;
                half totalWeight = 0;

                int s = SAMPLES >> LOD;
                float2 texel = _MainTex_TexelSize.xy * _BlurStrength;

                for (int y = 0; y < s; y++)
                {
                    for (int x = 0; x < s; x++)
                    {
                        float2 d = (float2(x, y) * (1 << LOD)) - (SAMPLES * 0.5);
                        float w = gaussian(d);
                        col += w * SAMPLE_TEXTURE2D_LOD(
                            _MainTex,
                            sampler_MainTex,
                            i.uv + d * texel,
                            LOD
                        );
                        totalWeight += w;
                    }
                }

                col /= totalWeight;
                col *= _Tint;
                return col;
            }
            ENDHLSL
        }
    }
}
