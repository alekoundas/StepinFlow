namespace Business.Executions
{
    public interface IDebuggerHotkeyService
    {
        /// <summary>Reads the debugger's hotkeys from Settings, for the execution about to start.</summary>
        Task BindAsync(CancellationToken ct);

        /// <summary>Hears the hotkeys in any window until the execution ends, however it ends.</summary>
        Task ListenUntilAsync(Task executionCompletion);
    }
}
