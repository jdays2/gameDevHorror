Shader "EmergencyShift/Tube Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        _CoreStrength ("White core strength", Range(0,2)) = .9
        _TubeStrength ("Coloured tube strength", Range(0,1)) = .36
        _HaloStrength ("Soft halo strength", Range(0,1)) = .22
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One One, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex TubeVertex
            #pragma fragment TubeFragment
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
            };
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _CoreStrength;
                half _TubeStrength;
                half _HaloStrength;
            CBUFFER_END

            Varyings TubeVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 TubeFragment(Varyings input) : SV_Target
            {
                // Full-rect sprite UV: long tube on X, soft cross-section on Y.
                half across = abs(input.uv.y * 2 - 1);
                half core = 1 - smoothstep(.025h, .10h, across);
                half glass = 1 - smoothstep(.20h, .42h, across);
                half halo = pow(saturate(1 - across), 2.8h);
                half ends = 1 - smoothstep(.96h, 1.0h, abs(input.uv.x * 2 - 1));
                half brightest = max(input.color.r, max(input.color.g, input.color.b));
                half3 hue = input.color.rgb / max(brightest, .001h);
                half3 glow = hue * (halo * _HaloStrength + glass * _TubeStrength);
                glow += lerp(hue, half3(1,1,1), .9h) * core * _CoreStrength;
                half opacity = saturate(glass + halo * .3h) * ends * input.color.a;
                return half4(glow * ends * input.color.a, opacity);
            }
            ENDHLSL
        }
    }
}
