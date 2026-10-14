namespace Core.Models.Dtos
{
    /// <summary>A stopped recording as the tester reviewed it: what the draft is built from.</summary>
    public class ReviewedRecordingDto
    {
        /// <summary>One per action Review listed, in order. A pause is not listed.</summary>
        public List<ReviewedActionDto> Actions { get; set; } = [];
    }
}
