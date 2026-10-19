Shader "EmergencyShift/Art Review/Furniture Light Study"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _MaskTex ("Light mask", 2D) = "white" {}
        _NormalMap ("Normal map", 2D) = "bump" {}
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        _StudySurfaceRect ("Surface center and half size", Vector) = (0, 0, .5, .5)
        _StudyLightLocal ("Handheld source in sprite space", Vector) = (0, -2, 0, 0)
        _StudyResponse ("Top / edge / width / edge enabled", Vector) = (.12, .9, .06, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "DisableBatching"="True" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "FurnitureStudyLit"
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex StudyVertex
            #pragma fragment StudyFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };
            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color : COLOR;
                float2 surfacePosition : TEXCOORD4;
            };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _StudySurfaceRect;
                float4 _StudyLightLocal;
                float4 _StudyResponse;
                float4x4 _StudyWorldToSurface;
            CBUFFER_END

            Varyings StudyVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonLitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                float3 worldPosition = TransformObjectToWorld(input.positionOS);
                output.surfacePosition = mul(_StudyWorldToSurface, float4(worldPosition, 1)).xy;
                return output;
            }

            half HandheldResponse(float2 position)
            {
                float2 p = position - _StudySurfaceRect.xy;
                float2 distances = _StudySurfaceRect.zw - abs(p);
                float width = max(_StudyResponse.z, .001);
                float edge = 1 - smoothstep(width * .35, width * 1.35, abs(min(distances.x, distances.y)));
                float xWeight = saturate(.5 + (distances.y - distances.x) / (2 * width));
                float2 normal = float2(sign(p.x) * xWeight, sign(p.y) * (1 - xWeight));
                normal /= max(length(normal), .001);
                float2 toLight = _StudyLightLocal.xy - position;
                toLight /= max(length(toLight), .001);
                float facing = saturate(dot(normal, toLight));
                half edgeResponse = max(_StudyResponse.x, _StudyResponse.y * facing);
                return lerp(_StudyResponse.x, edgeResponse, edge * _StudyResponse.w);
            }

            half4 StudyFragment(Varyings input) : SV_Target
            {
                half4 main = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(main.a - .001);
                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                half3 modulate = 0;
                half3 additive = 0;

                // The study assigns handheld lights to style 0, ambient/ceiling to style 2.
                // Both use the renderer's existing textures and shadow results; no emissive rim.
                #if USE_SHAPE_LIGHT_TYPE_0
                    half4 handheld = SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, input.lightingUV);
                    if (any(_ShapeLightMaskFilter0))
                    {
                        half4 processed = (1 - _ShapeLightInvertedFilter0) * mask + _ShapeLightInvertedFilter0 * (1 - mask);
                        handheld *= dot(processed, _ShapeLightMaskFilter0);
                    }
                    handheld *= HandheldResponse(input.surfacePosition);
                    modulate += handheld.rgb * _ShapeLightBlendFactors0.x;
                    additive += handheld.rgb * _ShapeLightBlendFactors0.y;
                #endif
                #if USE_SHAPE_LIGHT_TYPE_2
                    half4 ceiling = SAMPLE_TEXTURE2D(_ShapeLightTexture2, sampler_ShapeLightTexture2, input.lightingUV);
                    if (any(_ShapeLightMaskFilter2))
                    {
                        half4 processed = (1 - _ShapeLightInvertedFilter2) * mask + _ShapeLightInvertedFilter2 * (1 - mask);
                        ceiling *= dot(processed, _ShapeLightMaskFilter2);
                    }
                    modulate += ceiling.rgb * _ShapeLightBlendFactors2.x;
                    additive += ceiling.rgb * _ShapeLightBlendFactors2.y;
                #endif
                return half4(max(0, _HDREmulationScale * (main.rgb * modulate + additive)), main.a);
            }
            ENDHLSL
        }

    }
}
