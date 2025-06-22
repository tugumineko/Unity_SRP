Shader "SRPLearn/AOSA"
{
    Properties
    {
        _ColorMap("Texture",2D) = "white" {}
        _Color("Color",Color) = (1,1,1,1)
        
        [NoScaleOffset] _BaseShadowedMap ("Shadowed Texture",2D) = "white" {}
        _BaseShadowedColor("Shadowed Color", Color) = (0.0,0.0,0.0,1.0)
        _BreakupMap("Breakup map",2D) = "black" {}
        _BaseColorOverlay("Color overlay", Color) = (0.5,0.5,0.5,1.0)
        _BaseColorSaturation("Saturation", Float) = 1.0
        _Smoothness("Smoothness",Range(0,1)) = 0.5
        
        [Header(Edge breakup)]
        _WorldSpaceUVGradientX("World Space UV Gradient X",Float) = 1.0
        _WorldSpaceUVGradientY("World Space UV Gradient Y",Float) = 1.0
        _WarpDistanceFadeMultiplier("Distance fade multiplier",Float) = 1.0
        _WarpWidthMultiplier("Width (aka warp amount) multiplier",Float) = 1.0
        _WarpSkew ("Skew",Float) = 4.0
    }
    
    SubShader
    {
        Tags{"RenderType" = "Opaque"}
        LOD 100
        
        HLSLINCLUDE
        #pragma enable_cbuffer
        ENDHLSL
    
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back
            
            HLSLPROGRAM

            #pragma target 4.5
            
            #pragma multi_compile _ X_SHAODW_BIAS_CASTER_VERTEX

            #include "../ShaderLibrary/ShadowCaster.hlsl"
            #include "AOSA.hlsl"

            #pragma vertex ShadowCasterVertex
            #pragma fragment ShadowCasterFragment 
            
            ENDHLSL 
        }
    

    Pass
    {
        Tags {"LightMode" = "Deferred"}
        
        Name "DEFERRED"
        
        Cull Back
        
        HLSLPROGRAM

        #pragma target 4.5
        
        #pragma shader_feature GBUFFER_ACCURATE_NORMAL
        
        #pragma vertex VertGBuffer
        #pragma fragment FragGBuffer 
        #include "AOSA.hlsl"
        
        ENDHLSL
    }

    Pass
    {
        Tags {"LightMode" = "Warp"}
        
        Name "Warp"


        HLSLPROGRAM

        #pragma target 4.5
        
        #pragma multi_compile _ _USE_SMOOTH_UV_GRADIENT
        #pragma multi_compile _ _COMPENSATE_RADIAL_ANGLE
        #pragma multi_compile _ _COMPENSATE_SKEW
        #pragma multi_compile _ _COMPENSATE_DISTANCE
        //#pragma multi_compile _ _USE_ANIMATED_LINE_BOIL
        #pragma multi_compile _ _REORIENT_CONTOUR _REORIENT_ALL
        
        #pragma vertex WarpPassVertex
        #pragma fragment WarpPassFragment

        #include "AOSAWarp.hlsl"
        
        ENDHLSL
    }

    }
}