using UnityEngine;

namespace SRPLearn
{
    public enum AnimatedLineBoilFramerateType
    {
        Off = 0,
        FPS_24,
        FPS_12,
        FPS_8,
    };
    
    public enum OrientType
    {
        None,
        Contour,
        All
    };

    public enum DebugMode
    {
        Off,
        GBuffer0,
        GBuffer1,
        GBuffer2,
        GBuffer3,
        AOSAShadowTexture,
        SoftBlurTexture,
        HeavyBlurTexture,
        AOSASpecularTexture,
        SoftBlurColorTexture,
        HeavyBlurColorTexture,
        ScreenWarpTexture,
        DepthTexture
    }
    
    [System.Serializable]
    public class AOSASetting
    {
        [Header("AOSA Debug")] [SerializeField]
        public DebugMode debugmode = DebugMode.Off;

        [Header("Warp pass settings")]
        public bool useSmoothUVGradient = false;
        public bool compensateRadialAngle = false;
        public bool compensateSkew =  false;
        public bool compensateDistance = false;
        //public bool useAnimatedLineBoil = false;
        public AnimatedLineBoilFramerateType animatedLineBoilFramerate = AnimatedLineBoilFramerateType.Off;
        public OrientType orientation = OrientType.None;
        
        public Texture2D warpTexture;
        public float warpGlobalScale = 1.0f;
        public float warpGlobalDistanceFade = 1.0f;
        public float warpWidth = 1.0f;

        [Header("Blur pass settings")] 
        [Range(1.0f, 16.0f)] public float softBlurDownsample = 4;
        [Range(2.0f, 16.0f)] public float heavyBlurDownsample = 8;

        [Header("Final shadow pass settings")] 
        [Min(0)] public int shadowStepCount = 3;
        [Range(0.0f, 1.0f)] public float shadowThreshold = 0.5f;
        [Range(0.0f, 1.0f)] public float shadowThresholdSoftness = 0.3f;
        [Range(0.0f, 1.0f)] public float shadowInnerGlow = 0.3f;
        
        public bool useWarpBloom = false;
    }
}