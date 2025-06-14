#ifndef AOSA_DEFERRED_INCLUDED
#define AOSA_DEFERRED_INCLUDED

#include "GBuffer.hlsl"
#include "Light.hlsl"
#include "MetaTexture.hlsl"

///将normal分量从[-1,1]映射到[0,1]
static half3 PackNormal(half3 normalWS){
    return normalWS * 0.5 + 0.5;
}

//将c的分量从[0,1]映射到[-1,1]
static half3 UnpackNormal(half3 c){
    return c * 2 - 1;
}

static half2 SignNotZero(half2 xy){
    return xy >= 0 ? 1:-1;
}

static half2 PackNormalOct(half3 normalWS){
    half l = dot(abs(normalWS),1); //l = abs(x) + abs(y) + abs(z)
    half3 normalOct = normalWS * rcp(l); //投影到八面体
    if(normalWS.z > 0){ //八面体的上部分投影到xy平面
        return normalOct.xy; 
    }else{ //八面体下部分按对角线翻转投影到xy平面
        return (1 - abs(normalOct.yx)) * SignNotZero(normalOct.xy);
    }
}

static half3 UnpackNormalOct(half2 e){
    half3 v = half3(e.xy,1 - abs(e.x) - abs(e.y));
    if(v.z <= 0){
        v.xy = SignNotZero(v.xy) *(1 - abs(v.yx));
    } 
    return normalize(v);
}


static half2 PackNormalAccurate(half3 normalWS){
    return PackNormalOct(normalWS) * 0.5 + 0.5;
}

static half3 UnpackNormalAccurate(half2 e)
{
    return UnpackNormalOct(e * 2 - 1);
}

struct ShadePointDesc
{
    float3 positionWS;
    float3 normalWS;
};

struct AOSAShadeInput
{
    half3 baseColor;
    half smoothness;
    half3 normal;
    half breakup;
    half3 shadowedColor;
    half3 overlay;
    half saturation;
    float3 positionWS;
};

GBufferOutput EncodeAOSAInputToGBuffer(AOSAShadeInput aosaInput)
{
    GBufferOutput output;
    output.GBuffer0 = half4(aosaInput.baseColor,aosaInput.smoothness);
    half4 g1,g2,g3;
    #if GBUFFER_ACCURATE_NORMAL
    g1.xy = PackNormalAccurate(aosaInput.normal);
    g2.a = aosaInput.breakup;
    g3.a = aosaInput.saturation;
    #else
    g1.xyz = PackNormal(aosaInput.normal);
    g1.a = aosaInput.breakup;
    g2.a = aosaInput.saturation;
    #endif
    g2.rgb = aosaInput.shadowedColor;
    g3.rgb = aosaInput.overlay;
    output.GBuffer1 = g1;
    output.GBuffer2 = g2;
    output.GBuffer3 = g3;
    return output;
}

void DecodeGBuffer(inout AOSAShadeInput input, half4 gBuffer0, half4 gBuffer1, half4 gBuffer2, half4 gBuffer3)
{
    input.baseColor = gBuffer0.rgb;
    input.smoothness = gBuffer0.a;
    #if GBUFFER_ACCURATE_NORMAL
    input.normal = UnpackNormalAccurate(gBuffer1.xy);
    input.breakup = gBuffer2.a;
    input.saturation = gBuffer3.a;
    #else
    input.normal = UnpackNormal(gBuffer1.xyz);
    input.breakup = gBuffer1.a;
    input.saturation = gBuffer2.a;
    #endif
    input.shadowedColor = gBuffer2.rgb;
    input.overlay = gBuffer3.rgb;
    
}

#endif