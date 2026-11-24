// Ground of the battle map: blends the Codex terrain tiles by the map's terrain
// type. Two weight maps (one texel per 1 m cell, bilinear) say how much of each
// tile shows; the GPU's filtering gives soft one-metre transitions between
// terrain types. Tiles repeat every _TileMeters metres in world space.
// Lit by the main light (with building shadows) and the ambient light.
Shader "CloseFight/TerrainTiles"
{
    Properties
    {
        _WeightsA ("Weights: dirt, grass, road, rubble", 2D) = "red" {}
        _WeightsB ("Weights: rail, water, crater; A = brightness variation", 2D) = "black" {}
        _Dirt ("Dirt", 2D) = "white" {}
        _Grass ("Grass", 2D) = "white" {}
        _Road ("Road", 2D) = "white" {}
        _Rubble ("Rubble", 2D) = "white" {}
        _Rail ("Rail ballast", 2D) = "white" {}
        _Water ("Water", 2D) = "white" {}
        _TileMeters ("Tile size (m)", Float) = 8
        _MapSize ("Map size (m)", Vector) = (100, 100, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_WeightsA); SAMPLER(sampler_WeightsA);
            TEXTURE2D(_WeightsB);
            TEXTURE2D(_Dirt); SAMPLER(sampler_Dirt);
            TEXTURE2D(_Grass);
            TEXTURE2D(_Road);
            TEXTURE2D(_Rubble);
            TEXTURE2D(_Rail);
            TEXTURE2D(_Water);

            CBUFFER_START(UnityPerMaterial)
                float _TileMeters;
                float4 _MapSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 world = input.positionWS.xz;
                float2 mapUv = world / _MapSize.xy;
                half4 a = SAMPLE_TEXTURE2D(_WeightsA, sampler_WeightsA, mapUv);
                half4 b = SAMPLE_TEXTURE2D(_WeightsB, sampler_WeightsA, mapUv);

                float2 uv = world / _TileMeters;
                half3 dirt = SAMPLE_TEXTURE2D(_Dirt, sampler_Dirt, uv).rgb;
                half3 color = dirt * (a.r + b.b * 0.62h)            // craters: churned, darker earth
                    + SAMPLE_TEXTURE2D(_Grass, sampler_Dirt, uv).rgb * a.g
                    + SAMPLE_TEXTURE2D(_Road, sampler_Dirt, uv).rgb * a.b
                    + SAMPLE_TEXTURE2D(_Rubble, sampler_Dirt, uv).rgb * a.a
                    + SAMPLE_TEXTURE2D(_Rail, sampler_Dirt, uv).rgb * b.r
                    + SAMPLE_TEXTURE2D(_Water, sampler_Dirt, uv).rgb * b.g;
                half total = a.r + a.g + a.b + a.a + b.r + b.g + b.b;
                color /= max(total, 0.001h);
                color *= 0.88h + 0.24h * b.a;                         // gentle per-metre variation

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light light = GetMainLight(shadowCoord);
                half3 normal = half3(0.0h, 1.0h, 0.0h);
                half ndotl = saturate(dot(normal, light.direction));
                half3 lighting = light.color * (ndotl * light.shadowAttenuation) + SampleSH(normal);
                return half4(color * lighting, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
