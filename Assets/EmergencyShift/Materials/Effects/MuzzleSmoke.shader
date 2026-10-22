Shader "EmergencyShift/Muzzle Smoke"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [PerRendererData] _Age ("Normalized age", Float) = 0
        [PerRendererData] _Seed ("Shape seed", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex SmokeVertex
            #pragma fragment SmokeFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct Attributes { COMMON_2D_INPUTS half4 color:COLOR; };
            struct Varyings { COMMON_2D_OUTPUTS_SHARED half4 color:COLOR; };
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Age, _Seed;
            CBUFFER_END
            Varyings SmokeVertex(Attributes input)
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
            half4 SmokeFragment(Varyings input) : SV_Target
            {
                float age = saturate(_Age);
                float2 p = (input.uv - .5) * 2;
                float bend = sin(_Seed + p.y * 5 + age * 3) * .13;
                p.x += bend;
                float main = length(p / float2(.72, .8));
                float side = length((p - float2(.26, .15)) / float2(.48, .52));
                float d = min(main, side);
                float curl = .86 + .14 * sin(p.x * 11 + p.y * 8 + _Seed + age * 5);
                float density = (1 - smoothstep(.15, 1, d)) * curl;
                float fade = smoothstep(0, .12, age) * (1 - smoothstep(.15, 1, age));
                return half4(input.color.rgb, density * fade * input.color.a);
            }
            ENDHLSL
        }
    }
}
