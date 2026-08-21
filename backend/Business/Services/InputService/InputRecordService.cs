using Core.Enums;
using Core.Enums.Business;
using Core.Interfaces;
using Core.Models.Business;
using SharpHook;
using SharpHook.Data;

namespace Business.Services.InputService
{
    public sealed class InputRecordService : IInputRecordService, IDisposable
    {
        // SharpHook
        private readonly IGlobalHook _hook = new TaskPoolGlobalHook();

        /// <summary>
        /// Raised for every recorded input while any mode is active.
        ///
        /// An event rather than a queue: a queue here would have to outlive every session and
        /// belong to nobody, so it would fill whenever no recording was running and hand its
        /// backlog to whoever read it next. A subscriber that only exists for the length of a
        /// session cannot inherit anything.
        ///
        /// Handlers run on the hook thread, so a subscriber must return immediately.
        /// </summary>
        public event Action<RecordedInput>? ActionRecorded;

        // Recording mode. Handlers stay subscribed for the whole process lifetime and gate on this
        // instead: subscribing and unsubscribing per session drifted out of balance and left
        // duplicate handlers attached. 0 = not recording.
        private const int ModeNone = 0;
        private const int ModeAll = 1;
        private const int ModeOverlay = 2;
        private const int ModePointCapture = 3;
        private int _recordingMode = ModeNone;

        // Throttling events. Interlocked because TaskPoolGlobalHook dispatches on the thread pool.
        private long _lastDragBroadcastTicks;
        private long _lastMovedBroadcastTicks;
        private static readonly long DragThrottleTicks = TimeSpan.FromMilliseconds(16).Ticks;
        private static readonly long MoveThrottleTicks = TimeSpan.FromMilliseconds(16).Ticks;

        private readonly IIpcBroadcastService _broadcastService;
        public InputRecordService(IIpcBroadcastService broadcastService)
        {
            _broadcastService = broadcastService;

            _hook.MouseReleased += OnMouseReleased;
            _hook.MousePressed += OnMousePressed;
            _hook.MouseDragged += OnMouseDragged; // Only captures new cursor location when btn is pressed
            _hook.MouseMoved += OnMouseMoved;
            _hook.MouseWheel += OnMouseWheel;
            _hook.KeyPressed += OnKeyPressed;
            _hook.KeyReleased += OnKeyReleased;
        }

        // Broadcast events to specific type or no broadcast at all.
        private BroadcastTypeEnum? BroadcastType => Volatile.Read(ref _recordingMode) switch
        {
            ModeOverlay => BroadcastTypeEnum.OVERLAY_MOUSE_EVENT,
            ModePointCapture => BroadcastTypeEnum.POINT_CAPTURE_EVENT,
            _ => null,
        };

        private bool IsRecording => Volatile.Read(ref _recordingMode) != ModeNone;


        // ================================================================
        // Global methods
        // ================================================================

        /// <summary>
        /// Starts the global hook asynchronously and runs it until stopped or disposed.
        /// </summary>
        /// <remarks>This method initiates a long-running operation that continues until explicitly stopped.
        /// Can be called only once, preferably during app startup.</remarks>
        public async Task StartGlobalHookAsync()
        {
            await _hook.RunAsync();
        }

        public async Task StopGlobalHookAsync()
        {
            _hook.Stop();
        }


        // ================================================================
        // Public methods
        // ================================================================
        public Task<bool> StartRecordingAllAsync() => Task.FromResult(StartRecording(ModeAll));

        public Task<bool> StopRecordingAllAsync() => Task.FromResult(StopRecording(ModeAll));

        public Task<bool> StartRecordingOverlayAsync() => Task.FromResult(StartRecording(ModeOverlay));

        public Task<bool> StopRecordingOverlayAsync() => Task.FromResult(StopRecording(ModeOverlay));

        public Task<bool> StartRecordingPointCaptureAsync() => Task.FromResult(StartRecording(ModePointCapture));

        public Task<bool> StopRecordingPointCaptureAsync() => Task.FromResult(StopRecording(ModePointCapture));


        // ================================================================
        // Private methods
        // ================================================================

        // Idempotent for the same mode. Stopping is fire and forget from the caller's side, so a
        // reopen can reach us before the previous stop did; refusing that would leave the new
        // session with no events at all.
        private bool StartRecording(int mode)
        {
            int previousMode = Interlocked.CompareExchange(ref _recordingMode, mode, ModeNone);
            return previousMode == ModeNone || previousMode == mode;
        }

        // Only the mode that started a recording may stop it, so an overlay stop can never leave
        // an "all" recording half torn down.

        private bool StopRecording(int mode) =>
            Interlocked.CompareExchange(ref _recordingMode, ModeNone, mode) == mode;

        private void Publish(RecordedInput recordedInput)
        {
            ActionRecorded?.Invoke(recordedInput);

            BroadcastTypeEnum? broadcastType = BroadcastType;
            if (broadcastType != null)
                _broadcastService.SendAsync(broadcastType.Value, recordedInput);
        }

        // Returns true at most once per throttle window.
        private static bool TryPassThrottle(ref long lastTicks, long throttleTicks)
        {
            long now = DateTime.UtcNow.Ticks;
            long last = Interlocked.Read(ref lastTicks);

            if (now - last < throttleTicks)
                return false;

            return Interlocked.CompareExchange(ref lastTicks, now, last) == last;
        }

        private void OnMousePressed(object? sender, MouseHookEventArgs e)
        {
            if (!IsRecording)
                return;

            CursorButtonTypeEnum? buttonType;
            switch (e.Data.Button)
            {
                case MouseButton.Button1:
                    buttonType = CursorButtonTypeEnum.LEFT_BUTTON;
                    break;
                case MouseButton.Button2:
                    buttonType = CursorButtonTypeEnum.RIGHT_BUTTON;
                    break;
                case MouseButton.Button3:
                    buttonType = CursorButtonTypeEnum.MIDDLE_BUTTON;
                    break;
                case MouseButton.Button4:
                case MouseButton.Button5:
                case MouseButton.NoButton:
                default:
                    buttonType = null;
                    break;
            }

            RecordedInput recordedInput = new RecordedInput
            {
                Type = RecordedInputTypeEnum.BUTTON_DOWN,
                PhysicalX = e.Data.X,
                PhysicalY = e.Data.Y,
                CursorButtonType = buttonType,
            };

            if (buttonType != null)
                Publish(recordedInput);
        }

        private void OnMouseReleased(object? sender, MouseHookEventArgs e)
        {
            if (!IsRecording)
                return;

            CursorButtonTypeEnum? buttonType;
            switch (e.Data.Button)
            {
                case MouseButton.Button1:
                    buttonType = CursorButtonTypeEnum.LEFT_BUTTON;
                    break;
                case MouseButton.Button2:
                    buttonType = CursorButtonTypeEnum.RIGHT_BUTTON;
                    break;
                case MouseButton.Button3:
                    buttonType = CursorButtonTypeEnum.MIDDLE_BUTTON;
                    break;
                case MouseButton.Button4:
                case MouseButton.Button5:
                case MouseButton.NoButton:
                default:
                    buttonType = null;
                    break;
            }

            RecordedInput recordedInput = new RecordedInput
            {
                Type = RecordedInputTypeEnum.BUTTON_UP,
                PhysicalX = e.Data.X,
                PhysicalY = e.Data.Y,
                CursorButtonType = buttonType,
            };

            if (buttonType != null)
                Publish(recordedInput);
        }

        private void OnMouseDragged(object? sender, MouseHookEventArgs e)
        {
            if (!IsRecording)
                return;

            if (!TryPassThrottle(ref _lastDragBroadcastTicks, DragThrottleTicks))
                return;

            Publish(new RecordedInput
            {
                Type = RecordedInputTypeEnum.CURSOR_DRAG,
                PhysicalX = e.Data.X,
                PhysicalY = e.Data.Y,
                CursorButtonType = null,
            });
        }

        private void OnMouseMoved(object? sender, MouseHookEventArgs e)
        {
            // Nothing consumes a free moving cursor. The overlay only cares about drags, and a
            // recording wants actions: at 16ms a two minute session is ~8000 events that would
            // push the real ones out of a DropOldest channel.
        }

        private void OnMouseWheel(object? sender, MouseWheelHookEventArgs e)
        {
            if (!IsRecording)
                return;

            // SharpHook reports rotation as signed detents: positive is away from the user.
            int rotation = e.Data.Rotation;
            if (rotation == 0)
                return;

            Publish(new RecordedInput
            {
                Type = RecordedInputTypeEnum.CURSOR_SCROLL,
                PhysicalX = e.Data.X,
                PhysicalY = e.Data.Y,
                ScrollDirection = rotation > 0 ? CursorScrollDirectionTypeEnum.UP : CursorScrollDirectionTypeEnum.DOWN,
                ScrollAmount = Math.Abs(rotation),
            });
        }

        private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
        {
            if (!IsRecording)
                return;

            Publish(new RecordedInput
            {
                Type = RecordedInputTypeEnum.KEY_DOWN,
                KeyCode = MapKeyCode(e.Data.KeyCode),
                KeyChar = e.Data.KeyCode.ToString(),   // SharpHook enum name is already readable
                //Modifiers = MapModifiers(e.Data.RawModifiers),
            });
        }

        private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
        {
            if (!IsRecording)
                return;

            Publish(new RecordedInput
            {
                Type = RecordedInputTypeEnum.KEY_UP,
                KeyCode = MapKeyCode(e.Data.KeyCode),
                KeyChar = e.Data.KeyCode.ToString(),
                //Modifiers = MapModifiers(e.Data.RawModifiers),
            });
        }




        // ================================================================
        // Mappers
        // ================================================================
        private static KeyCodeEnum MapKeyCode(KeyCode kc) => kc switch
        {
            KeyCode.VcA => KeyCodeEnum.A,
            KeyCode.VcB => KeyCodeEnum.B,
            KeyCode.VcC => KeyCodeEnum.C,
            KeyCode.VcD => KeyCodeEnum.D,
            KeyCode.VcE => KeyCodeEnum.E,
            KeyCode.VcF => KeyCodeEnum.F,
            KeyCode.VcG => KeyCodeEnum.G,
            KeyCode.VcH => KeyCodeEnum.H,
            KeyCode.VcI => KeyCodeEnum.I,
            KeyCode.VcJ => KeyCodeEnum.J,
            KeyCode.VcK => KeyCodeEnum.K,
            KeyCode.VcL => KeyCodeEnum.L,
            KeyCode.VcM => KeyCodeEnum.M,
            KeyCode.VcN => KeyCodeEnum.N,
            KeyCode.VcO => KeyCodeEnum.O,
            KeyCode.VcP => KeyCodeEnum.P,
            KeyCode.VcQ => KeyCodeEnum.Q,
            KeyCode.VcR => KeyCodeEnum.R,
            KeyCode.VcS => KeyCodeEnum.S,
            KeyCode.VcT => KeyCodeEnum.T,
            KeyCode.VcU => KeyCodeEnum.U,
            KeyCode.VcV => KeyCodeEnum.V,
            KeyCode.VcW => KeyCodeEnum.W,
            KeyCode.VcX => KeyCodeEnum.X,
            KeyCode.VcY => KeyCodeEnum.Y,
            KeyCode.VcZ => KeyCodeEnum.Z,

            KeyCode.Vc0 => KeyCodeEnum.Num0,
            KeyCode.Vc1 => KeyCodeEnum.Num1,
            KeyCode.Vc2 => KeyCodeEnum.Num2,
            KeyCode.Vc3 => KeyCodeEnum.Num3,
            KeyCode.Vc4 => KeyCodeEnum.Num4,
            KeyCode.Vc5 => KeyCodeEnum.Num5,
            KeyCode.Vc6 => KeyCodeEnum.Num6,
            KeyCode.Vc7 => KeyCodeEnum.Num7,
            KeyCode.Vc8 => KeyCodeEnum.Num8,
            KeyCode.Vc9 => KeyCodeEnum.Num9,

            KeyCode.VcNumPad0 => KeyCodeEnum.Numpad0,
            KeyCode.VcNumPad1 => KeyCodeEnum.Numpad1,
            KeyCode.VcNumPad2 => KeyCodeEnum.Numpad2,
            KeyCode.VcNumPad3 => KeyCodeEnum.Numpad3,
            KeyCode.VcNumPad4 => KeyCodeEnum.Numpad4,
            KeyCode.VcNumPad5 => KeyCodeEnum.Numpad5,
            KeyCode.VcNumPad6 => KeyCodeEnum.Numpad6,
            KeyCode.VcNumPad7 => KeyCodeEnum.Numpad7,
            KeyCode.VcNumPad8 => KeyCodeEnum.Numpad8,
            KeyCode.VcNumPad9 => KeyCodeEnum.Numpad9,
            KeyCode.VcNumPadEnter => KeyCodeEnum.NumpadEnter,
            KeyCode.VcNumPadAdd => KeyCodeEnum.NumpadPlus,
            KeyCode.VcNumPadSubtract => KeyCodeEnum.NumpadMinus,
            KeyCode.VcNumPadMultiply => KeyCodeEnum.NumpadMultiply,
            KeyCode.VcNumPadDivide => KeyCodeEnum.NumpadDivide,

            KeyCode.VcF1 => KeyCodeEnum.F1,
            KeyCode.VcF2 => KeyCodeEnum.F2,
            KeyCode.VcF3 => KeyCodeEnum.F3,
            KeyCode.VcF4 => KeyCodeEnum.F4,
            KeyCode.VcF5 => KeyCodeEnum.F5,
            KeyCode.VcF6 => KeyCodeEnum.F6,
            KeyCode.VcF7 => KeyCodeEnum.F7,
            KeyCode.VcF8 => KeyCodeEnum.F8,
            KeyCode.VcF9 => KeyCodeEnum.F9,
            KeyCode.VcF10 => KeyCodeEnum.F10,
            KeyCode.VcF11 => KeyCodeEnum.F11,
            KeyCode.VcF12 => KeyCodeEnum.F12,

            KeyCode.VcEnter => KeyCodeEnum.Enter,
            KeyCode.VcEscape => KeyCodeEnum.Escape,
            KeyCode.VcBackspace => KeyCodeEnum.Backspace,
            KeyCode.VcTab => KeyCodeEnum.Tab,
            KeyCode.VcSpace => KeyCodeEnum.Space,
            KeyCode.VcDelete => KeyCodeEnum.Delete,
            KeyCode.VcInsert => KeyCodeEnum.Insert,
            KeyCode.VcHome => KeyCodeEnum.Home,
            KeyCode.VcEnd => KeyCodeEnum.End,
            KeyCode.VcPageUp => KeyCodeEnum.PageUp,
            KeyCode.VcPageDown => KeyCodeEnum.PageDown,
            KeyCode.VcUp => KeyCodeEnum.ArrowUp,
            KeyCode.VcDown => KeyCodeEnum.ArrowDown,
            KeyCode.VcLeft => KeyCodeEnum.ArrowLeft,
            KeyCode.VcRight => KeyCodeEnum.ArrowRight,
            KeyCode.VcPrintScreen => KeyCodeEnum.PrintScreen,
            KeyCode.VcScrollLock => KeyCodeEnum.ScrollLock,
            KeyCode.VcPause => KeyCodeEnum.Pause,

            KeyCode.VcLeftShift => KeyCodeEnum.LeftShift,
            KeyCode.VcRightShift => KeyCodeEnum.RightShift,
            KeyCode.VcLeftControl => KeyCodeEnum.LeftCtrl,
            KeyCode.VcRightControl => KeyCodeEnum.RightCtrl,
            KeyCode.VcLeftAlt => KeyCodeEnum.LeftAlt,
            KeyCode.VcRightAlt => KeyCodeEnum.RightAlt,
            KeyCode.VcLeftMeta => KeyCodeEnum.LeftMeta,
            KeyCode.VcRightMeta => KeyCodeEnum.RightMeta,
            KeyCode.VcCapsLock => KeyCodeEnum.CapsLock,
            KeyCode.VcNumLock => KeyCodeEnum.NumLock,

            KeyCode.VcComma => KeyCodeEnum.Comma,
            KeyCode.VcPeriod => KeyCodeEnum.Period,
            KeyCode.VcSlash => KeyCodeEnum.Slash,
            KeyCode.VcBackslash => KeyCodeEnum.Backslash,
            KeyCode.VcSemicolon => KeyCodeEnum.Semicolon,
            KeyCode.VcQuote => KeyCodeEnum.Quote,
            KeyCode.VcOpenBracket => KeyCodeEnum.BracketLeft,
            KeyCode.VcCloseBracket => KeyCodeEnum.BracketRight,
            KeyCode.VcMinus => KeyCodeEnum.Minus,
            KeyCode.VcEquals => KeyCodeEnum.Equal,
            KeyCode.VcBackQuote => KeyCodeEnum.Backtick,

            _ => KeyCodeEnum.Unknown
        };

        //private static KeyModifierEnum MapModifiers(ModifierMask raw)
        //{
        //    var mods = KeyModifierEnum.None;
        //    if ((raw & (ModifierMask.LeftShift | ModifierMask.RightShift)) != 0) mods |= KeyModifierEnum.Shift;
        //    if ((raw & (ModifierMask.LeftCtrl | ModifierMask.RightCtrl)) != 0) mods |= KeyModifierEnum.Ctrl;
        //    if ((raw & (ModifierMask.LeftAlt | ModifierMask.RightAlt)) != 0) mods |= KeyModifierEnum.Alt;
        //    if ((raw & (ModifierMask.LeftMeta | ModifierMask.RightMeta)) != 0) mods |= KeyModifierEnum.Meta;
        //    return mods;
        //}


        // ================================================================
        // Dispose
        // ================================================================
        public void Dispose()
        {
            _hook.Dispose();
        }
    }
}
