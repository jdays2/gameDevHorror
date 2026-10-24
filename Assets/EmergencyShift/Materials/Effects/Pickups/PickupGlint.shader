Shader "EmergencyShift/Pickup Glint"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _GlintColor ("Glint", Color) = (.85,.85,.78,.78)
        _Progress ("Progress", Float) = .5
        _StripeWidth ("Stripe width", Float) = .2
        _SweepPlane ("World sweep", Vector) = (1,1,0,1)
        _BeamOriginRange ("Flashlight origin/range", Vector) = (0,0,4.2,0)
        _BeamDirectionCos ("Flashlight direction/cosine", Vector) = (0,1,.76,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex GlintVertex
            #pragma fragment GlintFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes { COMMON_2D_INPUTS half4 color : COLOR; };
            struct Varyings { COMMON_2D_OUTPUTS_SHARED half4 color : COLOR; float2 world : TEXCOORD1; };
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _GlintColor;
                float4 _SweepPlane;
                float4 _BeamOriginRange;
                float4 _BeamDirectionCos;
                float _Progress;
                float _StripeWidth;
            CBUFFER_END

            Varyings GlintVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.world = TransformObjectToWorld(input.positionOS).xy;
                output.uv = input.uv;
                output.color = input.color * unity_SpriteColor;
                return output;
            }

            half4 GlintFragment(Varyings input) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * input.color.a;
                float sweep = (dot(input.world, _SweepPlane.xy) - _SweepPlane.z) / max(.001, _SweepPlane.w);
                float phase = lerp(-_StripeWidth, 1 + _StripeWidth, _Progress);
                float stripe = 1 - smoothstep(_StripeWidth * .35, _StripeWidth * .5, abs(sweep - phase));
                float2 offset = input.world - _BeamOriginRange.xy;
                float distanceToLight = length(offset);
                float facing = dot(offset / max(.0001, distanceToLight), _BeamDirectionCos.xy);
                float beam = step(distanceToLight, _BeamOriginRange.z) * smoothstep(_BeamDirectionCos.z, _BeamDirectionCos.z + .025, facing);
                float envelope = sin(saturate(_Progress) * 3.14159265);
                return half4(_GlintColor.rgb, alpha * _GlintColor.a * stripe * beam * envelope);
            }
            ENDHLSL
        }
    }
}
