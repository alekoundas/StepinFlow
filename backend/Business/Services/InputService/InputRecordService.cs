using Core.Enums;
using Core.Models.Business;
using OpenCvSharp;
using SharpHook;
using SharpHook.Data;
using System.Threading.Channels;

namespace Business.Services.InputService
{
    public sealed class InputRecordService : IInputRecordService
    {
        // SharpHook
        private readonly IGlobalHook _hook = new TaskPoolGlobalHook();
        private readonly Channel<RecordedInput> _actionChannel = Channel.CreateUnbounded<RecordedInput>();

        // Make sure only 1 recording is running.
        private bool _isRecording = false;

        public IAsyncEnumerable<RecordedInput> GetActions() => _actionChannel.Reader.ReadAllAsync();

        public async Task StartRecordingAllAsync()
        {
            if (_isRecording == true)
                throw new Exception("You cant run more than 1 recondings at the same time Broski");

            _isRecording = true;

            // Subscribe to the events 
            _hook.MouseReleased += OnMouseReleased;
            _hook.MouseClicked += OnMouseClicked;
            _hook.MouseDragged += OnMouseDragged; // Only captures new cursor location when btn is pressed
            _hook.MouseWheel += OnMouseWheel;
            _hook.KeyPressed += OnKeyPressed;
            _hook.KeyReleased += OnKeyReleased;

            //await _hook.RunAsync(); // non-blocking
        }

        public async Task StartRecordingOverlayAsync()
        {
            if (_isRecording == true)
                throw new Exception("You cant run more than 1 recondings at the same time Broski");

            _isRecording = true;

            // Subscribe to the events 
            _hook.MouseReleased += OnMouseReleased;
            _hook.MouseClicked += OnMouseClicked;
            _hook.MouseDragged += OnMouseDragged; // Only captures new cursor location when btn is pressed

        }

        public async Task StopRecordingAsync()
        {
            _isRecording = false;
            _hook.Stop();
        }

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

            if (buttonType != null)
                _actionChannel.Writer.TryWrite(new RecordedInput
                {
                    Type = RecordedInputEnum.CURSOR_CLICK,
                    X = e.Data.X,
                    Y = e.Data.Y,
                    CursorButtonType = buttonType,
                });
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

            if (buttonType != null)
                _actionChannel.Writer.TryWrite(new RecordedInput
                {
                    Type = RecordedInputEnum.CURSOR_CLICK_RELEASE,
                    X = e.Data.X,
                    Y = e.Data.Y,
                    CursorButtonType = buttonType,
                });
        }

        private void OnMouseDragged(object? sender, MouseHookEventArgs e)
        {
            _actionChannel.Writer.TryWrite(new RecordedInput
            {
                Type = RecordedInputEnum.CURSOR_DRAG,
                X = e.Data.X,
                Y = e.Data.Y,
                CursorButtonType = null,
            });
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
        public void Dispose() => _hook.Dispose();
    }
}
