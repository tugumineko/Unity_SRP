Shader "Hidden/SRPLearn/AOSABlur"
{
    SubShader
    {
        Tags {"RenderType" = "Opaque"}
        LOD 100
        
        HLSLINCLUDE
        #pragma enable_cbuffer

        #include "../ShaderLibrary/SpaceTransform.hlsl"
        #include "AOSABlur.hlsl"
        
        ENDHLSL 
        
        Pass
        {
            Name "DownSample"
            ZTest Always
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM

            
            #pragma vertex DefaultVertex 
            #pragma fragment DownSampleFragment  
            
            ENDHLSL 
        }        

        Pass
        {
            Name "BlurHorizontal"
            
            ZTest Always
            Zwrite Off
            Cull Off
            
            HLSLPROGRAM

            #pragma vertex DefaultVertex
            #pragma fragment BlurHorizontalFragment 
            
            ENDHLSL 
        }        

        Pass
        {
            Name "BlurVertical"
            
            ZTest Always
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM

            #pragma vertex DefaultVertex
            #pragma fragment BlurVerticalFragment 
            
            ENDHLSL 
        }
        
    }
    
    
}