Shader "SRPLearn/XSkybox"
{
    Properties
    {
        [HDR]_TopColor("Top Color", Color) = (1,1,1,1)
        [HDR]_MiddleColor("Middle Color", Color) = (1,1,1,1)
        [HDR]_BottomColor("Bottom Color", Color) = (1,1,1,1)
        _MiddleHeight("Middle Height", Range(0,1)) = 0.5
        _HorizonHeight ("Horizon Height", Range(0,1)) = 0.5
        
        [NoScaleOffset] _SunTex ("Sun Tex",2D) = "white" {}
        [Toggle(_SIMULATIONSUNSHAPE)] _SimulationSunShape ("Simulation Sun Shape",Float) = 1
        [HDR]_SunColor ("Sun Color",Color) = (1,1,1,1)
        _SunSize ("Sun Size",Range(0.0, 10.0)) = 5
        [HDR]_SunGlowColor("Sun Glow Color",Color) = (1,1,1,1)
        _SunGlowRadius ("Sun Glow Radius", Range(0,1)) = 0.5
        _SunIntensity ("Sun Intensity",Range(0,1)) = 1
        
        [NoScaleOffset] _MoonTex ("Moon Tex",2D) = "white" {}
        [HDR]_MoonColor ("Moon Color",Color) = (1,1,1,1)
        _MoonSize ("Moon Size",Float) = 5
        [HDR]_MoonGlowColor ("Moon Glow Color",Color) = (1,1,1,1)
        _MoonGlowRadius ("Moon Glow Radius", Range(0,1)) = 0.5
        _MoonIntensity ("Moon Intensity", Range(0,1)) = 1
        
        _StarTex ("Star Tex",2D) = "white" {}
        _StarIntensity ("Star Intensity", Float) = 3
        _StarReduceValue("Star Reduce Value",Range(0,1)) = 0.1
        
    }
    
    SubShader
    {
        Tags {
            "RenderType" = "Background" 
            "PreviewType" = "Skybox"
            "Queue" = "Background"
        }
        LOD 100

        HLSLINCLUDE

        #pragma enable_cbuffer
        #include "SkyboxInput.hlsl"
        #include "../ShaderLibrary/Tonemapping.hlsl"
        ENDHLSL 
        
        Pass
        {
            Name "XSkybox"
            
            HLSLPROGRAM

            #pragma vertex Vertex
            #pragma fragment Fragment
            
            #pragma shader_feature_local _SIMULATIONSUNSHAPE
            #pragma multi_compile _ _NIGHT
            
            struct Attributes
            {
                float4 positionOS : POSITION; //从原点出发的方向向量
                float3 directionWS : TEXCOORD0; //从摄像机出发的方向向量
            };

            struct Varyings
            {
                half4 positionCS : SV_POSITION;
                half3 directionWS : TEXCOORD0;
                float4 sunAndMoonUV : TEXCOORD1;
                half3 positionOS : TEXCOORD3;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionOS = input.positionOS;
                output.positionCS = ObjectToHClipPosition(input.positionOS.xyz);
                output.directionWS = input.directionWS;

                float3 posInLight = mul((float3x3)_XMainLightMatrixWorldToLocal,input.directionWS);
                output.sunAndMoonUV.xy = (posInLight * _SunSize).xy;
                output.sunAndMoonUV.zw = (posInLight * _MoonSize).xy;

                return output;
            }
            
            half4 Fragment(Varyings input) : SV_Target
            {
                half3 viewDirectionWS = input.directionWS;
                float skyHeight = saturate(viewDirectionWS.y);
                half middleHeight = _MiddleHeight + 0.0001;
                half3 skyColor = lerp(_BottomColor,_MiddleColor,pow(saturate(skyHeight / middleHeight), _HorizonHeight + 0.0001));
                skyColor = lerp(skyColor, _TopColor, pow2(saturate(saturate(skyHeight - middleHeight) / (1 - middleHeight))));

                //float3 posInLight = mul((float3x3)_XMainLightMatrixWorldToLocal,input.directionWS);
                
                #if !defined(_NIGHT)
                    float glowRadius = 1.0 + dot(viewDirectionWS, -_XMainLightDirection.xyz); //[0,2]
                    float lightRange = saturate(dot(viewDirectionWS,_XMainLightDirection.xyz)); //消除另一面
                    float sunGlow = 1.0 / (1 + glowRadius * lerp(150,10,_SunGlowRadius));
                    sunGlow *= pow(_SunGlowRadius, 0.5);

                    #if !defined(_SIMULATIONSUNSHAPE)
                        //float2 sunUV = (posInLight * _SunSize).xy + 0.5;
                        float2 sunUV = input.sunAndMoonUV.xy + 0.5;
                        half4 sunTex = UNITY_SAMPLE_TEX2D(_SunTex,sunUV);
                        half3 sunColor = sunTex.rgb * _SunColor;
                        half sunShape = sunTex.a * lightRange;
                    #else
                        half3 sunColor = _SunColor;
                        half sunShape = smoothstep(0.3, 0.25, distance(input.sunAndMoonUV.xy,float2(0,0))) * lightRange;
                    #endif
					skyColor = lerp(skyColor, _SunGlowColor, saturate(sunGlow * _SunIntensity));
					skyColor = lerp(skyColor, sunColor, saturate(sunShape * _SunIntensity));
                #else
                    float glowRadius = 1.0 + dot(viewDirectionWS, -_XMainLightDirection.xyz);
                    float lightRange = saturate(dot(viewDirectionWS, _XMainLightDirection.xyz));
                    float moonGlow = 1.0 / (1 + glowRadius * lerp(150,10,_MoonGlowRadius));
                    moonGlow *= pow(_MoonGlowRadius,0.5);

                    //float2 moonUV = (posInLight * _MoonSize).xy + 0.5;
                    float2 moonUV = input.sunAndMoonUV.zw + 0.5;
                    half4 moonTex = UNITY_SAMPLE_TEX2D(_MoonTex,moonUV);
                    half3 moonColor = moonTex.r * _MoonColor * _MoonIntensity;
                    half moonShape = lightRange; // * moonTex.a
                    moonColor *= moonShape;
                
                    half3 moonGlowColor = _MoonGlowColor * moonGlow * _MoonIntensity;

                    float3 positionDir = normalize(input.positionOS);
                    float2 starUV = float2(atan2(positionDir.z,positionDir.x), -acos(positionDir.y)) / float2(6.283185307,3.141592653589);
                    starUV.x += 0.5;
                    starUV = TRANSFORM_TEX(starUV,_StarTex);
                    half3 starTex = UNITY_SAMPLE_TEX2D(_StarTex,starUV);
                    half3 starColor = saturate(starTex - _StarReduceValue) * _StarIntensity * (1 - moonTex.a);
                    skyColor += starColor + moonColor + moonGlowColor;
                #endif

                return half4(ACESFilm(skyColor),1);

                
            }
            
            
            ENDHLSL 
            
            
            
        }
        
        
    }
    
    
    
    
}