using Core.Enums;
using Core.Models.Business;
using SharpHook;
using SharpHook.Data;
using System.Threading.Channels;

namespace Business.Services.InputService
{
    public sealed class InputRecordService: IInputRecordService
    {
        // SharpHook
        private readonly IGlobalHook _hook = new TaskPoolGlobalHook(); 
        private readonly Channel<RecordedInput> _actionChannel = Channel.CreateUnbounded<RecordedInput>();

        public IAsyncEnumerable<RecordedInput> GetActions() => _actionChannel.Reader.ReadAllAsync();

        public async Task StartRecordingAsync()
        {
            // Subscribe to the events 
            _hook.MouseClicked += OnMouseClicked;
            _hook.MouseDragged += OnMouseDragged;   
            _hook.MouseWheel += OnMouseWheel;       
            _hook.KeyPressed += OnKeyPressed;
            _hook.KeyReleased += OnKeyReleased;     

            await _hook.RunAsync(); // non-blocking
        }

        public async Task StopRecordingAsync()
        {
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

            _actionChannel.Writer.TryWrite(new RecordedInput
            {
                Type = RecordedInputEnum.CURSOR_CLICK,
                X = e.Data.X,
                Y = e.Data.Y,
                CursorButtonType = buttonType,
            });
        }

        private void OnMouseDragged(object? sender, MouseHookEventArgs e)
        {
            // Record drag start/end if you need drag-and-drop steps
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
