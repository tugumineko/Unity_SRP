#ifndef AOSA_INPUT_INCLUDE
#define AOSA_INPUT_INCLUDE

#include <HLSLSupport.cginc>

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    float3 normalOS : NORMAL;
};

struct GBufferVaryings
{
    float2 uv : TEXCOORD0;
    float4 positionCS : SV_POSITION;
    float3 normalWS : TEXCOORD1;
    float3 positionWS : TEXCOORD2;
};

UNITY_DECLARE_TEX2D(_ColorMap);
UNITY_DECLARE_TEX2D(_BreakupMap);
UNITY_DECLARE_TEX2D(_BaseShadowedMap);

CBUFFER_START(UnityPerMaterial)
half4 _Color;
float _Smoothness;
float4 _ColorMap_ST;
float4 _BreakupMap_ST;
float _BaseColorSaturation;
half4 _BaseShadowedColor;
half4 _BaseColorOverlay;
CBUFFER_END

#endif