namespace Core.Models.Business
{
    /// <summary>One hit, in coordinates relative to the searched area.</summary>
    public sealed class TemplateMatchResult
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public float Score { get; set; }

        /// <summary>What the template was resized by to match. 1 = not resized.</summary>
        public float Scale { get; set; }
    }
}
