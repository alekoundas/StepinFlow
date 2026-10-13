using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>
    /// Where a recording is, for the recording bar and the recording page. Sent whole on every
    /// change, and read when a page opens, so a page that opens late misses nothing.
    /// </summary>
    public class RecordingProgressDto
    {
        public RecordingStateEnum State { get; set; }

        /// <summary>Since the recording started, pauses included, as of this message.</summary>
        public int ElapsedMilliseconds { get; set; }

        /// <summary>What the tester did so far. Pauses are not counted.</summary>
        public int ActionCount { get; set; }

        /// <summary>The newest actions, oldest first.</summary>
        public List<RecordingLiveActionDto> RecentActions { get; set; } = [];
    }
}
