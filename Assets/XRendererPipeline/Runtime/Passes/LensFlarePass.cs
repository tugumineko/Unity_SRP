using CustomLensFlare;
using UnityEngine;
using UnityEngine.Rendering;

namespace SRPLearn
{
    public class LensFlarePass
    {
        private CommandBuffer _commandbuffer;
        
        public LensFlarePass()
        {
            _commandbuffer = new CommandBuffer()
            {
                name = "LensFlarePass",
            };
        }
        
        public void Execute(ScriptableRenderContext context)
        {
            _commandbuffer.Clear();
            foreach (var lensFlare in CustomLensFlareManager.Instance.LensFlares)
            {
                if (lensFlare != null)
                {
                    Matrix4x4 m = Matrix4x4.identity;
                    if (!lensFlare.IsDirectional)
                    {
                        m = lensFlare.transform.localToWorldMatrix;
                    }

                    if (lensFlare.UsedMesh && lensFlare.UsedMaterial)
                    {
                        _commandbuffer.DrawMesh(lensFlare.UsedMesh, m, lensFlare.UsedMaterial, 0, lensFlare.IsDirectional ? 1 : 0);
                    }
                }
            }    
            context.ExecuteCommandBuffer(_commandbuffer);
        }
        
    }
}