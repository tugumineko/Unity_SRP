#ifndef AOSA_BLUR_INCLUDED
#define AOSA_BLUR_INCLUDED

#include "../ShaderLibrary/SpaceTransform.hlsl"

UNITY_DECLARE_TEX2D(_BlurBlitTex);

CBUFFER_START(Blur)

float4 _BlitTex_TexelSize;

CBUFFER_END

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

Varyings DefaultVertex(Attributes input)
{
    Varyings output;
    output.positionCS = ObjectToHClipPosition(input.positionOS.xyz);
    output.uv = input.uv;
    return output;
}

float4 BoxSample(Texture2D source, float2 uv, float4 offset)
{
    return (source.SampleLevel(sampler_BlurBlitTex, uv + offset.zw,0) +
     source.SampleLevel(sampler_BlurBlitTex, uv + offset.zy,0) +
     source.SampleLevel(sampler_BlurBlitTex, uv + offset.xw,0) +
     source.SampleLevel(sampler_BlurBlitTex, uv + offset.xy,0)) * 0.25;
}

half4 DownSampleFragment(Varyings input): SV_Target{
    float2 uv = input.uv;
    float4 offset = _BlitTex_TexelSize.xyxy * float2(-1.0,1.0).xxyy;
    return BoxSample(_BlurBlitTex, uv, offset);
}

float4 Sample1DGaussian(Texture2D source, float2 uv, float2 offset)
{
    return source.SampleLevel(sampler_BlurBlitTex, uv, 0) * 0.227 +
        source.SampleLevel(sampler_BlurBlitTex, uv + offset * -3.231, 0) * 0.07 +
        source.SampleLevel(sampler_BlurBlitTex, uv + offset * 3.231, 0) * 0.07 +
        source.SampleLevel(sampler_BlurBlitTex, uv + offset * -1.385, 0) * 0.316 +
        source.SampleLevel(sampler_BlurBlitTex, uv + offset * 1.385, 0) * 0.316;
}

half4 BlurHorizontalFragment(Varyings input) : SV_Target{
    float2 uv = input.uv;
    float2 offset = float2(_BlitTex_TexelSize.x,0.0);

    return Sample1DGaussian(_BlurBlitTex, uv, offset);
}

half4 BlurVerticalFragment(Varyings input) : SV_Target{
    float2 uv = input.uv;
    float2 offset = float2(0.0, _BlitTex_TexelSize.y);

    return Sample1DGaussian(_BlurBlitTex, uv, offset);
}


#endif