#ifndef AOSA_INCLUDE
#define AOSA_INCLUDE

#include "../ShaderLibrary/AOSAInput.hlsl"
#include "../ShaderLibrary/AOSADeferred.hlsl"
#include "../ShaderLibrary/SpaceTransform.hlsl"

GBufferVaryings VertGBuffer(Attributes input)
 {
  GBufferVaryings output;
  output.positionCS = UnityObjectToClipPos(input.positionOS);
  output.uv = TRANSFORM_TEX(input.uv, _ColorMap);
  output.normalWS = TransformObjectToWorldNormal(input.normalOS);

  float3 positionWS = mul(unity_ObjectToWorld,input.positionOS).xyz;
  output.positionWS = positionWS;
 
  return output;
 }

GBufferOutput FragGBuffer(GBufferVaryings input)
 {
  AOSAShadeInput aosaShadeInput;
  aosaShadeInput.baseColor = UNITY_SAMPLE_TEX2D(_ColorMap,input.uv).rgb * _Color;
  aosaShadeInput.smoothness = _Smoothness;
  aosaShadeInput.normal = normalize(input.normalWS);
  aosaShadeInput.positionWS = input.positionWS;
  aosaShadeInput.breakup = UNITY_SAMPLE_TEX2D(_BreakupMap,TRANSFORM_TEX(input.uv,_BreakupMap));
  aosaShadeInput.shadowedColor = UNITY_SAMPLE_TEX2D(_BaseShadowedMap,input.uv) * _BaseShadowedColor;
  aosaShadeInput.overlay = _BaseColorOverlay;
  aosaShadeInput.saturation = _BaseColorSaturation;
  return EncodeAOSAInputToGBuffer(aosaShadeInput);
 }

#endif