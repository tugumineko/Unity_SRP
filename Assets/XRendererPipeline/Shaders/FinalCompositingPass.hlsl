#ifndef FINAL_COMPOSITING_PASS_INCLUDED
#define FINAL_COMPOSITING_PASS_INCLUDED

#include "FinalCompositingPassInput.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    half4 positionCS : SV_POSITION;
    half2 uv : TEXCOORD0;
};
            
Varyings Vertex(Attributes input)
{
    Varyings output;
    output.positionCS = ObjectToHClipPosition(input.positionOS.xyz);
    output.uv = input.uv;
    return output;
}

//色阶 + 叠加
void GetShadeParams(in half2 softBlur, in half2 heavyBlur, inout half2 shadows, inout half specular, inout half bloom)
{
    shadows.r = softBlur.r;
    shadows.g = heavyBlur.r;
    specular = step(0.5, softBlur.g);
    bloom = heavyBlur.g + 0.5 * softBlur.g;
}

//均匀分级
half SteppedGradient(half value, float threshold, float softness, float stepCount)
{
    half thresholdedValue = (value - threshold) / softness;
    return saturate(floor(thresholdedValue * stepCount) / stepCount);
}

half3 ApplyColoredShadows(in half3 shadowedColor, in half3 baseColor, in half2 shadows, in half specular, in half breakup)
{
    float stepCount = floor(breakup * _ShadowStepCount + 1.0); 
    float threshold = _ShadowThreshold;
    float thresholdSoftness = breakup * _ShadowThresholdSoftness;
    float innerGlow = _ShadowInnerGlow;
    
    //shadows = (softBlur, heavyBlur) 
    half shadow = SteppedGradient(shadows.r,threshold, thresholdSoftness, stepCount);
    shadow = lerp((1.0 - shadows.g * innerGlow * 2.0) * shadow,shadow + (1.0 - shadows.g) * (1.0 - shadow) * innerGlow,1);//末尾1表示平行光，0表示点光
    
    half3 color = lerp(shadowedColor, baseColor * _XMainLightColor.rgb, shadow) + specular * _XMainLightColor.rgb;
    
    return color;
}

float isSky(float2 uv)
{
    half depth;
    #if UNITY_REVERSED_Z
    depth = 1 - _XDepthTexture.SampleLevel(sampler_XDepthTexture,uv,0);
    #else
    depth = _XDepthTexture.SampleLevel(sampler_XDepthTexture,uv,0);
    #endif
    return step(0.999,depth);
}

struct CompositingOutput
{
    half4 _CompositingColor : SV_Target0;
    half4 _CompositingBloom : SV_Target1;
};

half Luminance(half3 color)
{
    return half3(0.2125 * color.r, 0.7154 * color.g, 0.0721 * color.b);
}

CompositingOutput CompositingFragment(Varyings input) : SV_Target
{
    float2 uv = input.uv;
    half3 baseColor = _GBuffer0.Sample(sampler_point_clamp, uv).rgb;
    half3 shadowedColor = _GBuffer2.Sample(sampler_point_clamp, uv,0).rgb;
    half3 softShadowBlur = _SoftBlurTexture.SampleLevel(sampler_SoftBlurTexture,uv,0);
    half3 heavyShadowBlur = _HeavyBlurTexture.SampleLevel(sampler_HeavyBlurTexture,uv,0);
    half3 softSpecularBlur = UNITY_SAMPLE_TEX2D_LOD(_SoftBlurTexture2,uv,0);
    half3 heavySpecularBlur = UNITY_SAMPLE_TEX2D_LOD(_HeavyBlurTexture2,uv,0);
    half breakup = _GBuffer2.Sample(sampler_point_clamp, uv,0).a; //只考虑GBUFFER_ACCURATE_NORMAL
    
    half3 specular = step(0.1, Luminance(softSpecularBlur)) * softSpecularBlur;

    float stepCount = floor(breakup * _ShadowStepCount + 1.0); 
    float threshold = _ShadowThreshold;
    float thresholdSoftness = breakup * _ShadowThresholdSoftness;
    float innerGlow = _ShadowInnerGlow;

    half thresholdedValue = (Luminance(softShadowBlur) + 0.27 - threshold) / thresholdSoftness;
    half3 shadow = saturate(floor(thresholdedValue * stepCount) / stepCount) * softShadowBlur;
    
    shadow = lerp((1.0 - heavyShadowBlur * innerGlow * 2.0) * shadow,shadow + (1.0 - heavyShadowBlur)* (1-shadow)* innerGlow,0);

    //half3 color = shadow;
    half3 color = shadowedColor + (baseColor - shadowedColor) * shadow;
    //half3 color = baseColor * shadow + shadowedColor * (1.0 - saturate(max(shadow.r, max(shadow.g, shadow.b))));

    color += specular;

    color = lerp(color, baseColor, isSky(input.uv));

    half3 bloom = heavySpecularBlur + 0.5 * softSpecularBlur;

    bloom = lerp(bloom, baseColor, isSky(input.uv));
    
    CompositingOutput output;
    output._CompositingColor = half4(color,1);
    output._CompositingBloom = half4(bloom,1);
    
    return output;
}

UNITY_DECLARE_TEX2D(_CompositingColorTex);
UNITY_DECLARE_TEX2D(_CompositingBloomTex);

// From https://www.ryanjuckett.com/photoshop-blend-modes-in-hlsl/
float BlendMode_Overlay(float base, float blend)
{
    float t = step(0.5, base);
    float low = 2.0 * base * blend;
    float high = 1.0 - 2.0 * (1.0 - base) * (1.0 - blend);
    return lerp(low, high, t);
}

// From https://www.ryanjuckett.com/photoshop-blend-modes-in-hlsl/
float3 BlendMode_Overlay(float3 base, float3 blend)
{
    return float3(BlendMode_Overlay(base.r, blend.r), BlendMode_Overlay(base.g, blend.g), BlendMode_Overlay(base.b, blend.b));
}

// From https://docs.unity3d.com/Packages/com.unity.shadergraph@6.9/manual/Saturation-Node.html
float3 Saturation(float3 color, float saturation)
{
    float luma = dot(color, float3(0.2126729, 0.7151522, 0.0721750));
    return luma.xxx + saturation.xxx * (color - luma.xxx);
}

void ApplyColoredBlooms(inout half3 color, in half bloom)
{
    color += bloom * _XMainLightColor.rgb;
}

half4 FinalCompositingFragment(Varyings input) : SV_Target
{
    float2 warp = _ScreenWarpTexture.SampleLevel(sampler_ScreenWarpTexture,input.uv,0).rg;
    warp = warp * 2.0 - 1.0;
    warp.rg *= _WarpWidth;
    
    float2 warpUV = input.uv + warp.rg;

    half3 color = UNITY_SAMPLE_TEX2D_LOD(_CompositingColorTex,warpUV,0).rgb;
    half3 baseColor = color;
    half3 bloom = lerp(
                    UNITY_SAMPLE_TEX2D_LOD(_CompositingBloomTex,input.uv,0).rgb,
                    UNITY_SAMPLE_TEX2D_LOD(_CompositingBloomTex,warpUV,0).rgb,
                    _WarpBloom
                    );
    
    //ApplyColoredBlooms(color,bloom);
    color += bloom;
    
    half4 g3 = _GBuffer3.SampleLevel(sampler_point_clamp,input.uv,0);
    half3 overlay = g3.rgb;
    half saturation = g3.a;
    
    // Overlay
    color = lerp(color, BlendMode_Overlay(color, overlay), 0);

    // Saturation
    color = Saturation(color, saturation);

    color = lerp(color, baseColor, isSky(input.uv));
    
    return half4(color,1);
    
}

#endif