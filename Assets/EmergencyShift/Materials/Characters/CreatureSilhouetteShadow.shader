Shader "EmergencyShift/Creature Silhouette Shadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Pivot ("Pivot", Vector) = (0,0,0,0)
        [HideInInspector] _Away ("Direction", Vector) = (0,1,0,0)
        [HideInInspector] _Offset ("Offset", Float) = 0
        [HideInInspector] _Stretch ("Stretch", Float) = 0
        [HideInInspector] _UseLightClip ("Use Light Clip", Float) = 0
        [HideInInspector] _Light ("Light Origin / Range / Cos Angle", Vector) = (0,0,4.2,0)
        [HideInInspector] _LightDirection ("Light Direction", Vector) = (0,1,0,0)
        [HideInInspector] _UseWallClip ("Use Wall Clip", Float) = 0
        [HideInInspector] _Wall ("Wall Point / Normal", Vector) = (0,0,0,0)
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
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct Attributes { COMMON_2D_INPUTS half4 color : COLOR; };
            struct Varyings
            {
                COMMON_2D_OUTPUTS_SHARED
                half4 color : COLOR;
                float2 floorWS : TEXCOORD1;
            };
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _Pivot, _Away, _Light, _LightDirection, _Wall;
                float _Offset, _Stretch, _UseLightClip, _UseWallClip;
            CBUFFER_END
            Varyings ShadowVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                float3 world = TransformObjectToWorld(input.positionOS);
                float along = dot(world.xy - _Pivot.xy, _Away.xy);
                world.xy += _Away.xy * (_Offset + along * _Stretch);
                Varyings output = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformWorldToHClip(world);
                output.floorWS = world.xy;
                output.uv = input.uv;
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }
            half4 ShadowFragment(Varyings input) : SV_Target
            {
                // Берём только альфа-силуэт кадра: глаз, цвет кожи и кровь не окрашивают тень.
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                half alpha = smoothstep(0.10h, 0.90h, mask) * input.color.a;
                if (_UseLightClip > 0.5)
                {
                    float2 offset = input.floorWS - _Light.xy;
                    float distance = length(offset);
                    clip(_Light.z - distance);
                    float angle = dot(offset / max(distance, 0.0001), _LightDirection.xy);
                    clip(angle - _Light.w);
                    alpha *= saturate((_Light.z - distance) / 0.18);
                }
                if (_UseWallClip > 0.5)
                    clip(dot(input.floorWS - _Wall.xy, _Wall.zw));
                return half4(0, 0, 0, alpha);
            }
            ENDHLSL
        }
    }
}
