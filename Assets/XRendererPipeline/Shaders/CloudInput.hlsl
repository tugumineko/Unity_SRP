#ifndef CLOUD_INPUT_INCLUDED
#define CLOUD_INPUT_INCLUDED

CBUFFER_START(XCloud)
float _CloudSpeedX;
float _CloudSpeedY;
float _CloudSize;
half _CloudFill;
float _CloudFillMax;
float _CloudFillMin;
float _CloudEdgeSoftUnevenTexSize;
float _CloudEdgeSoftMax;
float _CloudEdgeSoftMin;
float _CloudDetailSize;
float _CloudDetailIntensityFew;
float _CloudDetailIntensity;

half3 _CloudColor;
half3 _CloudRimColor;
half3 _CloudLightColor;
half _CloudRimEdgeSoft;
half _CloudLightRadius;
half _CloudLightRadiusIntensity;
half _CloudLightIntensity;
float _CloudLightUVOffset;
half _CloudHorizonSoft;
half _CloudSSSRadius;
half _CloudSSSIntensity;

CBUFFER_END

UNITY_DECLARE_TEX2D(_CloudShapeTex);
UNITY_DECLARE_TEX2D(_CloudEdgeSoftUnevenTex);

#endif