Shader "Hidden/WorldOfVictoria/VoxelVolumetricFog"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "VoxelVolumetricFog"
            Cull Off
            ZWrite Off
            ZTest Always
            Blend One Zero

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D_X(_BlitTexture);

            float4 _WovVolumetricFogColor;
            float _WovVolumetricEnabled;
            float _WovVolumetricDensity;
            float _WovVolumetricHeightFalloff;
            float _WovVolumetricSurfaceHeight;
            float _WovVolumetricUndergroundBoost;
            float _WovVolumetricMaxDistance;
            float _WovVolumetricStepCount;
            float _WovVolumetricScattering;
            float _WovVolumetricExtinction;
            float _WovVolumetricAnisotropy;
            float _WovVolumetricShaftIntensity;
            float _WovVolumetricAmbientBoost;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            float ComputeFogDensity(float3 samplePositionWS)
            {
                float belowSurface = saturate((_WovVolumetricSurfaceHeight - samplePositionWS.y) / max(_WovVolumetricSurfaceHeight, 1.0));
                float heightTerm = exp(-max(0.0, samplePositionWS.y - (_WovVolumetricSurfaceHeight * 0.22)) * _WovVolumetricHeightFalloff);
                float undergroundTerm = lerp(1.0, _WovVolumetricUndergroundBoost, belowSurface);
                return _WovVolumetricDensity * heightTerm * undergroundTerm;
            }

            float PhaseMie(float cosineTheta, float g)
            {
                float g2 = g * g;
                float denominator = pow(max(1e-3, 1.0 + g2 - 2.0 * g * cosineTheta), 1.5);
                return (1.0 - g2) / (4.0 * PI * denominator);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.uv);
                if (_WovVolumetricEnabled <= 0.001)
                {
                    return source;
                }

                float rawDepth = SampleSceneDepth(input.uv);
                if (rawDepth <= 0.0 || rawDepth >= 0.99999)
                {
                    return source;
                }

                float3 worldPosition = ComputeWorldSpacePosition(input.uv, rawDepth, UNITY_MATRIX_I_VP);
                float3 rayVector = worldPosition - _WorldSpaceCameraPos;
                float viewDistance = min(length(rayVector), _WovVolumetricMaxDistance);
                if (viewDistance <= 0.001)
                {
                    return source;
                }

                float3 rayDirection = rayVector / max(length(rayVector), 1e-4);
                Light mainLight = GetMainLight();
                float cosineTheta = dot(rayDirection, -mainLight.direction);
                float phase = PhaseMie(cosineTheta, _WovVolumetricAnisotropy);
                int stepCount = clamp((int)round(_WovVolumetricStepCount), 6, 32);
                float stepLength = viewDistance / stepCount;
                float transmittance = 1.0;
                float3 scattering = 0.0;

                [loop]
                for (int stepIndex = 0; stepIndex < 32; stepIndex++)
                {
                    if (stepIndex >= stepCount)
                    {
                        break;
                    }

                    float travel = (stepIndex + 0.5) * stepLength;
                    float3 samplePosition = _WorldSpaceCameraPos + (rayDirection * travel);
                    float density = ComputeFogDensity(samplePosition);
                    if (density <= 1e-4)
                    {
                        continue;
                    }

                    float shadow = MainLightRealtimeShadow(TransformWorldToShadowCoord(samplePosition));
                    float shaftLight = lerp(_WovVolumetricAmbientBoost, 1.0, shadow);
                    shaftLight = lerp(1.0, phase * 7.5, _WovVolumetricShaftIntensity) * shaftLight;

                    float extinction = density * _WovVolumetricExtinction * stepLength;
                    float scatteringStep = density * _WovVolumetricScattering * stepLength;
                    float3 sampleColor = (_WovVolumetricFogColor.rgb * _WovVolumetricAmbientBoost) + (mainLight.color * shaftLight * scatteringStep);
                    scattering += transmittance * sampleColor * scatteringStep;
                    transmittance *= exp(-extinction);
                }

                half3 finalColor = (source.rgb * transmittance) + scattering;
                return half4(finalColor, source.a);
            }
            ENDHLSL
        }
    }
}
