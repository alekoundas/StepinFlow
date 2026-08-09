namespace Core.Models.Business
{
    /// <summary>
    /// Uncompressed BGRA pixels straight from a capture. The match path never encodes: JPEG is
    /// both slower and lossy, and the artifacts wreck normalized template matching.
    /// </summary>
    public sealed class RawImage
    {
        public byte[] Pixels { get; set; } = [];
        public int Width { get; set; }
        public int Height { get; set; }

        /// <summary>Bytes per row, which is not always Width * 4.</summary>
        public int Stride { get; set; }

        public bool IsEmpty => Width <= 0 || Height <= 0 || Pixels.Length == 0;
    }
}
