// 원신풍 하늘 (스카이박스). 위는 파랑, 수평선은 복숭아색 그라데이션, 해 원반·후광, 셀 모양 구름.
// 해 방향은 주광(Directional Light)을 따른다. Lighting 창의 Skybox Material에 넣는다.
Shader "BariBarista/Sky"
{
    Properties
    {
        _ZenithColor("하늘 꼭대기 색", Color) = (0.26, 0.5, 0.88, 1)
        _SkyColor("하늘 중간 색", Color) = (0.55, 0.76, 0.96, 1)
        _HorizonColor("수평선 색", Color) = (1, 0.84, 0.7, 1)
        _GroundColor("지평선 아래 색", Color) = (0.42, 0.38, 0.36, 1)
        _SunColor("해 색", Color) = (1, 0.92, 0.75, 1)
        _SunSize("해 크기", Range(0.001, 0.1)) = 0.02
        _SunGlow("해 후광", Range(0, 2)) = 0.6
        _CloudColor("구름 밝은 면", Color) = (1, 0.98, 0.95, 1)
        _CloudShade("구름 어두운 면", Color) = (0.78, 0.78, 0.9, 1)
        _CloudCover("구름 양", Range(0, 1)) = 0.45
        _CloudScale("구름 크기", Range(0.5, 8)) = 2.5
        _CloudSpeed("구름 속도", Range(0, 0.05)) = 0.005
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _SkyColor;
                half4 _HorizonColor;
                half4 _GroundColor;
                half4 _SunColor;
                half _SunSize;
                half _SunGlow;
                half4 _CloudColor;
                half4 _CloudShade;
                half _CloudCover;
                half _CloudScale;
                half _CloudSpeed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.dir = input.positionOS.xyz;
                return o;
            }

            float Hash2(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise2(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash2(i), Hash2(i + float2(1, 0)), f.x), lerp(Hash2(i + float2(0, 1)), Hash2(i + float2(1, 1)), f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int k = 0; k < 4; k++) { v += Noise2(p) * a; p = p * 2.03 + 7.1; a *= 0.5; }
                return v;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.dir);
                float y = dir.y;

                // 하늘 그라데이션: 수평선 → 중간 → 꼭대기, 아래는 지면색
                half3 col = lerp(_HorizonColor.rgb, _SkyColor.rgb, smoothstep(0.0, 0.25, y));
                col = lerp(col, _ZenithColor.rgb, smoothstep(0.25, 0.9, y));
                col = lerp(col, _GroundColor.rgb, smoothstep(0.0, -0.08, y));

                // 해: 원반 + 후광
                float3 sunDir = _MainLightPosition.xyz;
                float sd = dot(dir, sunDir);
                float disk = smoothstep(1.0 - _SunSize, 1.0 - _SunSize + 0.002, sd);
                float glow = pow(saturate(sd), 48.0) * _SunGlow + pow(saturate(sd), 6.0) * _SunGlow * 0.25;
                col += _SunColor.rgb * (disk * 3.0 + glow);

                // 셀 구름: 하늘을 평면에 투영한 fbm을 문턱으로 끊고, 아래쪽은 어둡게
                if (y > 0.0)
                {
                    float2 uv = dir.xz / (y + 0.15) * _CloudScale * 0.25 + _Time.y * _CloudSpeed;
                    float n = Fbm(uv);
                    float th = 1.0 - _CloudCover * 0.6;
                    float aa = max(fwidth(n), 1e-4);
                    float cloud = smoothstep(th - aa, th + aa, n) * smoothstep(0.02, 0.15, y);
                    float shadeN = Fbm(uv + float2(0.05, -0.08));
                    float lit = smoothstep(th + 0.04 - aa, th + 0.04 + aa, shadeN);
                    half3 cloudCol = lerp(_CloudShade.rgb, _CloudColor.rgb, lit);
                    cloudCol += _SunColor.rgb * pow(saturate(sd), 8.0) * 0.4;
                    col = lerp(col, cloudCol, cloud);
                }
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
