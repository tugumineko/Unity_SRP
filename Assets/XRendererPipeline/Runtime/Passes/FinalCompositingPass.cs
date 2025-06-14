using UnityEngine;
using UnityEngine.Rendering;

namespace SRPLearn
{
    public class FinalCompositingPass
    {
        private CommandBuffer _commandBuffer;

        [System.NonSerialized]
        private Material _finalMaterial;

        private Mesh _fullscreenMesh;

        private static readonly int TempRTId = Shader.PropertyToID("_TempFinalRT");

        public FinalCompositingPass()
        {
            _commandBuffer = new CommandBuffer();
            _commandBuffer.name = "FinalCompositingPass";
        }

        public void Execute(ScriptableRenderContext context, Camera camera, ref AOSASetting setting)
        {
            if (!_finalMaterial)
                _finalMaterial = new Material(Shader.Find("Hidden/SRPLearn/FinalCompositingPass"));

            if (!_fullscreenMesh)
                _fullscreenMesh = Utils.CreateFullscreenMesh();

            int width = Screen.width;
            int height = Screen.height;
            var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 0)
            {
                sRGB = true
            };

            _commandBuffer.Clear();

            // Allocate temporary RT for first pass output
            _commandBuffer.GetTemporaryRT(TempRTId, descriptor);

            _commandBuffer.SetRenderTarget(TempRTId);
            _commandBuffer.ClearRenderTarget(true, true, Color.cyan);
            context.ExecuteCommandBuffer(_commandBuffer);
            _commandBuffer.Clear();
            
            // Set shadow parameters
            Vector4 finalShadowParams = new Vector4(setting.shadowStepCount, setting.shadowThreshold,
                setting.shadowThresholdSoftness, setting.shadowInnerGlow);
            _commandBuffer.SetGlobalVector(ShaderProperties.FinalShadowParams, finalShadowParams);
            _commandBuffer.SetGlobalFloat(ShaderProperties.WarpBloom, setting.useWarpBloom ? 1.0f : 0.0f);
            _commandBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

            // First Pass - draw to TempRT
            _commandBuffer.SetRenderTarget(TempRTId);
            _commandBuffer.DrawMesh(_fullscreenMesh, Matrix4x4.identity, _finalMaterial, 0, 0);

            // Second Pass - draw to screen using TempRT
            _commandBuffer.SetGlobalTexture(ShaderProperties.IntermediateTex, TempRTId);
            _commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            _commandBuffer.DrawMesh(_fullscreenMesh, Matrix4x4.identity, _finalMaterial, 0, 1);

            // Release temporary RT
            _commandBuffer.ReleaseTemporaryRT(TempRTId);

            context.ExecuteCommandBuffer(_commandBuffer);
        }

        public static class ShaderProperties
        {
            public static readonly int FinalShadowParams = Shader.PropertyToID("_FinalShadowParams");
            public static readonly int WarpBloom = Shader.PropertyToID("_WarpBloom");
            public static readonly int IntermediateTex = Shader.PropertyToID("_IntermediateTex");
        }
    }
}
