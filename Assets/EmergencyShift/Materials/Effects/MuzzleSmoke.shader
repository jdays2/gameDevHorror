Shader "EmergencyShift/Muzzle Smoke"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [PerRendererData] _Age ("Normalized age", Float) = 0
        [PerRendererData] _Seed ("Shape seed", Float) = 0
        [PerRendererData] _SideBurst ("Lateral gas mode", Float) = 0
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
                float _Age, _Seed, _SideBurst;
            CBUFFER_END
            float SmoothCloudUnion(float a, float b)
            {
                float blend = saturate(.5 + .5 * (b - a) / .24);
                return lerp(b, a, blend) - .24 * blend * (1 - blend);
            }
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
                if (_SideBurst > .5)
                {
                    float phase = _Seed + (p.x < 0 ? 2.73 : 0);
                    float reach = lerp(.10, .63, 1 - pow(1 - age, 2));
                    float2 q = float2(abs(p.x) - reach, p.y - sin(phase + age * 3) * .12);
                    float2 radius = lerp(float2(.14, .40), float2(.28, .85), age);
                    float body = length(q / radius);
                    float wisp = length((q - float2(.06, sin(phase) * .24)) / (radius * float2(.8, .72)));
                    float curl = sin(q.x * 21 + phase + age * 4) * sin(q.y * 7 - phase) * .12;
                    float density = 1 - smoothstep(.05, 1, min(body, wisp) + curl);
                    float fade = smoothstep(0, .06, age) * (1 - smoothstep(.12, 1, age));
                    return half4(input.color.rgb, density * fade * input.color.a);
                }
                // One stable cosmetic seed per shot. Vary the silhouette, never stripe its density.
                float4 shape = frac(sin((_Seed + 1.37) * float4(12.9898, 78.233, 39.425, 93.117)) * 43758.5453);
                float handedness = shape.z < .5 ? -1 : 1;
                float lean = lerp(-.18, .18, shape.w);
                p.x += p.y * lean + sin(_Seed + p.y * 1.6 + age * 1.2) * .045;
                float2 radius = float2(lerp(.54, .76, shape.x), lerp(.68, .84, shape.y));
                float main = length(p / radius);
                float2 shoulder = float2(handedness * lerp(.20, .32, shape.y), lerp(.02, .27, shape.w));
                float side = length((p - shoulder) / float2(lerp(.38, .51, shape.z), lerp(.40, .57, shape.x)));
                float2 lower = float2(-handedness * .22, lerp(-.28, -.08, shape.x));
                float wisp = length((p - lower) / float2(lerp(.32, .46, shape.w), lerp(.30, .46, shape.y)));
                float d = SmoothCloudUnion(SmoothCloudUnion(main, side), wisp);
                float density = .90 * (1 - smoothstep(.15, 1, d));
                float fade = smoothstep(0, .08, age) * (1 - smoothstep(.30, 1, age));
                return half4(input.color.rgb, density * fade * input.color.a);
            }
            ENDHLSL
        }
    }
}
