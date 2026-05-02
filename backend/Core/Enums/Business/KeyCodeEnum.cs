namespace Core.Enums.Business
{
    public enum KeyCodeEnum
    {
        // Letters
        A, B, C, D, E, F, G, H, I, J, K, L, M,
        N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

        // Numbers (row)
        Num0, Num1, Num2, Num3, Num4,
        Num5, Num6, Num7, Num8, Num9,

        // Numpad
        Numpad0, Numpad1, Numpad2, Numpad3, Numpad4,
        Numpad5, Numpad6, Numpad7, Numpad8, Numpad9,
        NumpadEnter, NumpadPlus, NumpadMinus, NumpadMultiply, NumpadDivide,

        // Function
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

        // Control
        Enter, Escape, Backspace, Tab, Space, Delete, Insert,
        Home, End, PageUp, PageDown,
        ArrowUp, ArrowDown, ArrowLeft, ArrowRight,
        PrintScreen, ScrollLock, Pause,

        // Modifiers
        LeftShift, RightShift,
        LeftCtrl, RightCtrl,
        LeftAlt, RightAlt,
        LeftMeta, RightMeta,   // Win/Cmd key
        CapsLock, NumLock,

        // Punctuation
        Comma, Period, Slash, Backslash, Semicolon,
        Quote, BracketLeft, BracketRight,
        Minus, Equal, Backtick,

        Unknown
    }
}
