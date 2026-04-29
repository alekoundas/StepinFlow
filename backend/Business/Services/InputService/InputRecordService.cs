using Core.Enums;
using Core.Interfaces;
using Core.Models.Business;
using SharpHook;
using SharpHook.Data;
using System.Threading.Channels;

namespace Business.Services.InputService
{
    public sealed class InputRecordService : IInputRecordService, IDisposable
    {
        // SharpHook
        private readonly IGlobalHook _hook = new TaskPoolGlobalHook();
        private readonly Channel<RecordedInput> _actionChannel = Channel.CreateUnbounded<RecordedInput>();

        // Make sure only 1 recording is running.
        private bool _isRecording = false;

        // Broadcast events to specific type or no broadcast at all.
        private BroadcastTypeEnum? _broadcastType = null;


        private readonly IIpcBroadcastService _broadcastService;
        public InputRecordService(IIpcBroadcastService broadcastService)
        {
            _broadcastService = broadcastService;
        }

        // ================================================================
        // Global methods
        // ================================================================

        /// <summary>
        /// Starts the global hook asynchronously and runs it until stopped or disposed.
        /// </summary>
        /// <remarks>This method initiates a long-running operation that continues until explicitly stopped.
        /// Can be called only once, preferably during app startup.</remarks>
        public async Task StartGlobalHookAsync()
        {
            await _hook.RunAsync();
        }

        public async Task StopGlobalHookAsync()
        {
            _hook.Stop();
        }


        // ================================================================
        // Public methods
        // ================================================================
        public IAsyncEnumerable<RecordedInput> GetActions()
        {
            return _actionChannel.Reader.ReadAllAsync();
        }

        public async Task StartRecordingAllAsync()
        {
            if (_isRecording)
                throw new Exception("You cant run more than 1 recondings at the same time Broski");

            _isRecording = true;
            _broadcastType = null;

            // Subscribe to the events 
            _hook.MouseReleased += OnMouseReleased;
            _hook.MouseClicked += OnMouseClicked;
            _hook.MouseDragged += OnMouseDragged; // Only captures new cursor location when btn is pressed
            _hook.MouseWheel += OnMouseWheel;
            _hook.KeyPressed += OnKeyPressed;
            _hook.KeyReleased += OnKeyReleased;
        }

        public async Task StopRecordingAllAsync()
        {
            if (!_isRecording)
                throw new Exception("Recording should be started first dummy");

            _isRecording = false;
            _broadcastType = null;

            // Unsubscribe to the events 
            _hook.MouseReleased -= OnMouseReleased;
            _hook.MouseClicked -= OnMouseClicked;
            _hook.MouseDragged -= OnMouseDragged;
            _hook.MouseWheel -= OnMouseWheel;
            _hook.KeyPressed -= OnKeyPressed;
            _hook.KeyReleased -= OnKeyReleased;
        }

        public async Task StartRecordingOverlayAsync()
        {
            if (_isRecording)
                throw new Exception("You cant run more than 1 recondings at the same time Broski");

            _isRecording = true;
            _broadcastType = BroadcastTypeEnum.OVERLAY_MOUSE_EVENT;

            // Subscribe to the events 
            _hook.MouseReleased += OnMouseReleased;
            _hook.MouseClicked += OnMouseClicked;
            _hook.MouseDragged += OnMouseDragged; // Only captures new cursor location when btn is pressed

        }

        public async Task StopRecordingOverlayAsync()
        {
            if (!_isRecording)
                throw new Exception("Recording should be started first dummy");

            _isRecording = true;
            _broadcastType = null;

            // Subscribe to the events 
            _hook.MouseReleased += OnMouseReleased;
            _hook.MouseClicked += OnMouseClicked;
            _hook.MouseDragged += OnMouseDragged; // Only captures new cursor location when btn is pressed

        }


        // ================================================================
        // Private methods
        // ================================================================
        private void OnMouseClicked(object? sender, MouseHookEventArgs e)
        {
            CursorButtonTypeEnum? buttonType;
            switch (e.Data.Button)
            {
                case MouseButton.Button1:
                    buttonType = CursorButtonTypeEnum.LEFT_BUTTON;
                    break;
                case MouseButton.Button2:
                    buttonType = CursorButtonTypeEnum.RIGHT_BUTTON;
                    break;
                case MouseButton.Button3:
                    buttonType = CursorButtonTypeEnum.MIDDLE_BUTTON;
                    break;
                case MouseButton.Button4:
                case MouseButton.Button5:
                case MouseButton.NoButton:
                default:
                    buttonType = null;
                    break;
            }

            RecordedInput recordedInput = new RecordedInput
            {
                Type = RecordedInputEnum.CURSOR_CLICK,
                X = e.Data.X,
                Y = e.Data.Y,
                CursorButtonType = buttonType,
            };

            if (buttonType != null)
            {
                _actionChannel.Writer.TryWrite(recordedInput);

                if (_broadcastType != null)
                    _broadcastService.SendAsync(_broadcastType.Value, recordedInput);
            }
        }

        private void OnMouseReleased(object? sender, MouseHookEventArgs e)
        {
            CursorButtonTypeEnum? buttonType;
            switch (e.Data.Button)
            {
                case MouseButton.Button1:
                    buttonType = CursorButtonTypeEnum.LEFT_BUTTON;
                    break;
                case MouseButton.Button2:
                    buttonType = CursorButtonTypeEnum.RIGHT_BUTTON;
                    break;
                case MouseButton.Button3:
                    buttonType = CursorButtonTypeEnum.MIDDLE_BUTTON;
                    break;
                case MouseButton.Button4:
                case MouseButton.Button5:
                case MouseButton.NoButton:
                default:
                    buttonType = null;
                    break;
            }

            RecordedInput recordedInput = new RecordedInput
            {
                Type = RecordedInputEnum.CURSOR_CLICK_RELEASE,
                X = e.Data.X,
                Y = e.Data.Y,
                CursorButtonType = buttonType,
            };

            if (buttonType != null)
            {
                _actionChannel.Writer.TryWrite(recordedInput);

                if (_broadcastType != null)
                    _broadcastService.SendAsync(_broadcastType.Value, recordedInput);
            }
        }

        private void OnMouseDragged(object? sender, MouseHookEventArgs e)
        {
            RecordedInput recordedInput = new RecordedInput
            {
                Type = RecordedInputEnum.CURSOR_DRAG,
                X = e.Data.X,
                Y = e.Data.Y,
                CursorButtonType = null,
            };


            _actionChannel.Writer.TryWrite(recordedInput);

            if (_broadcastType != null)
                _broadcastService.SendAsync(_broadcastType.Value, recordedInput);
        }

        private void OnMouseWheel(object? sender, MouseWheelHookEventArgs e)
        {
            // Record scroll
        }

        private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
        {
            // Record keystroke
        }

        private void OnKeyReleased(object? sender, KeyboardHookEventArgs e) { }


        // ================================================================
        // Dispose
        // ================================================================
        public void Dispose()
        {
            _actionChannel.Writer.Complete();
            _hook.Dispose();
        }
    }
}
