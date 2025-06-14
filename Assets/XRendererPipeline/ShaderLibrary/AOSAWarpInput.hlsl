#ifndef AOSA_WARP_INPUT_INCLUDED
#define AOSA_WARP_INPUT_INCLUDED

#include "MetaTexture.hlsl"
#include <HLSLSupport.cginc>

//(WarpWidth,WarpTextureScale,WarpGlobalDistanceFade,0)
float4 _WarpParams;

float4 _LineBoilTime;

#define _WarpWidth _WarpParams.x
#define _WarpTextureScale _WarpParams.y
#define _WarpGlobalDistanceFade _WarpParams.z
#define _AnimatedLineBoilFramerate _WarpParams.w

UNITY_DECLARE_TEX2D(_ColorMap);
UNITY_DECLARE_TEX2D(_WarpTexture);

CBUFFER_START(UnityPerMaterial)

//Edge Breakup
float _WorldSpaceUVGradientX;
float _WorldSpaceUVGradientY;
float _WarpDistanceFadeMultiplier;
float _WarpWidthMultiplier;
float _WarpSkew;

float4 _ColorMap_ST;

CBUFFER_END

struct WarpAttributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    #if defined(_USE_SMOOTH_UV_GRADIENT)
        float4 tangentOS : TANGENT ;
    #endif
    float2 uv : TEXCOORD0;
    float3 normalInflatedOS : TEXCOORD3;
};

struct WarpVaryings
{
    float4 positionCS : SV_POSITION;
    #if defined(_USE_SMOOTH_UV_GRADIENT)
    float4 uvGrad : VAR_UV_GRADIENT;
    #endif
    float2 uv : VAR_BASE_UV;
    #if defined(_COMPENSATE_RADIAL_ANGLE)
    float4 screenUV : VAR_SCREEN_UV;
    #endif
    #if defined(_COMPENSATE_DISTANCE)
    float dist : VAR_DIST;
    #endif
};



#endif