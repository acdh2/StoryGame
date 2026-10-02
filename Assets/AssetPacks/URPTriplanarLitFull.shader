Shader "Custom/URPTriplanarLitFull"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo (RGB)", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0

        _RoughnessMap("Roughness Map (G)", 2D) = "white" {}
        _Roughness("Roughness", Range(0.0, 1.0)) = 0.5

        _OcclusionMap("Ambient Occlusion (G)", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0

        _HeightMap("Height Map (R)", 2D) = "black" {}
        _HeightScale("Height Scale", Float) = 0.05
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "UniversalMaterialType" = "Lit"
            "Queue" = "Geometry"
        }

        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                float4 tangentWS    : TEXCOORD3;
                float4 shadowCoord  : TEXCOORD4;
                half4 fogFactorAndVertexLight : TEXCOORD5;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 6);
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _BumpScale;
                float _Roughness;
                float _OcclusionStrength;
                float _HeightScale;
            CBUFFER_END

            TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);        SAMPLER(sampler_BumpMap);
            TEXTURE2D(_RoughnessMap);   SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_OcclusionMap);   SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_HeightMap);      SAMPLER(sampler_HeightMap);

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                output.normalWS = normalInput.normalWS;
                real sign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = real4(normalInput.tangentWS, sign);

                output.shadowCoord = GetShadowCoord(vertexInput);

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);

                half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                output.fogFactorAndVertexLight = half4(fogFactor, 0, 0, 0);

                return output;
            }

            float4 TriplanarSample(Texture2D tex, SamplerState samp, float2 uvXY, float2 uvZY, float2 uvXZ, float3 weights)
            {
                float4 colXY = tex.Sample(samp, uvXY);
                float4 colZY = tex.Sample(samp, uvZY);
                float4 colXZ = tex.Sample(samp, uvXZ);
                return colXY * weights.z + colZY * weights.x + colXZ * weights.y;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 posWS = input.positionWS;
                float3 normalWS2 = abs(input.normalWS);

                float3 weights = pow(normalWS2, 4.0);
                weights = weights / (weights.x + weights.y + weights.z + 1e-5);

                float2 uvXY = posWS.xy;
                float2 uvZY = posWS.zy;
                float2 uvXZ = posWS.xz;

                float4 albedoTex = TriplanarSample(_BaseMap, sampler_BaseMap, uvXY, uvZY, uvXZ, weights);
                half3 albedo = albedoTex.rgb * _BaseColor.rgb;

                half4 normalTex = TriplanarSample(_BumpMap, sampler_BumpMap, uvXY, uvZY, uvXZ, weights);
                half3 normalTS = UnpackNormalScale(normalTex, _BumpScale);
                
                float3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                half3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS));
                normalWS = NormalizeNormalPerPixel(normalWS);

                half roughnessSample = TriplanarSample(_RoughnessMap, sampler_RoughnessMap, uvXY, uvZY, uvXZ, weights).g;
                half roughness = roughnessSample * _Roughness;
                half smoothness = 1.0 - roughness;

                half aoSample = TriplanarSample(_OcclusionMap, sampler_OcclusionMap, uvXY, uvZY, uvXZ, weights).g;
                half ao = lerp(1.0, aoSample, _OcclusionStrength);

                Light mainLight = GetMainLight(input.shadowCoord, input.positionWS, ao);

                InputData inputData;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceViewDir(input.positionWS);
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = input.fogFactorAndVertexLight.x;
                inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = 1.0;

                SurfaceData surfaceData;
                surfaceData.albedo = albedo;
                surfaceData.metallic = 0.0;
                surfaceData.specular = half3(0.0, 0.0, 0.0);
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.emission = half3(0,0,0);
                surfaceData.occlusion = ao;
                surfaceData.alpha = albedoTex.a * _BaseColor.a;
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);

                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}

// {
//     Properties
//     {
//         [MainTexture] _BaseMap("Albedo (RGB)", 2D) = "white" {}
//         [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        
//         _BumpMap("Normal Map", 2D) = "bump" {}
//         _BumpScale("Normal Scale", Float) = 1.0

//         _RoughnessMap("Roughness Map (G)", 2D) = "white" {}
//         _Roughness("Roughness", Range(0.0, 1.0)) = 0.5

//         _OcclusionMap("Ambient Occlusion (G)", 2D) = "white" {}
//         _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0

//         _HeightMap("Height Map (R)", 2D) = "black" {}
//         _HeightScale("Height Scale", Float) = 0.05
//     }

//     SubShader
//     {
//         Tags 
//         { 
//             "RenderType" = "Opaque" 
//             "RenderPipeline" = "UniversalPipeline" 
//             "UniversalMaterialType" = "Lit"
//             "Queue" = "Geometry"
//         }

//         LOD 300

//         Pass
//         {
//             Name "ForwardLit"
//             Tags { "LightMode" = "UniversalForward" }

//             HLSLPROGRAM
//             #pragma target 3.0

//             #pragma vertex vert
//             #pragma fragment frag

//             #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
//             #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
//             #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
//             #pragma multi_compile _ _SHADOWS_SOFT
//             #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
//             #pragma multi_compile _ LIGHTMAP_ON
//             #pragma multi_compile _ DIRLIGHTMAP_COMBINED
//             #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING

//             #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//             #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

//             struct Attributes
//             {
//                 float4 positionOS   : POSITION;
//                 float3 normalOS     : NORMAL;
//                 float4 tangentOS    : TANGENT;
//                 float2 uv           : TEXCOORD0;
//                 DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
//             };

//             struct Varyings
//             {
//                 float4 positionCS   : SV_POSITION;
//                 float2 uv           : TEXCOORD0;
//                 float3 positionWS   : TEXCOORD1;
//                 float3 normalWS     : TEXCOORD2;
//                 float4 tangentWS    : TEXCOORD3;
//                 float4 shadowCoord  : TEXCOORD4;
//                 DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);
//             };

//             CBUFFER_START(UnityPerMaterial)
//                 float4 _BaseMap_ST;
//                 float4 _BaseColor;
//                 float _BumpScale;
//                 float _Roughness;
//                 float _OcclusionStrength;
//                 float _HeightScale;
//             CBUFFER_END

//             TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
//             TEXTURE2D(_BumpMap);        SAMPLER(sampler_BumpMap);
//             TEXTURE2D(_RoughnessMap);   SAMPLER(sampler_RoughnessMap);
//             TEXTURE2D(_OcclusionMap);   SAMPLER(sampler_OcclusionMap);
//             TEXTURE2D(_HeightMap);      SAMPLER(sampler_HeightMap);

//             Varyings vert(Attributes input)
//             {
//                 Varyings output;
                
//                 VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
//                 VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

//                 output.positionCS = vertexInput.positionCS;
//                 output.positionWS = vertexInput.positionWS;
//                 output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

//                 output.normalWS = normalInput.normalWS;
//                 real sign = input.tangentOS.w * GetOddNegativeScale();
//                 output.tangentWS = real4(normalInput.tangentWS, sign);

//                 output.shadowCoord = GetShadowCoord(vertexInput);

//                 OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
//                 OUTPUT_SH(output.normalWS, output.vertexSH);

//                 return output;
//             }

//             float4 TriplanarSample(Texture2D tex, SamplerState samp, float2 uvXY, float2 uvZY, float2 uvXZ, float3 weights)
//             {
//                 float4 colXY = tex.Sample(samp, uvXY);
//                 float4 colZY = tex.Sample(samp, uvZY);
//                 float4 colXZ = tex.Sample(samp, uvXZ);
//                 return colXY * weights.z + colZY * weights.x + colXZ * weights.y;
//             }

//             half4 frag(Varyings input) : SV_Target
//             {
//                 float3 posWS = input.positionWS;
//                 float3 normalWS2 = abs(input.normalWS);

//                 // Weights for triplanar blending
//                 float3 weights = pow(normalWS2, 4.0);
//                 weights = weights / (weights.x + weights.y + weights.z + 1e-5);

//                 // Projections for XY, ZY, and XZ mapped to specified orientations
//                 float2 uvXY = posWS.xy;
//                 float2 uvZY = posWS.zy;
//                 float2 uvXZ = posWS.xz;

//                 float4 colXY = _BaseMap.Sample(sampler_BaseMap, uvXY);
//                 float4 colZY = _BaseMap.Sample(sampler_BaseMap, uvZY);
//                 float4 colXZ = _BaseMap.Sample(sampler_BaseMap, uvXZ);

//                 float4 triplanarTex = TriplanarSample(_BaseMap, sampler_BaseMap, uvXY, uvZY, uvXZ, weights);
                    
//                 //    colXY * weights.z + colZY * weights.x + colXZ * weights.y;
//                 float3 finalColor = triplanarTex.rgb;

//                 //float2 uv = uvXY * weights.z + uvZY * weights.x + uvXZ * weights.y;
                
//                 float height = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, input.uv).r;
//                 //float2 uv = input.uv - (height * _HeightScale);

//                 //half4 albedoTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
//                 half4 albedoTex = TriplanarSample(_BaseMap, sampler_BaseMap,  uvXY, uvZY, uvXZ, weights);
//                 //half3 albedo = albedoTex.rgb * _BaseColor.rgb;
//                 half3 albedo = TriplanarSample(_BaseMap, sampler_BaseMap, uvXY, uvZY, uvXZ, weights);

//                 //half4 normalTex = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv);
//                 half4 normalTex = TriplanarSample(_BumpMap, sampler_BumpMap, uvXY, uvZY, uvXZ, weights);
//                 half3 normalTS = UnpackNormalScale(normalTex, _BumpScale);
                
//                 float3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
//                 half3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS));
//                 normalWS = NormalizeNormalPerPixel(normalWS);

//                 //half roughness = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uv).g * _Roughness;
//                 half roughness = TriplanarSample(_RoughnessMap, sampler_RoughnessMap, uvXY, uvZY, uvXZ, weights);
//                 half smoothness = 1.0 - roughness;

//                 //half ao = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
//                 half ao = TriplanarSample(_OcclusionMap, sampler_OcclusionMap,  uvXY, uvZY, uvXZ, weights);
//                 ao = lerp(1.0, ao, _OcclusionStrength);

//                 Light mainLight = GetMainLight(input.shadowCoord, input.positionWS, ao);

//                 InputData inputData;
//                 inputData.positionWS = input.positionWS;
//                 inputData.normalWS = normalWS;
//                 inputData.viewDirectionWS = GetWorldSpaceViewDir(input.positionWS);
//                 inputData.shadowCoord = input.shadowCoord;
//                 inputData.fogCoord = 0;
//                 inputData.vertexLighting = half3(0,0,0);
//                 inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
//                 inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
//                 inputData.shadowMask = 1.0;

//                 SurfaceData surfaceData;
//                 surfaceData.albedo = albedo;
//                 surfaceData.metallic = 0.0;
//                 surfaceData.specular = half3(0.0, 0.0, 0.0);
//                 surfaceData.smoothness = smoothness;
//                 surfaceData.normalTS = normalTS;
//                 surfaceData.emission = half3(0,0,0);
//                 surfaceData.occlusion = ao;
//                 surfaceData.alpha = albedoTex.a * _BaseColor.a;
//                 surfaceData.clearCoatMask = 0.0;
//                 surfaceData.clearCoatSmoothness = 0.0;

//                 half4 color = UniversalFragmentPBR(inputData, surfaceData);

//                 return color;
//             }
//             ENDHLSL
//         }

//         Pass
//         {
//             Name "ShadowCaster"
//             Tags { "LightMode" = "ShadowCaster" }

//             ZWrite On
//             ZTest LEqual
//             ColorMask 0

//             HLSLPROGRAM
//             #pragma vertex ShadowPassVertex
//             #pragma fragment ShadowPassFragment

//             #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
//             #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
//             ENDHLSL
//         }
//     }
// }