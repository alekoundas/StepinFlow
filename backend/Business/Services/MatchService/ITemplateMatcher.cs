using Core.Models.Business;

namespace Business.Services.MatchService
{
    public interface ITemplateMatcher
    {
        /// <summary>
        /// Every match at or above the threshold, best first. Empty when nothing matches.
        /// </summary>
        IReadOnlyList<TemplateMatch> Match(TemplateMatchRequest request);
    }
}
