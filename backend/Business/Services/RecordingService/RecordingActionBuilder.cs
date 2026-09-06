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
        private static readonly TimeSpan PauseThreshold = TimeSpan.FromMilliseconds(550);

        // Two presses this close together, at the same spot, are one double click. Further apart
        // and they are two clicks, which is a different thing for a flow to repeat.
        private static readonly TimeSpan DoubleClickWindow = TimeSpan.FromMilliseconds(500);

        // A hand moves a little between the two presses.
        private const int DoubleClickSlopPixels = 6;

        // Held rather than typed, so they never become an action of their own.
        private static readonly HashSet<KeyCodeEnum> ModifierKeys =
        [
            KeyCodeEnum.LeftShift, KeyCodeEnum.RightShift,
            KeyCodeEnum.LeftCtrl, KeyCodeEnum.RightCtrl,
            KeyCodeEnum.LeftAlt, KeyCodeEnum.RightAlt,
            KeyCodeEnum.LeftMeta, KeyCodeEnum.RightMeta,
            KeyCodeEnum.CapsLock, KeyCodeEnum.NumLock,
        ];

        // The ones that turn a key into a shortcut. Shift is deliberately not here: shift and a
        // letter is still typing, only in capitals, where Ctrl and a letter is a command.
        private static readonly HashSet<KeyCodeEnum> CombiningModifiers =
        [
            KeyCodeEnum.LeftCtrl, KeyCodeEnum.RightCtrl,
            KeyCodeEnum.LeftAlt, KeyCodeEnum.RightAlt,
            KeyCodeEnum.LeftMeta, KeyCodeEnum.RightMeta,
        ];

        public static List<RecordedActionDto> Build(IReadOnlyList<RecordedInput> events)
        {
            List<RecordedActionDto> actions = new List<RecordedActionDto>();
            StringBuilder typed = new StringBuilder();
            DateTime? typedStartedOn = null;
            DateTime? previousEndedOn = null;

            // Every key physically down, and when it went down. Auto-repeat arrives as more
            // KEY_DOWNs with no KEY_UP between them, so a key already in here is the keyboard
            // repeating rather than the user pressing again.
            Dictionary<KeyCodeEnum, DateTime> downSince = new Dictionary<KeyCodeEnum, DateTime>();

            // The last action a key produced, so its release can say how long it was held.
            RecordedActionDto? keyAction = null;
            KeyCodeEnum? keyActionCode = null;

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

                        if (isDrag)
                        {
                            Emit(BuildDrag(current, release), current.CreatedOn, release.CreatedOn);
                            i = releaseIndex >= 0 ? releaseIndex : i;
                            break;
                        }

                        int secondPress = FindDoubleClick(trimmed, i, releaseIndex, current);
                        if (secondPress >= 0)
                        {
                            int secondRelease = FindRelease(trimmed, secondPress);
                            RecordedInput last = secondRelease >= 0 ? trimmed[secondRelease] : trimmed[secondPress];

                            Emit(
                                BuildClick(current, CursorButtonActionTypeEnum.DOUBLE_CLICK),
                                current.CreatedOn,
                                last.CreatedOn);

                            i = secondRelease >= 0 ? secondRelease : secondPress;
                            break;
                        }

                        Emit(
                            BuildClick(current, CursorButtonActionTypeEnum.SINGLE_CLICK),
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

                    // Keys are decided on the way down, not the way up. A modifier is often let go
                    // a moment before the letter - ctrl down, c down, ctrl up, c up is an ordinary
                    // Ctrl+C - and reading it at the release finds no ctrl and types a c.
                    case RecordedInputTypeEnum.KEY_DOWN:
                    {
                        if (current.KeyCode == null)
                            break;

                        KeyCodeEnum code = current.KeyCode.Value;

                        if (downSince.ContainsKey(code))
                        {
                            // The keyboard repeating itself. One press is what the user did.
                            if (keyAction != null && keyActionCode == code)
                                keyAction.RepeatCount++;

                            break;
                        }

                        downSince[code] = current.CreatedOn;

                        if (ModifierKeys.Contains(code))
                            break;

                        if (downSince.Keys.Any(x => CombiningModifiers.Contains(x)))
                        {
                            FlushTyping();
                            keyAction = BuildKeyCombination(current, ShortcutModifiers(downSince.Keys));
                            keyActionCode = code;
                            Emit(keyAction, current.CreatedOn, current.CreatedOn);
                            break;
                        }

                        string? character = PrintableCharacter(code);

                        if (character != null)
                        {
                            bool isShifted =
                                downSince.ContainsKey(KeyCodeEnum.LeftShift) ||
                                downSince.ContainsKey(KeyCodeEnum.RightShift);

                            typedStartedOn ??= current.CreatedOn;
                            typed.Append(isShifted ? character.ToUpperInvariant() : character);

                            keyAction = null;
                            keyActionCode = null;
                            break;
                        }

                        // Enter, Tab, arrows and the like end the run and stand on their own.
                        FlushTyping();
                        keyAction = BuildKeyCombination(current, []);
                        keyActionCode = code;
                        Emit(keyAction, current.CreatedOn, current.CreatedOn);
                        break;
                    }

                    case RecordedInputTypeEnum.KEY_UP:
                    {
                        if (current.KeyCode == null)
                            break;

                        KeyCodeEnum code = current.KeyCode.Value;

                        if (!downSince.TryGetValue(code, out DateTime pressedAt))
                            break;

                        downSince.Remove(code);

                        if (keyAction == null || keyActionCode != code)
                            break;

                        // The action ran until the key came back up, so a long hold is not also a
                        // pause before whatever came next.
                        keyAction.HoldMilliseconds = (int)Math.Round((current.CreatedOn - pressedAt).TotalMilliseconds);
                        previousEndedOn = current.CreatedOn;

                        if (keyAction.RepeatCount > 0)
                            keyAction.Summary += $", held {Math.Round(keyAction.HoldMilliseconds / 1000d, 1).ToString(CultureInfo.InvariantCulture)}s";

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

        // The second press of a double click: the same button, close behind, and near enough that
        // the hand did not move on to something else in between.
        private static int FindDoubleClick(IReadOnlyList<RecordedInput> events, int downIndex, int releaseIndex, RecordedInput down)
        {
            for (int i = Math.Max(downIndex, releaseIndex) + 1; i < events.Count; i++)
            {
                RecordedInput candidate = events[i];

                if (candidate.Type == RecordedInputTypeEnum.BUTTON_UP)
                    continue;

                if (candidate.Type != RecordedInputTypeEnum.BUTTON_DOWN ||
                    candidate.CursorButtonType != down.CursorButtonType ||
                    candidate.CreatedOn - down.CreatedOn > DoubleClickWindow ||
                    Math.Abs(candidate.PhysicalX - down.PhysicalX) > DoubleClickSlopPixels ||
                    Math.Abs(candidate.PhysicalY - down.PhysicalY) > DoubleClickSlopPixels)
                    return -1;

                return i;
            }

            return -1;
        }

        private static RecordedActionDto BuildClick(RecordedInput down, CursorButtonActionTypeEnum buttonAction)
        {
            bool isDouble = buttonAction == CursorButtonActionTypeEnum.DOUBLE_CLICK;

            return new RecordedActionDto
            {
                Kind = RecordedActionKindEnum.CLICK,
                Summary = $"{(isDouble ? "Double-clicked" : "Clicked")} at {down.PhysicalX}, {down.PhysicalY}",
                WindowTitle = down.WindowTitle,
                ScreenshotIndex = down.HasScreenshot ? down.Index : null,
                LocationX = down.PhysicalX,
                LocationY = down.PhysicalY,
                CursorButtonType = down.CursorButtonType,
                CursorButtonActionType = buttonAction,
            };
        }

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

        private static RecordedActionDto BuildKeyCombination(RecordedInput key, IReadOnlyList<string> modifiers)
        {
            string combination = string.Join("+", modifiers.Append(key.KeyCode.ToString()));

            return new RecordedActionDto
            {
                Kind = RecordedActionKindEnum.KEY_COMBINATION,
                Summary = $"Pressed {combination}",
                WindowTitle = key.WindowTitle,
                Text = combination,
            };
        }

        // Named and ordered the way a shortcut is written rather than the order the keys went down,
        // and left and right collapse into one because nobody writes "LeftCtrl+C".
        //
        // Shift is here even though it never starts a combination on its own: once Ctrl is down it
        // is part of the shortcut, and Ctrl+Shift+S is not Ctrl+S.
        private static List<string> ShortcutModifiers(ICollection<KeyCodeEnum> held)
        {
            List<string> names = new List<string>();

            if (held.Contains(KeyCodeEnum.LeftCtrl) || held.Contains(KeyCodeEnum.RightCtrl))
                names.Add("Ctrl");

            if (held.Contains(KeyCodeEnum.LeftAlt) || held.Contains(KeyCodeEnum.RightAlt))
                names.Add("Alt");

            if (held.Contains(KeyCodeEnum.LeftShift) || held.Contains(KeyCodeEnum.RightShift))
                names.Add("Shift");

            if (held.Contains(KeyCodeEnum.LeftMeta) || held.Contains(KeyCodeEnum.RightMeta))
                names.Add("Win");

            return names;
        }

        private static RecordedActionDto BuildPause(TimeSpan gap) => new RecordedActionDto
        {
            Kind = RecordedActionKindEnum.PAUSE,
            Summary = $"Paused for {Math.Round(gap.TotalSeconds, 1).ToString(CultureInfo.InvariantCulture)}s",
            PauseMilliseconds = (int)Math.Round(gap.TotalMilliseconds),
        };

        // What a key types, or null when it does something instead. Letters come back lowercase and
        // the caller raises them when shift was down; the punctuation keys stay unshifted, because
        // which symbol sits above a key is a property of the layout and guessing it wrong is worse
        // than letting the user fix the text in the wizard where they can see it.
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
