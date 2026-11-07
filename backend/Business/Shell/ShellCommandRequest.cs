using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Shell
{
    /// <summary>
    /// Everything the executor needs to execute one command, and nothing else.
    ///
    /// It used to take a whole <see cref="FlowStepDto"/>, which meant the execution worker had to map
    /// its entity into a dto it did not otherwise want - so the worker depended on AutoMapper, and on
    /// a profile that lives in the composition root, which is what made it the one worker with no
    /// test. Eight fields, named side by side, and the dependency is gone.
    /// </summary>
    public sealed class ShellCommandRequest
    {
        public ShellCommandModeEnum Mode { get; set; }

        /// <summary>The command, or the mode's parameter. Variables are already resolved.</summary>
        public string Value { get; set; } = string.Empty;

        public ShellTypeEnum ShellType { get; set; }
        public string WorkingDirectory { get; set; } = string.Empty;

        /// <summary>Zero waits for ever.</summary>
        public int TimeoutMilliseconds { get; set; }

        /// <summary>Comma separated. Anything else is a failure rather than an error.</summary>
        public string SuccessExitCodes { get; set; } = "0";

        public ShellResultSourceEnum ResultSource { get; set; }

        /// <summary>Regex, first capture group. Empty keeps the whole output.</summary>
        public string ResultExtractPattern { get; set; } = string.Empty;


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>The step's command with its variables already resolved, as an execution uses it.</summary>
        public static ShellCommandRequest From(FlowStep step, string value)
        {
            return new ShellCommandRequest
            {
                Mode = step.ShellCommandMode,
                Value = value,
                ShellType = step.ShellType,
                WorkingDirectory = step.ShellWorkingDirectory,
                TimeoutMilliseconds = step.TimeoutMilliseconds,
                SuccessExitCodes = step.ShellSuccessExitCodes,
                ResultSource = step.ShellResultSource,
                ResultExtractPattern = step.ResultExtractPattern,
            };
        }

        public static ShellCommandRequest From(FlowStepDto step)
        {
            return new ShellCommandRequest
            {
                Mode = step.ShellCommandMode,
                Value = step.ShellCommandValue,
                ShellType = step.ShellType,
                WorkingDirectory = step.ShellWorkingDirectory,
                TimeoutMilliseconds = step.TimeoutMilliseconds,
                SuccessExitCodes = step.ShellSuccessExitCodes,
                ResultSource = step.ShellResultSource,
                ResultExtractPattern = step.ResultExtractPattern,
            };
        }
    }
}
