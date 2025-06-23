using System.Collections.Generic;

namespace CustomLensFlare
{
    public class CustomLensFlareManager
    {
        private static CustomLensFlareManager _instance;
        public static CustomLensFlareManager Instance => _instance ??= new CustomLensFlareManager();
        
        private List<CustomLensFlare> _list = new List<CustomLensFlare>();
        public List<CustomLensFlare> LensFlares => _list;

        public void AddCustomLensFlare(CustomLensFlare flare)
        {
            if (_list.Contains(flare))
                return;
            _list.Add(flare);
        }

        public void RemoveCustomLensFlare(CustomLensFlare flare)
        {
            _list.Remove(flare);
        }
    }
}