using Core.Catalogs;
using Core.Enums;
using Core.Helpers;
using Core.Models.Dtos;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Business.Shell
{
    public sealed class ShellCommandExecutor : IShellCommandExecutor
    {
        public async Task<ShellCommandResultDto> ExecuteAsync(ShellCommandRequest request, CancellationToken ct = default)
        {
            // A command is waited for until its program closes, which an application never does on its own.
            if (request.Mode == ShellCommandModeEnum.LAUNCH_APP)
                return new ShellCommandResultDto { ErrorMessage = "A launch is started by its step, not run as a command." };

            // Find command.
            string command = ShellCommandModeCatalog.Resolve(request.Mode, request.Value);

            ShellCommandResultDto result = new ShellCommandResultDto { ResolvedCommand = command };

            if (string.IsNullOrWhiteSpace(command))
            {
                result.ErrorMessage = "There is no command to run.";
                return result;
            }

            // Start timeout timer.
            using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
            if (request.TimeoutMilliseconds > 0)
                timeoutSource.CancelAfter(request.TimeoutMilliseconds);

            Stopwatch stopwatch = Stopwatch.StartNew();

            // Actual execution.
            try
            {
                await ExecuteProcessAsync(request, command, result, timeoutSource.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                result.ErrorMessage = $"The command did not finish within {request.TimeoutMilliseconds} ms.";
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            stopwatch.Stop();
            result.DurationMilliseconds = stopwatch.ElapsedMilliseconds;

            if (result.ErrorMessage == null)
            {
                result.IsSuccess = IsSuccessExitCode(request.SuccessExitCodes, result.ExitCode);
                result.ResultValue = Extract(request, result);
            }

            return result;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static async Task ExecuteProcessAsync(ShellCommandRequest request, string command, ShellCommandResultDto result, CancellationToken ct)
        {
            using Process process = new Process { StartInfo = BuildStartInfo(request, command) };
            process.Start();

            // Read before waiting.
            Task<string> stdout = process.StandardOutput.ReadToEndAsync(ct);
            Task<string> stderr = process.StandardError.ReadToEndAsync(ct);

            try
            {
                await process.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                // The tree, not just the shell: cmd /c leaves whatever it launched behind.
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
                throw;
            }

            result.ExitCode = process.ExitCode;

            result.StandardOutput = (await stdout).TrimEnd('\r', '\n');
            result.StandardError = (await stderr).TrimEnd('\r', '\n');
        }

        private static ProcessStartInfo BuildStartInfo(ShellCommandRequest request, string command)
        {
            // cmd writes in the console's OEM code page, PowerShell 5.1 follows the console too.
            // Reading it as UTF-8 turns anything non ASCII into noise, which then silently fails
            // every text comparison downstream.
            Encoding encoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = encoding,
                StandardErrorEncoding = encoding,
            };

            if (!string.IsNullOrWhiteSpace(request.WorkingDirectory))
                startInfo.WorkingDirectory = request.WorkingDirectory;

            if (request.ShellType == ShellTypeEnum.POWERSHELL)
            {
                startInfo.FileName = "powershell.exe";
                startInfo.ArgumentList.Add("-NoProfile");
                startInfo.ArgumentList.Add("-NonInteractive");
                startInfo.ArgumentList.Add("-Command");
                startInfo.ArgumentList.Add(command);
            }
            else
            {
                startInfo.FileName = "cmd.exe";
                startInfo.ArgumentList.Add("/c");
                startInfo.ArgumentList.Add(command);
            }

            return startInfo;
        }

        private static bool IsSuccessExitCode(string successExitCodes, int exitCode)
        {
            return successExitCodes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(x => int.TryParse(x, out int code) && code == exitCode);
        }

        private static string Extract(ShellCommandRequest request, ShellCommandResultDto result)
        {
            string source = request.ResultSource switch
            {
                ShellResultSourceEnum.STDERR => result.StandardError,
                ShellResultSourceEnum.COMBINED => string.Join(Environment.NewLine, new[] { result.StandardOutput, result.StandardError }.Where(x => x.Length > 0)),
                ShellResultSourceEnum.EXIT_CODE => result.ExitCode.ToString(CultureInfo.InvariantCulture),
                _ => result.StandardOutput,
            };

            return RegexHelper.Extract(source, request.ResultExtractPattern);
        }
    }
}
