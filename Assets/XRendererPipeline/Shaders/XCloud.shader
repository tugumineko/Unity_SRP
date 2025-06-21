Shader "SRPLearn/XCloud"
{
    Properties
    {
        [Header(Cloud Shape)]
        [Space]
        [NoScaleOffset]_CloudShapeTex("Cloud Shape Tex",2D) = "white" {}
        _CloudSpeedX ("Cloud Speed X",Float) = 1
        _CloudSpeedY ("Cloud Speed Y",Float) = 1
        _CloudSize ("Cloud Size",Float) = 1
        _CloudFill ("Cloud Fill",Range(0,1)) = 0.5
        _CloudFillMax("Cloud Fill Max",Float) = 1
        _CloudFillMin("Cloud Fill Min",Float) = -1
        [NoScaleOffset]_CloudEdgeSoftUnevenTex ("Cloud Edge Soft Uneven Tex",2D) = "white" {}
        _CloudEdgeSoftUnevenTexSize ("Cloud Edge Soft Uneven Tex Size",Float) = 4
        _CloudEdgeSoftMax ("Cloud Edge Soft Max",Float) = 0.1
        _CloudEdgeSoftMin ("Cloud Edge Soft Min",Float) = 0.01
        _CloudDetailSize("Cloud Detail Size", Float) = 2
        _CloudDetailIntensityFew("Cloud Detail Intensity Few",Float) = 0.5
        _CloudDetailIntensity ("Cloud Detail Intensity", Float) = 0.5
        
        [Header(Cloud Color)]
        [Space]
        [HDR] _CloudColor ("Cloud Color",Color) = (1,1,1,1)
        [HDR] _CloudRimColor ("Cloud Rim Color", Color) = (1,1,1,1)
        [HDR] _CloudLightColor ("Cloud Light Color",Color) = (1,1,1,1)
        _CloudRimEdgeSoft ("Cloud Rim Edge Soft",Range(0,1)) = 0.3
        _CloudLightRadius ("Cloud Light Radius",Range(0,1)) = 0.75
        _CloudLightRadiusIntensity ("Cloud Light Radius Intensity", Range(0,1)) = 1.0
        _CloudLightIntensity("Cloud Light Intensity", Range(0,1)) = 1.0
        _CloudLightUVOffset ("Cloud Light UV Offset", Float) = 0.01
        _CloudHorizonSoft("Cloud Horizon Soft", Range(0,1)) = 0.2
        _CloudSSSRadius ("Cloud SSS Radius",Range(0,1)) = 0.1
        _CloudSSSIntensity ("Cloud SSS Intensity", Range(0,1)) = 1.0
    }
    
    SubShader
    {
        Tags{"Queue" = "Transparent" "LightMode" = "CloudPass"}
        
        Pass
        {
            Name "CloudPass"
            
            Blend SrcAlpha OneMinusSrcAlpha
            Zwrite Off
            ZTest LEqual
            
            Tags{"LightMode" = "CloudPass"}
            
            HLSLPROGRAM

            #pragma enable_cbuffer
            
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "../ShaderLibrary/Light.hlsl"
            #include "CloudInput.hlsl"
            #include "../ShaderLibrary/Tonemapping.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half3 Color : COLOR;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };            

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv : TEXCOORD0;
                float3 viewDirectionWS : TEXCOORD1;
                float3 WtoT0 : TEXCOORD2;
                float3 WtoT1 : TEXCOORD3;
                float3 WtoT2 : TEXCOORD4;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = ObjectToHClipPosition(input.positionOS);
                output.uv.xy = input.uv * _CloudSize * ((1 - input.Color.r) * 0.5 + 1);//2层差异
                output.uv.zw = (input.Color.r * 0.5 + 1) * _Time.x * float2(_CloudSpeedX, _CloudSpeedY);
                output.viewDirectionWS = _WorldSpaceCameraPos.xyz - TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normal);
                float3 tangentWS = TransformObjectToWorldVector(input.tangent);
                float3 binormalWS = normalize(cross(normalWS,tangentWS) * input.tangent.w);
                output.WtoT0 = tangentWS;
                output.WtoT1 = binormalWS;
                output.WtoT2 = normalWS;
                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                //cloud shape
                float2 baseUV = input.uv.xy;
                float2 cloudSpeed = input.uv.zw;
                float2 uv_Main = baseUV + cloudSpeed;
                float2 uv_Detail = baseUV * _CloudDetailSize + cloudSpeed;
                float2 uv_Edge = uv_Main * _CloudEdgeSoftUnevenTexSize;

                half cloudEdgeSoftUneven = UNITY_SAMPLE_TEX2D(_CloudEdgeSoftUnevenTex, uv_Edge).r;
                cloudEdgeSoftUneven = pow(cloudEdgeSoftUneven,4);
                half edgeSmooth = lerp(_CloudEdgeSoftMin, _CloudEdgeSoftMax, cloudEdgeSoftUneven);
                half edgeSmoothMin = saturate(0.5 - edgeSmooth);
                half edgeSmoothMax = saturate(0.5 + edgeSmooth);
                half cloudFillValue = lerp(_CloudFillMin, _CloudFillMax, _CloudFill);

                half cloudMainShape = UNITY_SAMPLE_TEX2D(_CloudShapeTex, uv_Main).r;
                half cloudDetailShape = UNITY_SAMPLE_TEX2D(_CloudShapeTex, uv_Detail).r;
                half detailIntensity = lerp(_CloudDetailIntensityFew * _CloudDetailIntensity, _CloudDetailIntensity, _CloudFill);
                half detailLerp = saturate((1 - abs(cloudMainShape - 0.5) * 2) * detailIntensity);
                half cloudShape = lerp(cloudMainShape, cloudDetailShape, detailLerp);
                cloudShape = saturate(cloudShape + cloudFillValue);
                half cloudFinalShape = smoothstep(edgeSmoothMin, edgeSmoothMax, cloudShape);

                //return cloudFinalShape;
                
                //cloud color
                float3 viewDirectionWS = normalize(input.viewDirectionWS);
                float3 sunDirection = _XMainLightDirection.xyz;
                float VoL = dot(viewDirectionWS, sunDirection);

                half3 rimColorArea = smoothstep(saturate(0.5 - _CloudRimEdgeSoft), saturate(0.5 + _CloudRimEdgeSoft),1 - cloudShape);
                half3 rimColor = lerp(_CloudColor, _CloudRimColor, rimColorArea);

                //return half4(rimColor,1);

                half3 brightColor = _CloudLightColor;
                float2 uvOffset = mul(float3x3(input.WtoT0,input.WtoT1,input.WtoT2),normalize(viewDirectionWS - sunDirection));
                uvOffset = uvOffset * (1.0 - smoothstep(0.55,1.0,VoL)) * _CloudLightUVOffset;
                float2 uv_MainBright = uv_Main + uvOffset;
                half cloudMainShapeBright = UNITY_SAMPLE_TEX2D(_CloudShapeTex, uv_MainBright).r;
                half detailLerpBright = (1 - abs(cloudMainShapeBright - 0.5) * 2) * detailIntensity;
                detailLerpBright = saturate(detailLerpBright);
                half cloudShapeBright = lerp(cloudMainShapeBright, cloudDetailShape, detailLerpBright);
                cloudShapeBright = saturate(cloudShapeBright + cloudFillValue);
                half cloudFinalShapeBright = smoothstep(edgeSmoothMin, edgeSmoothMax, cloudShapeBright);
                half cloudBrightArea = saturate(cloudFinalShape - cloudFinalShapeBright);

                //return cloudBrightArea;

                half3 cloudLightedColor = lerp(rimColor, brightColor, cloudBrightArea);

                //return half4(cloudLightedColor,1);

                float thickness = cloudFinalShape;
                thickness = saturate(pow(thickness,4) - 0.3);
                float sssArea = smoothstep(1.0 - _CloudSSSRadius, 1.0, VoL) * (1.0 - step(_CloudSSSRadius,0.0));
                sssArea = pow(sssArea,4);
                cloudLightedColor = lerp(cloudLightedColor, _CloudRimColor * 2.0, sssArea * (1.0 - thickness) * saturate(_CloudSSSIntensity));
 
                float lightArea = smoothstep(1.0 - _CloudLightRadius, 1.0, VoL) * (1.0 - step(_CloudLightRadius,0.0));
                lightArea = saturate(pow(lightArea,4) * _CloudLightRadiusIntensity + 0.2) * _CloudLightIntensity;
                half3 cloudColor = lerp(_CloudColor, cloudLightedColor, lightArea);

                //return half4(cloudColor,1);

                half VDotDown = saturate(dot(viewDirectionWS, float3(0.0,-1.0,0.0)));

                
                
                half cloudAlpha = smoothstep(0.0, _CloudHorizonSoft, VDotDown) * cloudFinalShape;
                
                return half4(ACESFilm(cloudColor * saturate(VDotDown + 0.5)), cloudAlpha);                
            }
            
            ENDHLSL 

        }
        
        
    }
    
    
}