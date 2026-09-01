using Core.Models.Business;

namespace Business.Services.MatchService
{
    public interface IOpenCvService
    {
        /// <summary>
        /// Every match at or above the threshold, best first. Empty when nothing matches.
        /// </summary>
        IReadOnlyList<TemplateMatchResult> Match(TemplateMatchRequest request);
    }
}
