using Rocket.API;

namespace FQPlayerDragging
{
    public class Config : IRocketPluginConfiguration
    {
        public float dragDistance { get; set; }
        public float dragStartDistance { get; set; }
        public string serverIcon { get; set; }
        public string dragPermission { get; set; }

        public void LoadDefaults()
        {
            dragDistance = 5f;
            dragStartDistance = 7f;
            serverIcon = "https://unturnedstore.com/api/images/1220";
            dragPermission = "fq.dragging";

        }
    }
}