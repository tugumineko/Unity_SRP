Shader "Hidden/SRPLearn/EditorOutline"
{
    SubShader
    {
       Pass
       {
            Cull Front
            ZWrite On

            HLSLPROGRAM

            #include "Assets/XRendererPipeline/ShaderLibrary/SpaceTransform.hlsl"

            half4 _OutlineColor;

            #pragma vertex Vertex 
            #pragma fragment Fragment 

            #define OFFSET_DISTANCE 0.01
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalInflatedOS : TEXCOORD3;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                float4 positionCS = TransformWorldToHClip(positionWS);
                
                //It is inflated, so it can't be normalized
                float3 normalInflatedWS = mul(input.normalInflatedOS, (float3x3)unity_WorldToObject);
                float3 normalInflatedVS = mul(normalInflatedWS, (float3x3)unity_MatrixInvV);
                float2 normalInflatedCS = mul((float2x2)unity_MatrixP,normalInflatedVS);
                float2 offset = normalize(normalInflatedCS.xy) / float2((_ScreenParams.y/ _ScreenParams.x),1.0) * positionCS.w * OFFSET_DISTANCE;
                
                positionCS.xy += offset;
                output.positionCS = positionCS;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
                       
            ENDHLSL 
           
       }     
        
        
    }
}