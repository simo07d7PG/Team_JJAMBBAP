// 미니게임 액체·얼음용 툰 셰이더 (URP 17, Forward+ 대응).
// 패스: ToonForward(2단 램프 + 하이라이트 띠 + 림) / ToonOutline(뒷면을 화면 기준으로 부풀림) / ShadowCaster / DepthOnly / DepthNormals.
// 불투명이다. 주광(Main Light)만 반영하고 추가 조명은 반영하지 않는다.
// 모든 값은 UnityPerMaterial에 있어 SRP Batcher로 묶인다. 컵 액체만 MaterialPropertyBlock으로 _BaseColor를 바꾼다.
Shader "BariBarista/Toon"
{
    Properties
    {
        [MainTexture] _BaseMap("밝은 면 무늬(없으면 흰색)", 2D) = "white" {}
        [MainColor] _BaseColor("밝은 면 색", Color) = (1, 1, 1, 1)
        _ShadeStrength("어두운 면 밝기 비율", Range(0, 1)) = 0.62
        _ShadeTint("어두운 면 색 틴트", Color) = (0.95, 0.85, 0.8, 1)
        _RampThreshold("명암 경계", Range(0, 1)) = 0.25
        _RampSmooth("명암 경계 부드러움", Range(0.001, 0.3)) = 0.03
        _HighlightColor("하이라이트 색", Color) = (1, 1, 1, 1)
        _HighlightSize("하이라이트 크기", Range(0, 1)) = 0.12
        _HighlightGloss("하이라이트 날카로움", Range(1, 256)) = 60
        _RimColor("림 색", Color) = (1, 1, 1, 1)
        _RimAmount("림 두께", Range(0, 1)) = 0.12
        _OutlineColor("외곽선 색", Color) = (0.16, 0.09, 0.06, 1)
        _OutlineWidth("외곽선 두께(화면 비율)", Range(0, 0.02)) = 0.0045
        [Toggle] _OutlineUsePosition("외곽선 방향을 위치로 계산(큐브·원기둥)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" "UniversalMaterialType" = "SimpleLit" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _ShadeStrength;
            half4 _ShadeTint;
            half _RampThreshold;
            half _RampSmooth;
            half4 _HighlightColor;
            half _HighlightSize;
            half _HighlightGloss;
            half4 _RimColor;
            half _RimAmount;
            half4 _OutlineColor;
            half _OutlineWidth;
            half _OutlineUsePosition;
            half _Cutoff;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ToonVert
            #pragma fragment ToonFrag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ToonVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 ToonFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 n = normalize(input.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                // 2단 램프: 빛과 그림자를 한 번에 본다
                float ndl = saturate(dot(n, mainLight.direction));
                float lit = smoothstep(_RampThreshold - _RampSmooth, _RampThreshold + _RampSmooth, ndl * mainLight.shadowAttenuation);

                half3 ambient = SampleSH(n);
                half3 lightColor = mainLight.color;
                half3 baseRgb = _BaseColor.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                half3 bright = baseRgb * (lightColor * 0.8 + ambient * 0.6 + 0.2);
                half3 shade = baseRgb * _ShadeTint.rgb * _ShadeStrength * (ambient + lightColor * 0.25 + 0.25);
                half3 col = lerp(shade, bright, lit);

                // 하이라이트 띠: 반사 벡터가 가까울 때만 켜는 단일 계단
                float3 h = normalize(mainLight.direction + viewDir);
                float spec = pow(saturate(dot(n, h)), _HighlightGloss);
                float specBand = step(1.0 - _HighlightSize, spec) * lit;
                col = lerp(col, _HighlightColor.rgb, specBand * _HighlightColor.a);

                // 림: 가장자리 한 줄
                float rim = 1.0 - saturate(dot(n, viewDir));
                float rimBand = step(1.0 - _RimAmount, rim) * lit;
                col = lerp(col, _RimColor.rgb, rimBand * _RimColor.a * 0.6);

                return half4(col, 1.0);
            }
            ENDHLSL
        }

        // 외곽선: 앞면을 지우고 뒷면만 화면 공간에서 부풀려 그린다. 두께는 거리와 상관없이 일정하다.
        Pass
        {
            Name "ToonOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings OutlineVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // 큐브·원기둥은 면마다 노멀이 갈라져 틈이 생기므로 원점에서 뻗는 방향을 쓴다
                float3 dirOS = _OutlineUsePosition > 0.5 ? input.positionOS.xyz : input.normalOS;
                float3 dirWS = normalize(TransformObjectToWorldDir(dirOS, false));
                float2 dirCS = mul((float3x3)UNITY_MATRIX_VP, dirWS).xy;
                dirCS = normalize(dirCS + 1e-5);

                float4 posCS = TransformObjectToHClip(input.positionOS.xyz);
                float aspect = _ScreenParams.x / _ScreenParams.y;
                // 클립 공간 xy는 w가 곱해진 값이라 w를 곱해야 화면 비율 두께가 거리와 상관없이 일정하다
                posCS.xy += dirCS * float2(1.0 / aspect, 1.0) * _OutlineWidth * 2.0 * posCS.w;
                output.positionCS = posCS;
                return output;
            }

            half4 OutlineFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return half4(_OutlineColor.rgb, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
