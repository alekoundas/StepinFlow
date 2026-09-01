using Core.Enums;
using Core.Models.Dtos;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Business.Services.CommandService
{
    public sealed class CommandRunner : ICommandRunner
    {
        public async Task<RunCommandTestResultDto> RunAsync(FlowStepDto step, CancellationToken ct = default)
        {
            string command = CommandPresetCatalog.Resolve(step.RunCommandPreset, step.RunCommandValue);

            RunCommandTestResultDto result = new RunCommandTestResultDto { ResolvedCommand = command };

            if (string.IsNullOrWhiteSpace(command))
            {
                result.ErrorMessage = "There is no command to run.";
                return result;
            }

            using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
            if (step.TimeoutMilliseconds > 0)
                timeoutSource.CancelAfter(step.TimeoutMilliseconds);

            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                await ExecuteAsync(step, command, result, timeoutSource.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                result.ErrorMessage = $"The command did not finish within {step.TimeoutMilliseconds} ms.";
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            stopwatch.Stop();
            result.DurationMilliseconds = stopwatch.ElapsedMilliseconds;

            if (result.ErrorMessage == null)
            {
                result.IsSuccess = IsSuccessExitCode(step.SuccessExitCodes, result.ExitCode);
                result.ResultValue = Extract(step, result);
            }

            return result;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static async Task ExecuteAsync(
            FlowStepDto step, string command, RunCommandTestResultDto result, CancellationToken ct)
        {
            using Process process = new Process { StartInfo = BuildStartInfo(step, command) };
            process.Start();

            // Read before waiting. The pipe buffer is a few KB and a command that fills it blocks
            // on the write while we block on the exit, and neither side ever moves again.
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
            result.StandardOutput = Clean(await stdout);
            result.StandardError = Clean(await stderr);
        }

        private static ProcessStartInfo BuildStartInfo(FlowStepDto step, string command)
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

            if (!string.IsNullOrWhiteSpace(step.RunCommandWorkingDirectory))
                startInfo.WorkingDirectory = step.RunCommandWorkingDirectory;

            if (step.RunCommandShell == RunCommandShellEnum.POWERSHELL)
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

        /// <summary>ReadToEnd keeps the final newline, and every comparison downstream would fail on it.</summary>
        private static string Clean(string output) => output.TrimEnd('\r', '\n');

        private static bool IsSuccessExitCode(string successExitCodes, int exitCode) =>
            successExitCodes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(x => int.TryParse(x, out int code) && code == exitCode);

        private static string Extract(FlowStepDto step, RunCommandTestResultDto result)
        {
            string source = step.ResultSource switch
            {
                ResultSourceEnum.STDERR => result.StandardError,
                ResultSourceEnum.COMBINED => string.Join(
                    Environment.NewLine,
                    new[] { result.StandardOutput, result.StandardError }.Where(x => x.Length > 0)),
                ResultSourceEnum.EXIT_CODE => result.ExitCode.ToString(),
                _ => result.StandardOutput,
            };

            if (string.IsNullOrWhiteSpace(step.ResultExtractPattern))
                return source;

            Match match = Regex.Match(source, step.ResultExtractPattern);
            if (!match.Success)
                return string.Empty;

            return match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
        }
    }
}
