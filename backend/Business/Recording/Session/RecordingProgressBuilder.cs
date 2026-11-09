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
                RecentActions = done.TakeLast(RECENT_ACTIONS).Select(x => LiveAction(x, startedOn, cache)).ToList(),
                ErrorMessage = cache.ErrorMessage,
            };

            RecordingGestureAsk? asking = cache.Asking;
            if (asking != null)
            {
                progress.Asking = new RecordingGestureAskDto
                {
                    PressIndex = asking.PressIndex,
                    Gesture = asking.Gesture,
                    IsElementFound = cache.HasLookedForGestureElement(asking.PressIndex),
                };
            }

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

        private static RecordingLiveActionDto LiveAction(RecordedActionDto action, DateTime startedOn, RecordingSessionCache cache)
        {
            return new RecordingLiveActionDto
            {
                Index = action.Index,
                Kind = action.Kind,
                Line = LineOf(action, cache),
                ElapsedMilliseconds = Milliseconds(action.StartedOn - startedOn),
            };
        }

        // Typed text is counted rather than shown: it may be a password. A gesture answered while
        // recording says what it was answered as.
        private static string LineOf(RecordedActionDto action, RecordingSessionCache cache)
        {
            ReviewedActionDto? answer = null;
            if (action.PressIndex != null)
                answer = cache.GetGestureAnswer(action.PressIndex.Value);

            if (answer != null)
                return AnsweredLineOf(answer);

            if (action.Kind != RecordedActionKindEnum.TYPING)
                return action.Summary;

            StringInfo text = new StringInfo(action.Text ?? string.Empty);
            if (text.LengthInTextElements == 1)
                return "Typed 1 character";

            return $"Typed {text.LengthInTextElements} characters";
        }

        private static string AnsweredLineOf(ReviewedActionDto answer)
        {
            string tells = string.Empty;
            if (answer.NotifyOnSuccess || answer.NotifyOnFailure)
                tells = ", telling Discord";

            switch (answer.Choice)
            {
                case ReviewChoiceEnum.CHECK_THERE:
                    return $"A check that it is there{tells}";
                case ReviewChoiceEnum.CHECK_GONE:
                    return $"A check that it goes{tells}";
                case ReviewChoiceEnum.EXECUTE_COMMAND:
                    return "A command";
                case ReviewChoiceEnum.LAUNCH_APP:
                    return "An application opened";
                case ReviewChoiceEnum.NOTIFY:
                    return "A notification";
                case ReviewChoiceEnum.TYPE_INPUT:
                    return $"Types {{{{{answer.InputName}}}}}";
                case ReviewChoiceEnum.WAIT:
                    return "A wait";
                case ReviewChoiceEnum.BEGIN_STAGE:
                    return $"A stage begins: {answer.StageName}";
                default:
                    return "Marked for Review";
            }
        }

        private static int Milliseconds(TimeSpan span)
        {
            return (int)Math.Max(0, Math.Round(span.TotalMilliseconds));
        }
    }
}
