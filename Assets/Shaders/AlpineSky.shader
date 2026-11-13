// Game Engines – Projekt 1
// Ein einfacher Himmel mit Farbverlauf, Dunst am Horizont und einem Sonnenfleck.
Shader "Project1/Alpine Sky"
{
    Properties
    {
        _Zenith("Himmelsfarbe", Color) = (0.34, 0.65, 0.76, 1)
        _Horizon("Horizont", Color) = (0.84, 0.91, 0.9, 1)
        _Mist("Dunst", Color) = (0.56, 0.73, 0.77, 1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Zenith;
                half4 _Horizon;
                half4 _Mist;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                half3 color = lerp(_Mist.rgb, _Horizon.rgb, smoothstep(-0.75, 0.02, direction.y));
                color = lerp(color, _Zenith.rgb, smoothstep(-0.03, 0.42, direction.y));
                float sunlight = dot(direction, normalize(float3(-0.54, 0.07, 1)));
                color = lerp(color, half3(1.0, 0.93, 0.73), smoothstep(0.93, 1.0, sunlight) * 0.27);
                color = lerp(color, half3(1.0, 0.96, 0.84), smoothstep(0.9990, 0.9994, sunlight) * 0.8);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
