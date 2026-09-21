namespace Core.Models.Business
{
    /// <summary>
    /// A window, as the adapter that found it knows it.
    ///
    /// Opaque on purpose: a Win32 HWND here, an X11 window id under Linux - 64 bits in one and 32
    /// in the other. Nothing outside the adapter may interpret one, it is carried and handed back.
    /// A type rather than an nint so that the compiler enforces what the comment used to ask for:
    /// a monitor handle cannot be passed where a window is wanted, and neither can arithmetic.
    /// </summary>
    public readonly record struct WindowHandle(nint Value)
    {
        /// <summary>No window. What a search returns when it found nothing.</summary>
        public static WindowHandle None
        {
            get { return new WindowHandle(0); }
        }

        public bool IsValid
        {
            get { return Value != 0; }
        }
    }
}
