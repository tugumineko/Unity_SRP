using System.Collections.Generic;

namespace EditorOutline
{
    public class EditorOutlineManager
    {
        private static EditorOutlineManager _instance;
        public static EditorOutlineManager Instance => _instance ??= new EditorOutlineManager();

        private readonly List<EditorOutline> _outlines = new List<EditorOutline>();
        public IReadOnlyList<EditorOutline> Outlines => _outlines;

        public void Register(EditorOutline outline)
        {
            if (!_outlines.Contains(outline))
                _outlines.Add(outline);
        }

        public void Unregister(EditorOutline outline)
        {
            _outlines.Remove(outline);
        }
    }
}