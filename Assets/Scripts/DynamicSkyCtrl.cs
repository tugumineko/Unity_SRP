

using System;
using UnityEngine;

[RequireComponent(typeof(TimeCtrl))]
[ExecuteAlways]
public class DynamicSkyCtrl : MonoBehaviour
{
    [Header("Sky Color")] 
    [GradientUsage(true, ColorSpace.Linear)]
    public Gradient TopColor;

    [GradientUsage(true, ColorSpace.Linear)]
    public Gradient MiddleColor;

    [GradientUsage(true, ColorSpace.Linear)]
    public Gradient BottomColor;

    [Header("OuterSpace")] 
    [Range(-180f, 180f)]
    public float Longitude = 0.0f;
    [Range(-180f, 180f)]
    public float Latitude = 0.0f;
    
    public AnimationCurve SunIntensity = AnimationCurve.Linear(0f, 1f, 100f, 1f);
    public AnimationCurve MoonIntensity = AnimationCurve.Linear(0f, 1f, 100f, 1f);
    [GradientUsage(true, ColorSpace.Linear)]
    public Gradient SunColor;
    [GradientUsage(true, ColorSpace.Linear)]
    public Gradient MoonColor;
    [GradientUsage(true, ColorSpace.Linear)]
    public Gradient SunGlowColor;
    [GradientUsage(true, ColorSpace.Linear)]
    public Gradient MoonGlowColor;
    public AnimationCurve SunGlowRadius = AnimationCurve.Linear(0f, 1f, 100f, 1f);
    public AnimationCurve MoonGlowRadius = AnimationCurve.Linear(0f, 1f, 100f, 1f);
    public AnimationCurve StarIntensity = AnimationCurve.Linear(0f, 1f, 100f, 1f);

    [Header("Cloud")] 
    public AnimationCurve CloudFill = AnimationCurve.Linear(0f, 0.5f, 100f, 0.5f);
    [GradientUsage(true, ColorSpace.Linear)]
    public Gradient CloudColor;
    [GradientUsage(true, ColorSpace.Linear)]
    public Gradient CloudRimColor;
    [GradientUsage(true, ColorSpace.Linear)]
    public Gradient CloudLightColor;
    public AnimationCurve CloudLightIntensity = AnimationCurve.Linear(0f, 1f, 100f, 1f);
    public AnimationCurve CloudLightRadius = AnimationCurve.Linear(0f, 0.75f, 100f, 0.75f);
    public AnimationCurve CloudLightRadiusIntensity = AnimationCurve.Linear(0f, 1.5f, 100f, 1.5f);    
    public AnimationCurve CloudSSSRadius = AnimationCurve.Linear(0f, 0.1f, 100f, 0.1f);
    public AnimationCurve CloudSSSIntensity = AnimationCurve.Linear(0f, 1.5f, 100f, 1.5f);
    
    [Header("Light")]
    public Gradient LightColor;
    public AnimationCurve LightIntensity = AnimationCurve.Linear(0f, 0f, 100f, 1f);

    [Header("Reference Node")] 
    public Material SkyMat;
    public Material CloudMat;
    public Transform Light;
    
    private TimeCtrl mTimeCtrl;
    private Light mLightCom;

    private void Start()
    {
        mTimeCtrl = GetComponent<TimeCtrl>();
        if (Light != null)
        {
            mLightCom = Light.GetComponent<Light>();
        }
    }

    private void LateUpdate()
    {
        UpdateLight();
        UpdateSkyBox();
        UpdateSkyCloud();
    }

    void UpdateSkyBox()
    {
        if (SkyMat)
        {
            RenderSettings.skybox = SkyMat;
            float colorKey = 0f;
            float floatKey = 0f;
            if (mTimeCtrl)
            {
                colorKey = mTimeCtrl.GradientTime;
                floatKey = mTimeCtrl.CurveTime;
            }
            SkyMat.SetVector(ShaderProperties.BottomColor, BottomColor.Evaluate(colorKey));
            SkyMat.SetVector(ShaderProperties.TopColor, TopColor.Evaluate(colorKey));
            SkyMat.SetVector(ShaderProperties.MiddleColor, MiddleColor.Evaluate(colorKey));
            SkyMat.SetVector(ShaderProperties.SunColor, SunColor.Evaluate(colorKey));
            SkyMat.SetVector(ShaderProperties.MoonColor, MoonColor.Evaluate(colorKey));
            SkyMat.SetFloat(ShaderProperties.SunIntensity, SunIntensity.Evaluate(floatKey));
            SkyMat.SetFloat(ShaderProperties.MoonIntensity, MoonIntensity.Evaluate(floatKey));
            SkyMat.SetVector(ShaderProperties.SunGlowColor, SunGlowColor.Evaluate(colorKey));
            SkyMat.SetVector(ShaderProperties.MoonGlowColor, MoonGlowColor.Evaluate(colorKey));
            SkyMat.SetFloat(ShaderProperties.SunGlowRadius, SunGlowRadius.Evaluate(floatKey));
            SkyMat.SetFloat(ShaderProperties.MoonGlowRadius, MoonGlowRadius.Evaluate(floatKey));
            SkyMat.SetFloat(ShaderProperties.StarIntensity, StarIntensity.Evaluate(floatKey));
            
        }
    }

    void UpdateSkyCloud()
    {
        if (CloudMat)
        {
            float colorKey = 0f;
            float floatKey = 0f;
            if (mTimeCtrl)
            {
                colorKey = mTimeCtrl.GradientTime;
                floatKey = mTimeCtrl.CurveTime;
            }
            CloudMat.SetVector(ShaderProperties.CloudColor, CloudColor.Evaluate(colorKey));
            CloudMat.SetVector(ShaderProperties.CloudRimColor, CloudRimColor.Evaluate(colorKey));
            CloudMat.SetVector(ShaderProperties.CloudLightColor, CloudLightColor.Evaluate(colorKey));
            CloudMat.SetFloat(ShaderProperties.CloudFill, CloudFill.Evaluate(floatKey));
            CloudMat.SetFloat(ShaderProperties.CloudLightIntensity, CloudLightIntensity.Evaluate(floatKey));
            CloudMat.SetFloat(ShaderProperties.CloudLightRadius, CloudLightRadius.Evaluate(floatKey));
            CloudMat.SetFloat(ShaderProperties.CloudLightRadiusIntensity, CloudLightRadiusIntensity.Evaluate(floatKey));
            CloudMat.SetFloat(ShaderProperties.CloudSSSRadius, CloudSSSRadius.Evaluate(floatKey));
            CloudMat.SetFloat(ShaderProperties.CloudSSSIntensity, CloudSSSIntensity.Evaluate(floatKey));
        }
    }
    
    void UpdateLight()
    {
        if (Light)
        {
            float sunProgression = 0f;
            float moonProgression = 0f;
            bool isNight = false;
            if (mTimeCtrl)
            {
                sunProgression = mTimeCtrl.DayProgression;
                moonProgression = mTimeCtrl.NightProgression;
                isNight = !mTimeCtrl.IsDay;
            }

            if (!isNight)
            {
                Light.rotation = Quaternion.Euler(0.0f, Longitude, Latitude) *
                                 Quaternion.Euler(Mathf.Lerp(-15f, 195f, sunProgression), 180f, 0f);
                SkyMat.DisableKeyword("_NIGHT");
                Shader.SetGlobalFloat(ShaderProperties.IsNight, 0);
            }
            else
            {
                Light.rotation = Quaternion.Euler(0.0f, Longitude, Latitude) *
                                 Quaternion.Euler(Mathf.Lerp(-15f, 195f, moonProgression), 180f, 0f);
               SkyMat.EnableKeyword("_NIGHT");
               Shader.SetGlobalFloat(ShaderProperties.IsNight, 1);
            }
            Shader.SetGlobalMatrix(ShaderProperties.MainLightMatrixWorldToLocal, Light.worldToLocalMatrix);
        }

        if (mLightCom)
        {
            float colorKey = 0f;
            float floatKey = 0f;
            if (mTimeCtrl)
            {
                colorKey = mTimeCtrl.GradientTime;
                floatKey = mTimeCtrl.CurveTime;
            }
            mLightCom.color = LightColor.Evaluate(colorKey);
            mLightCom.intensity = LightIntensity.Evaluate(floatKey);
            mLightCom.shadowStrength = mLightCom.intensity;
        }
        RenderSettings.ambientLight = mLightCom.color * mLightCom.intensity * 0.5f;
    }
    
    public class ShaderProperties
    {
        public static readonly int TopColor = Shader.PropertyToID("_TopColor");
        public static readonly int MiddleColor = Shader.PropertyToID("_MiddleColor");
        public static readonly int BottomColor = Shader.PropertyToID("_BottomColor");
        public static readonly int SunIntensity = Shader.PropertyToID("_SunIntensity");
        public static readonly int MoonIntensity = Shader.PropertyToID("_MoonIntensity");
        public static readonly int SunColor = Shader.PropertyToID("_SunColor");
        public static readonly int MoonColor = Shader.PropertyToID("_MoonColor");
        public static readonly int SunGlowColor = Shader.PropertyToID("_SunGlowColor");
        public static readonly int MoonGlowColor = Shader.PropertyToID("_MoonGlowColor");
        public static readonly int SunGlowRadius = Shader.PropertyToID("_SunGlowRadius");
        public static readonly int MoonGlowRadius = Shader.PropertyToID("_MoonGlowRadius");
        public static readonly int StarIntensity = Shader.PropertyToID("_StarIntensity");
    
        public static readonly int CloudFill =  Shader.PropertyToID("_CloudFill");
        public static readonly int CloudColor = Shader.PropertyToID("_CloudColor");
        public static readonly int CloudRimColor = Shader.PropertyToID("_CloudRimColor");
        public static readonly int CloudLightColor = Shader.PropertyToID("_CloudLightColor");
        public static readonly int CloudLightIntensity = Shader.PropertyToID("_CloudLightIntensity");
        public static readonly int CloudLightRadius = Shader.PropertyToID("_CloudLightRadius");
        public static readonly int CloudLightRadiusIntensity = Shader.PropertyToID("_CloudLightRadiusIntensity");
        public static readonly int CloudSSSRadius =  Shader.PropertyToID("_CloudSSSRadius");
        public static readonly int CloudSSSIntensity = Shader.PropertyToID("_CloudSSSIntensity");
        
        public static readonly int IsNight = Shader.PropertyToID("_IsNight");
        
        public static readonly int MainLightMatrixWorldToLocal = Shader.PropertyToID("_XMainLightMatrixWorldToLocal");
    }
}