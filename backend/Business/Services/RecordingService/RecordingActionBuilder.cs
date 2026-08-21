using Core.Enums;
using Core.Enums.Business;
using Core.Models.Business;
using Core.Models.Dtos;
using System.Globalization;
using System.Text;

namespace Business.Services.RecordingService
{
    /// <summary>
    /// Folds raw input into the things a person would say they did.
    ///
    /// A press and a release are one click; a burst of typing is one entry; a long gap is a pause
    /// worth mentioning. That much is mechanical and belongs here.
    ///
    /// What any of it should become is not decided here. A click could be a cursor click, an
    /// image search, or both, and only the user knows which — so this stops at the action and the
    /// wizard asks.
    /// </summary>
    public static class RecordingActionBuilder
    {
        /// <summary>Below this a press and release at different points is still a click, not a drag.</summary>
        private const int DragThresholdPixels = 5;

        /// <summary>Shorter gaps are just human latency, not a wait the flow needs to reproduce.</summary>
        private static readonly TimeSpan PauseThreshold = TimeSpan.FromMilliseconds(1500);

        /// <summary>Held rather than typed, so they never end a run or become an action.</summary>
        private static readonly HashSet<KeyCodeEnum> ModifierKeys =
        [
            KeyCodeEnum.LeftShift, KeyCodeEnum.RightShift,
            KeyCodeEnum.LeftCtrl, KeyCodeEnum.RightCtrl,
            KeyCodeEnum.LeftAlt, KeyCodeEnum.RightAlt,
            KeyCodeEnum.LeftMeta, KeyCodeEnum.RightMeta,
            KeyCodeEnum.CapsLock, KeyCodeEnum.NumLock,
        ];

        public static List<RecordedActionDto> Build(IReadOnlyList<RecordedInput> events)
        {
            List<RecordedActionDto> actions = new List<RecordedActionDto>();
            StringBuilder typed = new StringBuilder();
            DateTime? typedStartedOn = null;
            DateTime? previousEndedOn = null;

            // The click that stopped the recording is part of stopping it, not part of the task.
            List<RecordedInput> trimmed = TrimTrailingClick(events);

            void Emit(RecordedActionDto action, DateTime startedOn, DateTime endedOn)
            {
                if (previousEndedOn is DateTime previous)
                {
                    TimeSpan gap = startedOn - previous;
                    if (gap >= PauseThreshold)
                        actions.Add(Number(BuildPause(gap), actions.Count));
                }

                actions.Add(Number(action, actions.Count));
                previousEndedOn = endedOn;
            }

            void FlushTyping()
            {
                if (typed.Length == 0)
                    return;

                string text = typed.ToString();
                DateTime startedOn = typedStartedOn ?? DateTime.Now;
                typed.Clear();
                typedStartedOn = null;

                Emit(BuildTyping(text), startedOn, startedOn);
            }

            for (int i = 0; i < trimmed.Count; i++)
            {
                RecordedInput current = trimmed[i];

                switch (current.Type)
                {
                    case RecordedInputTypeEnum.BUTTON_DOWN:
                    {
                        FlushTyping();

                        int releaseIndex = FindRelease(trimmed, i);
                        RecordedInput release = releaseIndex >= 0 ? trimmed[releaseIndex] : current;

                        bool isDrag =
                            Math.Abs(release.PhysicalX - current.PhysicalX) > DragThresholdPixels ||
                            Math.Abs(release.PhysicalY - current.PhysicalY) > DragThresholdPixels;

                        Emit(
                            isDrag ? BuildDrag(current, release) : BuildClick(current),
                            current.CreatedOn,
                            release.CreatedOn);

                        i = releaseIndex >= 0 ? releaseIndex : i;
                        break;
                    }

                    case RecordedInputTypeEnum.CURSOR_SCROLL:
                    {
                        FlushTyping();

                        // Consecutive notches in the same direction are one gesture.
                        int amount = current.ScrollAmount;
                        RecordedInput last = current;

                        while (i + 1 < trimmed.Count &&
                               trimmed[i + 1].Type == RecordedInputTypeEnum.CURSOR_SCROLL &&
                               trimmed[i + 1].ScrollDirection == current.ScrollDirection)
                        {
                            i++;
                            amount += trimmed[i].ScrollAmount;
                            last = trimmed[i];
                        }

                        Emit(BuildScroll(current, amount), current.CreatedOn, last.CreatedOn);
                        break;
                    }

                    case RecordedInputTypeEnum.KEY_UP:
                    {
                        if (current.KeyCode == null || ModifierKeys.Contains(current.KeyCode.Value))
                            break;

                        string? character = PrintableCharacter(current.KeyCode.Value);

                        if (character != null)
                        {
                            typedStartedOn ??= current.CreatedOn;
                            typed.Append(character);
                            break;
                        }

                        // Enter, Tab, arrows and the like end the run and stand on their own.
                        FlushTyping();
                        Emit(BuildKeyCombination(current), current.CreatedOn, current.CreatedOn);
                        break;
                    }
                }
            }

            FlushTyping();

            return actions;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static RecordedActionDto Number(RecordedActionDto action, int index)
        {
            action.Index = index;
            return action;
        }

        private static List<RecordedInput> TrimTrailingClick(IReadOnlyList<RecordedInput> events)
        {
            List<RecordedInput> trimmed = events.ToList();

            for (int i = trimmed.Count - 1; i >= 0; i--)
            {
                if (trimmed[i].Type == RecordedInputTypeEnum.BUTTON_UP)
                {
                    trimmed.RemoveAt(i);
                    continue;
                }

                if (trimmed[i].Type == RecordedInputTypeEnum.BUTTON_DOWN)
                {
                    trimmed.RemoveRange(i, trimmed.Count - i);
                    break;
                }

                break;
            }

            return trimmed;
        }

        private static int FindRelease(IReadOnlyList<RecordedInput> events, int fromIndex)
        {
            for (int i = fromIndex + 1; i < events.Count; i++)
            {
                if (events[i].Type == RecordedInputTypeEnum.BUTTON_UP &&
                    events[i].CursorButtonType == events[fromIndex].CursorButtonType)
                    return i;
            }

            return -1;
        }

        private static RecordedActionDto BuildClick(RecordedInput down) => new RecordedActionDto
        {
            Kind = RecordedActionKindEnum.CLICK,
            Summary = $"Clicked at {down.PhysicalX}, {down.PhysicalY}",
            WindowTitle = down.WindowTitle,
            ScreenshotIndex = down.HasScreenshot ? down.Index : null,
            LocationX = down.PhysicalX,
            LocationY = down.PhysicalY,
            CursorButtonType = down.CursorButtonType,
        };

        private static RecordedActionDto BuildDrag(RecordedInput down, RecordedInput up) => new RecordedActionDto
        {
            Kind = RecordedActionKindEnum.DRAG,
            Summary = $"Dragged from {down.PhysicalX}, {down.PhysicalY} to {up.PhysicalX}, {up.PhysicalY}",
            WindowTitle = down.WindowTitle,
            ScreenshotIndex = down.HasScreenshot ? down.Index : null,
            LocationX = down.PhysicalX,
            LocationY = down.PhysicalY,
            LocationEndX = up.PhysicalX,
            LocationEndY = up.PhysicalY,
            CursorButtonType = down.CursorButtonType,
        };

        private static RecordedActionDto BuildScroll(RecordedInput scroll, int amount) => new RecordedActionDto
        {
            Kind = RecordedActionKindEnum.SCROLL,
            Summary = $"Scrolled {scroll.ScrollDirection.ToString()?.ToLowerInvariant()} {amount}",
            WindowTitle = scroll.WindowTitle,
            LocationX = scroll.PhysicalX,
            LocationY = scroll.PhysicalY,
            ScrollDirection = scroll.ScrollDirection,
            ScrollAmount = amount,
        };

        private static RecordedActionDto BuildTyping(string text) => new RecordedActionDto
        {
            Kind = RecordedActionKindEnum.TYPING,
            Summary = $"Typed \"{text}\"",
            Text = text,
        };

        private static RecordedActionDto BuildKeyCombination(RecordedInput key) => new RecordedActionDto
        {
            Kind = RecordedActionKindEnum.KEY_COMBINATION,
            Summary = $"Pressed {key.KeyCode}",
            WindowTitle = key.WindowTitle,
            Text = key.KeyCode.ToString(),
        };

        private static RecordedActionDto BuildPause(TimeSpan gap) => new RecordedActionDto
        {
            Kind = RecordedActionKindEnum.PAUSE,
            Summary = $"Paused for {Math.Round(gap.TotalSeconds, 1).ToString(CultureInfo.InvariantCulture)}s",
            PauseMilliseconds = (int)Math.Round(gap.TotalMilliseconds),
        };

        /// <summary>
        /// What a key types, or null when it does something instead. Deliberately unshifted: the
        /// recorder does not track modifier state, and guessing case wrong is worse than letting
        /// the user fix the text in the wizard where they can see it.
        /// </summary>
        private static string? PrintableCharacter(KeyCodeEnum keyCode) => keyCode switch
        {
            >= KeyCodeEnum.A and <= KeyCodeEnum.Z => keyCode.ToString().ToLowerInvariant(),
            KeyCodeEnum.Num0 or KeyCodeEnum.Numpad0 => "0",
            KeyCodeEnum.Num1 or KeyCodeEnum.Numpad1 => "1",
            KeyCodeEnum.Num2 or KeyCodeEnum.Numpad2 => "2",
            KeyCodeEnum.Num3 or KeyCodeEnum.Numpad3 => "3",
            KeyCodeEnum.Num4 or KeyCodeEnum.Numpad4 => "4",
            KeyCodeEnum.Num5 or KeyCodeEnum.Numpad5 => "5",
            KeyCodeEnum.Num6 or KeyCodeEnum.Numpad6 => "6",
            KeyCodeEnum.Num7 or KeyCodeEnum.Numpad7 => "7",
            KeyCodeEnum.Num8 or KeyCodeEnum.Numpad8 => "8",
            KeyCodeEnum.Num9 or KeyCodeEnum.Numpad9 => "9",
            KeyCodeEnum.Space => " ",
            KeyCodeEnum.Comma => ",",
            KeyCodeEnum.Period => ".",
            KeyCodeEnum.Slash => "/",
            KeyCodeEnum.Backslash => "\\",
            KeyCodeEnum.Semicolon => ";",
            KeyCodeEnum.Quote => "'",
            KeyCodeEnum.BracketLeft => "[",
            KeyCodeEnum.BracketRight => "]",
            KeyCodeEnum.Minus => "-",
            KeyCodeEnum.Equal => "=",
            KeyCodeEnum.Backtick => "`",
            _ => null,
        };
    }
}
