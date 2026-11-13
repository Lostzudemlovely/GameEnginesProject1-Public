// Game Engines – Projekt 1
// Lässt die HDR-Farbe des Zieltores zeitlich und entlang seiner Welthöhe pulsieren.
Shader "Project1/PulseGlow"
{
    Properties
    {
        [HDR] _BaseColor("Leuchtfarbe", Color) = (0.05, 1.6, 0.65, 1)
        _PulseSpeed("Pulsgeschwindigkeit", Range(0, 8)) = 2.5
        _PulseAmount("Pulsstaerke", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Name "PulseGlow"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float height : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _PulseSpeed;
                float _PulseAmount;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.height = TransformObjectToWorld(input.positionOS.xyz).y;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Eine Sinuswelle lässt das Ziel zeitlich und entlang seiner Höhe pulsieren.
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed + input.height * 3.0) * _PulseAmount;
                return half4(_BaseColor.rgb * pulse, 1.0);
            }
            ENDHLSL
        }
    }
}
