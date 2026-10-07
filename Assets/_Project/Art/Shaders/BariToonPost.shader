// 전역 카툰 후처리 (URP 17 Full Screen Pass용).
// 깊이·노멀 텍스처로 외곽선을 그리고, 밝기를 몇 단계로 나눈다. 재질은 바꾸지 않는다.
// Full Screen Pass Renderer Feature의 Requirements에 Depth, Normal, Color를 켜야 한다.
Shader "BariBarista/ToonPost"
{
    Properties
    {
        _EdgeColor("외곽선 색", Color) = (0.16, 0.09, 0.06, 1)
        _EdgeWidth("외곽선 두께(픽셀)", Range(0.5, 4)) = 1.2
        _DepthThreshold("깊이 경계(거리 비율)", Range(0.001, 0.2)) = 0.02
        _NormalThreshold("노멀 경계", Range(0.05, 2)) = 0.5
        _EdgeStrength("외곽선 세기", Range(0, 1)) = 1
        _BandCount("명암 단계 수", Range(2, 8)) = 4
        _BandStrength("명암 단계 적용 비율", Range(0, 1)) = 0.5
        _Saturation("채도 배율", Range(0.5, 1.5)) = 1.1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "ToonPost"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            half4 _EdgeColor;
            half _EdgeWidth;
            half _DepthThreshold;
            half _NormalThreshold;
            half _EdgeStrength;
            half _BandCount;
            half _BandStrength;
            half _Saturation;

            float EyeDepth(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                half3 col = src.rgb;

                // 1) 밝기 단계: 밝기를 몇 칸으로 나눠 만화처럼 끊는다. 색 비율은 유지한다.
                half lum = max(Luminance(col), 1e-4h);
                half banded = floor(lum * _BandCount + 0.5h) / _BandCount;
                half3 bandCol = col * (max(banded, 0.02h) / lum);
                col = lerp(col, bandCol, _BandStrength);

                // 2) 채도 배율
                half gray = Luminance(col);
                col = lerp(gray.xxx, col, _Saturation);

                // 3) 외곽선: 깊이 차이(거리 비율)와 노멀 차이를 십자 4방향에서 비교한다.
                float2 texel = _BlitTexture_TexelSize.xy * _EdgeWidth;
                float2 offs[4] = { float2(texel.x, 0), float2(-texel.x, 0), float2(0, texel.y), float2(0, -texel.y) };

                float d0 = EyeDepth(uv);
                float3 n0 = SampleSceneNormals(uv);
                float depthEdge = 0;
                float normalEdge = 0;
                [unroll] for (int i = 0; i < 4; i++)
                {
                    float2 suv = uv + offs[i];
                    float di = EyeDepth(suv);
                    depthEdge = max(depthEdge, abs(di - d0) / max(d0, 0.01));
                    normalEdge = max(normalEdge, 1.0 - saturate(dot(n0, SampleSceneNormals(suv))));
                }

                // 하늘(깊이 최대)은 경계에서 제외한다
                float rawDepth = SampleSceneDepth(uv);
            #if UNITY_REVERSED_Z
                float isSky = step(rawDepth, 1e-5);
            #else
                float isSky = step(0.99999, rawDepth);
            #endif
                float edge = saturate(max(step(_DepthThreshold, depthEdge), step(_NormalThreshold, normalEdge * 2.0)));
                edge *= _EdgeStrength * (1.0 - isSky);

                col = lerp(col, _EdgeColor.rgb, edge);
                return half4(col, src.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
