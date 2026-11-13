using Core.Models.Database;

namespace Business.Recording.Draft.Models
{
    /// <summary>
    /// One entry of a recording's draft, in the order it was done: a step with everything it says,
    /// and nothing yet of where it sits in the flow. The layout gives it its place, and can give it
    /// another once a stage or a check has been added among the entries.
    /// </summary>
    public sealed class DraftItem
    {
        public DraftItemKindEnum Kind { get; init; }

        /// <summary>The step itself: the search, the stage marker, the Find All Images, the read, or the step.</summary>
        public FlowStep Step { get; init; } = new FlowStep();

        /// <summary>The recorded action it came from, by its index. -1 for one no action made: a check a model proposed, a QA test's pass.</summary>
        public int ActionIndex { get; init; } = -1;

        /// <summary>For a search: the End Execution its Failure branch ends with.</summary>
        public FlowStep? End { get; init; }

        /// <summary>For a search: what its Failure branch does before it ends, in order.</summary>
        public List<FlowStep> BeforeEnd { get; } = [];

        /// <summary>
        /// Its Success branch, in order: for a Find All Images what is done at each hit, for a read
        /// what is done with what it read.
        /// </summary>
        public List<FlowStep> InSuccess { get; } = [];

        /// <summary>For a search: the page its press was on. Null when the screens were not read.</summary>
        public int? Page { get; init; }

        /// <summary>For a search: a check, the tester's or a model's, rather than a click's wait.</summary>
        public bool IsCheck { get; init; }

        /// <summary>For a search: a check a model proposed, which sits right after what it proves.</summary>
        public bool IsProposed { get; init; }

        /// <summary>For a stage: one the tester named, which a model may not touch.</summary>
        public bool IsTesters { get; init; }
    }
}
