using UnityEngine;

namespace EditorOutline
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    [ExecuteInEditMode]
    public class EditorOutline : MonoBehaviour
    {
        public Color OutlineColor = Color.red;

        private void OnEnable()
        {
            EditorOutlineManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            EditorOutlineManager.Instance.Unregister(this);
        }
    }
}