using System.Collections.Concurrent;
using Core.Catalogs;
using Core.Enums;
using Core.Models.Business;
using SharpHook;
using SharpHook.Data;

namespace Platform.Shared.Input
{
    /// <summary>
    /// SharpHook's event loop hook, with one thing decided on the hook's own thread before an input
    /// is queued: whether a press is a recording gesture. One is held back from the application, and
    /// so is its release - a Ctrl + right click would open its menu, a Ctrl + click a link in a new
    /// tab. The handlers run on the loop's thread, too late to hold anything back, which is why this
    /// is not EventLoopGlobalHook itself: that one is sealed.
    ///
    /// Holding back works on Windows and macOS only. On Linux the press reaches the application too.
    /// </summary>
    internal sealed class GestureGlobalHook : GlobalHookBase
    {
        private static readonly (EventMask Mask, string Modifier)[] ModifierMasks =
        [
            (EventMask.Ctrl, KeyCatalog.CTRL),
            (EventMask.Alt, KeyCatalog.ALT),
            (EventMask.Shift, KeyCatalog.SHIFT),
            (EventMask.Meta, KeyCatalog.WIN),
        ];

        private readonly BlockingCollection<UioHookEvent> _queue = new BlockingCollection<UioHookEvent>();
        private readonly Thread _loop;

        private volatile IReadOnlyList<PressGesture> _gestures = [];

        // The buttons whose press was held back, so their release is too. The hook's thread only.
        private readonly HashSet<MouseButton> _heldBack = new HashSet<MouseButton>();

        public GestureGlobalHook() : base(GlobalHookType.All, null, false)
        {
            _loop = new Thread(Loop) { IsBackground = true, Name = "Input hook handlers" };
        }


        // ================================================================
        // Public methods
        // ================================================================

        public void HoldBack(IReadOnlyList<PressGesture> gestures)
        {
            _gestures = gestures;
        }

        /// <summary>The gesture a press is, from the modifiers the hook saw held with it.</summary>
        public RecordingGestureEnum? Match(EventMask mask, MouseButton button)
        {
            CursorButtonTypeEnum? pressed = ButtonOf(button);
            if (pressed == null)
                return null;

            HashSet<string> held = ModifierMasks
                .Where(x => (mask & x.Mask) != EventMask.None)
                .Select(x => x.Modifier)
                .ToHashSet();

            return _gestures.FirstOrDefault(x => x.Matches(held, pressed.Value))?.Gesture;
        }

        public static CursorButtonTypeEnum? ButtonOf(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Button1:
                    return CursorButtonTypeEnum.LEFT_BUTTON;
                case MouseButton.Button2:
                    return CursorButtonTypeEnum.RIGHT_BUTTON;
                case MouseButton.Button3:
                    return CursorButtonTypeEnum.MIDDLE_BUTTON;
                default:
                    return null;
            }
        }


        // ================================================================
        // Protected methods
        // ================================================================

        protected override void BeforeRun()
        {
            if (_loop.ThreadState.HasFlag(ThreadState.Unstarted))
                _loop.Start();
        }

        // Marked before it is queued, so the handler sees it was held back.
        protected override void HandleHookEvent(ref UioHookEvent e)
        {
            if (e.Type == EventType.MousePressed && Match(e.Mask, e.Mouse.Button) != null)
            {
                e.Mask |= EventMask.SuppressEvent;
                _heldBack.Add(e.Mouse.Button);
            }
            else if (e.Type == EventType.MouseReleased && _heldBack.Remove(e.Mouse.Button))
            {
                e.Mask |= EventMask.SuppressEvent;
            }

            if (ShouldDispatchEvent(ref e) && !_queue.IsAddingCompleted)
                _queue.Add(e);
        }

        protected override void Dispose(bool disposing)
        {
            _queue.CompleteAdding();
            base.Dispose(disposing);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // One at a time, in the order they were raised. One handler throwing must not end the loop,
        // or every input after it would be lost.
        private void Loop()
        {
            foreach (UioHookEvent queued in _queue.GetConsumingEnumerable())
            {
                UioHookEvent e = queued;
                try
                {
                    DispatchEvent(ref e);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Input] A handler failed: {ex.Message}");
                }
            }
        }
    }
}
