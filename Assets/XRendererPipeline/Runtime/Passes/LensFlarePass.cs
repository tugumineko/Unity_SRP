using CustomLensFlare;
using UnityEngine;
using UnityEngine.Rendering;

namespace SRPLearn
{
    public class LensFlarePass
    {
        private CommandBuffer _commandBuffer;
        private Material _occlusionMaterial;

        // Reusable 1x1 render texture
        private RenderTexture _occlusionRT;

        private static readonly int OcclusionRT_ID = Shader.PropertyToID("_OcclusionRT");

        public LensFlarePass()
        {
            _commandBuffer = new CommandBuffer() { name = "LensFlarePass" };

            Shader occlusionShader = Shader.Find("Hidden/SRPLearn/LensFlareOcclusion");
            _occlusionMaterial = new Material(occlusionShader);

            _occlusionRT = new RenderTexture(1, 1, 0, RenderTextureFormat.RHalf);
            _occlusionRT.name = "_OcclusionRT";
            _occlusionRT.Create();
        }

        public void Execute(ScriptableRenderContext context)
        {
            _commandBuffer.Clear();

            foreach (var lensFlare in CustomLensFlareManager.Instance.LensFlares)
            {
                if (lensFlare == null || !lensFlare.UsedMesh || !lensFlare.UsedMaterial)
                    continue;

                Matrix4x4 matrix = lensFlare.IsDirectional
                    ? Matrix4x4.identity
                    : lensFlare.transform.localToWorldMatrix;
                int shaderPass = lensFlare.IsDirectional ? 1 : 0;

                // ========== Step 1: Render Occlusion ==========
                _commandBuffer.SetRenderTarget(_occlusionRT);
                _occlusionMaterial.SetFloat(Shader.PropertyToID("_OcclusionRadius"),lensFlare.OcclusionRadius);
                _commandBuffer.DrawProcedural(matrix, _occlusionMaterial, shaderPass, MeshTopology.Triangles, 3);

                // ========== Step 2: Render Flare ==========
                _commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                lensFlare.UsedMaterial.SetTexture(OcclusionRT_ID, _occlusionRT);
                _commandBuffer.DrawMesh(lensFlare.UsedMesh, matrix, lensFlare.UsedMaterial, 0, shaderPass);
            }
            Cleanup();
            context.ExecuteCommandBuffer(_commandBuffer);
        }

        public void Cleanup()
        {
            _occlusionRT?.Release();
        }
    }
}
