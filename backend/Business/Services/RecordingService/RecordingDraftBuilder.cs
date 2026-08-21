using Core.Enums;
using Core.Enums.Business;
using Core.Models.Business;
using Core.Models.Dtos;
using System.Text;

namespace Business.Services.RecordingService
{
    /// <summary>
    /// Turns raw input into the steps a person would have written by hand.
    ///
    /// The mapping is not one to one and that is the whole point: a press and a release are one
    /// click, a burst of typing is one Keyboard Input, and the pause between them is a Wait the
    /// user probably wants. Everything here is a proposal the wizard lets the user correct.
    /// </summary>
    public static class RecordingDraftBuilder
    {
        /// <summary>Below this a press and release at different points is still a click, not a drag.</summary>
        private const int DragThresholdPixels = 5;

        /// <summary>Shorter gaps are just human latency, not a wait the flow needs to reproduce.</summary>
        private static readonly TimeSpan WaitThreshold = TimeSpan.FromMilliseconds(1500);

        /// <summary>Keys that type a character rather than doing something.</summary>
        private static readonly HashSet<KeyCodeEnum> ModifierKeys =
        [
            KeyCodeEnum.LeftShift, KeyCodeEnum.RightShift,
            KeyCodeEnum.LeftCtrl, KeyCodeEnum.RightCtrl,
            KeyCodeEnum.LeftAlt, KeyCodeEnum.RightAlt,
            KeyCodeEnum.LeftMeta, KeyCodeEnum.RightMeta,
            KeyCodeEnum.CapsLock, KeyCodeEnum.NumLock,
        ];

        public static List<DraftStepDto> Build(IReadOnlyList<RecordedInput> events)
        {
            List<DraftStepDto> steps = new List<DraftStepDto>();
            StringBuilder typed = new StringBuilder();
            DateTime? typedStartedOn = null;
            DateTime? previousActionEndedOn = null;
            int tempId = 1;

            // The click that stopped the recording is part of stopping it, not part of the task.
            List<RecordedInput> trimmed = TrimTrailingClick(events);

            void Emit(DraftStepDto step, DateTime startedOn, DateTime endedOn)
            {
                if (previousActionEndedOn is DateTime previous)
                {
                    TimeSpan gap = startedOn - previous;
                    if (gap >= WaitThreshold)
                        steps.Add(BuildWait(tempId++, gap));
                }

                step.TempId = tempId++;
                steps.Add(step);
                previousActionEndedOn = endedOn;
            }

            void FlushTyping()
            {
                if (typed.Length == 0)
                    return;

                string text = typed.ToString();
                DateTime startedOn = typedStartedOn ?? DateTime.Now;
                typed.Clear();
                typedStartedOn = null;

                Emit(BuildKeyboardText(text), startedOn, startedOn);
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

                        // Enter, Tab, arrows and the like end the run and become their own step.
                        FlushTyping();
                        Emit(BuildKeyCombination(current), current.CreatedOn, current.CreatedOn);
                        break;
                    }
                }
            }

            FlushTyping();

            return steps;
        }


        // ================================================================
        // Private methods
        // ================================================================

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

        private static DraftStepDto BuildClick(RecordedInput down) => new DraftStepDto
        {
            Source = DraftStepSourceEnum.RECORDING,
            Values = new FlowStepDto
            {
                FlowStepType = FlowStepTypeEnum.CURSOR_CLICK,
                Name = "Click",
                IsPointCustom = true,
                CursorButtonType = down.CursorButtonType,
                CursorButtonActionType = CursorButtonActionTypeEnum.SINGLE_CLICK,
            },
            NewPoint = PointAt(down, "Click point"),
            Evidence = Evidence(down, $"Clicked at {down.PhysicalX}, {down.PhysicalY}"),
        };

        private static DraftStepDto BuildDrag(RecordedInput down, RecordedInput up) => new DraftStepDto
        {
            Source = DraftStepSourceEnum.RECORDING,
            Values = new FlowStepDto
            {
                FlowStepType = FlowStepTypeEnum.CURSOR_DRAG,
                Name = "Drag",
                IsPointCustom = true,
                IsPointEndCustom = true,
                CursorButtonType = down.CursorButtonType,
            },
            NewPoint = PointAt(down, "Drag from"),
            NewPointEnd = PointAt(up, "Drag to"),
            Evidence = Evidence(down, $"Dragged from {down.PhysicalX}, {down.PhysicalY} to {up.PhysicalX}, {up.PhysicalY}"),
        };

        private static DraftStepDto BuildScroll(RecordedInput scroll, int amount) => new DraftStepDto
        {
            Source = DraftStepSourceEnum.RECORDING,
            Values = new FlowStepDto
            {
                FlowStepType = FlowStepTypeEnum.CURSOR_SCROLL,
                Name = $"Scroll {scroll.ScrollDirection.ToString()?.ToLowerInvariant()}",
                IsPointCustom = true,
                CursorScrollDirectionType = scroll.ScrollDirection,
                LoopCount = amount,
            },
            NewPoint = PointAt(scroll, "Scroll point"),
            Evidence = Evidence(scroll, $"Scrolled {scroll.ScrollDirection.ToString()?.ToLowerInvariant()} {amount}"),
        };

        private static DraftStepDto BuildKeyboardText(string text) => new DraftStepDto
        {
            Source = DraftStepSourceEnum.RECORDING,
            Values = new FlowStepDto
            {
                FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT,
                Name = "Type text",
                KeyboardInputType = KeyboardInputTypeEnum.TEXT,
                KeyboardInputText = text,
            },
            Evidence = new DraftEvidenceDto { Summary = $"Typed \"{text}\"" },
        };

        private static DraftStepDto BuildKeyCombination(RecordedInput key) => new DraftStepDto
        {
            Source = DraftStepSourceEnum.RECORDING,
            Values = new FlowStepDto
            {
                FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT,
                Name = $"Press {key.KeyCode}",
                KeyboardInputType = KeyboardInputTypeEnum.COMBINATION,
                KeyboardInputText = key.KeyCode.ToString() ?? string.Empty,
            },
            Evidence = new DraftEvidenceDto
            {
                WindowTitle = key.WindowTitle,
                Summary = $"Pressed {key.KeyCode}",
            },
        };

        private static DraftStepDto BuildWait(int tempId, TimeSpan gap) => new DraftStepDto
        {
            TempId = tempId,
            Source = DraftStepSourceEnum.RECORDING,
            Values = new FlowStepDto
            {
                FlowStepType = FlowStepTypeEnum.WAIT,
                Name = "Wait",
                WaitForMilliseconds = (int)Math.Round(gap.TotalMilliseconds),
            },
            Evidence = new DraftEvidenceDto
            {
                Summary = $"You paused for {Math.Round(gap.TotalSeconds, 1)}s here",
            },
        };

        private static DraftPointDto PointAt(RecordedInput input, string name) => new DraftPointDto
        {
            Name = name,
            LocationX = input.PhysicalX,
            LocationY = input.PhysicalY,
        };

        private static DraftEvidenceDto Evidence(RecordedInput input, string summary) => new DraftEvidenceDto
        {
            ScreenshotIndex = input.HasScreenshot ? input.Index : null,
            WindowTitle = input.WindowTitle,
            Summary = summary,
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
