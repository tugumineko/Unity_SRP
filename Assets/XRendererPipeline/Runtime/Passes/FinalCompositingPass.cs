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

        private RenderTargetIdentifier[] _mrt;

        public FinalCompositingPass()
        {
            _commandBuffer = new CommandBuffer();
            _commandBuffer.name = "FinalCompositingPass";

            _mrt = new RenderTargetIdentifier[2];
        }

        public void Execute(RenderTargetIdentifier backbufferTarget, ScriptableRenderContext context, Camera camera, ref AOSASetting setting)
        {
            if (!_finalMaterial)
                _finalMaterial = new Material(Shader.Find("Hidden/SRPLearn/FinalCompositingPass"));

            if (!_fullscreenMesh)
                _fullscreenMesh = Utils.CreateFullscreenMesh();

            int width = camera.pixelWidth;
            int height = camera.pixelHeight;

            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 0)
            {
                sRGB = true
            };

            _commandBuffer.Clear();

            // Allocate MRT targets
            _commandBuffer.GetTemporaryRT(ShaderProperties.CompositingColorTex, descriptor);
            _commandBuffer.GetTemporaryRT(ShaderProperties.CompositingBloomTex, descriptor);

            _mrt[0] = new RenderTargetIdentifier(ShaderProperties.CompositingColorTex);
            _mrt[1] = new RenderTargetIdentifier(ShaderProperties.CompositingBloomTex);

            // Set MRT with dummy depth
            _commandBuffer.SetRenderTarget(_mrt, BuiltinRenderTextureType.None);
            _commandBuffer.ClearRenderTarget(false, true, Color.clear);

            // Setup global parameters
            Vector4 finalShadowParams = new Vector4(setting.shadowStepCount, setting.shadowThreshold,
                setting.shadowThresholdSoftness, setting.shadowInnerGlow);

            _commandBuffer.SetGlobalVector(ShaderProperties.FinalShadowParams, finalShadowParams);
            _commandBuffer.SetGlobalFloat(ShaderProperties.WarpBloom, setting.useWarpBloom ? 1.0f : 0.0f);
            _commandBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

            // First pass: render fullscreen quad to MRTs
            _commandBuffer.DrawMesh(_fullscreenMesh, Matrix4x4.identity, _finalMaterial, 0, 0);

            // Second pass: composite MRTs into screen
            _commandBuffer.SetGlobalTexture(ShaderProperties.CompositingColorTex, ShaderProperties.CompositingColorTex);
            _commandBuffer.SetGlobalTexture(ShaderProperties.CompositingBloomTex, ShaderProperties.CompositingBloomTex);

            _commandBuffer.SetRenderTarget(backbufferTarget);
            _commandBuffer.DrawMesh(_fullscreenMesh, Matrix4x4.identity, _finalMaterial, 0, 1);

            // Release temporary MRTs
            _commandBuffer.ReleaseTemporaryRT(ShaderProperties.CompositingColorTex);
            _commandBuffer.ReleaseTemporaryRT(ShaderProperties.CompositingBloomTex);

            context.ExecuteCommandBuffer(_commandBuffer);
        }

        public static class ShaderProperties
        {
            public static readonly int FinalShadowParams = Shader.PropertyToID("_FinalShadowParams");
            public static readonly int WarpBloom = Shader.PropertyToID("_WarpBloom");

            public static readonly int CompositingColorTex = Shader.PropertyToID("_CompositingColorTex");
            public static readonly int CompositingBloomTex = Shader.PropertyToID("_CompositingBloomTex");
        }
    }
}
