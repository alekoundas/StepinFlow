using System.Globalization;
using Business.Recording.Models.Session;
using Core.Enums;
using Core.Models.Dtos;

namespace Business.Recording.Session
{
    /// <summary>
    /// What the live list shows while recording: the actions as they fold, "Typed 15 characters"
    /// rather than every key-down and key-up. A pause is not something the tester did, so it is
    /// neither listed nor counted. Once stopped, the stage it is being worked through.
    /// </summary>
    public static class RecordingProgressBuilder
    {
        private const int RECENT_ACTIONS = 8;

        // ================================================================
        // Public methods
        // ================================================================

        public static RecordingProgressDto Build(RecordingSessionCache cache, DateTime now)
        {
            RecordingStateEnum state = cache.State;
            DateTime startedOn = cache.StartedOn;
            List<RecordedActionDto> done = cache.Actions.Where(x => x.Kind != RecordedActionKindEnum.PAUSE).ToList();

            int elapsed = 0;
            if (state != RecordingStateEnum.NONE)
                elapsed = Milliseconds((cache.EndedOn ?? now) - startedOn);

            RecordingProgressDto progress = new RecordingProgressDto
            {
                State = state,
                ElapsedMilliseconds = elapsed,
                ActionCount = done.Count,
                RecentActions = done.TakeLast(RECENT_ACTIONS).Select(x => LiveAction(x, startedOn)).ToList(),
                ErrorMessage = cache.ErrorMessage,
            };

            RecordingStageProgress? stage = cache.Stage;
            if (stage != null)
            {
                progress.Stage = stage.Stage;
                progress.StageNumber = stage.Number;
                progress.StageCount = stage.Count;
                progress.StageDone = stage.Done;
                progress.StageTotal = stage.Total;
            }

            return progress;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static RecordingLiveActionDto LiveAction(RecordedActionDto action, DateTime startedOn)
        {
            return new RecordingLiveActionDto
            {
                Index = action.Index,
                Kind = action.Kind,
                Line = LineOf(action),
                ElapsedMilliseconds = Milliseconds(action.StartedOn - startedOn),
            };
        }

        // Typed text is counted rather than shown: it may be a password.
        private static string LineOf(RecordedActionDto action)
        {
            if (action.Kind != RecordedActionKindEnum.TYPING)
                return action.Summary;

            StringInfo text = new StringInfo(action.Text ?? string.Empty);
            if (text.LengthInTextElements == 1)
                return "Typed 1 character";

            return $"Typed {text.LengthInTextElements} characters";
        }

        private static int Milliseconds(TimeSpan span)
        {
            return (int)Math.Max(0, Math.Round(span.TotalMilliseconds));
        }
    }
}
