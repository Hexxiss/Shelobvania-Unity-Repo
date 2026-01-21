Shader "URP2D/Foreground_StencilWrite_Masked"
{
    Properties
    {
        [PerRendererData]_MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // ----- These hidden props let SpriteRenderer drive SpriteMask behavior -----
        [HideInInspector]_StencilComp      ("Stencil Comparison", Float) = 8      // 8 = Always (Unity default when no mask)
        [HideInInspector]_Stencil          ("Stencil ID",         Float) = 0      // mask id (set by SpriteMask system)
        [HideInInspector]_StencilReadMask  ("Stencil Read Mask",  Float) = 255
        [HideInInspector]_StencilWriteMask ("Stencil Write Mask", Float) = 255
    }

    SubShader
    {
        Tags {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
            "IgnoreProjector"="True"
        }
        LOD 100

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        // IMPORTANT:
        // Use SpriteRenderer's mask comparison so this pass only draws where the sprite is actually visible
        // after SpriteMask has clipped it.
        Stencil
        {
            // We still write 1 for the x-ray system...
            Ref 1

            // ...but only where SpriteMask says this sprite may render:
            Comp      [_StencilComp]
            ReadMask  [_StencilReadMask]
            // We want to write our x-ray bit regardless of mask value; full write mask is fine:
            WriteMask 255

            Pass Replace
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // (optional) include core helpers
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Color;

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };
            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv          = TRANSFORM_TEX(v.uv, _MainTex);
                o.color       = v.color * _Color;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                return c;
            }
            ENDHLSL
        }
    }
}
