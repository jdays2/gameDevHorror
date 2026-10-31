Shader "EmergencyShift/Scorpion Palette Lit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Dark ("Shadow pigment", Color) = (.12,.04,.055,1)
        _Middle ("Middle pigment", Color) = (.42,.115,.14,1)
        _Light ("Highlight pigment", Color) = (.64,.32,.27,1)
        _UseMaxChannel ("Use strongest channel", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #pragma vertex PaletteVertex
            #pragma fragment PaletteFragment
            #pragma multi_compile_instancing
            struct Attributes { COMMON_2D_INPUTS half4 color : COLOR; };
            struct Varyings { COMMON_2D_LIT_OUTPUTS half4 color : COLOR; };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Dark;
                half4 _Middle;
                half4 _Light;
                half _UseMaxChannel;
            CBUFFER_END
            Varyings PaletteVertex(Attributes input)
            {
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonLitVertex(input);
                output.color = input.color * unity_SpriteColor;
                return output;
            }
            half4 PaletteFragment(Varyings input) : SV_Target
            {
                half4 pixel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half value = lerp(dot(pixel.rgb, half3(.25,.60,.15)), max(pixel.r,max(pixel.g,pixel.b)), _UseMaxChannel);
                half3 pigment = lerp(_Dark.rgb, _Middle.rgb, smoothstep(0,.5,value));
                pigment = lerp(pigment, _Light.rgb, smoothstep(.45,1,value));
                SurfaceData2D surface;
                InputData2D lighting;
                InitializeSurfaceData(pigment * input.color.rgb, pixel.a * input.color.a, half4(1,1,1,1), half3(0,0,1), surface);
                InitializeInputData(input.uv, input.lightingUV, lighting);
                return CombinedShapeLightShared(surface, lighting);
            }
            ENDHLSL
        }
    }
}
