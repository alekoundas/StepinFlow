using System.Drawing;
using Business.Recording.Models.Session;
using Core.Enums;
using Core.Enums.Business;
using Core.Models.Business;
using Core.Ports;

namespace Business.Recording.Session
{
    /// <summary>
    /// Decides at the hook what of the input belongs to the recording, and which window each press
    /// landed on. Read there rather than on the drain side, because by then the press may have
    /// changed which window is in front.
    ///
    /// Nothing done on StepinFlow's own windows is recorded: the Stop click, a name typed into a
    /// form. A release goes with its press, kept when the press was whatever happened in between.
    /// A key let go over StepinFlow or during a pause would otherwise stay held, and every letter
    /// after it would read as a shortcut.
    /// </summary>
    public sealed class RecordingInputFilter
    {
        private readonly IWindowService _windowService;

        private readonly HashSet<CursorButtonTypeEnum> _buttonsDown = new HashSet<CursorButtonTypeEnum>();
        private readonly HashSet<KeyCodeEnum> _keysDown = new HashSet<KeyCodeEnum>();
        private readonly Lock _lock = new Lock();

        private int _electronProcessId;
        private volatile bool _isPaused;
        private volatile bool _isLastKeyDownKept;
        private volatile RecordingTarget? _target;

        public RecordingInputFilter(IWindowService windowService)
        {
            _windowService = windowService;
        }

        /// <summary>StepinFlow's process, given with each recording. With zero, nothing is dropped as its own.</summary>
        public int ElectronProcessId
        {
            get { return Volatile.Read(ref _electronProcessId); }
            set { Volatile.Write(ref _electronProcessId, value); }
        }

        public bool IsPaused
        {
            get { return _isPaused; }
            set { _isPaused = value; }
        }

        /// <summary>The main area at the last screenshot, to sort presses against. Null with none.</summary>
        public RecordingTarget? Target
        {
            get { return _target; }
            set { _target = value; }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _buttonsDown.Clear();
                _keysDown.Clear();
            }

            ElectronProcessId = 0;
            _isPaused = false;
            _isLastKeyDownKept = false;
            _target = null;
        }

        /// <summary>Whether the input is part of the recording. A kept press is told the window it landed on.</summary>
        public bool Keep(RecordedInput input)
        {
            switch (input.Type)
            {
                case RecordedInputTypeEnum.BUTTON_DOWN:
                    if (input.CursorButtonType == null || !KeepPointed(input))
                        return false;

                    lock (_lock)
                        _buttonsDown.Add(input.CursorButtonType.Value);

                    return true;

                case RecordedInputTypeEnum.BUTTON_UP:
                    if (input.CursorButtonType == null)
                        return false;

                    lock (_lock)
                        return _buttonsDown.Remove(input.CursorButtonType.Value);

                case RecordedInputTypeEnum.CURSOR_SCROLL:
                    return KeepPointed(input);

                case RecordedInputTypeEnum.KEY_DOWN:
                    _isLastKeyDownKept = false;
                    if (input.KeyCode == null || !KeepTyped(input))
                        return false;

                    lock (_lock)
                        _keysDown.Add(input.KeyCode.Value);

                    _isLastKeyDownKept = true;
                    return true;

                // What a key typed is raised right after its key-down, and goes with it.
                case RecordedInputTypeEnum.KEY_TYPED:
                    return _isLastKeyDownKept;

                case RecordedInputTypeEnum.KEY_UP:
                    if (input.KeyCode == null)
                        return false;

                    lock (_lock)
                        return _keysDown.Remove(input.KeyCode.Value);

                // A drag is read from its press and its release. The moves between are noise.
                default:
                    return false;
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private bool KeepPointed(RecordedInput input)
        {
            if (_isPaused)
                return false;

            Point point = new Point(input.PhysicalX, input.PhysicalY);
            TopLevelWindow pointed = _windowService.GetWindowAt(point);
            if (IsOwn(pointed))
                return false;

            input.WindowTitle = TitleOf(pointed);
            input.WindowHandle = pointed.Handle;
            input.Window = Sort(pointed, point, _target);
            return true;
        }

        private bool KeepTyped(RecordedInput input)
        {
            if (_isPaused)
                return false;

            TopLevelWindow inFront = _windowService.GetWindowInFront();
            if (IsOwn(inFront))
                return false;

            input.WindowTitle = TitleOf(inFront);
            input.WindowHandle = inFront.Handle;
            return true;
        }

        private bool IsOwn(TopLevelWindow window)
        {
            int own = ElectronProcessId;
            return own != 0 && window.ProcessId == own;
        }

        // A main area that is a window sorts by the window, so a dialog of the same application
        // is told apart from whatever else sits over the same pixels. A monitor or a region has no
        // window, so it sorts by where the press landed.
        private static RecordedWindowEnum? Sort(TopLevelWindow pointed, Point point, RecordingTarget? target)
        {
            if (target == null)
                return null;

            if (!target.Window.IsValid)
            {
                if (target.Bounds.Contains(point))
                    return RecordedWindowEnum.MAIN_AREA;

                return RecordedWindowEnum.OTHER;
            }

            if (pointed.Handle == target.Window)
                return RecordedWindowEnum.MAIN_AREA;

            if (target.ProcessId != 0 && pointed.ProcessId == target.ProcessId)
                return RecordedWindowEnum.SAME_APPLICATION;

            return RecordedWindowEnum.OTHER;
        }

        private static string? TitleOf(TopLevelWindow window)
        {
            if (string.IsNullOrWhiteSpace(window.Title))
                return null;

            return window.Title;
        }
    }
}
