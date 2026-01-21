Shader "URP2D/PlayerOutline_Stencil"
{
    Properties {
        [PerRendererData]_MainTex ("Sprite", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0,1,1,0.8)
        _OutlineWidth ("Outline Width (px)", Float) = 1.0
    }
    SubShader
    {
        Tags {
            "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"
            "IgnoreProjector"="True"
        }
        LOD 100
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Stencil
        {
            Ref 1
            Comp Equal
            Pass Keep
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;   // x=1/w, y=1/h
            float4 _OutlineColor;
            float  _OutlineWidth;

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };
            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert (Attributes v) {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 px = _OutlineWidth * _MainTex_TexelSize.xy;

                // center & neighbors
                half a0 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                half a1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2( px.x, 0)).a;
                half a2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-px.x, 0)).a;
                half a3 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0,  px.y)).a;
                half a4 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0, -px.y)).a;
                half a5 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2( px.x,  px.y)).a;
                half a6 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-px.x,  px.y)).a;
                half a7 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2( px.x, -px.y)).a;
                half a8 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-px.x, -px.y)).a;

                // Edge = neighbors minus center (simple dilation-based outline)
                half maxN = max(max(max(a1,a2),max(a3,a4)), max(max(a5,a6),max(a7,a8)));
                half edge = saturate(maxN - a0);

                return half4(_OutlineColor.rgb, _OutlineColor.a * edge);
            }
            ENDHLSL
        }
    }
}
