namespace Core.Models.Business
{
    /// <summary>
    /// A window a person can see and click, rather than a control inside one: what a press landed
    /// on, or what keys go to. Cheap enough to read inside an input hook.
    /// </summary>
    public sealed class TopLevelWindow
    {
        public TopLevelWindow(WindowHandle handle, int processId, string title)
        {
            Handle = handle;
            ProcessId = processId;
            Title = title;
        }

        public WindowHandle Handle { get; }
        public int ProcessId { get; }
        public string Title { get; }

        /// <summary>No window: nothing under the point, or nothing in front.</summary>
        public static TopLevelWindow None
        {
            get { return new TopLevelWindow(WindowHandle.None, 0, string.Empty); }
        }
    }
}
