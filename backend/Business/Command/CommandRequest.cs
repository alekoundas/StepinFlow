using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Command
{
    /// <summary>
    /// Everything the runner needs to run one command, and nothing else.
    ///
    /// It used to take a whole <see cref="FlowStepDto"/>, which meant the execution worker had to map
    /// its entity into a dto it did not otherwise want - so the worker depended on AutoMapper, and on
    /// a profile that lives in the composition root, which is what made it the one worker with no
    /// test. Eight fields, named side by side, and the dependency is gone.
    /// </summary>
    public sealed class CommandRequest
    {
        public RunCommandPresetEnum Preset { get; set; }

        /// <summary>The command, or the preset's parameter. Variables are already resolved.</summary>
        public string Value { get; set; } = string.Empty;

        public RunCommandShellEnum Shell { get; set; }
        public string WorkingDirectory { get; set; } = string.Empty;

        /// <summary>Zero waits for ever.</summary>
        public int TimeoutMilliseconds { get; set; }

        /// <summary>Comma separated. Anything else is a failure rather than an error.</summary>
        public string SuccessExitCodes { get; set; } = "0";

        public ResultSourceEnum ResultSource { get; set; }

        /// <summary>Regex, first capture group. Empty keeps the whole output.</summary>
        public string ResultExtractPattern { get; set; } = string.Empty;


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>The step's command with its variables already resolved, as an execution runs it.</summary>
        public static CommandRequest From(FlowStep step, string value)
        {
            return new CommandRequest
            {
                Preset = step.RunCommandPreset,
                Value = value,
                Shell = step.RunCommandShell,
                WorkingDirectory = step.RunCommandWorkingDirectory,
                TimeoutMilliseconds = step.TimeoutMilliseconds,
                SuccessExitCodes = step.SuccessExitCodes,
                ResultSource = step.ResultSource,
                ResultExtractPattern = step.ResultExtractPattern,
            };
        }

        public static CommandRequest From(FlowStepDto step)
        {
            return new CommandRequest
            {
                Preset = step.RunCommandPreset,
                Value = step.RunCommandValue,
                Shell = step.RunCommandShell,
                WorkingDirectory = step.RunCommandWorkingDirectory,
                TimeoutMilliseconds = step.TimeoutMilliseconds,
                SuccessExitCodes = step.SuccessExitCodes,
                ResultSource = step.ResultSource,
                ResultExtractPattern = step.ResultExtractPattern,
            };
        }
    }
}
