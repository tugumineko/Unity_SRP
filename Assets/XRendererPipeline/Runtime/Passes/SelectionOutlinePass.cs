#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

namespace SRPLearn
{
    public class SelectionOutlinePass
    {
        private readonly Material _outlineMaterial;
        private readonly CommandBuffer _commandBuffer;

        public SelectionOutlinePass()
        {
            _outlineMaterial = new Material(Shader.Find("Hidden/SRPLearn/EditorOutline"));
            _commandBuffer = new CommandBuffer { name = "Selection Outline Pass" };
        }

        public void Execute(ScriptableRenderContext context)
        {
#if UNITY_EDITOR
            var selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length == 0)
                return;

            _commandBuffer.Clear();

            foreach (var go in selectedObjects)
            {
                if (go == null || !go.activeInHierarchy)
                    continue;

                var renderer = go.GetComponent<Renderer>();
                if (renderer == null)
                    continue;

                _outlineMaterial.SetColor("_OutlineColor", Color.cyan);

                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    _commandBuffer.DrawRenderer(renderer, _outlineMaterial, i);
                }
            }

            context.ExecuteCommandBuffer(_commandBuffer);
#endif
        }
    }
}