using Core.Enums;
using Core.Models.Dtos;

namespace Business.Shell
{
    /// <summary>
    /// The one definition of every mode. The form fetches this to draw the picker and preview
    /// the command, and the executor reads it to build what it executes, so the preview cannot
    /// drift from what actually executes.
    ///
    /// Every mode returns a meaningful exit code, which is what lets a step branch on its own.
    /// tasklist and ping do not, so those go through PowerShell instead.
    /// </summary>
    public static class ShellCommandModeCatalog
    {
        public static IReadOnlyList<ShellCommandModeDto> All { get; } =
        [
            new ShellCommandModeDto
            {
                Mode = ShellCommandModeEnum.CUSTOM,
                Label = "Custom",
                Description = "Write your own command. Chain several with & in cmd or ; in PowerShell.",
                ShellType = ShellTypeEnum.CMD,
                IsEditable = true,
            },
            new ShellCommandModeDto
            {
                Mode = ShellCommandModeEnum.KILL_PROCESS,
                Label = "Kill a process",
                Description = "Force close every instance of a program.",
                ShellType = ShellTypeEnum.CMD,
                CommandTemplate = "taskkill /IM \"{0}\" /F",
                HasParameter = true,
                ParameterLabel = "Process name",
                ParameterPlaceholder = "chrome.exe",
                IsConfirmationRequired = true,
            },
            new ShellCommandModeDto
            {
                Mode = ShellCommandModeEnum.IS_PROCESS_RUNNING,
                Label = "Is a process running",
                Description = "Succeeds when the program is running, fails when it is not.",
                ShellType = ShellTypeEnum.POWERSHELL,
                CommandTemplate = "if (Get-Process -Name '{0}' -ErrorAction SilentlyContinue) { exit 0 } else { exit 1 }",
                HasParameter = true,
                ParameterLabel = "Process name",
                ParameterPlaceholder = "chrome",
            },
            new ShellCommandModeDto
            {
                Mode = ShellCommandModeEnum.READ_CLIPBOARD,
                Label = "Read clipboard",
                Description = "Puts the clipboard text into this step's result.",
                ShellType = ShellTypeEnum.POWERSHELL,
                CommandTemplate = "Get-Clipboard",
            },
            new ShellCommandModeDto
            {
                Mode = ShellCommandModeEnum.WRITE_CLIPBOARD,
                Label = "Write clipboard",
                Description = "Replaces the clipboard contents.",
                ShellType = ShellTypeEnum.POWERSHELL,
                CommandTemplate = "Set-Clipboard -Value '{0}'",
                HasParameter = true,
                ParameterLabel = "Text",
                ParameterPlaceholder = "The text to put on the clipboard",
            },
            new ShellCommandModeDto
            {
                Mode = ShellCommandModeEnum.CHECK_INTERNET,
                Label = "Check internet",
                Description = "Succeeds when the host answers. Test-Connection is used because ping reports success too readily.",
                ShellType = ShellTypeEnum.POWERSHELL,
                CommandTemplate = "if (Test-Connection -ComputerName '{0}' -Count 1 -Quiet) { exit 0 } else { exit 1 }",
                HasParameter = true,
                ParameterLabel = "Host",
                ParameterPlaceholder = "1.1.1.1",
                ParameterDefault = "1.1.1.1",
            },
            new ShellCommandModeDto
            {
                Mode = ShellCommandModeEnum.SHUTDOWN_IN,
                Label = "Shut down in",
                Description = "Schedules a shutdown. Cancel shutdown calls it off.",
                ShellType = ShellTypeEnum.CMD,
                CommandTemplate = "shutdown /s /t {0}",
                HasParameter = true,
                ParameterLabel = "Seconds",
                ParameterPlaceholder = "60",
                ParameterDefault = "60",
                IsConfirmationRequired = true,
            },
            new ShellCommandModeDto
            {
                Mode = ShellCommandModeEnum.CANCEL_SHUTDOWN,
                Label = "Cancel shutdown",
                Description = "Calls off a shutdown that has not happened yet.",
                ShellType = ShellTypeEnum.CMD,
                CommandTemplate = "shutdown /a",
            },
            // Started by its step rather than executed here: see SystemCommandStepWorker.
            new ShellCommandModeDto
            {
                Mode = ShellCommandModeEnum.LAUNCH_APP,
                Label = "Launch an app",
                Description = "Starts the app it opens, or a program, a file or a URL, and carries on without waiting for it to close. The app is left alone when it is already open, and waited for when it is not.",
                ShellType = ShellTypeEnum.CMD,
                CommandTemplate = "{0}",
                HasParameter = true,
                ParameterLabel = "Program and arguments",
                ParameterPlaceholder = "\"C:\\Program Files\\App\\app.exe\" --flag",
            },
        ];

        public static ShellCommandModeDto Get(ShellCommandModeEnum mode)
        {
            return All.First(x => x.Mode == mode);
        }

        /// <summary>The command a step would execute, with its mode's parameter filled in.</summary>
        public static string Resolve(ShellCommandModeEnum mode, string value)
        {
            if (mode == ShellCommandModeEnum.CUSTOM)
                return value;

            ShellCommandModeDto definition = Get(mode);

            if (definition.HasParameter)
            {
                return definition.CommandTemplate.Replace("{0}", value);
            }
            else
            {
                return definition.CommandTemplate;
            }
        }
    }
}
