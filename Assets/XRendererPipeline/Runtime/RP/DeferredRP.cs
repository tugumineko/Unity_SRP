#define GBUFFER_NORMAL_ACCURATE

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace SRPLearn{

    using FrameBufferOutputDebug = DeferredRPSetting.FrameBufferOutputDebug;

    public class DeferredRP : BaseRP
    {
        private const string LightModeId = "Deferred";
        private RenderObjectPass _opaquePass = new RenderObjectPass(false,LightModeId,false);
       
        private ShadowCasterPass _shadowCastPass = new ShadowCasterPass();
        private DeferredLightingPass _deferredLightingPass = new DeferredLightingPass();
        private DeferredTileLightCulling _deferredLightingCulling;
        private DeferredLightConfigurator _deferredLightConfigurator = new DeferredLightConfigurator();
        private WarpPass _warpPass = new WarpPass();
        private FinalCompositingPass _finalPass = new FinalCompositingPass();
        private LensFlarePass _lensFlarePass = new LensFlarePass();
        
        private List<RenderTexture> _GBuffers = new List<RenderTexture>();
        private RenderTargetIdentifier[] _GBufferRTIs;
        private int[] _GBufferNameIDs = {
            ShaderConstants.GBuffer0,
            ShaderConstants.GBuffer1,
            ShaderConstants.GBuffer2,
            ShaderConstants.GBuffer3,
        };
        private RenderTextureFormat[] _GBufferFormats = {
            RenderTextureFormat.ARGB32,
            RenderTextureFormat.ARGB32,
            RenderTextureFormat.ARGB32,
            RenderTextureFormat.ARGB32
        };
        
        private RenderTexture _depthTexture;
        private RenderTexture _softBlurTexture;
        private RenderTexture _softBlurTexture2;
        private RenderTexture _heavyBlurTexture;
        private RenderTexture _heavyBlurTexture2;
        private RenderTexture _warpTexture;
        private RenderTexture _colorTexture;
        
        private List<RenderTexture> _AOSATextures = new List<RenderTexture>();
        private RenderTargetIdentifier[] _AOSARTIs;
        private  int[] _AOSANameIDs =
        {
            ShaderConstants.AOSAShadowTexture,
            ShaderConstants.AOSASpecularTexture,
        };
        private RenderTextureFormat[] _AOSAFormats = 
        {
            RenderTextureFormat.ARGB32,
            RenderTextureFormat.ARGB32,
        };
        
        private BlurPass _blurPass = new BlurPass();
        private BlitPass _blitPass = new BlitPass();
        private FrameBufferOutputDebug? _currentOutputDebug;
        
        public DeferredRP(XRendererPipelineAsset setting):base(setting){
            _deferredLightingCulling = new DeferredTileLightCulling(setting.builtinAssets.deferredLightingCullingCS);
            if(setting.deferredRPSetting.accurateNormals){
                _GBufferFormats[1] = RenderTextureFormat.RG32;
            }else{
                _GBufferFormats[1] = RenderTextureFormat.ARGB32;
            }
        }
        
        private void ConfigShaderPropertiesPerCamera(ScriptableRenderContext context,Camera camera){
            _commandbuffer.Clear();
            CameraUtil.ConfigShaderProperties(_commandbuffer,camera);
            AntiAliasUtil.ConfigShaderPerCamera(_commandbuffer,_setting.antiAliasSetting);
            context.ExecuteCommandBuffer(_commandbuffer);
        }

        private void AcquireARGB32TextureIfNot(ScriptableRenderContext context, Camera camera,
            int textureNameId, ref RenderTexture texture, bool enableRandomWrite = false)
        {
            if (texture)
            {
                if (texture.width != camera.pixelWidth || texture.height != camera.pixelHeight ||
                    texture.enableRandomWrite != enableRandomWrite)
                {
                    ReleaseARGB32Texture(ref texture);
                }
            }
            
            if (texture == null)
            {
                RenderTextureDescriptor descriptor = new RenderTextureDescriptor(camera.pixelWidth,camera.pixelHeight);
                descriptor.depthBufferBits = 0;
                descriptor.sRGB = true;
                descriptor.colorFormat = RenderTextureFormat.ARGB32;
                descriptor.enableRandomWrite = enableRandomWrite;
                texture = RenderTexture.GetTemporary(descriptor);
                texture.Create();
                _commandbuffer.Clear();
                _commandbuffer.SetGlobalTexture(textureNameId, texture);
                context.ExecuteCommandBuffer(_commandbuffer);
            }
            
        }

        private void ReleaseARGB32Texture(ref RenderTexture texture)
        {
            if (texture)
            {
                RenderTexture.ReleaseTemporary(texture);
                texture = null;
            }
        }

        private void AcquireWarpTextureIfNot(ScriptableRenderContext context, Camera camera)
        {
            if (_warpTexture)
            {
                if (_warpTexture.width != camera.pixelWidth || _warpTexture.height != camera.pixelHeight)
                {
                    ReleaseWarpTexture();
                }
            }

            if (_warpTexture == null)
            {
                RenderTextureDescriptor descriptor = new RenderTextureDescriptor(camera.pixelWidth,camera.pixelHeight);
                descriptor.depthBufferBits = 32;
                descriptor.sRGB = false;
                descriptor.colorFormat = RenderTextureFormat.RG16;
                descriptor.enableRandomWrite = false;
                _warpTexture = RenderTexture.GetTemporary(descriptor);
                _warpTexture.Create();
                _commandbuffer.Clear();
                _commandbuffer.SetGlobalTexture(ShaderConstants.ScreenWarpTexture, _warpTexture);
                context.ExecuteCommandBuffer(_commandbuffer);
            }
            
        }

        private void ReleaseWarpTexture()
        {
            if (_warpTexture)
            {
                RenderTexture.ReleaseTemporary(_warpTexture);
                _warpTexture = null;
            }
        }
        
        protected override void ConfigShaderPropertiesPipeline(ScriptableRenderContext context)
        {
            base.ConfigShaderPropertiesPipeline(context);
            var deferredSetting = _setting.deferredRPSetting;

            ChangeDeferredDebugKeyword(context,deferredSetting.outputDebug);
            _commandbuffer.Clear();
            if(deferredSetting.lightShadeByComputeShader){
                _commandbuffer.EnableShaderKeyword(ShaderKeywords.lightShadeByCS);
            }else{
                _commandbuffer.DisableShaderKeyword(ShaderKeywords.lightShadeByCS);
            }
            Utils.SetGlobalShaderKeyword(_commandbuffer,ShaderKeywords.deferredLightCullingDepthSlice,deferredSetting.enableDepthSliceForLightCulling);
            Utils.SetGlobalShaderKeyword(_commandbuffer,ShaderKeywords.deferredLightCullingMode_AABB,deferredSetting.tileLightCullingAlgorithm == DeferredRPSetting.TileLightCullingAlgorithm.AABB);
            Utils.SetGlobalShaderKeyword(_commandbuffer,ShaderKeywords.deferredLightCullingMode_SideFaces,deferredSetting.tileLightCullingAlgorithm == DeferredRPSetting.TileLightCullingAlgorithm.SideFace);
            Utils.SetGlobalShaderKeyword(_commandbuffer,ShaderKeywords.GBufferAccurateNormals,deferredSetting.accurateNormals);
            context.ExecuteCommandBuffer(_commandbuffer);
        }

        

        private void ChangeDeferredDebugKeyword(ScriptableRenderContext context, FrameBufferOutputDebug outputDebug){
            if(_currentOutputDebug == outputDebug){
                return;
            }
            _currentOutputDebug = outputDebug;
            _commandbuffer.Clear();
            if(outputDebug != FrameBufferOutputDebug.Off){
                _commandbuffer.EnableShaderKeyword(ShaderKeywords.deferredDebugOn);
            }else{
                _commandbuffer.DisableShaderKeyword(ShaderKeywords.deferredDebugOn);
            }
            _commandbuffer.SetGlobalInt(ShaderConstants.DeferredDebugMode,(int)outputDebug);
            context.ExecuteCommandBuffer(_commandbuffer);
        }

        protected override void OnPostCameraCulling(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
        {
            base.OnPostCameraCulling(context, camera, ref cullingResults);
            //生成延迟着色需要的灯光数据
            var lightData = _deferredLightConfigurator.Prepare(ref cullingResults);

            //投影Pass
            var casterSetting = new ShadowCasterPass.ShadowCasterSetting();
            casterSetting.cullingResults = cullingResults;
            casterSetting.lightData = lightData;
            casterSetting.shadowSetting = _setting.shadowSetting;
            casterSetting.camera = camera;
            _shadowCastPass.Execute(context,ref casterSetting);

            //重设摄像机参数
            context.SetupCameraProperties(camera);
            ConfigShaderPropertiesPerCamera(context,camera);

            //设置MRT
            var cameraDesc = Utils.GetCameraRenderDescription(camera,_setting);
            this.ConfigMRT(context,ref cameraDesc);
            
            //渲染非透明物体
            _opaquePass.Execute(context,camera,ref cullingResults);
            
            AOSASetting setting = _setting.aosaSetting;

            //进行一个完整的渲染流程得到warpPass
            AcquireWarpTextureIfNot(context,camera);
            _warpPass.Config(_warpTexture);
            _warpPass.Execute(context, ref cullingResults,camera,ref setting);
            
            //光照剔除
            var deferredTileLightCullingParams = new DeferredTileLightCulling.DeferredTileLightCullingParams(){
                cameraRenderDescription = cameraDesc,
                lightShadeByComputeShader = true
            };
            _deferredLightingCulling.Execute(context,ref deferredTileLightCullingParams);
            
            //设置MRT
            _commandbuffer.Clear();
            AcquireAOSATexturesIfNot(context,camera);
            AcquireDepthTextureIfNot(context,camera);
            _commandbuffer.SetRenderTarget(_AOSARTIs,BuiltinRenderTextureType.None);
            _commandbuffer.ClearRenderTarget(true, true, Color.clear);
            context.ExecuteCommandBuffer(_commandbuffer);
            
            _deferredLightingPass.Execute(context);
            
            //进行最后的图像处理阶段
            AcquireARGB32TextureIfNot(context, camera, ShaderConstants.SoftBlurTexture,ref _softBlurTexture);
            AcquireARGB32TextureIfNot(context, camera, ShaderConstants.HeavyBlurTexture,ref _heavyBlurTexture);
            
            _blurPass.Config(_AOSATextures[0],_softBlurTexture,_heavyBlurTexture,ref setting);
            _blurPass.Execute(context,camera);
            
            AcquireARGB32TextureIfNot(context, camera, ShaderConstants.SoftBlurTexture2, ref _softBlurTexture2);
            AcquireARGB32TextureIfNot(context, camera, ShaderConstants.HeavyBlurTexture2, ref _heavyBlurTexture2);
            
            _blurPass.Config(_AOSATextures[1], _softBlurTexture2, _heavyBlurTexture2, ref setting);
            _blurPass.Execute(context,camera);
            
            if (setting.debugmode != 0)
            {
                AOSADebug(context,(int)setting.debugmode);
                return;
            }
            
            AntiAliasSetting  AASetting = _setting.antiAliasSetting;
            bool isFXAAOn = AASetting.isFXAAOn;

            if (!isFXAAOn)
            {
                _finalPass.Execute(BuiltinRenderTextureType.CameraTarget,context, camera, ref setting);
                _lensFlarePass.Execute(context);
            }
            else
            {
                AntiAliasUtil.ConfigShaderPerCamera(_commandbuffer, AASetting);
                AcquireARGB32TextureIfNot(context,camera,ShaderConstants.CameraColorTexture,ref _colorTexture,false);
                _finalPass.Execute(_colorTexture,context, camera, ref setting);
                _lensFlarePass.Execute(context);
                PresentTextureToScreen(context, _colorTexture);
            }
            
            #if UNITY_EDITOR
            if(camera.cameraType == CameraType.SceneView && UnityEditor.Handles.ShouldRenderGizmos()){
                context.DrawGizmos(camera,GizmoSubset.PostImageEffects);
            }
            #endif
            OnCameraRenderingEnd(context);
        }


        private void ReleaseGBuffers(){
            if(_GBuffers.Count > 0){
                foreach(var g in _GBuffers){
                    if(g){
                        RenderTexture.ReleaseTemporary(g);
                    }
                }
                _GBuffers.Clear();
                _GBufferRTIs = null;
            }
        }

        private void AcquireGBuffersIfNot(ScriptableRenderContext context, Camera camera){
            if(_GBuffers.Count > 0){
                var g0 = _GBuffers[0];
                if(g0.width != camera.pixelWidth || g0.height != camera.pixelHeight){
                    this.ReleaseGBuffers();
                }
            }
            if(_GBuffers.Count == 0){
                _commandbuffer.Clear();
                _GBufferRTIs = new RenderTargetIdentifier[4];
                for(var i = 0; i < 4; i ++){
                    RenderTextureDescriptor descriptor = new RenderTextureDescriptor(camera.pixelWidth,camera.pixelHeight,_GBufferFormats[i],0,1);
                    var rt = RenderTexture.GetTemporary(descriptor);
                    rt.filterMode = FilterMode.Bilinear;
                    rt.Create();
                    _GBuffers.Add(rt);
                    _commandbuffer.SetGlobalTexture(_GBufferNameIDs[i],rt);
                    _GBufferRTIs[i] = rt;
                }
                context.ExecuteCommandBuffer(_commandbuffer);
            }
        }

        private void AcquireDepthTextureIfNot(ScriptableRenderContext context, Camera camera){
            if(_depthTexture){
                if(_depthTexture.width != camera.pixelWidth || _depthTexture.height != camera.pixelHeight){
                    this.ReleaseDepthTexture();
                }
            }
            if(!_depthTexture){
                RenderTextureDescriptor depthDesc = new RenderTextureDescriptor(camera.pixelWidth,camera.pixelHeight,RenderTextureFormat.Depth,32,1);
                _depthTexture = RenderTexture.GetTemporary(depthDesc);
                _depthTexture.Create();
                _commandbuffer.Clear();
                _commandbuffer.SetGlobalTexture(ShaderConstants.CameraDepthTexture,_depthTexture);
                context.ExecuteCommandBuffer(_commandbuffer);
            }
        }

        private void ReleaseDepthTexture(){
            if(_depthTexture){
                RenderTexture.ReleaseTemporary(_depthTexture);
                _depthTexture = null;
            }
        }

        private void ReleaseAOSATextures()
        {
            if (_AOSATextures.Count > 0)
            {
                foreach (var aosa in _AOSATextures)
                {
                    if (aosa)
                    {
                        RenderTexture.ReleaseTemporary(aosa);
                    }
                }
                _AOSATextures.Clear();
                _AOSARTIs = null;
            }
        }

        private void AcquireAOSATexturesIfNot(ScriptableRenderContext context, Camera camera)
        {
            if (_AOSATextures.Count > 0)
            {
                var aosa = _AOSATextures[0];
                if (aosa.width != camera.pixelWidth || aosa.height != camera.pixelHeight)
                {
                    this.ReleaseAOSATextures();
                }
            }

            if (_AOSATextures.Count == 0)
            {
                _commandbuffer.Clear();
                _AOSARTIs = new RenderTargetIdentifier[2];
                for (var i = 0; i < 2; i++)
                {
                    RenderTextureDescriptor descriptor = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight, _AOSAFormats[i], 0, 1);
                    descriptor.sRGB =  true;
                    var rt = RenderTexture.GetTemporary(descriptor);
                    rt.filterMode = FilterMode.Bilinear;
                    rt.Create();
                    _AOSATextures.Add(rt);
                    _commandbuffer.SetGlobalTexture(_AOSANameIDs[i], rt);
                    _AOSARTIs[i] = rt;
                }
                context.ExecuteCommandBuffer(_commandbuffer);
            }
        }
        
        private void ConfigMRT(ScriptableRenderContext context,ref CameraRenderDescription cameraRenderDescription){
            this.AcquireGBuffersIfNot(context,cameraRenderDescription.camera);
            this.AcquireDepthTextureIfNot(context,cameraRenderDescription.camera);
            _commandbuffer.Clear();
            _commandbuffer.SetRenderTarget(_GBufferRTIs,_depthTexture);
            _commandbuffer.ClearRenderTarget(true,true,cameraRenderDescription.camera.backgroundColor);
            context.ExecuteCommandBuffer(_commandbuffer);
        }

        private void OnCameraRenderingEnd(ScriptableRenderContext context){
            
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _deferredLightConfigurator.Dispose();
            _deferredLightingCulling.Dispose();
            this.ReleaseGBuffers();
            this.ReleaseDepthTexture();
            this.ReleaseAOSATextures();
            this.ReleaseARGB32Texture(ref _softBlurTexture);
            this.ReleaseARGB32Texture(ref _heavyBlurTexture);
            this.ReleaseWarpTexture();
        }


        public static class ShaderConstants
        {
            public static readonly int GBuffer0 = Shader.PropertyToID("_GBuffer0");
            public static readonly int GBuffer1 = Shader.PropertyToID("_GBuffer1");
            public static readonly int GBuffer2 = Shader.PropertyToID("_GBuffer2");
            public static readonly int GBuffer3 = Shader.PropertyToID("_GBuffer3");

            public static readonly int CameraColorTexture = Shader.PropertyToID("_CameraColorTexture");
            public static readonly int AOSAShadowTexture = Shader.PropertyToID("_AOSAShadowTexture");
            public static readonly int AOSASpecularTexture = Shader.PropertyToID("_AOSASpecularTexture");
            public static readonly int SoftBlurTexture = Shader.PropertyToID("_SoftBlurTexture");
            public static readonly int HeavyBlurTexture = Shader.PropertyToID("_HeavyBlurTexture");
            public static readonly int SoftBlurTexture2 = Shader.PropertyToID("_SoftBlurTexture2");
            public static readonly int HeavyBlurTexture2 =  Shader.PropertyToID("_HeavyBlurTexture2");
            public static readonly int ScreenWarpTexture = Shader.PropertyToID("_ScreenWarpTexture");
            
            public static readonly int CameraDepthTexture = Shader.PropertyToID("_XDepthTexture");
            public static readonly int DeferredDebugMode = Shader.PropertyToID("_DeferredDebugMode");

            public static readonly int TileCullingIntersectAlgroThreshold = Shader.PropertyToID("_TileCullingIntersectAlgroThreshold");
        }

        public static bool support{
            get{
                return SystemInfo.supportedRenderTargetCount >= 4;
            }
        }

        private void PresentTextureToScreen(ScriptableRenderContext context, RenderTexture renderTexture)
        {
            _blitPass.Config(renderTexture,BuiltinRenderTextureType.CameraTarget);
            _blitPass.Execute(context);
        }
        
        public static class ShaderKeywords
        {

            public const string lightShadeByCS = "DEFERRED_LIGHTSHADE_BY_CS";
            public const string deferredDebugOn = "DEFERRED_BUFFER_DEBUGON";
            public const string deferredLightCullingDepthSlice = "DEFERRED_LIGHT_CULLING_DEPTH_SLICE";
            public const string deferredLightCullingMode_SideFaces = "DEFERRED_LIGHT_CULLING_SIDES";
            public const string deferredLightCullingMode_AABB = "DEFERRED_LIGHT_CULLING_AABB";
            public const string GBufferAccurateNormals = "GBUFFER_ACCURATE_NORMAL";
            
        }

        private void AOSADebug(ScriptableRenderContext context, int debugMode)
        {
            switch (debugMode)
            {
                case 1:
                    PresentTextureToScreen(context, _GBuffers[0]); 
                    break;
                case 2:
                    PresentTextureToScreen(context, _GBuffers[1]);
                    break;
                case 3:
                    PresentTextureToScreen(context, _GBuffers[2]);
                    break;
                case 4: 
                    PresentTextureToScreen(context, _GBuffers[3]);
                    break;
                case 5:
                    PresentTextureToScreen(context, _AOSATextures[0]);
                    break;
                case 6:
                    PresentTextureToScreen(context, _softBlurTexture);
                    break;
                case 7:
                    PresentTextureToScreen(context, _heavyBlurTexture);
                    break;
                case 8:
                    PresentTextureToScreen(context, _AOSATextures[1]);
                    break;
                case 9:
                    PresentTextureToScreen(context, _softBlurTexture2);
                    break;
                case 10: 
                    PresentTextureToScreen(context, _heavyBlurTexture2);
                    break;
                case 11:
                    PresentTextureToScreen(context, _warpTexture);
                    break;
                case 12:
                    PresentTextureToScreen(context, _depthTexture);
                    break;
            }
        }
    }
}
