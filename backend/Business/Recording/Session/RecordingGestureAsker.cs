using Business.Recording.Elements;
using Business.Recording.Models.Session;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Ports;

namespace Business.Recording.Session
{
    /// <summary>
    /// A gesture pauses the recording and asks what it is for: the question is kept until it is
    /// answered, and a resume or a stop meanwhile leaves it for Review. While it asks, what a check
    /// pressed is found - the CPU is the tester's while paused - and the stop's finding takes it as
    /// it was found. The answer is kept by the press, for Review to start from, and the window that
    /// had the keyboard gets it back.
    /// </summary>
    public sealed class RecordingGestureAsker
    {
        private readonly RecordingSessionCache _cache;
        private readonly RecordingInputFilter _filter;
        private readonly IWindowService _windowService;
        private readonly IClickedElementFinder _clickedElementFinder;
        private readonly Action _pause;
        private readonly Action _resume;
        private readonly Func<Task> _sendProgress;

        // Which recording asked: an element found after the next recording began is not that recording's.
        private int _recordingNumber;

        public RecordingGestureAsker(
            RecordingSessionCache cache,
            RecordingInputFilter filter,
            IWindowService windowService,
            IClickedElementFinder clickedElementFinder,
            Action pause,
            Action resume,
            Func<Task> sendProgress)
        {
            _cache = cache;
            _filter = filter;
            _windowService = windowService;
            _clickedElementFinder = clickedElementFinder;
            _pause = pause;
            _resume = resume;
            _sendProgress = sendProgress;
        }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>A new recording: an element still being found is no longer kept.</summary>
        public void Reset()
        {
            Interlocked.Increment(ref _recordingNumber);
        }

        /// <summary>
        /// Paused, so nothing the tester does in the question's window, or meanwhile, is recorded.
        /// The press's release is still held back: the hook holds back the button it held, whatever
        /// the pause does to the list.
        /// </summary>
        public void Ask(int pressIndex, RecordedInput press, RecordingGestureEnum gesture)
        {
            if (_filter.IsPaused)
                return;

            TopLevelWindow inFront = _windowService.GetWindowInFront();
            _cache.Asking = new RecordingGestureAsk { PressIndex = pressIndex, Gesture = gesture, InFront = inFront.Handle };

            // A step that is not a click is looked at for nothing.
            if (gesture == RecordingGestureEnum.CHECK)
                FindElement(pressIndex, press);
            else
                _cache.AddGestureElement(pressIndex, null);

            _pause();
        }

        public RecordingGestureDto? GetAsked()
        {
            RecordingGestureAsk? asking = _cache.Asking;
            if (asking == null)
                return null;

            RecordedInput? press = _cache.GetInputs().ElementAtOrDefault(asking.PressIndex);
            RecordedPress? kept = _cache.GetPress(asking.PressIndex);

            RecordingGestureDto gesture = new RecordingGestureDto
            {
                PressIndex = asking.PressIndex,
                Gesture = asking.Gesture,
                LocationX = press?.PhysicalX ?? 0,
                LocationY = press?.PhysicalY ?? 0,
                Element = _cache.GetGestureElement(asking.PressIndex),
                IsElementFound = _cache.HasLookedForGestureElement(asking.PressIndex),
            };

            if (kept != null)
            {
                gesture.Image = kept.Screenshot;
                gesture.X = kept.MainAreaBounds.X;
                gesture.Y = kept.MainAreaBounds.Y;
                gesture.Width = kept.MainAreaBounds.Width;
                gesture.Height = kept.MainAreaBounds.Height;
                gesture.Dpi = kept.Dpi;
            }

            return gesture;
        }

        public ResultDto<bool> Decide(RecordingGestureDecisionDto decision)
        {
            RecordingGestureAsk? asking = _cache.Asking;
            if (asking == null || asking.PressIndex != decision.PressIndex)
                return ResultDto<bool>.Failure("The recording is no longer asking about that.");

            if (decision.Answer != null)
                _cache.AddGestureAnswer(asking.PressIndex, decision.Answer);

            _resume();

            // Whichever window the question's window took the keyboard from. Not every system lets
            // one application give another the keyboard, so this is a try.
            if (asking.InFront.IsValid)
                _windowService.FocusWindow(asking.InFront);

            return ResultDto<bool>.Success(true);
        }

        public bool TestRule(RecordingGestureRuleDto rule)
        {
            ClickedElementDto? element = _cache.GetGestureElement(rule.PressIndex);
            return ConditionEvaluatorHelper.IsSatisfied(element?.Text, rule.ConditionType, rule.Text ?? string.Empty, rule.TextEnd ?? string.Empty);
        }

        /// <summary>Each answer by the action its press became.</summary>
        public List<ReviewedActionDto> AnswersOf(IReadOnlyList<RecordedActionDto> actions)
        {
            List<ReviewedActionDto> answers = new List<ReviewedActionDto>();

            foreach (RecordedActionDto action in actions.Where(x => x.PressIndex != null))
            {
                ReviewedActionDto? answer = _cache.GetGestureAnswer(action.PressIndex!.Value);
                if (answer == null)
                    continue;

                answer.ActionIndex = action.Index;
                answers.Add(answer);
            }

            return answers;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Off the drain, which every input after this one waits on.
        private void FindElement(int pressIndex, RecordedInput press)
        {
            int recordingNumber = Volatile.Read(ref _recordingNumber);

            _ = Task.Run(async () =>
            {
                ClickedElementDto? found = null;
                try
                {
                    RecordedPress? kept = _cache.GetPress(pressIndex);
                    if (kept != null)
                        found = await _clickedElementFinder.FindAtAsync(kept, press.PhysicalX, press.PhysicalY, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Recording] Could not find what a check pressed while asking about it: {ex.Message}");
                }

                if (recordingNumber != Volatile.Read(ref _recordingNumber))
                    return;

                _cache.AddGestureElement(pressIndex, found);
                await _sendProgress();
            });
        }
    }
}
