using Core.Enums;
using Core.Models.Dtos;

namespace Business.Services.CommandService
{
    /// <summary>
    /// The one definition of every preset. The form fetches this to draw the picker and preview
    /// the command, and the runner reads it to build what it executes, so the preview cannot
    /// drift from what actually runs.
    ///
    /// Every preset returns a meaningful exit code, which is what lets a step branch on its own.
    /// tasklist and ping do not, so those go through PowerShell instead.
    /// </summary>
    public static class CommandPresetCatalog
    {
        public static IReadOnlyList<CommandPresetDto> All { get; } =
        [
            new CommandPresetDto
            {
                Preset = RunCommandPresetEnum.CUSTOM,
                Label = "Custom",
                Description = "Write your own command. Chain several with & in cmd or ; in PowerShell.",
                Shell = RunCommandShellEnum.CMD,
                IsEditable = true,
            },
            new CommandPresetDto
            {
                Preset = RunCommandPresetEnum.KILL_PROCESS,
                Label = "Kill a process",
                Description = "Force close every instance of a program.",
                Shell = RunCommandShellEnum.CMD,
                CommandTemplate = "taskkill /IM \"{0}\" /F",
                HasParameter = true,
                ParameterLabel = "Process name",
                ParameterPlaceholder = "chrome.exe",
                IsConfirmationRequired = true,
            },
            new CommandPresetDto
            {
                Preset = RunCommandPresetEnum.IS_PROCESS_RUNNING,
                Label = "Is a process running",
                Description = "Succeeds when the program is running, fails when it is not.",
                Shell = RunCommandShellEnum.POWERSHELL,
                CommandTemplate = "if (Get-Process -Name '{0}' -ErrorAction SilentlyContinue) { exit 0 } else { exit 1 }",
                HasParameter = true,
                ParameterLabel = "Process name",
                ParameterPlaceholder = "chrome",
            },
            new CommandPresetDto
            {
                Preset = RunCommandPresetEnum.READ_CLIPBOARD,
                Label = "Read clipboard",
                Description = "Puts the clipboard text into this step's result.",
                Shell = RunCommandShellEnum.POWERSHELL,
                CommandTemplate = "Get-Clipboard",
            },
            new CommandPresetDto
            {
                Preset = RunCommandPresetEnum.WRITE_CLIPBOARD,
                Label = "Write clipboard",
                Description = "Replaces the clipboard contents.",
                Shell = RunCommandShellEnum.POWERSHELL,
                CommandTemplate = "Set-Clipboard -Value '{0}'",
                HasParameter = true,
                ParameterLabel = "Text",
                ParameterPlaceholder = "Anything, including {{variables}}",
            },
            new CommandPresetDto
            {
                Preset = RunCommandPresetEnum.CHECK_INTERNET,
                Label = "Check internet",
                Description = "Succeeds when the host answers. Test-Connection is used because ping reports success too readily.",
                Shell = RunCommandShellEnum.POWERSHELL,
                CommandTemplate = "if (Test-Connection -ComputerName '{0}' -Count 1 -Quiet) { exit 0 } else { exit 1 }",
                HasParameter = true,
                ParameterLabel = "Host",
                ParameterPlaceholder = "1.1.1.1",
                ParameterDefault = "1.1.1.1",
            },
            new CommandPresetDto
            {
                Preset = RunCommandPresetEnum.SHUTDOWN_IN,
                Label = "Shut down in",
                Description = "Schedules a shutdown. Cancel shutdown calls it off.",
                Shell = RunCommandShellEnum.CMD,
                CommandTemplate = "shutdown /s /t {0}",
                HasParameter = true,
                ParameterLabel = "Seconds",
                ParameterPlaceholder = "60",
                ParameterDefault = "60",
                IsConfirmationRequired = true,
            },
            new CommandPresetDto
            {
                Preset = RunCommandPresetEnum.CANCEL_SHUTDOWN,
                Label = "Cancel shutdown",
                Description = "Calls off a shutdown that has not happened yet.",
                Shell = RunCommandShellEnum.CMD,
                CommandTemplate = "shutdown /a",
            },
        ];

        public static CommandPresetDto Get(RunCommandPresetEnum preset) =>
            All.First(x => x.Preset == preset);

        /// <summary>The command a step would run, with its preset parameter filled in.</summary>
        public static string Resolve(RunCommandPresetEnum preset, string presetValue, string customCommand)
        {
            if (preset == RunCommandPresetEnum.CUSTOM)
                return customCommand;

            CommandPresetDto definition = Get(preset);

            return definition.HasParameter
                ? definition.CommandTemplate.Replace("{0}", presetValue)
                : definition.CommandTemplate;
        }
    }
}
