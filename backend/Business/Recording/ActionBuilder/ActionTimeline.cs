using System.Globalization;
using Business.Recording.Models;
using Core.Enums;
using Core.Models.Dtos;

namespace Business.Recording.ActionBuilder
{
    /// <summary>
    /// Creates the timeline of HumanActions as:
    /// The actions in the order they happened, numbered, with a wait between them if needed.
    /// </summary>
    public sealed class ActionTimeline
    {
        private static readonly TimeSpan PAUSE_THRESHOLD = TimeSpan.FromMilliseconds(550); // Shorter gaps are human latency, not a wait step.

        private readonly List<RecordedActionDto> _actions = new List<RecordedActionDto>();
        private DateTime? _lastEndedOn;

        public List<RecordedActionDto> Actions { get { return _actions; } }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// Adds the human action to the timeline, after a pause when the gap since the last one is long enough.
        /// </summary>
        public void Add(HumanAction humanAction)
        {
            DateTime startedOn = humanAction.Inputs[0].CreatedOn;
            DateTime endedOn = humanAction.Inputs[^1].CreatedOn;

            if (_lastEndedOn != null)
            {
                TimeSpan gap = startedOn - _lastEndedOn.Value;
                if (gap >= PAUSE_THRESHOLD)
                    Append(Pause(_lastEndedOn.Value, gap));

                humanAction.Action.WaitedMilliseconds = (int)Math.Max(0, Math.Round(gap.TotalMilliseconds));
            }

            humanAction.Action.StartedOn = startedOn;
            Append(humanAction.Action);

            // A key held down can end after the actions that came while it was held, and the gap
            // after them starts where the last of all of them ended.
            if (_lastEndedOn == null || endedOn > _lastEndedOn.Value)
                _lastEndedOn = endedOn;
        }

        /// <summary>A mark in its place, with no pause before it and nothing taken from the next action's wait.</summary>
        public void AddMark(HumanAction mark)
        {
            mark.Action.StartedOn = mark.Inputs[0].CreatedOn;
            Append(mark.Action);
        }

        /// <summary>The tester resumed after a pause. The time paused was theirs, not a wait.</summary>
        public void Resume()
        {
            _lastEndedOn = null;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private void Append(RecordedActionDto action)
        {
            action.Index = _actions.Count;
            _actions.Add(action);
        }

        private static RecordedActionDto Pause(DateTime startedOn, TimeSpan gap)
        {
            return new RecordedActionDto
            {
                Kind = RecordedActionKindEnum.PAUSE,
                Summary = $"Paused for {Math.Round(gap.TotalSeconds, 1).ToString(CultureInfo.InvariantCulture)}s",
                StartedOn = startedOn,
                PauseMilliseconds = (int)Math.Round(gap.TotalMilliseconds),
            };
        }
    }
}
