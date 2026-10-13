using System.Collections.Concurrent;
using System.Drawing;
using Business.Recording.Models.Session;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Recording.Session
{
    /// <summary>
    /// What one recording holds in memory.
    ///
    /// 1) the inputs kept, in order, each numbered by its place in the session.
    /// 2) per press, the screenshot around it the review shows, and the whole main area the draft cuts its templates from.
    /// 3) the last few screenshots of the main area. A press takes the newest one from before it, so
    ///    a template shows the screen the tester decided on rather than what the press changed - the
    ///    pressed button, or the menu it opened.
    /// 4) where the recording is, and its actions as last folded, for the live list.
    ///
    /// The inputs and the screenshots per press outlive the recording, so the review can still ask
    /// for them, until the next recording starts or the review discards it.
    /// </summary>
    public sealed class RecordingSessionCache
    {
        private const int RECENT_SCREENSHOTS = 3;

        private readonly List<RecordedInput> _inputs = new List<RecordedInput>();
        private readonly Queue<RecordingScreenshot> _recentScreenshots = new Queue<RecordingScreenshot>();
        private readonly ConcurrentDictionary<int, byte[]> _screenshotsByIndex = new ConcurrentDictionary<int, byte[]>();
        private readonly ConcurrentDictionary<int, RecordedPress> _pressesByIndex = new ConcurrentDictionary<int, RecordedPress>();
        private readonly Lock _inputsLock = new Lock();
        private readonly Lock _recentScreenshotsLock = new Lock();

        private volatile RecordingStateEnum _state;
        private volatile IReadOnlyList<RecordedActionDto> _actions = [];

        /// <summary>The flow's main area. Null with no flow, as the wizard records.</summary>
        public FlowArea? MainArea { get; set; }

        /// <summary>The size of the screenshot around a press. Read once per recording, so they all match.</summary>
        public Size CaptureSize { get; set; } = new Size(400, 400);

        public RecordingStateEnum State
        {
            get { return _state; }
            set { _state = value; }
        }

        /// <summary>Set before the first input can arrive, and kept with the recording.</summary>
        public DateTime StartedOn { get; set; }

        /// <summary>The inputs as last folded into actions.</summary>
        public IReadOnlyList<RecordedActionDto> Actions
        {
            get { return _actions; }
            set { _actions = value; }
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

            _screenshotsByIndex.Clear();
            _pressesByIndex.Clear();
            ClearRecentScreenshots();
            MainArea = null;
            State = RecordingStateEnum.NONE;
            Actions = [];
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
        public void AddScreenshot(int index, byte[] screenshot)
        {
            _screenshotsByIndex[index] = screenshot;
        }

        public byte[]? GetScreenshot(int index)
        {
            _screenshotsByIndex.TryGetValue(index, out byte[]? screenshot);
            return screenshot;
        }

        public void AddPress(RecordedPress press)
        {
            _pressesByIndex[press.Index] = press;
        }

        public RecordedPress? GetPress(int index)
        {
            _pressesByIndex.TryGetValue(index, out RecordedPress? press);
            return press;
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
