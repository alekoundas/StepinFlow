namespace Core.Models.Business.OpenCV
{
    /// <summary>A clicked element as a picture, and the screen just around it, both encoded.</summary>
    public sealed class ElementPicture
    {
        /// <summary>The element alone: its rectangle, cut from the screenshot.</summary>
        public byte[] Element { get; init; } = [];

        /// <summary>The same rectangle widened by a few pixels, from the same screenshot.</summary>
        public byte[] Around { get; init; } = [];
    }
}
