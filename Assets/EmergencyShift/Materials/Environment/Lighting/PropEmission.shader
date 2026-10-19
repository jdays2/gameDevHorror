Shader "EmergencyShift/Prop Emission"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        _EmissionKey ("Emissive surface hue", Color) = (0,.8,1,1)
        _MinimumChroma ("Minimum chroma", Range(0,1)) = .18
        _HueMatch ("Hue match", Range(0,1)) = .96
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
            #pragma vertex EmissionVertex
            #pragma fragment EmissionFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes { COMMON_2D_INPUTS half4 color : COLOR; };
            struct Varyings { COMMON_2D_OUTPUTS_SHARED half4 color : COLOR; };
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _EmissionKey;
                half _MinimumChroma;
                half _HueMatch;
            CBUFFER_END

            Varyings EmissionVertex(Attributes input)
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

            half4 EmissionFragment(Varyings input) : SV_Target
            {
                half4 pixel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half brightest = max(pixel.r, max(pixel.g, pixel.b));
                half darkest = min(pixel.r, min(pixel.g, pixel.b));
                half hue = dot(normalize(pixel.rgb + .0001), normalize(_EmissionKey.rgb + .0001));
                half mask = step(_MinimumChroma, brightest - darkest) * step(_HueMatch, hue);
                // Keep source texture detail; only the saturated aperture emits, never its housing.
                return half4(input.color.rgb * brightest, pixel.a * mask * input.color.a);
            }
            ENDHLSL
        }
    }
}
