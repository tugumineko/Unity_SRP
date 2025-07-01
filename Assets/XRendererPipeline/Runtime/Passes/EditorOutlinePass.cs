using System.Linq;
using EditorOutline;
using UnityEngine;
using UnityEngine.Rendering;

namespace SRPLearn
{
    public class EditorOutlinePass
    {
        private readonly Material _outlineMaterial;
        private CommandBuffer _commandBuffer;
        
        public EditorOutlinePass()
        {
            _outlineMaterial = new Material(Shader.Find("Hidden/SRPLearn/EditorOutline"));
            _commandBuffer = new CommandBuffer();
        }

        public void Execute(ScriptableRenderContext context)
        {
            var outlines = EditorOutlineManager.Instance.Outlines;
            if (outlines.Count <= 0) return;
            _commandBuffer.Clear();
            foreach (var outline in outlines)
            {
                if (outline == null || !outline.isActiveAndEnabled) continue;

#if UNITY_EDITOR
                if (!UnityEditor.Selection.Contains(outline.gameObject))
                    continue;
#endif

                
                var renderer = outline.GetComponent<Renderer>();
                if (renderer == null) continue;

                _outlineMaterial.SetColor("_OutlineColor", outline.OutlineColor);
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    _commandBuffer.DrawRenderer(renderer, _outlineMaterial, i);
                }
            }

            context.ExecuteCommandBuffer(_commandBuffer);
        }
    }
}