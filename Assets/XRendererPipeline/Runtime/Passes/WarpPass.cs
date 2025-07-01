
using UnityEngine;
using UnityEngine.Rendering;

namespace SRPLearn
{
    public class WarpPass
    {
        private RenderTargetIdentifier _target;
        private CommandBuffer _commandBuffer;

        public WarpPass()
        {
            _commandBuffer = new CommandBuffer();
            _commandBuffer.name = "WarpPass";
        }

        public void Config(RenderTargetIdentifier target)
        {
            _target = target;
        }
        
        public void Execute(ScriptableRenderContext context,ref CullingResults cullingResults,Camera camera, ref AOSASetting setting)
        {
            ConfigWarpPassSetting(context, ref setting);
            
            _commandBuffer.Clear();
            _commandBuffer.SetRenderTarget(_target);
            _commandBuffer.ClearRenderTarget(true,true,new Color(0.5f,0.5f,0,1));
            context.ExecuteCommandBuffer(_commandBuffer);
            
            _commandBuffer.Clear();
            var drawingSettings = new DrawingSettings(new ShaderTagId("Warp"), new SortingSettings(camera));
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
            context.ExecuteCommandBuffer(_commandBuffer);
        }
        
        private void SetWarpPassParams(CommandBuffer commandBuffer, ref AOSASetting setting)
        {
            float t = (float)(Time.realtimeSinceStartupAsDouble % 3600.0);
            Vector4 lineBoilTime = new Vector4(0,//Off
                                            t,//Realtime
                                            Mathf.Floor(t * 8)/8,//8fps
                                            Mathf.Floor(t * 4)/4);//4fps
            commandBuffer.SetGlobalVector(ShaderProperties.LineBoilTime,lineBoilTime);
            commandBuffer.SetGlobalTexture(ShaderProperties.WarpTexture,setting.warpTexture);
            commandBuffer.SetGlobalVector(ShaderProperties.WarpParams,new Vector4(setting.warpWidth,setting.warpGlobalScale,setting.warpGlobalDistanceFade,(int)setting.animatedLineBoilFramerate));
        }

        private void SetWarpPassKeywords(CommandBuffer commandBuffer, ref AOSASetting setting)
        {
            Utils.SetGlobalShaderKeyword(commandBuffer,ShaderKeywords.UseSmoothUVGradient,setting.useSmoothUVGradient);
            Utils.SetGlobalShaderKeyword(commandBuffer,ShaderKeywords.CompensateDistance,setting.compensateDistance);
            Utils.SetGlobalShaderKeyword(commandBuffer,ShaderKeywords.CompensateSkew,setting.compensateSkew);
            Utils.SetGlobalShaderKeyword(commandBuffer,ShaderKeywords.CompensateRadialAngle,setting.compensateRadialAngle);
            //Utils.SetGlobalShaderKeyword(commandBuffer,ShaderKeywords.UseAnimatedLineBoil,setting.useAnimatedLineBoil);
            Utils.SetGlobalShaderKeyword(commandBuffer,ShaderKeywords.ReorientNone,setting.orientation == OrientType.None);
            Utils.SetGlobalShaderKeyword(commandBuffer,ShaderKeywords.ReorientContour,setting.orientation == OrientType.Contour);
            Utils.SetGlobalShaderKeyword(commandBuffer,ShaderKeywords.ReorientAll,setting.orientation == OrientType.All);
        }
        
        private void ConfigWarpPassSetting(ScriptableRenderContext context, ref AOSASetting setting)
        {
            _commandBuffer.Clear();
            SetWarpPassParams(_commandBuffer, ref setting);
            SetWarpPassKeywords(_commandBuffer, ref setting);
            context.ExecuteCommandBuffer(_commandBuffer);
        }
        
        public static class ShaderProperties{
            public static readonly int WarpTexture = Shader.PropertyToID("_WarpTexture");
            public static readonly int LineBoilTime = Shader.PropertyToID("_LineBoilTime");
            
            //(WarpWidth,WarpTextureScale,WarpGlobalDistanceFade,AnimatedLineBoilFramerate)
            public static readonly int WarpParams = Shader.PropertyToID("_WarpParams");
            
        }

        public static class ShaderKeywords
        {
            public const string UseSmoothUVGradient = "_USE_SMOOTH_UV_GRADIENT";
            public const string CompensateRadialAngle = "_COMPENSATE_RADIAL_ANGLE";
            public const string CompensateSkew =  "_COMPENSATE_SKEW";
            public const string CompensateDistance = "_COMPENSATE_DISTANCE";
            //public const string UseAnimatedLineBoil =  "_USE_ANIMATED_LINE_BOIL";
            public const string ReorientNone =   "_REORIENT_NONE";
            public const string ReorientContour =   "_REORIENT_CONTOUR";
            public const string ReorientAll =    "_REORIENT_ALL";
            
        }
        
    }
    
    
    
}
