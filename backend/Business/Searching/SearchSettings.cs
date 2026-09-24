using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Searching
{
    /// <summary>
    /// What the step says about the search, for every template in it: the mode and how many
    /// matches. The accuracy is each template's own.
    /// </summary>
    public sealed record SearchSettings
    {
        public TemplateMatchModeEnum Mode { get; init; }
        public SearchModeEnum SearchMode { get; init; }
        public int MaxMatches { get; init; }

        public static SearchSettings From(FlowStep step)
        {
            return new SearchSettings
            {
                Mode = step.TemplateMatchMode,
                SearchMode = step.SearchMode,
                MaxMatches = step.MaxMatches,
            };
        }

        public static SearchSettings From(FlowStepDto step)
        {
            return new SearchSettings
            {
                Mode = step.TemplateMatchMode,
                SearchMode = step.SearchMode,
                MaxMatches = step.MaxMatches,
            };
        }
    }
}
