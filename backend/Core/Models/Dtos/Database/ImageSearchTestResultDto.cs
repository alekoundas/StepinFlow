namespace Core.Models.Dtos
{
    /// <summary>
    /// What an image step finds on this machine right now. Drives the Test button, and is the
    /// same shape dry-run will report per step later.
    /// </summary>
    public class ImageSearchTestResultDto
    {
        public bool IsResolved { get; set; }
        public string? ErrorMessage { get; set; }

        public int SearchAreaX { get; set; }
        public int SearchAreaY { get; set; }
        public int SearchAreaWidth { get; set; }
        public int SearchAreaHeight { get; set; }

        public int TotalMatches { get; set; }
        public bool WouldSucceed { get; set; }

        public List<ImageSearchTestImageDto> Images { get; set; } = new List<ImageSearchTestImageDto>();
    }

    public class ImageSearchTestImageDto
    {
        public int FlowStepImageId { get; set; }
        public string Name { get; set; } = string.Empty;

        public bool IsFound { get; set; }
        public bool IsRequired { get; set; }
        public int MatchCount { get; set; }

        public float BestScore { get; set; }

        /// <summary>Click point of the best match, offset applied, in physical pixels.</summary>
        public int BestX { get; set; }
        public int BestY { get; set; }

        /// <summary>Scale the template had to be resized by. 1 = the frame matches how it was captured.</summary>
        public float Scale { get; set; }
    }
}
