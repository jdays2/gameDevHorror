Shader "EmergencyShift/Localized Eye Burn"
{
    Properties
    {
        [PerRendererData] _MainTex ("Eye sprite", 2D) = "white" {}
        _MaskTex ("Light mask", 2D) = "white" {}
        _NormalMap ("Normal map", 2D) = "bump" {}
        _Darkening ("Overall darkening", Range(0, .7)) = .38
        _Desaturation ("Desaturation", Range(0, 1)) = .25
        _BruiseStrength ("Local redness", Range(0, 1)) = .65
        _BruiseTint ("Bruise multiplier", Color) = (.9, .55, .48, 1)
        _EyeRadius ("Eye radius in sprite units", Float) = .22
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half4 _BruiseTint;
            float _Darkening;
            float _Desaturation;
            float _BruiseStrength;
            float _EyeRadius;
        CBUFFER_END

        half3 BurnedTissue(half3 original, float2 position)
        {
            // Two unequal, fixed patches follow the organ across all atlas frames.
            // Only colour ratios change: the original folds, fibres and highlights remain.
            float2 p = position / max(_EyeRadius, .001);
            float edge = .07 * sin(p.y * 9 + p.x * 3) + .035 * sin(p.x * 17 - p.y * 6);
            float upper = length((p - float2(-.55, .23)) * float2(1.4, .95));
            float lower = length((p - float2(.28, -.55)) * float2(1.15, 1.65));
            half primary = 1 - smoothstep(.42, .96, upper + edge);
            half secondary = .6 * (1 - smoothstep(.22, .62, lower - edge));
            half localDamage = saturate(max(primary, secondary)) * _BruiseStrength;
            half luminance = dot(original, half3(.2126, .7152, .0722));
            half3 tissue = lerp(original, luminance.xxx, _Desaturation) * (1 - _Darkening);
            return lerp(tissue, tissue * _BruiseTint.rgb, localDamage);
        }
        ENDHLSL

        Pass
        {
            Name "EyeLit"
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex EyeVertex
            #pragma fragment EyeFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE
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
                float2 eyePosition : TEXCOORD4;
            };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"
            Varyings EyeVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonLitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                output.eyePosition = input.positionOS.xy;
                return output;
            }
            half4 EyeFragment(Varyings input) : SV_Target
            {
                half4 lit = CommonLitFragment(input, input.color);
                lit.rgb = BurnedTissue(lit.rgb, input.eyePosition);
                return lit;
            }
            ENDHLSL
        }

        Pass
        {
            Name "EyeNormals"
            Tags { "LightMode"="NormalsRendering" }
            HLSLPROGRAM
            #pragma vertex EyeNormalVertex
            #pragma fragment EyeNormalFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE
            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };
            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4 color : COLOR;
            };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"
            Varyings EyeNormalVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonNormalsVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }
            half4 EyeNormalFragment(Varyings input) : SV_Target
            {
                SetUpSpriteInstanceProperties();
                return CommonNormalsFragment(input, input.color);
            }
            ENDHLSL
        }
    }
}
