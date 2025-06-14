Shader "Hidden/SRPLearn/FinalCompositingPass"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100
        
        HLSLINCLUDE
        #pragma enable_cbuffer
        #include "../ShaderLibrary/SpaceTransform.hlsl"
        #include "FinalCompositingPass.hlsl"
        ENDHLSL 
        
        Pass
        {
            Name "First Compositing"
            
            HLSLPROGRAM
            #pragma vertex Vertex 
            #pragma fragment CompositingFragment 
            
            ENDHLSL
        }

        Pass
        {
            Name "Final Compositing"
            
            ZTest Off
            ZWrite Off
            Cull Back  
            
            HLSLPROGRAM

            #pragma vertex Vertex 
            #pragma fragment FinalCompositingFragment
            ENDHLSL 
        }

    }
    
    
}