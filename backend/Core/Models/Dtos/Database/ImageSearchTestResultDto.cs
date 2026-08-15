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

        /// <summary>
        /// The exact pixels the matches were found in, so the details view can draw the boxes over
        /// the frame they belong to. JPEG on purpose: this one is for looking at, not matching,
        /// and a monitor sized area as PNG would be megabytes over the pipe.
        /// </summary>
        public byte[]? Screenshot { get; set; }

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

        /// <summary>Scale the template had to be resized by. 1 = the area matches how it was captured.</summary>
        public float Scale { get; set; }

        /// <summary>Every hit, capped by MaxMatches. Empty when nothing matched.</summary>
        public List<ImageSearchTestMatchDto> Matches { get; set; } = new List<ImageSearchTestMatchDto>();
    }

    /// <summary>
    /// One hit, in coordinates relative to the search area. The screenshot is that same area, so
    /// these need no conversion before being drawn on it.
    /// </summary>
    public class ImageSearchTestMatchDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public float Score { get; set; }
        public float Scale { get; set; }

        /// <summary>Where the cursor would actually land: the template's click offset, scaled.</summary>
        public int ClickX { get; set; }
        public int ClickY { get; set; }
    }
}
