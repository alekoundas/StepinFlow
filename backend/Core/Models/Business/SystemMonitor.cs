namespace Core.Models.Business
{
    public class SystemMonitor
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;        // e.g. "\\\\.\\DISPLAY1"
        public bool IsPrimary { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int WorkingWidth { get; set; }  
        public int WorkingHeight { get; set; }
    }
}
