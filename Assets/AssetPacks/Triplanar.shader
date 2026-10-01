Shader "Custom/URPTriplanarLit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _Scale ("Scale", Float) = 1.0
        _BlendSharpness ("Blend Sharpness", Range(1, 16)) = 4.0
        _Roughness ("Roughness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
            };

            Texture2D _MainTex;
            SamplerState sampler_MainTex;

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Tint;
                float _Scale;
                float _BlendSharpness;
                float _Roughness;
                float _Metallic;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 posWS = input.positionWS * _Scale;
                float3 normalWS = abs(input.normalWS);

                // Weights for triplanar blending
                float3 weights = pow(normalWS, _BlendSharpness);
                weights = weights / (weights.x + weights.y + weights.z + 1e-5);

                // Projections for XY, ZY, and XZ mapped to specified orientations
                float2 uvXY = posWS.xy;
                float2 uvZY = posWS.zy;
                float2 uvXZ = posWS.xz;

                float4 colXY = _MainTex.Sample(sampler_MainTex, uvXY);
                float4 colZY = _MainTex.Sample(sampler_MainTex, uvZY);
                float4 colXZ = _MainTex.Sample(sampler_MainTex, uvXZ);

                float4 triplanarTex = colXY * weights.z + colZY * weights.x + colXZ * weights.y;
                float3 finalColor = triplanarTex.rgb * _Tint.rgb;

                // Simple Lambert lighting calculation with Universal Pipeline light data
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float NdotL = saturate(dot(input.normalWS, mainLight.direction));
                float3 lighting = mainLight.color * (NdotL * mainLight.shadowAttenuation);
                
                float3 ambient = SampleSH(input.normalWS);
                float3 finalRGB = finalColor * (lighting + ambient);

                return float4(finalRGB, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}