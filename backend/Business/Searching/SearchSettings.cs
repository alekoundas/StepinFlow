using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Searching
{
    /// <summary>
    /// What the step says about the search, as opposed to what each template says. A template may
    /// override the mode and the accuracy; nothing overrides the search mode.
    /// </summary>
    public sealed record SearchSettings
    {
        public TemplateMatchModeEnum Mode { get; init; }
        public float Accuracy { get; init; }
        public SearchModeEnum SearchMode { get; init; }
        public int MaxMatches { get; init; }

        public static SearchSettings From(FlowStep step)
        {
            return new SearchSettings
            {
                Mode = step.TemplateMatchMode,
                Accuracy = step.Accuracy,
                SearchMode = step.SearchMode,
                MaxMatches = step.MaxMatches,
            };
        }

        public static SearchSettings From(FlowStepDto step)
        {
            return new SearchSettings
            {
                Mode = step.TemplateMatchMode,
                Accuracy = step.Accuracy,
                SearchMode = step.SearchMode,
                MaxMatches = step.MaxMatches,
            };
        }
    }
}
