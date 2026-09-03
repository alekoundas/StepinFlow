using Core.Models.Business;

namespace Business.Services.MatchService
{
    public interface IOpenCvService
    {
        /// <summary>
        /// Every match at or above the threshold, best first, and how close the frame came when
        /// none of them cleared it.
        /// </summary>
        TemplateMatchOutcome Match(TemplateMatchRequest request);
    }
}
