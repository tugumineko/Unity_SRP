#ifndef META_TEXTURE_INCLUDED
#define META_TEXTURE_INCLUDED


void GetUVGradientFromDerivatives(float2 uv, out float2 gradU, out float2 gradV)
{
    float2 dx = ddx_fine(uv);
    float2 dy = ddy_fine(uv);
    gradU = float2(dx.x, dy.x);
    gradV = float2(dx.y, dy.y);
}

// 3.2. Approximate smooth UV gradients
float4 GetApproximateSmoothUVGradient(float3 tangentVS, float3 binormalVS, float3 viewDir, float2 gradUWS, float2 gradVWS)
{
    //不考虑视线方向做出的贡献，投影到屏幕
    float3 tangentSS = tangentVS - (dot(tangentVS, viewDir) * viewDir);
    float3 binormalSS = binormalVS - (dot(binormalVS, viewDir) * viewDir);

    // Eq.9
    // ∇ₛu = |∇ᵥᵥu|Tₛ/|Tₛ|²
    float2 gradU = gradUWS * tangentSS.xy / pow(length(tangentSS),2.0);

    // Eq.10
    // ∇ₛv = |∇ᵥᵥv|Bₛ/|Bₛ|²
    float2 gradV = gradVWS * binormalSS.xy / pow(length(binormalSS),2.0);

    // 基于屏幕，计算出单位屏幕像素对应的uv
    return float4(gradU, gradV);
}

float CubicBlend(float x)
{
    // β(x) = −2x³ + 3x²
    return -2 * x * x * x + 3 * x * x;
}
float2 CubicBlend(float2 t)
{
    return float2(CubicBlend(t.x), CubicBlend(t.y));
}

// Eq.25
// k(t) = 2t² − 2t + 1
float K (float t)
{
    return 2 * t * t - 2 * t + 1;
}

// 3.3. Compensating for radial angle : Obtain the radial angle compensation coefficient
float GetRadialAngleCompensationCoefficient(float2 S, float4x4 P)
{
    // Eq.12
    //      /  Sₓ     Sᵧ  \
    // Q = <  ---- , ----  >
    //      \ P₀,₀   P₁,₁ /
    float2 Q = float2(S.x / P[0][0], S.y / P[1][1]);

    // Eq.13
    // α = |Q|² + 1
    float lenQ = length(Q);
    return lenQ * lenQ + 1.0;
}

// 3.5. Orienting texture to indicate contour
void Reorient(in float diff, inout float2 uv0, inout float2 uv1, inout float2 uv2, inout float2 uv3)
{
    #if defined(_REORIENT_CONTOUR)
    float diffA = step(1.0, diff);
    float diffB = step(0.0, diff);
    float diffC = step(-1.0, diff);

    uv0.xy = lerp(uv0.xy, uv0.yx, diffB);
    uv1.xy = lerp(uv1.xy, uv1.yx, diffA);
    uv2.xy = lerp(uv2.xy, uv2.yx, diffC);
    uv3.xy = lerp(uv3.xy, uv3.yx, diffB);
    #elif defined(_REORIENT_ALL)
    uv0.xy = uv0.yx;
    uv1.xy = uv1.yx;
    uv2.xy = uv2.yx;
    uv3.xy = uv3.yx;
    #endif    
}

// 4.4. Compensating for distance
void CompensateDistance(float intensity, float d, inout float4 result)
{
    // Eq.26
    // ρ = "warp effect" 强度
    // d = 离相机的距离
    //
    //        ρ
    // ρ′ = ------
    //      1 + d²
    float pPrime = saturate(intensity / (1.0 + d * d));

    //在"warp value"和"no warp value"之间进行插值
    result = lerp(float4(0.5,0.5,0.0,1.0), result, saturate(pPrime));
}

// 3.6. Compensating for contrast reduction
float4 Contrast(float4 color, float contrast)
{
    return saturate(lerp(float4(0.5,0.5,0.5,0.5),color,contrast));
}
void CompensateContrastReduction(in float2 blend, inout float4 x)
{
    // Eq.24 (Modified)
    // c = k(Bᵤ)k(Bᵥ)
    float c = K(blend.x) * K(blend.y);

    x = Contrast(x, 1.0 + (1.0 - saturate(c)) * 2.0);
}

// 4.4. Compensating for distance
float CompensateDistance(float intensity, float d)
{
    return saturate(intensity / (1.0 + d * d));
}

// 3.1. Texture scales and blend coefficients
float4 SampleMetaTexture(Texture2D _Tex, SamplerState sampler_Tex, float2 uv, float2 gradU, float2 gradV, float a, float w)
{
    // Eq.1
    //      /   1       1   \
    // S = <  ----- , -----  >
    //      \ w|∇u|   w|∇v| /
    //
    //float2 S = float2(1.0 / (w * length(gradU)), 1.0 / (w * length(gradV)));

    // Eq.11 (Replaces Eq.1)
    //      /    1        1   \
    // S = <  ------ , ------  >
    //      \ αw|∇u|   αw|∇v| /
    float2 S = float2(1.0 / (a * w * length(gradU)), 1.0 / (a * w * length(gradV)));

    // Eq.2
    // E = { log₂(Sᵤ), log₂(Sᵥ) }
    //
    // float2 E = float2(log2(S.x), log2(S.y));
    float2 E = log2(S);
    float2 flooredE = floor(E);

    // Eq.3
    // U₀ = { 2⌊ᴱᵘ⌋u, 2⌊ᴱᵛ⌋v }
    // 
    // float2 uv0 = float2(pow(2, (floor(E.x))) * uv.x, pow(2, (floor(E.y))) * uv.y);
    // float2 uv0 = float2(exp2(floor(E.x)) * uv.x, exp2(floor(E.y)) * uv.y);
    float2 uv0 = exp2(flooredE) * uv;
    
    //相当于2^(n+1),原uv在中间
    
    // Eq.4 (Error in original paper: Eq.4 and Eq.5 are inverted)
    // U₁ = { 2u₀, v₀ }
    float2 uv1 = float2(uv0.x, 2 * uv0.y);

    // Eq.5 (Error in original paper: Eq.4 and Eq.5 are inverted)
    // U₂ = { u₀, 2v₀ }
    float2 uv2 = float2(2 * uv0.x, uv0.y);
    
    // Eq.6
    // U₃ = { 2u₀, 2v₀ }
    float2 uv3 = float2(2 * uv0.x, 2 * uv0.y);

    // 3.5. Orienting texture to indicate contour
    Reorient(flooredE.x - flooredE.y, uv0, uv1, uv2, uv3);

    //等效于smoothstep函数
    
    // Eq.7
    // B = { β(Eᵤ − ⌊Eᵤ⌋), β(Eᵥ − ⌊Eᵥ⌋) }
    float2 blend = CubicBlend(frac(E));

    float4 tex0 = _Tex.SampleLevel(sampler_Tex,uv0,0);
    float4 tex1 = _Tex.SampleLevel(sampler_Tex,uv1,0);
    float4 tex2 = _Tex.SampleLevel(sampler_Tex,uv2,0);
    float4 tex3 = _Tex.SampleLevel(sampler_Tex,uv3,0);

    // Eq.8
    // M(U) = (1 − Bᵥ)[(1 − Bᵤ)T(U₀) + BᵤT(U₂)] +
    //             Bᵥ [(1 − Bᵤ)T(U₁) + BᵤT(U₃)]
    float4 result = lerp(lerp(tex0,tex2,blend.x),lerp(tex1,tex3, blend.x),blend.y);

    CompensateContrastReduction(blend, result);

    return result;
}




#endif
