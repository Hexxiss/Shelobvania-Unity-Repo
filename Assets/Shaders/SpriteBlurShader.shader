Shader "Sprites/BoxBlur2D"
{
    Properties
    {
        [PerRendererData]_MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _BlurRadius("Blur Radius (px)", Float) = 1.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;   // x=1/width, y=1/height
            float4 _Color;
            float  _BlurRadius;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float4 col : COLOR;
            };

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                o.col = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 px = _BlurRadius * _MainTex_TexelSize.xy;

                // 9-tap box blur (center + 8 neighbors)
                fixed4 c  = tex2D(_MainTex, i.uv) * 1.0;
                c += tex2D(_MainTex, i.uv + float2( px.x,  0));
                c += tex2D(_MainTex, i.uv + float2(-px.x,  0));
                c += tex2D(_MainTex, i.uv + float2( 0,   px.y));
                c += tex2D(_MainTex, i.uv + float2( 0,  -px.y));
                c += tex2D(_MainTex, i.uv + float2( px.x,  px.y));
                c += tex2D(_MainTex, i.uv + float2(-px.x,  px.y));
                c += tex2D(_MainTex, i.uv + float2( px.x, -px.y));
                c += tex2D(_MainTex, i.uv + float2(-px.x, -px.y));
                c *= (1.0/9.0);

                return c * i.col;
            }
            ENDCG
        }
    }
}
