// 미니게임 액체·얼음용 툰 셰이더 (URP 17, Forward+ 대응).
// 패스: ToonForward(원신식 램프 + 하이라이트 띠 + 림) / ToonOutline(뒷면을 화면 기준으로 부풀림) / ShadowCaster / DepthOnly / DepthNormals.
// 불투명이다. 명암은 half-Lambert 램프로 정한다: 그림자 쪽은 차가운 색, 경계에는 따뜻한 색 띠, 밝은 쪽은 빛 색.
// 그림자 맵은 넓게 부드럽게 곱해 경계에 텍셀 계단이 생기지 않게 한다. 추가 조명은 부드럽게 더하고, SSAO·안개를 반영한다.
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
        _ShadowCoolTint("그림자 차가운 틴트", Color) = (0.78, 0.76, 0.92, 1)
        _TerminatorColor("명암 경계 따뜻한 띠 색", Color) = (1, 0.55, 0.35, 1)
        _TerminatorWidth("명암 경계 따뜻한 띠 폭", Range(0, 0.3)) = 0.08
        _Metallic("금속(반사 섞기)", Range(0, 1)) = 0
        _Smoothness("매끄러움(반사 흐림)", Range(0, 1)) = 0.5
        [HDR] _EmissionColor("발광 색", Color) = (0, 0, 0, 1)
        _PaintNoise("페인트 얼룩 세기", Range(0, 0.3)) = 0
        _PaintNoiseScale("페인트 얼룩 크기(1m당)", Range(0.5, 20)) = 4
        _HeightAO("높이 AO 세기(바닥·천장 근처 어둡게)", Range(0, 0.6)) = 0.25
        _FloorHeight("바닥 높이(월드 Y)", Float) = 0
        _CeilingHeight("천장 높이(월드 Y)", Float) = 3.05
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
            half4 _ShadowCoolTint;
            half4 _TerminatorColor;
            half _TerminatorWidth;
            half _Metallic;
            half _Smoothness;
            half4 _EmissionColor;
            half _PaintNoise;
            half _PaintNoiseScale;
            half _HeightAO;
            float _FloorHeight;
            float _CeilingHeight;
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
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
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
                half fogFactor : TEXCOORD3;
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
                output.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return output;
            }

            // 페인트 얼룩용 3D 값 노이즈 (월드 위치 기준이라 UV가 없어도 된다)
            float Hash3(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float ValueNoise3(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(Hash3(i), Hash3(i + float3(1, 0, 0)), f.x),
                                 lerp(Hash3(i + float3(0, 1, 0)), Hash3(i + float3(1, 1, 0)), f.x), f.y),
                            lerp(lerp(Hash3(i + float3(0, 0, 1)), Hash3(i + float3(1, 0, 1)), f.x),
                                 lerp(Hash3(i + float3(0, 1, 1)), Hash3(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }

            half4 ToonFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 n = normalize(input.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                // 원신식 램프: half-Lambert로 명암 위치를 정하고 경계는 1픽셀 안에서 끊는다.
                // 그림자 맵은 넓게 부드럽게 곱한다(좁은 램프에 넣으면 텍셀 계단이 경계에 드러난다)
                float halfLambert = dot(n, mainLight.direction) * 0.5 + 0.5;
                float shadow = smoothstep(0.05, 0.95, mainLight.shadowAttenuation);
                float x = halfLambert * lerp(0.4, 1.0, shadow);
                float threshold = _RampThreshold * 0.5 + 0.5;
                float edge = max(_RampSmooth, fwidth(x));
                float lit = smoothstep(threshold - edge, threshold + edge, x);
                // 경계의 따뜻한 띠: 그림자 쪽으로 조금 들어간 곳에서 가장 진하다
                float terminator = saturate(1.0 - abs(x - threshold + _TerminatorWidth * 0.5) / max(_TerminatorWidth, 1e-3)) * (1.0 - lit * 0.5);

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(screenUV);
                // 높이 AO: 바닥에서 솟은 면(벽 밑·가구 다리)과 천장 모서리 근처를 어둡게 (텍스처·패스 없이 계산만)
                // 바닥·천장 면 자체는 빼고(노멀이 위·아래), 천장보다 높은 바깥 건물에는 적용하지 않는다
                float y = input.positionWS.y;
                float floorT = max(smoothstep(0.0, 0.4, y - _FloorHeight), saturate(n.y * 2.0));
                float ceilT = max(smoothstep(0.0, 0.5, _CeilingHeight - y), saturate(-n.y * 2.0));
                ceilT = max(ceilT, step(_CeilingHeight + 0.05, y));
                half heightAO = lerp(1.0 - _HeightAO, 1.0, floorT * ceilT);
                half3 ambient = SampleSH(n) * ao.indirectAmbientOcclusion * heightAO;
                half3 lightColor = mainLight.color;
                half3 baseRgb = _BaseColor.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                // 페인트 얼룩: 단색 면에 손으로 칠한 듯한 밝기 차이를 준다
                float3 np = input.positionWS * _PaintNoiseScale;
                float paint = ValueNoise3(np) * 0.65 + ValueNoise3(np * 2.7 + 13.1) * 0.35;
                baseRgb *= 1.0 + (paint - 0.5) * 2.0 * _PaintNoise;
                // 금속은 확산광을 줄이고 반사를 더한다
                half3 diffuseRgb = baseRgb * (1.0 - _Metallic * 0.7);
                half3 bright = diffuseRgb * (lightColor * 0.8 + ambient * 0.6 + 0.2);
                half3 shade = diffuseRgb * _ShadeTint.rgb * _ShadowCoolTint.rgb * _ShadeStrength * (ambient + lightColor * 0.25 + 0.25);
                half3 col = lerp(shade, bright, lit);
                col += diffuseRgb * _TerminatorColor.rgb * (terminator * _TerminatorColor.a * 0.6);

                // 반사: 반사 프로브(하늘)를 매끄러움만큼 흐려 읽고, 비스듬히 볼수록 강하게
                if (_Metallic > 0.001)
                {
                    half3 reflDir = reflect(-viewDir, n);
                    half3 env = GlossyEnvironmentReflection(reflDir, input.positionWS, 1.0 - _Smoothness, ao.indirectAmbientOcclusion, screenUV);
                    half fresnel = 0.35 + 0.65 * pow(1.0 - saturate(dot(n, viewDir)), 3.0);
                    col += env * lerp(half3(0.04, 0.04, 0.04), baseRgb, _Metallic) * fresnel * _Metallic;
                }

                // 추가 조명: 램버트로 더해 Lit 재질과 같은 밝기의 빛 웅덩이를 만든다
                #if defined(_ADDITIONAL_LIGHTS) || USE_CLUSTER_LIGHT_LOOP
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = screenUV;
                half3 extra = 0;
                uint lightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    Light light = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                    half atten = light.distanceAttenuation * light.shadowAttenuation;
                    extra += light.color * (atten * saturate(dot(n, light.direction)));
                LIGHT_LOOP_END
                col += baseRgb * extra * ao.directAmbientOcclusion;
                #endif

                // 하이라이트 띠: 반사 벡터가 가까울 때만 켜는 단일 계단
                float3 h = normalize(mainLight.direction + viewDir);
                float spec = pow(saturate(dot(n, h)), _HighlightGloss);
                float specBand = step(1.0 - _HighlightSize, spec) * lit;
                col = lerp(col, _HighlightColor.rgb, specBand * _HighlightColor.a);

                // 림: 가장자리 한 줄
                float rim = 1.0 - saturate(dot(n, viewDir));
                float rimBand = step(1.0 - _RimAmount, rim) * lit;
                col = lerp(col, _RimColor.rgb, rimBand * _RimColor.a * 0.6);

                col *= lerp(1.0, heightAO, 0.5);
                col += _EmissionColor.rgb;
                col = MixFog(col, input.fogFactor);
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
