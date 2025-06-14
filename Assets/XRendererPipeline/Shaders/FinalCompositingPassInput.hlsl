#ifndef FINAL_COMPOSITING_PASS_INPUT_INCLUDED
#define FINAL_COMPOSITING_PASS_INPUT_INCLUDED

#include "../ShaderLibrary/GBuffer.hlsl"
#include "../ShaderLibrary/Light.hlsl"

CBUFFER_START(final)

float4 _FinalShadowParams;            
            
CBUFFER_END

#define _ShadowStepCount _FinalShadowParams.x
#define _ShadowThreshold _FinalShadowParams.y
#define _ShadowThresholdSoftness _FinalShadowParams.z
#define _ShadowInnerGlow _FinalShadowParams.w

float4 _WarpParams;
#define _WarpWidth _WarpParams.x
half _WarpBloom;



UNITY_DECLARE_TEX2D(_AOSAShadowTexture);
UNITY_DECLARE_TEX2D(_SoftBlurTexture);
UNITY_DECLARE_TEX2D(_HeavyBlurTexture);
UNITY_DECLARE_TEX2D(_ScreenWarpTexture);
UNITY_DECLARE_TEX2D(_XDepthTexture);

SamplerState sampler_point_clamp;


#endif