Shader "EmergencyShift/UI/Directional Blood"
{
    Properties
    {
        [PerRendererData] _MainTex ("UI texture", 2D) = "white" {}
        _BloodColor ("Blood", Color) = (0.58, 0.018, 0.035, 1)
        _Depth ("Inward reach", Range(0.03, 0.25)) = 0.13
        _Spread ("Width along edge", Range(0.1, 0.8)) = 0.46
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 localPosition : TEXCOORD1;
            };

            fixed4 _BloodColor;
            float _Depth;
            float _Spread;
            float4 _ClipRect;
            float4 _DamagePulses[8];

            v2f vert(appdata input)
            {
                v2f output;
                output.localPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            float hash(float2 coordinate)
            {
                return frac(sin(dot(coordinate, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 coordinate)
            {
                float2 cell = floor(coordinate);
                float2 blend = frac(coordinate);
                blend = blend * blend * (3 - 2 * blend);
                return lerp(lerp(hash(cell), hash(cell + float2(1, 0)), blend.x),
                    lerp(hash(cell + float2(0, 1)), hash(cell + 1), blend.x), blend.y);
            }

            float stain(float2 uv, float4 pulse, float2 aspect)
            {
                float2 offset = (uv - pulse.xy) * aspect;
                float2 outward;
                if (abs(pulse.x - .5) > .499)
                {
                    outward = float2(sign(pulse.x - .5), 0);
                }
                else
                {
                    outward = float2(0, sign(pulse.y - .5));
                }

                float inward = dot(offset, -outward);
                float along = dot(offset, float2(-outward.y, outward.x));
                float width = _Spread * .5;
                float flank = 1 - smoothstep(width * .4, width, abs(along));
                float seed = pulse.w * 73.1;
                float coarse = noise(float2(along * 37 + seed, seed));
                float fine = noise(float2(along * 123 - seed, inward * 95 + seed));
                float reach = _Depth * (.28 + .45 * coarse + .14 * fine) * flank;
                float pooled = 1 - smoothstep(0, max(.001, reach), max(0, inward));
                pooled = pooled * pooled * flank;

                // Unequal small lobes and detached droplets break up the inner fringe.
                float cell = floor((along + seed) * 46);
                float random = hash(float2(cell, seed));
                float local = frac((along + seed) * 46) - lerp(.25, .75, random);
                float dropletDepth = _Depth * lerp(.23, .91, hash(float2(cell + 19, seed)));
                float radius = lerp(.0015, .006, random);
                float2 droplet = float2(local / 46, (inward - dropletDepth) * lerp(.8, 1.7, random));
                float drops = (1 - smoothstep(radius * .5, radius, length(droplet))) * step(.45, random) * flank;
                return saturate(max(pooled, drops * .6)) * pulse.z;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float ratio = _ScreenParams.x / max(1, _ScreenParams.y);
                float2 aspect = float2(max(1, ratio), max(1, 1 / ratio));
                float opacity = 0;
                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    if (_DamagePulses[i].z > 0)
                    {
                        opacity = max(opacity, stain(input.uv, _DamagePulses[i], aspect));
                    }
                }

                float mottling = noise(input.uv * aspect * 130);
                fixed4 color = fixed4(_BloodColor.rgb * lerp(.68, 1.05, mottling), opacity * _BloodColor.a) * input.color;
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.localPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - .001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
