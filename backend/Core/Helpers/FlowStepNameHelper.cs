using Core.Enums;
using Core.Models.Database;

namespace Core.Helpers
{
    /// <summary>
    /// The steps nobody names. A cursor, wait, keyboard, window, system action or break step is called
    /// what its own settings say, "Double click" or "Wait 2s", and renamed whenever it is saved. Never
    /// from another row, such as the point a Move goes to or the loop a Break leaves, which could be
    /// renamed under it.
    /// </summary>
    public static class FlowStepNameHelper
    {
        private const int TEXT_LENGTH = 30;

        private static readonly FlowStepTypeEnum[] NamedBySettingsTypes =
        [
            FlowStepTypeEnum.WAIT,
            FlowStepTypeEnum.SYSTEM_ACTION,
            FlowStepTypeEnum.CURSOR_CLICK,
            FlowStepTypeEnum.CURSOR_RELOCATE,
            FlowStepTypeEnum.CURSOR_DRAG,
            FlowStepTypeEnum.CURSOR_SCROLL,
            FlowStepTypeEnum.WINDOW_FOCUS,
            FlowStepTypeEnum.WINDOW_RESIZE,
            FlowStepTypeEnum.WINDOW_RELOCATE,
            FlowStepTypeEnum.KEYBOARD_INPUT,
            FlowStepTypeEnum.BREAK,
        ];

        public static bool IsNamedBySettings(FlowStepTypeEnum type)
        {
            return NamedBySettingsTypes.Contains(type);
        }

        /// <summary>
        /// What the step's settings say it does. Not yet unique in the flow.
        /// </summary>
        public static string NameOf(FlowStep step)
        {
            switch (step.FlowStepType)
            {
                case FlowStepTypeEnum.WAIT:
                    return WaitName(step);

                case FlowStepTypeEnum.SYSTEM_ACTION:
                    return SystemActionName(step.SystemActionType);

                case FlowStepTypeEnum.CURSOR_CLICK:
                    return ClickName(step);

                case FlowStepTypeEnum.CURSOR_RELOCATE:
                    return "Move cursor";

                case FlowStepTypeEnum.CURSOR_DRAG:
                    return DragName(step);

                case FlowStepTypeEnum.CURSOR_SCROLL:
                    return ScrollName(step);

                case FlowStepTypeEnum.WINDOW_FOCUS:
                    return $"Focus {WindowName(step)}";

                case FlowStepTypeEnum.WINDOW_RESIZE:
                    return $"Resize {WindowName(step)} to {step.WindowWidth}x{step.WindowHeight}";

                case FlowStepTypeEnum.WINDOW_RELOCATE:
                    return $"Move {WindowName(step)}";

                case FlowStepTypeEnum.KEYBOARD_INPUT:
                    return KeyboardName(step);

                case FlowStepTypeEnum.BREAK:
                    return "Break";

                default:
                    return step.Name;
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static string WaitName(FlowStep step)
        {
            if (step.WaitForMillisecondsMax > step.WaitForMilliseconds)
                return $"Wait {Duration(step.WaitForMilliseconds)} to {Duration(step.WaitForMillisecondsMax)}";

            if (step.WaitForMilliseconds <= 0)
                return "Wait";

            return $"Wait {Duration(step.WaitForMilliseconds)}";
        }

        private static string SystemActionName(SystemActionTypeEnum action)
        {
            return action switch
            {
                SystemActionTypeEnum.LOCK_WORKSTATION => "Lock workstation",
                SystemActionTypeEnum.SLEEP_PC => "Sleep PC",
                SystemActionTypeEnum.MONITOR_OFF => "Monitor off",
                SystemActionTypeEnum.MONITOR_ON => "Monitor on",
                _ => "System action",
            };
        }

        // A left click is just a click; the other buttons say which.
        private static string ClickName(FlowStep step)
        {
            CursorButtonTypeEnum button = step.CursorButtonType ?? CursorButtonTypeEnum.LEFT_BUTTON;
            string buttonName = ButtonName(button);

            string clickName = "click";
            if (button != CursorButtonTypeEnum.LEFT_BUTTON)
                clickName = $"{buttonName} click";

            return step.CursorButtonActionType switch
            {
                CursorButtonActionTypeEnum.DOUBLE_CLICK => $"Double {clickName}",
                CursorButtonActionTypeEnum.HOLD_CLICK => $"Hold {buttonName} button",
                CursorButtonActionTypeEnum.RELEASE_CLICK => $"Release {buttonName} button",
                _ => Capitalized(clickName),
            };
        }

        private static string DragName(FlowStep step)
        {
            CursorButtonTypeEnum button = step.CursorButtonType ?? CursorButtonTypeEnum.LEFT_BUTTON;
            if (button == CursorButtonTypeEnum.LEFT_BUTTON)
                return "Drag";

            string buttonName = ButtonName(button);
            return $"{Capitalized(buttonName)} drag";
        }

        private static string ScrollName(FlowStep step)
        {
            CursorScrollDirectionTypeEnum direction = step.CursorScrollDirectionType ?? CursorScrollDirectionTypeEnum.DOWN;
            string directionName = direction.ToString().ToLowerInvariant();

            return $"Scroll {directionName} {step.LoopCount}";
        }

        private static string KeyboardName(FlowStep step)
        {
            string text = Shortened(step.KeyboardInputText);

            if (step.KeyboardInputType != KeyboardInputTypeEnum.COMBINATION)
                return WithText("Type text", "Type", text);

            return step.KeyboardKeyActionType switch
            {
                KeyboardKeyActionTypeEnum.HOLD => WithText("Hold keys", "Hold", text),
                KeyboardKeyActionTypeEnum.RELEASE => WithText("Release keys", "Release", text),
                _ => WithText("Press keys", "Press", text),
            };
        }

        // The application when there is one, otherwise what the title is matched against.
        private static string WindowName(FlowStep step)
        {
            if (step.ProcessName.Trim().Length > 0)
                return Shortened(step.ProcessName);

            if (step.TitlePattern.Trim().Length > 0)
                return Shortened(step.TitlePattern);

            return "window";
        }

        private static string WithText(string without, string prefix, string text)
        {
            if (text.Length == 0)
                return without;

            return $"{prefix} {text}";
        }

        private static string ButtonName(CursorButtonTypeEnum button)
        {
            return button switch
            {
                CursorButtonTypeEnum.RIGHT_BUTTON => "right",
                CursorButtonTypeEnum.MIDDLE_BUTTON => "middle",
                _ => "left",
            };
        }

        // As the form reads it: 1s, 1m 30s, 250ms.
        private static string Duration(int milliseconds)
        {
            TimeSpan time = TimeSpan.FromMilliseconds(milliseconds);
            List<string> parts = new List<string>();

            if (time.Days > 0)
                parts.Add($"{time.Days}d");
            if (time.Hours > 0)
                parts.Add($"{time.Hours}h");
            if (time.Minutes > 0)
                parts.Add($"{time.Minutes}m");
            if (time.Seconds > 0)
                parts.Add($"{time.Seconds}s");
            if (time.Milliseconds > 0)
                parts.Add($"{time.Milliseconds}ms");

            return string.Join(' ', parts);
        }

        // Typed text and window titles are the user's, so a name built from them is cut short and
        // kept clear of the brackets the script quotes names in.
        private static string Shortened(string text)
        {
            string unquoted = text.Replace("<[", " ").Replace("]>", " ");
            string[] words = unquoted.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            string cleaned = string.Join(' ', words);
            if (cleaned.Length > TEXT_LENGTH)
                cleaned = cleaned[..TEXT_LENGTH].TrimEnd() + "...";

            return cleaned;
        }

        private static string Capitalized(string text)
        {
            if (text.Length == 0)
                return text;

            return char.ToUpperInvariant(text[0]) + text[1..];
        }
    }
}
