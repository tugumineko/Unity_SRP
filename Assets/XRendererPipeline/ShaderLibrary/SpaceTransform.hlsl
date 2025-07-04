
#ifndef X_SPACE_TRANSFORM_INCLUDE
#define X_SPACE_TRANSFORM_INCLUDE

#include "./CommonInput.hlsl"

float4 ObjectToHClipPosition(float3 positionOS){
    float4 positionWS = mul(unity_ObjectToWorld,float4(positionOS,1));
    return mul(unity_MatrixVP,positionWS);
}

float3 TransformObjectToWorld(float3 positionOS){
    float4 positionWS = mul(unity_ObjectToWorld,float4(positionOS,1));
    return positionWS.xyz;
}

float3 TransformObjectToWorldVector(float3 vectorOS){
    float4 vectorWS = mul(unity_ObjectToWorld,float4(vectorOS,0));
    return normalize(vectorWS.xyz);
}

float3 TransformObjectToWorldNormal(float3 normalOS)
{
    return normalize(mul(normalOS, (float3x3)unity_WorldToObject));
}

float3 TransformWorldToViewNormal(float3 normalWS){
    return normalize(mul(normalWS, (float3x3)unity_MatrixInvV));
}

half3 TransformViewToWorldNormal(half3 normalVS){
    return normalize(mul(normalVS, (float3x3)_CameraMatrixV));
}

float3 TransformWorldToView(float3 positionWS){
    return mul(_CameraMatrixV,float4(positionWS,1)).xyz;
}

float3 TransformWorldToViewVector(float3 vectorWS)
{
    return normalize(mul(_CameraMatrixV,float4(vectorWS,0)).xyz);
}

float4 TransformWorldToHClip(float3 positionWS){
    return mul(unity_MatrixVP,float4(positionWS,1));
}

float3 TransformPositionCSToWS(float3 positionCS){
    float4 positionWS = mul(_CameraMatrixVPInv,float4(positionCS,1));
    positionWS /= positionWS.w;
    return positionWS.xyz;
}

float3 ReconstructPositionWS(float2 uv, float depth){
    //使用uv和depth，可以得到ClipSpace的坐标
    float3 positionCS = float3(uv * 2 -1,depth);
    //然后将坐标从ClipSpace转换到世界坐标
    float3 positionWS = TransformPositionCSToWS(positionCS);
    return positionWS;
}

/*/**
 * Creates a rotation matrix that orients an object to face a specific direction.
 * Ideal for billboard effects.
 *
 * @param lookDirection The normalized direction the object should face.
 * @return A 3x3 rotation matrix.
 #1#
float3x3 CreateViewFacingMatrix(float3 lookDirection)
{
    // Use Z-axis as up vector if lookDirection is too close to the world Y-axis
    float3 up = lerp(float3(0, 0, 1), float3(0, 1, 0), step(abs(lookDirection.y), 0.99));
    
    float3 right = normalize(cross(up, lookDirection));
    float3 newUp = normalize(cross(right, lookDirection)); 
    
    // The matrix columns are the new X, Y, and Z axes
    return float3x3(right, newUp, lookDirection);
}*/

#define UnityObjectToClipPos ObjectToHClipPosition

#endif