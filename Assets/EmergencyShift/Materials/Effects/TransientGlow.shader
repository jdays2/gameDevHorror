Shader "EmergencyShift/Transient Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture",2D)="white" {}
        [HideInInspector] _Color ("Tint",Color)=(1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex GlowVertex
            #pragma fragment GlowFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct Attributes { COMMON_2D_INPUTS half4 color:COLOR; };
            struct Varyings { COMMON_2D_OUTPUTS_SHARED half4 color:COLOR; };
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END
            Varyings GlowVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);SetUpSpriteInstanceProperties();
                input.positionOS=UnityFlipSprite(input.positionOS,unity_SpriteProps.xy);
                Varyings output=(Varyings)0;UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS=TransformObjectToHClip(input.positionOS);
                output.uv=input.uv;output.color=input.color*_Color*unity_SpriteColor;return output;
            }
            half4 GlowFragment(Varyings input):SV_Target
            {
                half alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv).a*input.color.a;
                return half4(input.color.rgb,alpha);
            }
            ENDHLSL
        }
    }
}
