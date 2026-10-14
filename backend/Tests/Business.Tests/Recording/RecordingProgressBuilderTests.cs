using Business.Recording.Models.Session;
using Business.Recording.Session;
using Core.Enums;
using Core.Models.Dtos;

namespace Business.Tests.Recording
{
    public sealed class RecordingProgressBuilderTests
    {
        private static readonly DateTime START = new DateTime(2026, 10, 9, 12, 0, 0);

        private readonly RecordingSessionCache _cache = new RecordingSessionCache { StartedOn = START };

        [Fact]
        public void While_recording_the_clock_runs()
        {
            _cache.State = RecordingStateEnum.RECORDING;

            RecordingProgressDto progress = RecordingProgressBuilder.Build(_cache, START.AddSeconds(5));

            progress.ElapsedMilliseconds.ShouldBe(5000);
            progress.Stage.ShouldBe(RecordingStageEnum.NONE);
        }

        // The clock is for the recording, not for what comes after it.
        [Fact]
        public void Once_stopped_the_clock_stands_at_the_stop()
        {
            _cache.State = RecordingStateEnum.PROCESSING;
            _cache.EndedOn = START.AddSeconds(3);
            _cache.Stage = new RecordingStageProgress { Stage = RecordingStageEnum.FINDING_ELEMENTS, Number = 1, Count = 1, Done = 44, Total = 90 };

            RecordingProgressDto progress = RecordingProgressBuilder.Build(_cache, START.AddSeconds(40));

            progress.ElapsedMilliseconds.ShouldBe(3000);
            (progress.Stage, progress.StageNumber, progress.StageCount).ShouldBe((RecordingStageEnum.FINDING_ELEMENTS, 1, 1));
            (progress.StageDone, progress.StageTotal).ShouldBe((44, 90));
        }
    }
}
