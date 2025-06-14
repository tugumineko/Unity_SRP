#ifndef AOSA_WARP_INCLUDED
#define AOSA_WARP_INCLUDED

#include "../ShaderLibrary/AOSAWarpInput.hlsl"
#include "../ShaderLibrary/CommonInput.hlsl"
#include "../ShaderLibrary/SpaceTransform.hlsl"

void Inflate(inout float4 positionCS, in float2 normalCS, in float offsetDistance, in float distanceFromCamera)
{
    float2 offset = normalize(normalCS.xy) / float2((_ScreenParams.y/ _ScreenParams.x),1.0) * positionCS.w * offsetDistance;
    #if _COMPENSATE_DISTANCE
        offset *= CompensateDistance(1.0,distanceFromCamera * _WarpGlobalDistanceFade * _WarpDistanceFadeMultiplier);
    #endif
    positionCS.xy += offset;
}

WarpVaryings WarpPassVertex ( WarpAttributes input)
{
    WarpVaryings output;

    float3 positionWS = TransformObjectToWorld(input.positionOS);
    float3 positionVS = TransformWorldToView(positionWS);

    float distanceFromCamera = length(positionVS);
    #if _COMPENSATE_DISTANCE
        output.dist = distanceFromCamera;
    #endif

    output.positionCS = TransformWorldToHClip(float4(positionWS,1.0));

    float3 normalInflatedWS = TransformObjectToWorldNormal(input.normalInflatedOS);
    float3 normalInflatedVS = TransformWorldToViewNormal(normalInflatedWS);
    float2 normalInflatedCS = mul((float2x2)unity_MatrixP,normalInflatedVS);
    Inflate(output.positionCS, normalInflatedCS, _WarpWidth * _WarpWidthMultiplier, distanceFromCamera);

    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    
    #if _USE_SMOOTH_UV_GRADIENT
        float3 tangentWS = TransformObjectToWorldVector(input.tangentOS.xyz);
        float3 binormalWS = normalize(cross(normalWS,tangentWS) * input.tangentOS.w);

        float3 tangentVS = TransformWorldToViewVector(tangentWS);
        float3 binormalVS = TransformWorldToViewVector(binormalWS);

        float scale = (distanceFromCamera / unity_MatrixP[0][0]) * 0.0002;
        float2 gradUWS = _WorldSpaceUVGradientX * scale;
        float2 gradVWS = _WorldSpaceUVGradientY * scale;
        
        output.uvGrad = GetApproximateSmoothUVGradient(tangentVS, binormalVS, positionVS / distanceFromCamera, gradUWS, gradVWS);
    #endif
    
    #if _COMPENSATE_RADIAL_ANGLE
        output.screenUV = output.positionCS;
    #endif

    output.uv = TRANSFORM_TEX(input.uv,_ColorMap);

    return output;
}

float4 WarpPassFragment(WarpVaryings input) : SV_Target
{
    float4 warp  = float4(0.5, 0.5, 0.0, 1.0);
    float2 uv = input.uv;

    // 4.2. Animated line boil
    #if _USE_ANIMATED_LINE_BOIL
        uv.x += (_LineBoilTime[_AnimatedLineBoilFramerate] * 0.3) % 1.0;  
    #endif

    float2 gradU, gradV;
    #if _USE_SMOOTH_UV_GRADIENT
        gradU = input.uvGrad.xy;
        gradV = input.uvGrad.zw;
    #else
        GetUVGradientFromDerivatives(uv, gradU, gradV);
    #endif
    
    // 3.3. Compensateing for radial angle
    #if _COMPENSATE_RADIAL_ANGLE
        float2 screenUV = input.screenUV.xy / input.screenUV.w;
        float a = GetRadialAngleCompensationCoefficient(screenUV, unity_MatrixP);
    #else
        float a = 1.0;
    #endif

    //#if defined(_COMPENSATE_SKEW)

    warp = SampleMetaTexture(_WarpTexture, sampler_WarpTexture, uv, gradU, gradV, a, _WarpTextureScale);

    warp = (warp - 0.5) * _WarpWidthMultiplier + 0.5;
    
    // 4.3. Compensating for camera roll
    float2 heading = normalize(gradU);
    warp.rg -= 0.5;
    warp.rg = float2(warp.r * heading.x + warp.g * heading.y, warp.r * heading.y - warp.g * heading.x) + 0.5;

    float intensity = 1.0;
    
    // 4.4. Compensating for distance
    #if _COMPENSATE_DISTANCE
        CompensateDistance(intensity,input.dist * _WarpGlobalDistanceFade * _WarpDistanceFadeMultiplier,warp);
    #endif

    return warp;
}

#endif