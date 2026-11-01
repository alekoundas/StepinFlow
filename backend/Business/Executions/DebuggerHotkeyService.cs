using Business.AppSettings;
using Core.Catalogs;
using Core.Enums;
using Core.Models.Business;
using Core.Ports;

namespace Business.Executions
{
    /// <summary>
    /// The debugger's hotkeys, whatever window is in front. While an execution goes on the hook
    /// hears every key a person presses, and a match acts on the engine as its button on the
    /// execution page does; the page follows from the engine's events. A key a flow presses never
    /// arrives - the hook marks it as simulated - so a flow that presses F9 cannot pause itself.
    /// </summary>
    public sealed class DebuggerHotkeyService : IDebuggerHotkeyService
    {
        private readonly IExecutionEngine _executionEngine;
        private readonly IInputRecordService _inputRecordService;
        private readonly IAppSettingService _appSettingService;
        private readonly DebuggerHotkeyReader _reader = new DebuggerHotkeyReader();

        public DebuggerHotkeyService(IExecutionEngine executionEngine, IInputRecordService inputRecordService, IAppSettingService appSettingService)
        {
            _executionEngine = executionEngine;
            _inputRecordService = inputRecordService;
            _appSettingService = appSettingService;

            // Subscribed once, as the recording session is: the hook raises nothing until it is
            // asked to listen, and the subscriptions cannot drift out of balance.
            inputRecordService.PersonKeyChanged += OnPersonKeyChanged;
        }

        public async Task BindAsync(CancellationToken ct)
        {
            _reader.Bind(DebuggerHotkeyEnum.CONTINUE, await _appSettingService.GetTextAsync(AppSettingCatalog.HotkeyContinue, ct));
            _reader.Bind(DebuggerHotkeyEnum.STEP_INTO, await _appSettingService.GetTextAsync(AppSettingCatalog.HotkeyStepInto, ct));
            _reader.Bind(DebuggerHotkeyEnum.STEP_OVER, await _appSettingService.GetTextAsync(AppSettingCatalog.HotkeyStepOver, ct));
            _reader.Bind(DebuggerHotkeyEnum.PAUSE, await _appSettingService.GetTextAsync(AppSettingCatalog.HotkeyPause, ct));
            _reader.Bind(DebuggerHotkeyEnum.STOP, await _appSettingService.GetTextAsync(AppSettingCatalog.HotkeyStop, ct));
        }

        public async Task ListenUntilAsync(Task executionCompletion)
        {
            // A key let go while nothing listened was never heard, so what was held then is not.
            _reader.Reset();
            _inputRecordService.StartListeningForKeys();

            try
            {
                await executionCompletion;
            }
            finally
            {
                _inputRecordService.StopListeningForKeys();
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        // On the hook's thread, so it only does what the page's buttons do: each is live in the same
        // states they are.
        private void OnPersonKeyChanged(RecordedInput input)
        {
            DebuggerHotkeyEnum? hotkey = _reader.Read(input);
            if (hotkey == null || !_executionEngine.IsRunning)
                return;

            RunStateEnum state = _executionEngine.State;
            switch (hotkey.Value)
            {
                case DebuggerHotkeyEnum.CONTINUE:
                    if (state == RunStateEnum.PAUSED)
                        _executionEngine.Continue();
                    break;

                case DebuggerHotkeyEnum.STEP_INTO:
                    if (state == RunStateEnum.PAUSED)
                        _executionEngine.StepInto();
                    break;

                case DebuggerHotkeyEnum.STEP_OVER:
                    if (state == RunStateEnum.PAUSED)
                        _executionEngine.StepOver();
                    break;

                case DebuggerHotkeyEnum.PAUSE:
                    if (state == RunStateEnum.RUNNING)
                        _executionEngine.Pause();
                    break;

                case DebuggerHotkeyEnum.STOP:
                    if (state != RunStateEnum.STOPPING)
                        _executionEngine.Stop();
                    break;
            }
        }
    }
}
