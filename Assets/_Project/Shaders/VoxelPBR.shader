Shader "WorldOfVictoria/VoxelPBR"
{
    Properties
    {
        _AlbedoArray("Albedo Array", 2DArray) = "" {}
        _NormalArray("Normal Array", 2DArray) = "" {}
        _RoughnessArray("Roughness Array", 2DArray) = "" {}
        _SkyLightVolume("Sky Light Volume", 3D) = "" {}
        _BaseTint("Base Tint", Color) = (1.06,1.05,1,1)
        _NormalScale("Normal Scale", Range(0,2)) = 1
        _Metallic("Metallic", Range(0,1)) = 0.03
        _AlbedoContrast("Albedo Contrast", Range(0.5,2)) = 1.22
        _VertexLightBlend("Vertex Light Blend", Range(0,1)) = 1
        _AoStrength("AO Strength", Range(0,1)) = 0.08
        _LightVolumeStrength("Light Volume Strength", Range(0,1)) = 0
        _UseVertexBrightness("Use Vertex Brightness", Range(0,1)) = 1
        _BrightnessFloor("Brightness Floor", Range(0,1)) = 0
        _BrightnessBlackPoint("Brightness Black Point", Range(0,0.5)) = 0.05
        _BrightnessWhitePoint("Brightness White Point", Range(0.5,1)) = 0.97
        _BrightnessGamma("Brightness Gamma", Range(0.5,3)) = 1.24
        _ProbeGiStrength("Probe GI Strength", Range(0,1)) = 0.14
        _ShadowBoost("Shadow Boost", Range(0,1)) = 0.1
        _RoughnessBias("Roughness Bias", Range(-1,1)) = -0.04
        _WorldLightVolumeSize("World Light Volume Size", Vector) = (256,64,256,0)
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
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D_ARRAY(_AlbedoArray);
            SAMPLER(sampler_AlbedoArray);
            TEXTURE2D_ARRAY(_NormalArray);
            SAMPLER(sampler_NormalArray);
            TEXTURE2D_ARRAY(_RoughnessArray);
            SAMPLER(sampler_RoughnessArray);
            TEXTURE3D(_SkyLightVolume);
            SAMPLER(sampler_SkyLightVolume);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseTint;
                half _NormalScale;
                half _Metallic;
                half _AlbedoContrast;
                half _VertexLightBlend;
                half _AoStrength;
                half _LightVolumeStrength;
                half _UseVertexBrightness;
                half _BrightnessFloor;
                half _BrightnessBlackPoint;
                half _BrightnessWhitePoint;
                half _BrightnessGamma;
                half _ProbeGiStrength;
                half _ShadowBoost;
                half _RoughnessBias;
                float4 _WorldLightVolumeSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float4 metadata : TEXCOORD1;
                float4 lightCorners : TEXCOORD2;
                float4 aoCorners : TEXCOORD3;
                float4 faceCenter : TEXCOORD4;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float4 metadata : TEXCOORD4;
                float4 lightCorners : TEXCOORD5;
                float4 aoCorners : TEXCOORD6;
                float4 faceCenter : TEXCOORD7;
                float fogFactor : TEXCOORD8;
                half4 color : COLOR;
            };

            int ResolveTextureLayer(float blockType, float faceId)
            {
                int decodedBlock = (int)round(blockType);
                int decodedFace = (int)round(faceId);

                if (decodedBlock == 1)
                {
                    if (decodedFace == 1)
                    {
                        return 1;
                    }

                    if (decodedFace == 0)
                    {
                        return 3;
                    }

                    return 2;
                }

                if (decodedBlock == 2)
                {
                    return 3;
                }

                return 0;
            }

            half SampleSkyLightVolume(float3 samplePositionWS, half fallbackBrightness)
            {
                float3 safeWorldSize = max(_WorldLightVolumeSize.xyz, float3(1.0, 1.0, 1.0));
                float3 volumeUv = (samplePositionWS + 0.5) / safeWorldSize;

                if (any(volumeUv < 0.0) || any(volumeUv > 1.0))
                {
                    return fallbackBrightness;
                }

                float3 texel = 1.0 / safeWorldSize;
                half center = SAMPLE_TEXTURE3D(_SkyLightVolume, sampler_SkyLightVolume, saturate(volumeUv)).r;
                half sampleX = SAMPLE_TEXTURE3D(_SkyLightVolume, sampler_SkyLightVolume, saturate(volumeUv + float3(texel.x, 0.0, 0.0))).r;
                half sampleY = SAMPLE_TEXTURE3D(_SkyLightVolume, sampler_SkyLightVolume, saturate(volumeUv + float3(0.0, texel.y, 0.0))).r;
                half sampleZ = SAMPLE_TEXTURE3D(_SkyLightVolume, sampler_SkyLightVolume, saturate(volumeUv + float3(0.0, 0.0, texel.z))).r;
                return (center * 0.55h) + ((sampleX + sampleY + sampleZ) * 0.15h);
            }

            half ApplyRenderedBrightnessCurve(half normalizedLight)
            {
                normalizedLight = saturate(normalizedLight);
                if (normalizedLight <= 0.001h)
                {
                    return 0.0h;
                }

                half whitePoint = max(_BrightnessWhitePoint, _BrightnessBlackPoint + 0.001h);
                half remapped = smoothstep(_BrightnessBlackPoint, whitePoint, normalizedLight);
                return pow(remapped, _BrightnessGamma);
            }

            half SampleBilinearQuad(float2 uv, float4 corners)
            {
                float2 clampedUv = saturate(uv);
                half bottom = lerp(corners.x, corners.y, clampedUv.x);
                half top = lerp(corners.w, corners.z, clampedUv.x);
                return lerp(bottom, top, clampedUv.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = float4(normalInputs.tangentWS.xyz, input.tangentOS.w);
                output.uv = input.uv;
                output.metadata = input.metadata;
                output.lightCorners = input.lightCorners;
                output.aoCorners = input.aoCorners;
                output.faceCenter = input.faceCenter;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                int textureLayer = ResolveTextureLayer(input.metadata.x, input.metadata.y);

                half4 albedoSample = SAMPLE_TEXTURE2D_ARRAY(_AlbedoArray, sampler_AlbedoArray, input.uv, textureLayer);
                half4 normalSample = SAMPLE_TEXTURE2D_ARRAY(_NormalArray, sampler_NormalArray, input.uv, textureLayer);
                half roughnessSample = SAMPLE_TEXTURE2D_ARRAY(_RoughnessArray, sampler_RoughnessArray, input.uv, textureLayer).r;

                half3 normalTS = UnpackNormalScale(normalSample, _NormalScale);
                float3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 tbn = float3x3(normalize(input.tangentWS.xyz), normalize(bitangentWS), normalize(input.normalWS));
                float3 normalWS = normalize(mul(normalTS, tbn));

                half faceBrightness = max(_BrightnessFloor, input.metadata.z);
                half cornerBrightness = max(_BrightnessFloor, SampleBilinearQuad(input.uv, input.lightCorners));
                half logicalLight = lerp(faceBrightness, cornerBrightness, _VertexLightBlend);
                half volumeLight = SampleSkyLightVolume(input.faceCenter.xyz, logicalLight);
                half smoothedLight = lerp(logicalLight, volumeLight, _LightVolumeStrength);
                half brightness = lerp(1.0h, smoothedLight, _UseVertexBrightness);
                half vertexAo = saturate(SampleBilinearQuad(input.uv, input.aoCorners));
                half occlusionAo = lerp(1.0h, vertexAo, _AoStrength);
                half albedoAo = lerp(1.0h, vertexAo, _AoStrength * 0.2h);
                half lightVisibility = ApplyRenderedBrightnessCurve(brightness);
                half skyVisibility = saturate(pow(lightVisibility, 0.8h));
                half probeVisibility = saturate(smoothstep(0.08h, 0.88h, brightness));
                half3 albedo = saturate(((albedoSample.rgb - 0.5h) * _AlbedoContrast) + 0.5h) * _BaseTint.rgb * albedoAo;
                half3 probeGi = SampleSH(normalWS) * probeVisibility * _ProbeGiStrength;

                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = normalWS;
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lightingInput.bakedGI = (half3(0.30h, 0.32h, 0.35h) * lightVisibility) + probeGi;
                lightingInput.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lightingInput.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.alpha = 1.0h;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = saturate((1.0h - roughnessSample) + _RoughnessBias);
                surfaceData.normalTS = normalTS;
                surfaceData.occlusion = saturate((lerp(0.08h, 1.0h, lightVisibility) + (_ShadowBoost * 0.18h)) * occlusionAo);
                surfaceData.emission = 0;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;

                half4 color = UniversalFragmentPBR(lightingInput, surfaceData);
                color.rgb *= lightVisibility;
                color.rgb = MixFog(color.rgb, input.fogFactor * skyVisibility);
                return color;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
