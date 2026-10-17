Shader "EmergencyShift/Localized Eye Burn"
{
    Properties
    {
        [PerRendererData] _MainTex ("Eye sprite", 2D) = "white" {}
        _MaskTex ("Light mask", 2D) = "white" {}
        _NormalMap ("Normal map", 2D) = "bump" {}
        [PerRendererData] _BurnAmount ("Accumulated burn", Range(0, 1)) = 0
        _Redness ("Local ocular redness", Range(0, 1)) = .42
        _CloudColor ("Clouded pupil", Color) = (.76, .73, .65, 1)
        _IrisRadius ("Exposed iris radii in sprite units", Vector) = (.133, .126, 0, 0)
        _HalfOpening ("Half lid aperture ratio", Range(0, 1)) = .3
        _SlitOpening ("Slit aperture ratio", Range(0, 1)) = .025
        [PerRendererData] _GazeOffset ("Gaze in sprite units", Vector) = (0, 0, 0, 0)
        [PerRendererData] _AtlasUVPerUnit ("Atlas UV per sprite unit", Vector) = (1, 1, 0, 0)
        [PerRendererData] _EyeFrame ("Open / half / slit / shut", Float) = 0
        [PerRendererData] _PupilLayer ("Pupil layer", Float) = 0
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
            half4 _CloudColor;
            float4 _IrisRadius;
            float4 _GazeOffset;
            float4 _AtlasUVPerUnit;
            float _Redness;
            float _HalfOpening;
            float _SlitOpening;
            float _BurnAmount;
            float _EyeFrame;
            float _PupilLayer;
        CBUFFER_END

        float OcularMask(float2 position)
        {
            if (_EyeFrame > 2.5)
            {
                return 0;
            }

            float opening = 1;
            if (_EyeFrame > 1.5)
            {
                opening = _SlitOpening;
            }
            else if (_EyeFrame > .5)
            {
                opening = _HalfOpening;
            }

            float2 radius = max(_IrisRadius.xy * float2(1, opening), .001);
            return 1 - smoothstep(.91, 1, length(position / radius));
        }

        float CloudNoise(float2 position)
        {
            float2 cell = floor(position);
            float2 f = frac(position);
            f = f * f * (3 - 2 * f);
            float a = frac(sin(dot(cell, float2(127.1, 311.7))) * 43758.5453);
            float b = frac(sin(dot(cell + float2(1, 0), float2(127.1, 311.7))) * 43758.5453);
            float c = frac(sin(dot(cell + float2(0, 1), float2(127.1, 311.7))) * 43758.5453);
            float d = frac(sin(dot(cell + float2(1, 1), float2(127.1, 311.7))) * 43758.5453);
            return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
        }

        half3 CloudedPupil(half3 original, float2 position)
        {
            // A stable cloudy core with an irregular, still dark outer rim.
            float2 p = position * 2;
            float grain = .6 * CloudNoise(p * 3 + 4.7) + .4 * CloudNoise(p * 9 - 3.1);
            float edge = length(p) + .035 * (grain - .5);
            float cloud = 1 - smoothstep(.67, .97, edge);
            half3 tissue = _CloudColor.rgb * (.70 + .30 * grain);
            return lerp(original, tissue, saturate(_BurnAmount) * cloud);
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
                half4 main = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                if (_PupilLayer > .5)
                {
                    main.rgb = CloudedPupil(main.rgb, input.eyePosition);
                }
                else
                {
                    float aperture = OcularMask(input.eyePosition);
                    float radialDistance = length(input.eyePosition / max(_IrisRadius.xy, .001));
                    float warpWeight = 1 - smoothstep(.45, 1, radialDistance);
                    float2 shiftedUV = input.uv - _GazeOffset.xy * _AtlasUVPerUnit.xy * warpWeight;
                    half3 shifted = input.color.rgb * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, shiftedUV).rgb;
                    main.rgb = lerp(main.rgb, shifted, aperture);

                    float2 p = (input.eyePosition - _GazeOffset.xy) / max(_IrisRadius.xy, .001);
                    float patch = 1 - smoothstep(.15, .75, length((p - float2(-.48, .22)) * float2(1.1, 1.5)));
                    float redness = _BurnAmount * _Redness * patch * aperture;
                    main.rgb *= lerp(half3(1, 1, 1), half3(1, .48, .43), redness);
                }

                // Change the surface before URP lighting, so neither lids nor cloudy pupils emit light.
                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(main.rgb, main.a, mask, normalTS, surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);
                #if defined(DEBUG_DISPLAY)
                    SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);
                    surfaceData.normalWS = input.normalWS;
                #endif
                return CombinedShapeLightShared(surfaceData, inputData);
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
