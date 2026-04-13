using System.Drawing;

namespace Core.Models.Business
{
    public class MonitorInfo
    {
        public string UniqueId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string FriendlyName { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }

        //public int X { get; set; }
        //public int Y { get; set; }
        //public int Width { get; set; }
        //public int Height { get; set; }
        //public int WorkingWidth { get; set; }  
        //public int WorkingHeight { get; set; }


        public Rectangle Bounds { get; set; }
    }
}
