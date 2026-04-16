Shader "WorldOfVictoria/VoxelPreview"
{
    Properties
    {
        _MainTex("Block Atlas", 2D) = "white" {}
        _FogEnabled("Fog Enabled", Float) = 1
        _FogColor("Fog Color", Color) = (0.055, 0.043, 0.039, 1)
        _FogStart("Fog Start", Float) = -10
        _FogEnd("Fog End", Float) = 20
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _FogEnabled;
            half4 _FogColor;
            float _FogStart;
            float _FogEnd;

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float dist = distance(_WorldSpaceCameraPos.xyz, worldPos);
                float fogDenominator = max(0.0001, _FogEnd - _FogStart);
                output.fogFactor = saturate((_FogEnd - dist) / fogDenominator);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 finalColor = texColor * input.color;

                if (_FogEnabled > 0.5)
                {
                    finalColor.rgb = lerp(_FogColor.rgb, finalColor.rgb, input.fogFactor);
                }

                return finalColor;
            }
            ENDHLSL
        }
    }
}
