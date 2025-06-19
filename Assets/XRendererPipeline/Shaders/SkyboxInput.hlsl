#ifndef SKYBOX_INPUT_INCLUDED
#define SKYBOX_INPUT_INCLUDED

#include "Assets/XRendererPipeline/ShaderLibrary/Light.hlsl"

CBUFFER_START(XSkybox)

half4 _TopColor;
half4 _MiddleColor;
half4 _BottomColor;
float _MiddleHeight;
float _HorizonHeight;

half3 _SunColor;
float _SunSize;
half3 _SunGlowColor;
float _SunGlowRadius;
half _SunIntensity;

half3 _MoonColor;
float _MoonSize;
half3 _MoonGlowColor;
float _MoonGlowRadius;
half _MoonIntensity;

half _StarIntensity;
float4 _StarTex_ST;
half _StarReduceValue;

float _IsNight;

CBUFFER_END

UNITY_DECLARE_TEX2D(_SunTex);
UNITY_DECLARE_TEX2D(_MoonTex);
UNITY_DECLARE_TEX2D(_StarTex);

half pow2(half2 v)
{
    return v * v;
}


#endif