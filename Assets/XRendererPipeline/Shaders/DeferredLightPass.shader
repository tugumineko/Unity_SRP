Shader "Hidden/SRPLearn/DeferredLightPass"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque"}
        LOD 100

        HLSLINCLUDE
        #pragma enable_cbuffer
        #include "../ShaderLibrary/DeferredParams.hlsl"
        #include "../ShaderLibrary/SpaceTransform.hlsl"
        #include "../ShaderLibrary/AOSADeferred.hlsl"
        #include "../ShaderLibrary/TileDeferredInput.hlsl"
        
        ENDHLSL

        Pass
        {
            Name "DEFAULT"
            ZTest Off
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma multi_compile _ X_SHADOW_BIAS_RECEIVER_PIXEL
            #pragma multi_compile _ X_SHADOW_PCF
            #pragma shader_feature X_CSM_BLEND
            #pragma shader_feature _RECEIVE_SHADOWS_OFF
            #pragma shader_feature DEFERRED_BUFFER_DEBUGON
            #pragma shader_feature GBUFFER_ACCURATE_NORMAL

            #pragma vertex Vertex
            #pragma fragment Fragment

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                half4 positionCS    : SV_POSITION;
                half2 uv            : TEXCOORD0;
            };

            struct Textures
            {
                half4 ShadowColorMap : SV_Target0;
                half4 SpecularColorMap : SV_Target1;
            };

            Texture2D _AOSAShadowTexture;
            Texture2D _AOSASpecularTexture;
            
            SamplerState sampler_pointer_clamp;
            Texture2D _XDepthTexture;
            
            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = ObjectToHClipPosition(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            Textures Fragment(Varyings input) 
            {   
                float2 uv = input.uv;
                float depth = _XDepthTexture.Sample(sampler_pointer_clamp,input.uv).x;
                half4 g0 =  _GBuffer0.Sample(sampler_pointer_clamp,input.uv);
                half4 g1 =  _GBuffer1.Sample(sampler_pointer_clamp,input.uv);
                half4 g2 =  _GBuffer2.Sample(sampler_pointer_clamp,input.uv);
                half4 g3 =  _GBuffer3.Sample(sampler_pointer_clamp,input.uv);
                AOSAShadeInput shadeInput;
                float3 positionWS = ReconstructPositionWS(uv,depth);
                shadeInput.positionWS = positionWS;
                DecodeGBuffer(shadeInput,g0,g1,g2,g3);

                float2 coord = _ScreenParams.xy * uv; 
                uint2 tileId = floor(coord / _DeferredTileParams.xy);
                uint tileIndex = tileId.y * _DeferredTileParams.z + tileId.x;
                uint lightCount = _TileLightsArgsBuffer[tileIndex];

                half3 shadowMap = 0, specularMap = 0;
                float3 viewDirection = normalize(_WorldSpaceCameraPos - positionWS);
                float shadow = 0, specular = 0;
                float mainLightAttenuation = GetMainLightShadowAtten(shadeInput.positionWS, shadeInput.normal);
                GetLighting(viewDirection, shadeInput.normal, shadeInput.smoothness,_XMainLightDirection,mainLightAttenuation,shadow,specular);
                shadowMap += shadow * _XMainLightColor;
                specularMap += specular * _XMainLightColor;
                
                uint tileLightOffset = tileIndex * MAX_LIGHT_COUNT_PER_TILE;
                for(uint i = 0; i < lightCount; i ++){
                    uint lightIndex = _TileLightsIndicesBuffer[tileLightOffset + i];
                    float4 lightSphere = _DeferredOtherLightPositionAndRanges[lightIndex];
                    half4 lightColor = _DeferredOtherLightColors[lightIndex];
                    ShadeLightDesc lightDesc = GetPointLightShadeDesc(lightSphere,1,shadeInput.positionWS);
                    shadow = 0;
                    specular = 0;
                    GetLighting(viewDirection,shadeInput.normal,shadeInput.smoothness,lightDesc.dir,lightDesc.color.r,shadow,specular);    
                    shadowMap += shadow * lightColor;
                    specularMap += specular * lightColor;
                }

                Textures output;
                output.ShadowColorMap = half4(shadowMap,1);
                output.SpecularColorMap = half4(specularMap,1);
                return output;
            }
            ENDHLSL
        }
    }
}
