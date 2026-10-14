using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>
    /// Where a recording is, from the first click to the review being ready, for the recording bar
    /// and the recording page. Sent whole on every change.
    /// </summary>
    public class RecordingProgressDto
    {
        public RecordingStateEnum State { get; set; }

        /// <summary>Since the recording started, pauses included, up to the stop.</summary>
        public int ElapsedMilliseconds { get; set; }

        /// <summary>What the tester did so far. Pauses are not counted.</summary>
        public int ActionCount { get; set; }

        /// <summary>The newest actions, oldest first.</summary>
        public List<RecordingLiveActionDto> RecentActions { get; set; } = [];

        /// <summary>What a stopped recording is being worked through. NONE unless PROCESSING.</summary>
        public RecordingStageEnum Stage { get; set; }

        /// <summary>Which stage, from 1, of StageCount. Both 0 unless PROCESSING.</summary>
        public int StageNumber { get; set; }
        public int StageCount { get; set; }

        /// <summary>How far the stage has got, of StageTotal. StageTotal is null when it cannot be counted ahead.</summary>
        public int StageDone { get; set; }
        public int? StageTotal { get; set; }

        /// <summary>Set when a stage failed. The recording is still READY, with less in it.</summary>
        public string? ErrorMessage { get; set; }
    }
}
