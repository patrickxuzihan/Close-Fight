// Colour pictures in the IMGUI layer (roster unit pictures, M18).
// The project renders in linear colour space, and its IMGUI layer comes out one
// sRGB encoding too bright: GUI.DrawTexture does not decode sRGB textures before
// the final encode. Pictures drawn with this shader are imported as plain (non-sRGB)
// textures and decoded here, so they show their true colours.
// Clipping (GUI groups, scroll areas) works like Unity's built-in GUI texture shader.
Shader "Hidden/CloseFight/GuiPicture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "ForceSupported" = "True" }
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 clipUV : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _GUIClipTexture;
            uniform float4x4 unity_GUIClipTextureMatrix;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float3 eyePos = UnityObjectToViewPos(v.vertex);
                o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0)).xy;
                o.color = v.color;
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord);
                col.rgb = GammaToLinearSpace(col.rgb);
                col *= i.color;
                col.a *= tex2D(_GUIClipTexture, i.clipUV).a;
                return col;
            }
            ENDCG
        }
    }
}
