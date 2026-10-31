using System.Collections.Concurrent;
using Business.Recording.Models.Session;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Business.Recording.Session
{
    /// <summary>
    /// What one recording holds in memory.
    ///
    /// 1) the inputs kept, in order, each numbered by its place in the session.
    /// 2) per press, the whole main area, which Review shows and the draft cuts its templates from,
    ///    and what it landed on as its application told Windows.
    /// 3) the last few screenshots of the main area. A press takes the newest one from before it, so
    ///    a template shows the screen the tester decided on rather than what the press changed - the
    ///    pressed button, or the menu it opened.
    /// 4) where the recording is, and its actions as last folded, for the live list.
    /// 5) once it stopped, the main area as it was left, how far it has been worked through, what
    ///    each click hit, what changed on the screen between one press and the next, and every
    ///    line read on it.
    ///
    /// The inputs and the screenshots per press outlive the recording, so the review can still ask
    /// for them, until the next recording starts or the review discards it.
    /// </summary>
    public sealed class RecordingSessionCache
    {
        private const int RECENT_SCREENSHOTS = 3;

        private readonly List<RecordedInput> _inputs = new List<RecordedInput>();
        private readonly Queue<RecordingScreenshot> _recentScreenshots = new Queue<RecordingScreenshot>();
        private readonly ConcurrentDictionary<int, RecordedPress> _pressesByIndex = new ConcurrentDictionary<int, RecordedPress>();
        private readonly ConcurrentDictionary<int, AccessibleElement> _pressedElementsByIndex = new ConcurrentDictionary<int, AccessibleElement>();
        private readonly Lock _inputsLock = new Lock();
        private readonly Lock _recentScreenshotsLock = new Lock();

        private volatile RecordingStateEnum _state;
        private volatile IReadOnlyList<RecordedActionDto> _actions = [];
        private volatile IReadOnlyList<ClickedElementDto> _clickedElements = [];
        private volatile IReadOnlyList<ClickGroupDto> _clickGroups = [];
        private volatile IReadOnlyList<ScreenChangeDto> _screenChanges = [];
        private volatile IReadOnlyList<string> _screenLines = [];
        private volatile RecordingScreenshot? _lastScreenshot;
        private volatile RecordingStageProgress? _stage;
        private volatile string? _errorMessage;

        public RecordingStateEnum State
        {
            get { return _state; }
            set { _state = value; }
        }

        /// <summary>Set before the first input can arrive, and kept with the recording.</summary>
        public DateTime StartedOn { get; set; }

        /// <summary>Set at the stop, so the clock stands still while the recording is worked through.</summary>
        public DateTime? EndedOn { get; set; }

        /// <summary>The inputs as last folded into actions.</summary>
        public IReadOnlyList<RecordedActionDto> Actions
        {
            get { return _actions; }
            set { _actions = value; }
        }

        /// <summary>What each click hit, once it has been found.</summary>
        public IReadOnlyList<ClickedElementDto> ClickedElements
        {
            get { return _clickedElements; }
            set { _clickedElements = value; }
        }

        /// <summary>What the tester decided in Review, once they continued from it.</summary>
        public ReviewedRecordingDto? Reviewed { get; set; }

        /// <summary>The clicks that hit the same picture, once grouped.</summary>
        public IReadOnlyList<ClickGroupDto> ClickGroups
        {
            get { return _clickGroups; }
            set { _clickGroups = value; }
        }

        /// <summary>What changed on the screen between one press and the next, once read.</summary>
        public IReadOnlyList<ScreenChangeDto> ScreenChanges
        {
            get { return _screenChanges; }
            set { _screenChanges = value; }
        }

        /// <summary>Every line read on the main area, once read.</summary>
        public IReadOnlyList<string> ScreenLines
        {
            get { return _screenLines; }
            set { _screenLines = value; }
        }

        /// <summary>The main area as the tester left it, taken at the stop, until its text has been read.</summary>
        public RecordingScreenshot? LastScreenshot
        {
            get { return _lastScreenshot; }
            set { _lastScreenshot = value; }
        }

        /// <summary>The stage a stopped recording is in. Null unless it is being worked through.</summary>
        public RecordingStageProgress? Stage
        {
            get { return _stage; }
            set { _stage = value; }
        }

        /// <summary>Why a stage failed, when one did.</summary>
        public string? ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; }
        }

        public int InputCount
        {
            get
            {
                lock (_inputsLock)
                    return _inputs.Count;
            }
        }


        // ================================================================
        // Public methods
        // ================================================================

        public void Clear()
        {
            lock (_inputsLock)
                _inputs.Clear();

            _pressesByIndex.Clear();
            _pressedElementsByIndex.Clear();
            ClearRecentScreenshots();
            State = RecordingStateEnum.NONE;
            EndedOn = null;
            Actions = [];
            ClickedElements = [];
            ClickGroups = [];
            ScreenChanges = [];
            ScreenLines = [];
            LastScreenshot = null;
            Reviewed = null;
            Stage = null;
            ErrorMessage = null;
        }


        // Inputs
        /// <summary>Adds the input, numbered by its place in the session, and returns that number.</summary>
        public int AddInput(RecordedInput input)
        {
            lock (_inputsLock)
            {
                input.Index = _inputs.Count;
                _inputs.Add(input);
                return input.Index;
            }
        }

        public IReadOnlyList<RecordedInput> GetInputs()
        {
            lock (_inputsLock)
                return _inputs.ToList();
        }


        // Screenshots per press
        public void AddPress(RecordedPress press)
        {
            _pressesByIndex[press.Index] = press;
        }

        public RecordedPress? GetPress(int index)
        {
            _pressesByIndex.TryGetValue(index, out RecordedPress? press);
            return press;
        }

        public void AddPressedElement(int index, AccessibleElement element)
        {
            _pressedElementsByIndex[index] = element;
        }

        public AccessibleElement? GetPressedElement(int index)
        {
            _pressedElementsByIndex.TryGetValue(index, out AccessibleElement? element);
            return element;
        }


        // Recent screenshots of the main area
        public void AddRecentScreenshot(RecordingScreenshot screenshot)
        {
            lock (_recentScreenshotsLock)
            {
                _recentScreenshots.Enqueue(screenshot);
                while (_recentScreenshots.Count > RECENT_SCREENSHOTS)
                    _recentScreenshots.Dequeue();
            }
        }

        /// <summary>The newest screenshot finished before the moment, or null when none was.</summary>
        public RecordingScreenshot? GetRecentScreenshotBefore(DateTime moment)
        {
            lock (_recentScreenshotsLock)
                return _recentScreenshots.LastOrDefault(x => x.CapturedOn <= moment);
        }

        public void ClearRecentScreenshots()
        {
            lock (_recentScreenshotsLock)
                _recentScreenshots.Clear();
        }
    }
}
