Shader "EmergencyShift/Wall Cap"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        _InnerEdges ("Inner edges: left, right, bottom, top", Vector) = (1,1,1,1)
        _RimWidth ("Rim width in world units", Range(0,.08)) = .025
        _RimContrast ("Rim contrast", Range(0,.5)) = .3
        _SurfaceVariation ("Quiet surface variation", Range(0,.2)) = .045
    }
    SubShader
    {
        // Preserve the weak charcoal wall silhouette used by the existing unlit caps.
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "DisableBatching"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex CapVertex
            #pragma fragment CapFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
            };
            struct Varyings
            {
                COMMON_2D_OUTPUTS_SHARED
                half4 color : COLOR;
                float2 worldPosition : TEXCOORD1;
                float2 wallSize : TEXCOORD2;
            };
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _InnerEdges;
                float _RimWidth;
                half _RimContrast;
                half _SurfaceVariation;
            CBUFFER_END

            float CellValue(float2 cell)
            {
                return frac(sin(dot(cell, float2(127.1,311.7))) * 43758.5453);
            }

            float SurfaceValue(float2 position)
            {
                float2 cell = floor(position);
                float2 blend = frac(position);
                blend = blend * blend * (3 - 2 * blend);
                return lerp(lerp(CellValue(cell), CellValue(cell + float2(1,0)), blend.x),
                    lerp(CellValue(cell + float2(0,1)), CellValue(cell + 1), blend.x), blend.y);
            }

            Varyings CapVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color * _Color * unity_SpriteColor;
                output.worldPosition = TransformObjectToWorld(input.positionOS).xy;
                output.wallSize = float2(length(TransformObjectToWorldDir(float3(1,0,0), false)),
                    length(TransformObjectToWorldDir(float3(0,1,0), false)));
                return output;
            }

            half4 CapFragment(Varyings input) : SV_Target
            {
                half4 pixel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                float4 distanceToEdge = float4(input.uv.x, 1-input.uv.x, input.uv.y, 1-input.uv.y);
                distanceToEdge *= input.wallSize.xxyy;
                float4 antialias = max(fwidth(distanceToEdge), .001);
                half4 edges = (1 - smoothstep(_RimWidth-antialias, _RimWidth+antialias, distanceToEdge)) * _InnerEdges;
                half rim = max(max(edges.x,edges.y),max(edges.z,edges.w));
                half surface = (SurfaceValue(input.worldPosition * 1.7) - .5) * _SurfaceVariation;
                // One restrained surface repair on the first entry cap, inside its original footprint.
                float2 repair = (input.worldPosition - float2(3.7,0)) / float2(.5,.13);
                surface += exp(-dot(repair,repair)*3) * .025;
                pixel.rgb *= 1 + surface + rim * _RimContrast;
                return pixel;
            }
            ENDHLSL
        }
    }
}
