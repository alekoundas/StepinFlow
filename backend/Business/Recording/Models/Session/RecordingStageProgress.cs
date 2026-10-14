using Core.Enums;

namespace Business.Recording.Models.Session
{
    /// <summary>
    /// How far a stopped recording has got through one stage. Replaced whole on every change, so
    /// the progress sent never mixes two of them.
    /// </summary>
    public sealed class RecordingStageProgress
    {
        public RecordingStageEnum Stage { get; init; }

        /// <summary>Which stage, from 1, of Count.</summary>
        public int Number { get; init; }
        public int Count { get; init; }

        /// <summary>How far it has got, of Total. Total is null when it cannot be counted ahead.</summary>
        public int Done { get; init; }
        public int? Total { get; init; }
    }
}
