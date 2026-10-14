Shader "EmergencyShift/Muzzle Plume"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture",2D)="white" {}
        [HideInInspector] _Color ("Tint",Color)=(1,1,1,1)
        [PerRendererData] _Age ("Normalized age",Float)=0
        [PerRendererData] _Seed ("Shape seed",Float)=0
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
            #pragma vertex PlumeVertex
            #pragma fragment PlumeFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct Attributes { COMMON_2D_INPUTS half4 color:COLOR; };
            struct Varyings { COMMON_2D_OUTPUTS_SHARED half4 color:COLOR; };
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Age, _Seed;
            CBUFFER_END
            Varyings PlumeVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);SetUpSpriteInstanceProperties();
                input.positionOS=UnityFlipSprite(input.positionOS,unity_SpriteProps.xy);
                Varyings output=(Varyings)0;UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS=TransformObjectToHClip(input.positionOS);
                output.uv=input.uv;output.color=input.color*_Color*unity_SpriteColor;return output;
            }
            float Lobe(float2 p, float2 center, float2 radius)
            { return length((p-center)/radius); }
            float Join(float a, float b)
            {
                float h=saturate(.5+.5*(b-a)/.24);
                return lerp(b,a,h)-.24*h*(1-h);
            }
            half4 PlumeFragment(Varyings input):SV_Target
            {
                // A broad, joined gas cloud. The hot body and its feathered rim share one mask.
                float age=saturate(_Age);
                float expansion=lerp(.78,1.12,saturate(age*2.6));
                float2 p=float2((input.uv.x-.5)*2,input.uv.y);
                p.x/=expansion;
                p.y=(p.y-.08)/lerp(.88,1.06,age)+.08;
                float side=sin(_Seed*3.1)*.065;
                float d=Lobe(p,float2(0,.29),float2(.46,.245));
                d=Join(d,Lobe(p,float2(side,.49),float2(.36,.25)));
                d=Join(d,Lobe(p,float2(side-.045,.65),float2(.18,.22)));
                d=Join(d,Lobe(p,float2(-.32,.37),float2(.20,.12)));
                d=Join(d,Lobe(p,float2(.33,.43),float2(.19,.135)));
                // Low amplitude ripples break the silhouette without detached star spokes.
                float ripple=sin(p.x*31+p.y*21+_Seed+age*9)*sin(p.y*37-p.x*13-_Seed)*.055;
                d+=ripple;
                float body=1-smoothstep(.78,1.09,d);
                float halo=(1-smoothstep(.85,1.85,d))*.18;
                float hot=1-smoothstep(.12,.95,Lobe(p,float2(0,.31),float2(.29,.27)));
                float3 fire=lerp(float3(1,.19,.012),float3(1,.67,.11),1-smoothstep(.48,.95,d));
                fire=lerp(fire,float3(1.28,1.19,.91),hot);
                // Brief white-yellow peak, then shrinking orange fringes; light has its own envelope.
                float fade=1-smoothstep(.35,1,age);
                fire=lerp(fire,float3(1,.23,.025),age*.24);
                float alpha=max(body,halo)*fade*input.color.a;
                return half4(fire*input.color.rgb,alpha);
            }
            ENDHLSL
        }
    }
}
