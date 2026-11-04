// 창문 빛줄기. 가산 블렌드 반투명 메쉬에 쓴다.
// 메쉬: 창 사각형의 네 변마다 면 하나. 모든 정점은 창 위에 있고, UV.y = 1인 정점만 버텍스 셰이더가
// 현재 해 방향(주광)으로 바닥 높이까지 밀어낸다. 그래서 해를 돌리면 따라가고, 바닥 햇빛 자국에서 끝난다.
// 탄젠트 = 그 면이 붙은 창 변의 방향(로컬). 면 노멀은 변 방향 × 해 방향으로 다시 구한다.
// 조각마다 주광 그림자 맵을 한 번 읽어, 실제로 햇빛이 닿는 공기에서만 보인다(처마·창틀·가구에 잘림).
Shader "BariBarista/LightShaft"
{
    Properties
    {
        [HDR] _Color("빛 색", Color) = (1, 0.82, 0.6, 1)
        _Intensity("세기", Range(0, 2)) = 0.35
        _LengthFade("길이 감쇠", Range(0.3, 4)) = 1.2
        _EdgeSoftness("가장자리 부드러움", Range(0.01, 0.5)) = 0.2
        _NoiseScale("일렁임 크기", Range(0.5, 20)) = 5
        _NoiseSpeed("일렁임 속도", Range(0, 1)) = 0.08
        _FloorHeight("바닥 높이(월드 Y)", Float) = 0
        _MaxLength("최대 길이(m)", Float) = 8
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Shaft"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _LengthFade;
                half _EdgeSoftness;
                half _NoiseScale;
                half _NoiseSpeed;
                float _FloorHeight;
                float _MaxLength;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float4 tangentOS : TANGENT; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half fade : TEXCOORD3;
                half fogFactor : TEXCOORD4;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                float3 sunDir = -_MainLightPosition.xyz;           // 빛이 나아가는 방향
                float3 baseWS = TransformObjectToWorld(input.positionOS.xyz);
                float down = max(-sunDir.y, 0.05);
                float len = min((baseWS.y - _FloorHeight) / down, _MaxLength);
                float3 posWS = baseWS + sunDir * len * input.uv.y;
                float3 edgeWS = TransformObjectToWorldDir(input.tangentOS.xyz);
                o.normalWS = normalize(cross(edgeWS, sunDir));
                // 해가 창 안쪽(로컬 +z)으로 들어오지 않으면 끈다
                float3 inward = TransformObjectToWorldDir(float3(0, 0, 1));
                o.fade = saturate(dot(sunDir, inward) * 4.0);
                o.positionWS = posWS;
                o.positionCS = TransformWorldToHClip(posWS);
                o.uv = input.uv;
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            float Hash3(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float Noise3(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(Hash3(i), Hash3(i + float3(1, 0, 0)), f.x), lerp(Hash3(i + float3(0, 1, 0)), Hash3(i + float3(1, 1, 0)), f.x), f.y),
                            lerp(lerp(Hash3(i + float3(0, 0, 1)), Hash3(i + float3(1, 0, 1)), f.x), lerp(Hash3(i + float3(0, 1, 1)), Hash3(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float v = input.uv.y;
                float u = input.uv.x;
                float lengthMask = pow(saturate(1.0 - v), _LengthFade) * smoothstep(0.0, 0.04, v);
                float edgeMask = smoothstep(0.0, _EdgeSoftness, u) * smoothstep(0.0, _EdgeSoftness, 1.0 - u);
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float facing = abs(dot(normalize(input.normalWS), viewDir));
                float viewMask = smoothstep(0.05, 0.5, facing);
                // 이 공기에 햇빛이 실제로 닿는지 (그림자 맵 1회)
                float sunlit = MainLightRealtimeShadow(TransformWorldToShadowCoord(input.positionWS));
                float dust = Noise3(input.positionWS * _NoiseScale + float3(0, -_Time.y * _NoiseSpeed, _Time.y * _NoiseSpeed * 0.5));
                float a = lengthMask * edgeMask * viewMask * sunlit * input.fade * (0.65 + dust * 0.7) * _Intensity;
                half3 col = _Color.rgb * _MainLightColor.rgb * a;
                col *= ComputeFogIntensity(input.fogFactor);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
