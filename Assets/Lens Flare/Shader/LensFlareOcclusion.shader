Shader "Hidden/SRPLearn/LensFlareOcclusion"
{
    
    SubShader
    {
        HLSLINCLUDE
        #include "Assets/XRendererPipeline/ShaderLibrary/SpaceTransform.hlsl"
        #include "Assets/XRendererPipeline/ShaderLibrary/Light.hlsl"

        UNITY_DECLARE_TEX2D(_XDepthTexture);
		
		CBUFFER_START(OcclusionCompute)
			float _OcclusionRadius;
        CBUFFER_END
        
        /*
        //link : https://github.com/Unity-Technologies/FontainebleauDemo/tree/master/Assets/Scripts/LensFlare
        static const uint DEPTH_SAMPLE_COUNT = 32;
        static const float2 samples[DEPTH_SAMPLE_COUNT] = {
            float2(0.658752441406,-0.0977704077959),
			float2(0.505380451679,-0.862896621227),
			float2(-0.678673446178,0.120453640819),
			float2(-0.429447203875,-0.501827657223),
			float2(-0.239791020751,0.577527523041),
			float2(-0.666824519634,-0.745214760303),
			float2(0.147858589888,-0.304675519466),
			float2(0.0334240831435,0.263438135386),
			float2(-0.164710089564,-0.17076793313),
			float2(0.289210408926,0.0226817727089),
			float2(0.109557107091,-0.993980526924),
			float2(-0.999996423721,-0.00266989553347),
			float2(0.804284930229,0.594243884087),
			float2(0.240315377712,-0.653567194939),
			float2(-0.313934922218,0.94944447279),
			float2(0.386928111315,0.480902403593),
			float2(0.979771316051,-0.200120285153),
			float2(0.505873680115,-0.407543361187),
			float2(0.617167234421,0.247610524297),
			float2(-0.672138273716,0.740425646305),
			float2(-0.305256098509,-0.952270269394),
			float2(0.493631094694,0.869671344757),
			float2(0.0982239097357,0.995164275169),
			float2(0.976404249668,0.21595069766),
			float2(-0.308868765831,0.150203511119),
			float2(-0.586166858673,-0.19671548903),
			float2(-0.912466347218,-0.409151613712),
			float2(0.0959918648005,0.666364192963),
			float2(0.813257217407,-0.581904232502),
			float2(-0.914829492569,0.403840065002),
			float2(-0.542099535465,0.432246923447),
			float2(-0.106764614582,-0.618209302425)
        };
        */

		float4 _ZBufferParams;//Unity内置变量

		inline float Linear01Depth(float rawDepth, float4 zBufferParams)
		{
		    return 1.0 / (zBufferParams.x * rawDepth + zBufferParams.y);
		}
        
        /*float GetOcclusion(float2 screenPos, float depth, float2 radius)
        {
            float contrib = 0.0f;
            float sample_Contrib = 1.0 / DEPTH_SAMPLE_COUNT;
            for (uint i = 0; i < DEPTH_SAMPLE_COUNT; i ++)
            {
            	float2 pos = screenPos + samples[i] * radius;
            	pos.y *= _ProjectionParams.x;
            	pos = pos * 0.5 + 0.5;
            	float sampledDepth = Linear01Depth(UNITY_SAMPLE_TEX2D_LOD(_XDepthTexture,pos,0).r,_ZBufferParams);
            	contrib += sample_Contrib * step(depth,sampledDepth);
            }
			return contrib;
        }*/

		float Hash12(float2 p)
		{
		    // Hash function to generate rotation offset per-pixel
		    float3 p3 = frac(float3(p.xyx) * 0.1031);
		    p3 += dot(p3, p3.yzx + 19.19);
		    return frac((p3.x + p3.y) * p3.z);
		}

		float2 Rotate(float2 v, float angle)
		{
		    float s = sin(angle);
		    float c = cos(angle);
		    return float2(c * v.x - s * v.y, s * v.x + c * v.y);
		}

        //link: https://github.com/dillongoostudios/goo-engine/blob/goo-engine-main/source/blender/draw/engines/eevee/shaders/closure_eval_surface_lib.glsl#L497
		float GetOcclusion(float2 screenPos, float depth, float2 radius)
		{
		    const int NUM_RAYS = 6;               
		    float totalSamples = NUM_RAYS * 2;

		    float contrib = 0.0;

		    // Add small randomized rotation to break regularity
		    float hashAngle = Hash12(screenPos * _ScreenParams.xy) * 6.2831; // [0, 2PI)

		    for (int r = 0; r < NUM_RAYS; r++)
		    {
		        // evenly distributed rotation (360° / NUM_RAYS)
		        float angle = hashAngle + 6.2831 * r / NUM_RAYS;
		        float2 dir = Rotate(float2(1, 0), angle);
		    	
	            float2 offset = dir * radius;

	            float2 posA = screenPos + offset;
	            float2 posB = screenPos - offset;

	            posA.y *= _ProjectionParams.x;
	            posB.y *= _ProjectionParams.x;

	            posA = posA * 0.5 + 0.5;
	            posB = posB * 0.5 + 0.5;

	            float depthA = Linear01Depth(UNITY_SAMPLE_TEX2D_LOD(_XDepthTexture, posA, 0).r, _ZBufferParams);
	            float depthB = Linear01Depth(UNITY_SAMPLE_TEX2D_LOD(_XDepthTexture, posB, 0).r, _ZBufferParams);

	            contrib += step(depth, depthA);
	            contrib += step(depth, depthB);
	        
		    }

		    return contrib / totalSamples;
		}
        
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
        };
        
        Varyings Vertex(uint vertexID : SV_VertexID)
        {
            Varyings o;
            float2 pos = float2((vertexID << 1) & 2, vertexID & 2);
            o.positionCS = float4(pos * 2.0f - 1.0f, 0.0f, 1.0f);
            return o;
        }
        
        ENDHLSL 
        
        Pass
        {
            Name "Point Light OcclusionCompute"
            
            Cull Off
            ZTest Always
            ZWrite Off
            ColorMask R
            Blend Off
            
            HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment Fragment


            float4 Fragment(Varyings input) : SV_Target
            {
				float2 sunPosNDC;
				float sunDepth;
				float clipRadius;

				float3 sunPosWS = TransformObjectToWorld(float3(0,0,0)); //挂在灯光下，所以是自然的灯光空间
				float3 sunPosVS = TransformWorldToView(sunPosWS);
            	float4 sunPosCS = TransformWorldToHClip(sunPosWS);
				sunDepth = sunPosCS.w * _ProjectionParams.w; // trick : 线性深度到 0 - 1
				sunPosNDC = sunPosCS.xy / sunPosCS.w;  // NDC [-1,1]
				float4 sunRadiusCS = TransformWorldToHClip(sunPosWS + float3(0,1,0) * _OcclusionRadius);
				float2 sunRadiusNDC = sunRadiusCS.xy / sunRadiusCS.w;
				clipRadius = distance(sunPosNDC,sunRadiusNDC);

				float ratio = _ScreenParams.x / _ScreenParams.y; //screenWidth/screenHeight
				float occlusion = GetOcclusion(sunPosNDC, sunDepth - _OcclusionRadius * _ProjectionParams.w, clipRadius * float2(1/ratio,1));
				float maxSunPosNDC = saturate(max(abs(sunPosNDC.x),abs(sunPosNDC.y)));
				occlusion *= (1 - saturate(maxSunPosNDC - 0.85) / 0.15);
				occlusion *= step(sunPosVS.z,0);
            	
            	return float4(occlusion,0,0,0);
            }
            
            ENDHLSL 
        }

        Pass
        {
            Name "Directional Light OcclusionCompute"
            
            Cull Off
            ZTest Always
            ZWrite Off
            ColorMask R
            Blend Off
            
            HLSLPROGRAM
            #pragma vertex Vertex
			#pragma fragment Fragment


            float4 Fragment(Varyings input) : SV_Target
            {
            	float2 sunPosNDC;
				float sunDepth;
				float clipRadius;

				float3 sunPosVS = mul((float3x3)_CameraMatrixV, _XMainLightDirection);
				float4 sunPosCS = mul(unity_MatrixP,float4(sunPosVS,1));
				sunDepth = 0.999;
				sunPosNDC  = sunPosCS.xy / sunPosCS.w;
				clipRadius = _OcclusionRadius;

				float ratio = _ScreenParams.x / _ScreenParams.y;
				float occlusion = GetOcclusion(sunPosNDC, sunDepth - _OcclusionRadius, clipRadius * float2(1/ratio,1));
				float maxSunPosNDC = saturate(max(abs(sunPosNDC.x),abs(sunPosNDC.y)));
				occlusion *= (1 - saturate(maxSunPosNDC - 0.85) / 0.15);
				occlusion *= step(sunPosVS.z,0);
            	
            	return float4(occlusion,0,0,0);
            }
            
            ENDHLSL 
        }
        
    }
    
    
    
}