using Core.Enums;
using Core.Models.Database;

namespace Core.Helpers
{
    /// <summary>
    /// Which of FlowStep's columns mean anything for each step type.
    ///
    /// The model has always carried this as comments over each group of fields. As comments it can
    /// only be read; as a list it can be used - to hand a step to a model without fifty nulls
    /// around it, and later to tell one which fields a type it is generating actually has.
    ///
    /// Written with nameof, so renaming a column breaks the build here rather than quietly
    /// dropping the field from everything that reads this.
    /// </summary>
    public static class FlowStepFieldCatalog
    {
        private static readonly string[] _windowMatch =
        [
            nameof(FlowStep.ProcessName),
            nameof(FlowStep.TitlePattern),
            nameof(FlowStep.TitleMatchMode),
        ];

        private static readonly string[] _condition =
        [
            nameof(FlowStep.ConditionType),
            nameof(FlowStep.ConditionText),
            nameof(FlowStep.ConditionTextEnd),
        ];

        private static readonly string[] _waiting =
        [
            nameof(FlowStep.PollIntervalMilliseconds),
            nameof(FlowStep.TimeoutMilliseconds),
        ];

        private static readonly Dictionary<FlowStepTypeEnum, string[]> _byType = new Dictionary<FlowStepTypeEnum, string[]>
        {
            [FlowStepTypeEnum.WAIT] =
            [
                nameof(FlowStep.WaitForMilliseconds),
                nameof(FlowStep.WaitForMillisecondsMax),
            ],

            [FlowStepTypeEnum.LOOP] =
            [
                nameof(FlowStep.LoopCount),
                nameof(FlowStep.IsLoopInfinite),
            ],

            // The step it jumps to.
            [FlowStepTypeEnum.GO_TO] = [nameof(FlowStep.FlowStepReferenceId)],

            [FlowStepTypeEnum.SUB_FLOW] = [nameof(FlowStep.SubFlowId)],

            // Click and scroll act where the cursor already is, so they carry no position.
            [FlowStepTypeEnum.CURSOR_CLICK] =
            [
                nameof(FlowStep.CursorButtonType),
                nameof(FlowStep.CursorButtonActionType),
            ],

            [FlowStepTypeEnum.CURSOR_SCROLL] =
            [
                nameof(FlowStep.CursorScrollDirectionType),
                nameof(FlowStep.LoopCount),
            ],

            [FlowStepTypeEnum.CURSOR_RELOCATE] =
            [
                nameof(FlowStep.FlowPointId),
                nameof(FlowStep.FlowStepReferenceId),
            ],

            [FlowStepTypeEnum.CURSOR_DRAG] =
            [
                nameof(FlowStep.FlowPointId),
                nameof(FlowStep.FlowStepReferenceId),
                nameof(FlowStep.FlowPointEndId),
                nameof(FlowStep.FlowStepReferenceEndId),
                nameof(FlowStep.CursorButtonType),
            ],

            [FlowStepTypeEnum.WINDOW_FOCUS] = _windowMatch,

            [FlowStepTypeEnum.WINDOW_RESIZE] =
            [
                .. _windowMatch,
                nameof(FlowStep.WindowWidth),
                nameof(FlowStep.WindowHeight),
            ],

            [FlowStepTypeEnum.WINDOW_RELOCATE] =
            [
                .. _windowMatch,
                nameof(FlowStep.FlowPointId),
            ],

            [FlowStepTypeEnum.KEYBOARD_INPUT] =
            [
                nameof(FlowStep.KeyboardInputText),
                nameof(FlowStep.KeyboardInputType),
            ],

            [FlowStepTypeEnum.IMAGE_SEARCH] =
            [
                nameof(FlowStep.FlowAreaId),
                nameof(FlowStep.SearchMode),
                nameof(FlowStep.Accuracy),
                nameof(FlowStep.TemplateMatchMode),
                nameof(FlowStep.MaxMatches),
                .. _waiting,
            ],

            [FlowStepTypeEnum.READ_TEXT] =
            [
                nameof(FlowStep.FlowAreaId),
                nameof(FlowStep.SearchMode),
                nameof(FlowStep.OcrLanguage),
                nameof(FlowStep.ResultExtractPattern),
                .. _condition,
                .. _waiting,
            ],

            [FlowStepTypeEnum.CHECK_VALUE] =
            [
                nameof(FlowStep.FlowStepReferenceId),
                .. _condition,
            ],

            [FlowStepTypeEnum.SYSTEM_COMMAND] =
            [
                nameof(FlowStep.RunCommandShell),
                nameof(FlowStep.RunCommandPreset),
                nameof(FlowStep.RunCommandValue),
                nameof(FlowStep.RunCommandWorkingDirectory),
                nameof(FlowStep.SuccessExitCodes),
                nameof(FlowStep.ResultSource),
                nameof(FlowStep.ResultExtractPattern),
                nameof(FlowStep.TimeoutMilliseconds),
            ],

            [FlowStepTypeEnum.SYSTEM_ACTION] = [nameof(FlowStep.SystemActionType)],

            [FlowStepTypeEnum.NOTIFY] =
            [
                nameof(FlowStep.DiscordBotId),
                nameof(FlowStep.NotifyMessage),
                nameof(FlowStep.FlowStepReferenceId),
            ],
        };

        /// <summary>The columns this type uses. Empty for the structural branch nodes.</summary>
        public static IReadOnlyList<string> FieldsFor(FlowStepTypeEnum type)
        {
            return _byType.TryGetValue(type, out string[]? fields) ? fields : [];
        }

        /// <summary>Types that search an area, so a caller knows to fetch the area with the step.</summary>
        public static bool UsesArea(FlowStepTypeEnum type)
        {
            return FieldsFor(type).Contains(nameof(FlowStep.FlowAreaId));
        }
    }
}
