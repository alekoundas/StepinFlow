namespace Core.Enums
{
    /// <summary>
    /// What makes the things inside an area bigger or smaller when a flow runs on another screen.
    /// </summary>
    public enum ScalesWithEnum
    {
        // Browsers, native apps, the OS. Content keeps its size and reflows when the window
        // changes, so only the monitor's DPI makes it bigger or smaller.
        DPI,

        // Games. Content is drawn to fill the area and grows and shrinks with it - by the smaller of
        // the two ratios, because a game whose window changes shape letterboxes rather than stretches.
        AREA,
    }
}
